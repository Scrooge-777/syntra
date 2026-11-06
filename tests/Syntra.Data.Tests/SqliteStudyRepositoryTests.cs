using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Syntra.Core.Entities;
using Syntra.Core.Enums;
using Syntra.Data;
using Xunit;

namespace Syntra.Data.Tests;

public class SqliteStudyRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SyntraDbContext _context;
    private readonly SqliteStudyRepository _repository;

    public SqliteStudyRepositoryTests()
    {
        // Open an in-memory SQLite connection that persists across queries for the test lifetime
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SyntraDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new SyntraDbContext(options);
        _context.Database.EnsureCreated();
        SyntraDbContext.ApplySqlitePragmas(_context);

        _repository = new SqliteStudyRepository(_context);
    }

    [Fact]
    public async Task Profile_SaveAndRetrieve_ShouldMatchAllProperties()
    {
        var profile = new StudyProfile(
            Guid.NewGuid(),
            "Sreeharan",
            dailyGoalMinutes: 120,
            emergencyPassphrase: "Focus over distraction always",
            minimumPassRatio: 0.85);

        await _repository.SaveProfileAsync(profile);

        var retrieved = await _repository.GetProfileAsync(profile.Id);

        Assert.NotNull(retrieved);
        Assert.Equal("Sreeharan", retrieved.UserName);
        Assert.Equal(120, retrieved.DailyGoalMinutes);
        Assert.Equal(0.85, retrieved.MinimumPassRatio);
        Assert.True(retrieved.VerifyEmergencyPassphrase("Focus over distraction always"));
    }

    [Fact]
    public async Task StudyPlan_WithTasksAndAssessment_ShouldPersistCascade()
    {
        var profileId = Guid.NewGuid();
        var plan = new StudyPlan(
            Guid.NewGuid(),
            profileId,
            "Operating Systems Mastery",
            "Processes, Threads, and Synchronization",
            "Computer Science");

        var task = new StudyTask(
            Guid.NewGuid(),
            plan.Id,
            "Kernel Synchronization Primitives",
            allottedMinutes: 25,
            learningObjectives: new[] { "Mutexes", "Semaphores", "Spinlocks" });

        var assessment = new Assessment(Guid.NewGuid(), task.Id, minimumPassRatio: 0.80);
        var q1 = new AssessmentQuestion(
            Guid.NewGuid(),
            "Which primitive spins in user/kernel mode instead of sleeping?",
            QuestionType.MultipleChoice,
            "Spinlock",
            "Spinlocks busy-wait on CPU cores.",
            options: new[] { "Mutex", "Semaphore", "Spinlock" },
            weight: 2.0);

        assessment.AddQuestion(q1);
        task.AttachAssessment(assessment);
        plan.AddTask(task);

        await _repository.SavePlanAsync(plan);

        var retrievedPlan = await _repository.GetPlanAsync(plan.Id);

        Assert.NotNull(retrievedPlan);
        Assert.Single(retrievedPlan.Tasks);
        Assert.Equal("Kernel Synchronization Primitives", retrievedPlan.Tasks[0].Title);
        Assert.Equal(3, retrievedPlan.Tasks[0].LearningObjectives.Count);
        Assert.NotNull(retrievedPlan.Tasks[0].Assessment);
        Assert.Single(retrievedPlan.Tasks[0].Assessment!.Questions);
        Assert.Equal("Spinlock", retrievedPlan.Tasks[0].Assessment!.Questions[0].ExpectedAnswer);
    }

    [Fact]
    public async Task StudySession_SaveAndTelemetryUpdate_ShouldPersist()
    {
        var profileId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var session = new StudySession(Guid.NewGuid(), profileId, planId, null, DateTimeOffset.UtcNow);

        session.UpdateState(SessionState.Active);
        session.AddActiveFocusSeconds(1500);
        session.RecordDistractionIntervention();
        session.RecordDistractionIntervention();

        var completedTaskId = Guid.NewGuid();
        session.MarkTaskCompleted(completedTaskId);

        await _repository.SaveSessionAsync(session);

        var retrieved = await _repository.GetSessionAsync(session.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(SessionState.Active, retrieved.State);
        Assert.Equal(1500, retrieved.TotalActiveFocusSeconds);
        Assert.Equal(2, retrieved.DistractionInterventionsCount);
        Assert.Single(retrieved.CompletedTaskIds);
        Assert.Equal(completedTaskId, retrieved.CompletedTaskIds[0]);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
