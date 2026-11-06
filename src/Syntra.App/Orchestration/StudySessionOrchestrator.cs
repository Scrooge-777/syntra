using Syntra.Core.Entities;
using Syntra.Core.Enums;
using Syntra.Core.Ports;
using Syntra.Core.Scoring;
using Syntra.Core.Security;
using Syntra.Core.State;

namespace Syntra.App.Orchestration;

/// <summary>
/// End-to-end orchestrator binding domain state machine, SQLite repository,
/// AI curriculum and assessment services, and Windows enforcement infrastructure.
/// </summary>
public class StudySessionOrchestrator
{
    private readonly IStudyRepository _repository;
    private readonly IAiCurriculumService _curriculumService;
    private readonly IAiAssessmentService _assessmentService;
    private readonly IWindowsEnforcementService _enforcementService;
    private readonly AssessmentScorer _scorer;
    private readonly EmergencyExitHatch _exitHatch;
    private readonly IClock _clock;

    public SessionStateMachine StateMachine { get; }
    public StudyProfile? ActiveProfile { get; private set; }
    public StudyPlan? ActivePlan { get; private set; }
    public StudySession? ActiveSession { get; private set; }
    public StudyTask? CurrentTask { get; private set; }

    public StudySessionOrchestrator(
        IStudyRepository repository,
        IAiCurriculumService curriculumService,
        IAiAssessmentService assessmentService,
        IWindowsEnforcementService enforcementService,
        IClock? clock = null,
        AssessmentScorer? scorer = null,
        EmergencyExitHatch? exitHatch = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _curriculumService = curriculumService ?? throw new ArgumentNullException(nameof(curriculumService));
        _assessmentService = assessmentService ?? throw new ArgumentNullException(nameof(assessmentService));
        _enforcementService = enforcementService ?? throw new ArgumentNullException(nameof(enforcementService));
        _clock = clock ?? new SystemClock();
        _scorer = scorer ?? new AssessmentScorer();
        _exitHatch = exitHatch ?? new EmergencyExitHatch(_clock);

        StateMachine = new SessionStateMachine(_clock);
    }

    /// <summary>
    /// Loads or initializes a user study profile.
    /// </summary>
    public async Task<StudyProfile> InitializeProfileAsync(
        string userName,
        int dailyGoalMinutes,
        string emergencyPassphrase,
        CancellationToken cancellationToken = default)
    {
        var profile = new StudyProfile(
            Guid.NewGuid(),
            userName,
            dailyGoalMinutes,
            emergencyPassphrase,
            minimumPassRatio: 0.80);

        await _repository.SaveProfileAsync(profile, cancellationToken);
        ActiveProfile = profile;
        return profile;
    }

    /// <summary>
    /// Ingests a study topic or syllabus, generates an AI curriculum plan, and saves it to SQLite.
    /// </summary>
    public async Task<StudyPlan> CreateCurriculumPlanAsync(
        string topicOrSyllabus,
        int totalMinutes = 50,
        int taskIntervalMinutes = 25,
        CancellationToken cancellationToken = default)
    {
        if (ActiveProfile == null)
            throw new InvalidOperationException("Profile must be initialized before creating a plan.");

        var plan = await _curriculumService.GeneratePlanAsync(
            ActiveProfile.Id,
            topicOrSyllabus,
            totalMinutes,
            taskIntervalMinutes,
            cancellationToken);

        await _repository.SavePlanAsync(plan, cancellationToken);
        ActivePlan = plan;
        CurrentTask = plan.GetNextPendingTask();

        StateMachine.Fire(SessionTrigger.Initialize, $"Curriculum plan '{plan.Title}' initialized.");
        return plan;
    }

    /// <summary>
    /// Initiates a study session, engages Windows enforcement, and starts the focus interval.
    /// </summary>
    public async Task StartFocusSessionAsync(CancellationToken cancellationToken = default)
    {
        if (ActiveProfile == null || ActivePlan == null || CurrentTask == null)
            throw new InvalidOperationException("Curriculum and active task must be ready.");

        var session = new StudySession(
            Guid.NewGuid(),
            ActiveProfile.Id,
            ActivePlan.Id,
            CurrentTask.Id,
            _clock.UtcNow);

        await _repository.SaveSessionAsync(session, cancellationToken);
        ActiveSession = session;

        // Advance state machine
        StateMachine.Fire(SessionTrigger.StartSession, $"Starting focus on task: {CurrentTask.Title}");
        CurrentTask.MarkInProgress();

        // Engage native Windows lockdown
        await _enforcementService.EngageLockdownAsync(cancellationToken);
    }

    /// <summary>
    /// Concludes the timed focus interval.
    /// INVARIANT: Can NEVER complete the task. Transitions strictly to TaskAssessment.
    /// </summary>
    public async Task CompleteFocusIntervalAsync(int focusSeconds, CancellationToken cancellationToken = default)
    {
        if (ActiveSession == null || CurrentTask == null)
            throw new InvalidOperationException("No active session or task.");

        ActiveSession.AddActiveFocusSeconds(focusSeconds);
        CurrentTask.AddElapsedTime(focusSeconds);
        CurrentTask.RequireAssessment();

        // Transition FSM: Active -> TaskAssessment
        StateMachine.Fire(SessionTrigger.TriggerAssessment, "Focus interval elapsed. Comprehension assessment required.");

        // Generate assessment if not already present
        if (CurrentTask.Assessment == null)
        {
            var assessment = await _assessmentService.GenerateAssessmentAsync(
                CurrentTask,
                questionCount: 3,
                minimumPassRatio: ActiveProfile?.MinimumPassRatio ?? 0.80,
                cancellationToken);

            CurrentTask.AttachAssessment(assessment);
            await _repository.SaveAssessmentAsync(assessment, cancellationToken);
        }

        await _repository.SaveSessionAsync(ActiveSession, cancellationToken);
    }

    /// <summary>
    /// Evaluates submitted answers. If score >= 80%, unlocks task completion and releases lockdown.
    /// </summary>
    public async Task<ScoringResult> SubmitAssessmentAsync(
        IDictionary<Guid, string> answers,
        CancellationToken cancellationToken = default)
    {
        if (ActiveSession == null || CurrentTask?.Assessment == null)
            throw new InvalidOperationException("No assessment in progress.");

        var scoringResult = _scorer.Score(CurrentTask.Assessment, answers);

        var attempt = new AssessmentAttempt(
            Guid.NewGuid(),
            CurrentTask.Assessment.Id,
            _clock.UtcNow,
            answers,
            scoringResult.EarnedPoints,
            scoringResult.TotalPossiblePoints,
            scoringResult.ScorePercentage,
            scoringResult.Passed,
            scoringResult.SummaryFeedback);

        CurrentTask.RecordAssessmentAttempt(attempt, _clock.UtcNow);

        if (scoringResult.Passed)
        {
            // Transition FSM: TaskAssessment -> TaskCompleted
            StateMachine.Fire(SessionTrigger.PassAssessment, $"Comprehension verified: {scoringResult.ScorePercentage:F1}%");
            ActiveSession.MarkTaskCompleted(CurrentTask.Id);

            // Release Windows desktop lockdown
            await _enforcementService.ReleaseLockdownAsync(cancellationToken);

            // Check if all tasks in plan are complete
            if (ActivePlan != null && ActivePlan.AreAllTasksCompleted())
            {
                StateMachine.Fire(SessionTrigger.CompleteSession, "All study tasks completed and verified.");
                ActiveSession.EndSession(_clock.UtcNow);
            }
        }
        else
        {
            // Transition FSM: TaskAssessment -> Active (Remediation / retry)
            StateMachine.Fire(SessionTrigger.FailAssessment, $"Comprehension check failed: {scoringResult.ScorePercentage:F1}% < 80%");
        }

        await _repository.SaveAssessmentAsync(CurrentTask.Assessment, cancellationToken);
        await _repository.SaveSessionAsync(ActiveSession, cancellationToken);

        return scoringResult;
    }

    /// <summary>
    /// Executes emergency exit unlock via passphrase or cooldown.
    /// </summary>
    public async Task<EmergencyExitResult> AttemptEmergencyUnlockAsync(
        string? passphrase = null,
        CancellationToken cancellationToken = default)
    {
        EmergencyExitResult result;

        if (!string.IsNullOrWhiteSpace(passphrase) && ActiveProfile != null)
        {
            result = _exitHatch.AttemptPassphraseUnlock(ActiveProfile, passphrase);
        }
        else
        {
            result = _exitHatch.InitiateCooldownUnlock();
        }

        if (result.Success)
        {
            StateMachine.Fire(SessionTrigger.TriggerEmergencyExit, $"Emergency unlock via {result.Method}");
            await _enforcementService.ReleaseLockdownAsync(cancellationToken);

            if (ActiveSession != null)
            {
                ActiveSession.EndSession(_clock.UtcNow);
                await _repository.SaveSessionAsync(ActiveSession, cancellationToken);
            }
        }

        return result;
    }
}
