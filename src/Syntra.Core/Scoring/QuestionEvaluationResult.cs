using Syntra.Core.Enums;

namespace Syntra.Core.Scoring;

/// <summary>
/// Evaluation breakdown for a single assessment question.
/// </summary>
public record QuestionEvaluationResult(
    Guid QuestionId,
    QuestionType QuestionType,
    double EarnedPoints,
    double MaxPoints,
    bool IsCorrect,
    string Feedback);
