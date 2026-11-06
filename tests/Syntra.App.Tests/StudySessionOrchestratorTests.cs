using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Syntra.AI.Services;
using Syntra.App.Orchestration;
using Syntra.App.Tests.TestDoubles;
using Syntra.Core.Enums;
using Syntra.Data;
using Syntra.Windows.Services;
using Xunit;
using Xunit.Abstractions;

namespace Syntra.App.Tests;

public class StudySessionOrchestratorTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly SqliteConnection _connection;
    private readonly SyntraDbContext _dbContext;
    private readonly SqliteStudyRepository _repository;
    private readonly TestClock _clock;
    private readonly WindowsEnforcementService _enforcementService;
    private readonly StudySessionOrchestrator _orchestrator;

    public StudySessionOrchestratorTests(ITestOutputHelper output)
    {
        _output = output;
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SyntraDbContext>()
            .UseSqlite(_connection)
            .LogTo(output.WriteLine, LogLevel.Information)
            .EnableSensitiveDataLogging()
            .Options;

        _dbContext = new SyntraDbContext(options);
        _dbContext.Database.EnsureCreated();
        SyntraDbContext.ApplySqlitePragmas(_dbContext);

        _repository = new SqliteStudyRepository(_dbContext);
        _clock = new TestClock();
        _enforcementService = new WindowsEnforcementService();

        _orchestrator = new StudySessionOrchestrator(
            _repository,
            new AiCurriculumService(),
            new AiAssessmentService(),
            _enforcementService,
            _clock);
    }

    [Fact]
    public async Task EndToEnd_StudySession_EnforcesAssessmentGateBeforeCompletion()
    {
        // 1. Initialize Profile
        var profile = await _orchestrator.InitializeProfileAsync("Sreeharan", 120, "Passphrase123");
        Assert.NotNull(profile);

        // 2. Create Plan
        var plan = await _orchestrator.CreateCurriculumPlanAsync("Computer Architecture: Cache Coherence", 50, 25);
        Assert.NotNull(plan);
        Assert.Equal(SessionState.Ready, _orchestrator.StateMachine.CurrentState);

        // 3. Start Focus Session
        await _orchestrator.StartFocusSessionAsync();
        Assert.Equal(SessionState.Active, _orchestrator.StateMachine.CurrentState);
        Assert.True(_enforcementService.IsLockdownActive);

        // 4. Complete Focus Timer (25 minutes)
        _clock.AdvanceMinutes(25);
        await _orchestrator.CompleteFocusIntervalAsync(1500);

        // INVARIANT CHECK: Timer elapsed MUST NOT complete task or session!
        Assert.Equal(SessionState.TaskAssessment, _orchestrator.StateMachine.CurrentState);
        Assert.Equal(StudyTaskStatus.AssessmentRequired, _orchestrator.CurrentTask!.Status);
        Assert.True(_enforcementService.IsLockdownActive);

        // 5. Submit Failing Answers (all empty or wrong -> 0%)
        var failingAnswers = new Dictionary<Guid, string>();
        var failResult = await _orchestrator.SubmitAssessmentAsync(failingAnswers);

        Assert.False(failResult.Passed);
        Assert.Equal(0.0, failResult.ScorePercentage);
        Assert.Equal(SessionState.Active, _orchestrator.StateMachine.CurrentState);
        Assert.Equal(StudyTaskStatus.AssessmentRequired, _orchestrator.CurrentTask.Status);
        Assert.True(_enforcementService.IsLockdownActive);

        // 6. Remediate and Re-attempt: Submit Passing Answers
        var passingAnswers = new Dictionary<Guid, string>();
        foreach (var q in _orchestrator.CurrentTask.Assessment!.Questions)
        {
            passingAnswers[q.Id] = q.ExpectedAnswer;
        }

        _orchestrator.StateMachine.Fire(SessionTrigger.TriggerAssessment);
        var passResult = await _orchestrator.SubmitAssessmentAsync(passingAnswers);

        Assert.True(passResult.Passed);
        Assert.True(passResult.ScorePercentage >= 80.0);
        Assert.Equal(StudyTaskStatus.Completed, _orchestrator.CurrentTask.Status);
        Assert.False(_enforcementService.IsLockdownActive);
    }

    [Fact]
    public async Task EmergencyExit_WithPassphrase_UnlocksAndReleasesEnforcement()
    {
        await _orchestrator.InitializeProfileAsync("Sreeharan", 120, "UnlockMyMachineNow");
        await _orchestrator.CreateCurriculumPlanAsync("Algorithms", 50, 25);
        await _orchestrator.StartFocusSessionAsync();

        Assert.True(_enforcementService.IsLockdownActive);
        Assert.Equal(SessionState.Active, _orchestrator.StateMachine.CurrentState);

        var exitResult = await _orchestrator.AttemptEmergencyUnlockAsync("UnlockMyMachineNow");

        Assert.True(exitResult.Success);
        Assert.Equal(SessionState.EmergencyExit, _orchestrator.StateMachine.CurrentState);
        Assert.False(_enforcementService.IsLockdownActive);
    }

    public void Dispose()
    {
        _enforcementService.Dispose();
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
