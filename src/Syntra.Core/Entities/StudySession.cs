using Syntra.Core.Common;
using Syntra.Core.Enums;

namespace Syntra.Core.Entities;

/// <summary>
/// A live or completed study session entity capturing operational telemetry,
/// active task state, and distraction intervention metrics.
/// </summary>
public class StudySession
{
    private readonly List<Guid> _completedTaskIds = new();

    public Guid Id { get; }
    public Guid ProfileId { get; }
    public Guid PlanId { get; }
    public Guid? ActiveTaskId { get; private set; }
    public SessionState State { get; private set; }
    public DateTimeOffset StartedAtUtc { get; }
    public DateTimeOffset? EndedAtUtc { get; private set; }
    public int TotalActiveFocusSeconds { get; private set; }
    public int DistractionInterventionsCount { get; private set; }

    public IReadOnlyList<Guid> CompletedTaskIds => _completedTaskIds.AsReadOnly();

    public StudySession(
        Guid id,
        Guid profileId,
        Guid planId,
        Guid? activeTaskId,
        DateTimeOffset startedAtUtc,
        SessionState initialState = SessionState.Ready)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        ProfileId = profileId;
        PlanId = planId;
        ActiveTaskId = activeTaskId;
        StartedAtUtc = startedAtUtc;
        State = initialState;
        TotalActiveFocusSeconds = 0;
        DistractionInterventionsCount = 0;
    }

    public void SetActiveTask(Guid taskId)
    {
        ActiveTaskId = taskId;
    }

    public void UpdateState(SessionState newState)
    {
        State = newState;
    }

    public void AddActiveFocusSeconds(int seconds)
    {
        if (seconds < 0)
            throw new DomainException("Focus seconds increment cannot be negative.");

        TotalActiveFocusSeconds += seconds;
    }

    public void RecordDistractionIntervention()
    {
        DistractionInterventionsCount++;
    }

    public void MarkTaskCompleted(Guid taskId)
    {
        if (!_completedTaskIds.Contains(taskId))
        {
            _completedTaskIds.Add(taskId);
        }
    }

    public void EndSession(DateTimeOffset endedAtUtc)
    {
        EndedAtUtc = endedAtUtc;
    }
}
