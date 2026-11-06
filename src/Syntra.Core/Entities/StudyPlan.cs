using Syntra.Core.Common;
using Syntra.Core.Enums;

namespace Syntra.Core.Entities;

/// <summary>
/// A structured study curriculum containing ordered study tasks.
/// </summary>
public class StudyPlan
{
    private readonly List<StudyTask> _tasks = new();

    public Guid Id { get; }
    public Guid ProfileId { get; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public string Subject { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }

    public IReadOnlyList<StudyTask> Tasks => _tasks.AsReadOnly();
    public int TotalEstimatedMinutes => _tasks.Sum(t => t.AllottedMinutes);

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
        return _tasks.FirstOrDefault(t => t.Status is StudyTaskStatus.Pending or StudyTaskStatus.AssessmentRequired);
    }

    public bool AreAllTasksCompleted()
    {
        return _tasks.Count > 0 && _tasks.All(t => t.Status == StudyTaskStatus.Completed);
    }

    public void SetActive(bool active)
    {
        IsActive = active;
    }
}
