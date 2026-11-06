using Syntra.Core.Entities;
using Syntra.Core.Enums;
using Syntra.Core.Tests.TestDoubles;
using Xunit;

namespace Syntra.Core.Tests;

public class StudyTaskInvariantTests
{
    private readonly TestClock _clock = new();

    [Fact]
    public void TaskCompletion_RequiresPassingAssessmentAttempt()
    {
        var planId = Guid.NewGuid();
        var task = new StudyTask(Guid.NewGuid(), planId, "Algorithms: Binary Search", allottedMinutes: 25);
        var assessment = new Assessment(Guid.NewGuid(), task.Id, minimumPassRatio: 0.80);
        task.AttachAssessment(assessment);

        task.MarkInProgress();
        Assert.Equal(StudyTaskStatus.InProgress, task.Status);

        // Timer elapses
        task.RequireAssessment();
        Assert.Equal(StudyTaskStatus.AssessmentRequired, task.Status);

        // Failing attempt (60%)
        var failedAttempt = new AssessmentAttempt(
            Guid.NewGuid(),
            assessment.Id,
            _clock.UtcNow,
            new Dictionary<Guid, string>(),
            earnedPoints: 6.0,
            totalPossiblePoints: 10.0,
            scorePercentage: 60.0,
            passed: false,
            feedback: "Need remediation");

        task.RecordAssessmentAttempt(failedAttempt, _clock.UtcNow);
        Assert.Equal(StudyTaskStatus.AssessmentRequired, task.Status);
        Assert.Equal(1, task.AttemptCount);
        Assert.True(task.IsEligibleForRetry());
        Assert.Null(task.CompletedAtUtc);

        // Passing attempt (90%)
        _clock.AdvanceMinutes(5);
        var passedAttempt = new AssessmentAttempt(
            Guid.NewGuid(),
            assessment.Id,
            _clock.UtcNow,
            new Dictionary<Guid, string>(),
            earnedPoints: 9.0,
            totalPossiblePoints: 10.0,
            scorePercentage: 90.0,
            passed: true,
            feedback: "Mastered");

        task.RecordAssessmentAttempt(passedAttempt, _clock.UtcNow);
        Assert.Equal(StudyTaskStatus.Completed, task.Status);
        Assert.Equal(2, task.AttemptCount);
        Assert.NotNull(task.CompletedAtUtc);
    }

    [Fact]
    public void ExceedingMaxAttempts_TransitionsTaskToFailed()
    {
        var task = new StudyTask(Guid.NewGuid(), Guid.NewGuid(), "Data Structures", allottedMinutes: 20, maxAttempts: 2);
        var assessment = new Assessment(Guid.NewGuid(), task.Id, minimumPassRatio: 0.80);
        task.AttachAssessment(assessment);

        task.RequireAssessment();

        // Attempt 1 fails
        var attempt1 = new AssessmentAttempt(Guid.NewGuid(), assessment.Id, _clock.UtcNow, new Dictionary<Guid, string>(), 5, 10, 50, false, "Fail 1");
        task.RecordAssessmentAttempt(attempt1, _clock.UtcNow);
        Assert.Equal(StudyTaskStatus.AssessmentRequired, task.Status);

        // Attempt 2 fails (hits limit of 2)
        var attempt2 = new AssessmentAttempt(Guid.NewGuid(), assessment.Id, _clock.UtcNow, new Dictionary<Guid, string>(), 6, 10, 60, false, "Fail 2");
        task.RecordAssessmentAttempt(attempt2, _clock.UtcNow);
        Assert.Equal(StudyTaskStatus.Failed, task.Status);
        Assert.False(task.IsEligibleForRetry());
    }
}
