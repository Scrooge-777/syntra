using Syntra.Core.Common;
using Syntra.Core.Enums;

namespace Syntra.Core.Entities;

/// <summary>
/// A focused study task with strict comprehension gating.
/// INVARIANT: Task completion CANNOT occur via timer expiration alone.
/// It strictly requires a passing assessment (>= MinimumPassRatio).
/// </summary>
public class StudyTask
{
    private readonly List<string> _learningObjectives = new();

    public Guid Id { get; private set; }
    public Guid PlanId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public int AllottedMinutes { get; private set; }
    public int ElapsedSeconds { get; private set; }
    public StudyTaskStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public int MaxAttempts { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public Assessment? Assessment { get; private set; }

    public IReadOnlyList<string> LearningObjectives => _learningObjectives.AsReadOnly();

    protected StudyTask()
    {
    }

    public StudyTask(
        Guid id,
        Guid planId,
        string title,
        int allottedMinutes,
        IEnumerable<string>? learningObjectives = null,
        int maxAttempts = 3,
        Assessment? assessment = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Task title cannot be empty.");
        if (allottedMinutes <= 0)
            throw new DomainException("Allotted minutes must be greater than zero.");
        if (maxAttempts < 1)
            throw new DomainException("Max attempts must be at least 1.");

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        PlanId = planId;
        Title = title.Trim();
        AllottedMinutes = allottedMinutes;
        ElapsedSeconds = 0;
        Status = StudyTaskStatus.Pending;
        AttemptCount = 0;
        MaxAttempts = maxAttempts;
        Assessment = assessment;

        if (learningObjectives != null)
        {
            _learningObjectives.AddRange(learningObjectives.Select(l => l.Trim()));
        }
    }

    public void AttachAssessment(Assessment assessment)
    {
        ArgumentNullException.ThrowIfNull(assessment);
        if (assessment.TaskId != Id)
            throw new DomainException($"Assessment TaskId ({assessment.TaskId}) does not match task Id ({Id}).");

        Assessment = assessment;
    }

    public void MarkInProgress()
    {
        if (Status is StudyTaskStatus.Completed)
            throw new DomainException("Cannot start an already completed task.");

        Status = StudyTaskStatus.InProgress;
    }

    public void AddElapsedTime(int seconds)
    {
        if (seconds < 0)
            throw new DomainException("Elapsed seconds increment cannot be negative.");

        ElapsedSeconds += seconds;
    }

    /// <summary>
    /// Invoked when focus timer expires.
    /// Invariant: Transitions status to AssessmentRequired, never Completed.
    /// </summary>
    public void RequireAssessment()
    {
        if (Status == StudyTaskStatus.Completed)
            throw new DomainException("Completed task cannot require assessment.");

        Status = StudyTaskStatus.AssessmentRequired;
    }

    public void RecordAssessmentAttempt(AssessmentAttempt attempt, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        if (Assessment == null)
            throw new DomainException("Cannot record attempt on a task without an assessment.");

        Assessment.RecordAttempt(attempt);
        AttemptCount++;

        if (attempt.Passed)
        {
            Status = StudyTaskStatus.Completed;
            CompletedAtUtc = nowUtc;
        }
        else
        {
            if (AttemptCount >= MaxAttempts)
            {
                Status = StudyTaskStatus.Failed;
            }
            else
            {
                Status = StudyTaskStatus.AssessmentRequired;
            }
        }
    }

    public bool IsEligibleForRetry()
    {
        return Status == StudyTaskStatus.AssessmentRequired && AttemptCount < MaxAttempts;
    }
}
