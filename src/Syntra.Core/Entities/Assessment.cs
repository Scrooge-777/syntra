using Syntra.Core.Common;

namespace Syntra.Core.Entities;

/// <summary>
/// Assessment entity containing pedagogical questions and tracking attempts for an associated study task.
/// </summary>
public class Assessment
{
    private readonly List<AssessmentQuestion> _questions = new();
    private readonly List<AssessmentAttempt> _attempts = new();

    public Guid Id { get; private set; }
    public Guid TaskId { get; private set; }
    public double MinimumPassRatio { get; private set; }
    public DateTimeOffset GeneratedAtUtc { get; private set; }

    public IReadOnlyList<AssessmentQuestion> Questions => _questions.AsReadOnly();
    public IReadOnlyList<AssessmentAttempt> Attempts => _attempts.AsReadOnly();

    protected Assessment()
    {
    }

    public Assessment(
        Guid id,
        Guid taskId,
        double minimumPassRatio = 0.80,
        DateTimeOffset? generatedAtUtc = null,
        IEnumerable<AssessmentQuestion>? questions = null)
    {
        if (taskId == Guid.Empty)
            throw new DomainException("TaskId cannot be empty.");
        if (minimumPassRatio is < 0.50 or > 1.00)
            throw new DomainException("Minimum pass ratio must be between 0.50 and 1.00.");

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        TaskId = taskId;
        MinimumPassRatio = minimumPassRatio;
        GeneratedAtUtc = generatedAtUtc ?? DateTimeOffset.UtcNow;

        if (questions != null)
        {
            _questions.AddRange(questions);
        }
    }

    public void AddQuestion(AssessmentQuestion question)
    {
        ArgumentNullException.ThrowIfNull(question);
        _questions.Add(question);
    }

    public void RecordAttempt(AssessmentAttempt attempt)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        _attempts.Add(attempt);
    }

    public bool HasPassed()
    {
        return _attempts.Any(a => a.Passed);
    }

    public AssessmentAttempt? GetLatestAttempt()
    {
        return _attempts.OrderByDescending(a => a.AttemptedAtUtc).FirstOrDefault();
    }
}
