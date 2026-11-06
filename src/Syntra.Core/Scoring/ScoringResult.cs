namespace Syntra.Core.Scoring;

/// <summary>
/// Comprehensive outcome of an assessment scoring evaluation.
/// </summary>
public record ScoringResult(
    double EarnedPoints,
    double TotalPossiblePoints,
    double ScorePercentage,
    bool Passed,
    double MinimumPassRatio,
    IReadOnlyList<QuestionEvaluationResult> QuestionResults,
    string SummaryFeedback);
