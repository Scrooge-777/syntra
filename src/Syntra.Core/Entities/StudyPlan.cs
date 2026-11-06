using Syntra.Core.Common;

namespace Syntra.Core.Entities;

/// <summary>
/// A structured study curriculum containing ordered study tasks.
/// </summary>
public class StudyPlan
{
    private readonly List<StudyTask> _tasks = new();

    public Guid Id { get; private set; }
    public Guid ProfileId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyList<StudyTask> Tasks => _tasks.AsReadOnly();
    public int TotalEstimatedMinutes => _tasks.Sum(t => t.AllottedMinutes);

    protected StudyPlan()
    {
    }

    public StudyPlan(
        Guid id,
        Guid profileId,
        string title,
        string description,
        string subject,
        bool isActive = true,
        DateTimeOffset? createdAtUtc = null,
        IEnumerable<StudyTask>? tasks = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Plan title cannot be empty.");
        if (string.IsNullOrWhiteSpace(subject))
            throw new DomainException("Plan subject cannot be empty.");

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        ProfileId = profileId;
        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        Subject = subject.Trim();
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow;

        if (tasks != null)
        {
            _tasks.AddRange(tasks);
        }
    }

    public void AddTask(StudyTask task)
    {
        ArgumentNullException.ThrowIfNull(task);
        if (task.PlanId != Id)
            throw new DomainException($"Task PlanId ({task.PlanId}) does not match plan Id ({Id}).");

        _tasks.Add(task);
    }

    public StudyTask? GetNextPendingTask()
    {
        return _tasks.FirstOrDefault(t => t.Status is Enums.StudyTaskStatus.Pending or Enums.StudyTaskStatus.AssessmentRequired);
    }

    public bool AreAllTasksCompleted()
    {
        return _tasks.Count > 0 && _tasks.All(t => t.Status == Enums.StudyTaskStatus.Completed);
    }

    public void SetActive(bool active)
    {
        IsActive = active;
    }
}
