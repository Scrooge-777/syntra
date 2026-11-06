namespace Syntra.Core.Entities;

/// <summary>
/// Record of a student's attempt to pass a task assessment.
/// </summary>
public class AssessmentAttempt
{
    public Guid Id { get; private set; }
    public Guid AssessmentId { get; private set; }
    public DateTimeOffset AttemptedAtUtc { get; private set; }
    public IReadOnlyDictionary<Guid, string> SubmittedAnswers { get; private set; } = new Dictionary<Guid, string>();
    public double EarnedPoints { get; private set; }
    public double TotalPossiblePoints { get; private set; }
    public double ScorePercentage { get; private set; }
    public bool Passed { get; private set; }
    public string Feedback { get; private set; } = string.Empty;

    protected AssessmentAttempt()
    {
    }

    public AssessmentAttempt(
        Guid id,
        Guid assessmentId,
        DateTimeOffset attemptedAtUtc,
        IDictionary<Guid, string> submittedAnswers,
        double earnedPoints,
        double totalPossiblePoints,
        double scorePercentage,
        bool passed,
        string feedback)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        AssessmentId = assessmentId;
        AttemptedAtUtc = attemptedAtUtc;
        SubmittedAnswers = new Dictionary<Guid, string>(submittedAnswers);
        EarnedPoints = earnedPoints;
        TotalPossiblePoints = totalPossiblePoints;
        ScorePercentage = scorePercentage;
        Passed = passed;
        Feedback = feedback ?? string.Empty;
    }
}
