namespace Syntra.Core.Entities;

/// <summary>
/// Record of a student's attempt to pass a task assessment.
/// </summary>
public class AssessmentAttempt
{
    public Guid Id { get; }
    public Guid AssessmentId { get; }
    public DateTimeOffset AttemptedAtUtc { get; }
    public IReadOnlyDictionary<Guid, string> SubmittedAnswers { get; }
    public double EarnedPoints { get; }
    public double TotalPossiblePoints { get; }
    public double ScorePercentage { get; }
    public bool Passed { get; }
    public string Feedback { get; }

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
