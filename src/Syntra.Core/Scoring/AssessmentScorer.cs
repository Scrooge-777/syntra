using System.Text.RegularExpressions;
using Syntra.Core.Common;
using Syntra.Core.Entities;
using Syntra.Core.Enums;

namespace Syntra.Core.Scoring;

/// <summary>
/// Domain scoring authority enforcing comprehension standards.
/// Evaluates question types (MCQ, Fill-in-the-blank, Free Text, Code) and calculates
/// weighted scores against the minimum passing threshold (default 80%).
/// </summary>
public class AssessmentScorer
{
    private static readonly Regex TokenSanitizerRegex = new(@"[^\w\s]", RegexOptions.Compiled);

    /// <summary>
    /// Scores submitted answers against an assessment and produces an official ScoringResult.
    /// </summary>
    public ScoringResult Score(Assessment assessment, IDictionary<Guid, string> submittedAnswers)
    {
        ArgumentNullException.ThrowIfNull(assessment);
        ArgumentNullException.ThrowIfNull(submittedAnswers);

        if (assessment.Questions.Count == 0)
        {
            throw new DomainException("Cannot score an assessment with zero questions.");
        }

        var results = new List<QuestionEvaluationResult>();
        double totalEarned = 0.0;
        double totalPossible = 0.0;

        foreach (var question in assessment.Questions)
        {
            var maxPoints = question.Weight;
            totalPossible += maxPoints;

            submittedAnswers.TryGetValue(question.Id, out var rawAnswer);
            var answer = rawAnswer?.Trim() ?? string.Empty;

            var evaluation = EvaluateQuestion(question, answer);
            results.Add(evaluation);
            totalEarned += evaluation.EarnedPoints;
        }

        var scorePercentage = totalPossible > 0 ? (totalEarned / totalPossible) * 100.0 : 0.0;
        // Invariant: Exact threshold comparison without loose upward rounding
        var requiredPercentage = assessment.MinimumPassRatio * 100.0;
        var passed = scorePercentage >= requiredPercentage;

        var summary = passed
            ? $"Assessment PASSED with {scorePercentage:F1}% (Required: {requiredPercentage:F1}%). High comprehension demonstrated."
            : $"Assessment FAILED with {scorePercentage:F1}% (Required: {requiredPercentage:F1}%). Further remediation required.";

        return new ScoringResult(
            EarnedPoints: totalEarned,
            TotalPossiblePoints: totalPossible,
            ScorePercentage: Math.Round(scorePercentage, 2),
            Passed: passed,
            MinimumPassRatio: assessment.MinimumPassRatio,
            QuestionResults: results.AsReadOnly(),
            SummaryFeedback: summary);
    }

    private static QuestionEvaluationResult EvaluateQuestion(AssessmentQuestion question, string answer)
    {
        return question.QuestionType switch
        {
            QuestionType.MultipleChoice => EvaluateMultipleChoice(question, answer),
            QuestionType.FillInTheBlank => EvaluateFillInTheBlank(question, answer),
            QuestionType.FreeTextExplanation => EvaluateFreeText(question, answer),
            QuestionType.CodeSnippet => EvaluateCodeSnippet(question, answer),
            _ => throw new DomainException($"Unsupported question type: {question.QuestionType}")
        };
    }

    private static QuestionEvaluationResult EvaluateMultipleChoice(AssessmentQuestion question, string answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            return new QuestionEvaluationResult(question.Id, question.QuestionType, 0, question.Weight, false, "No answer provided.");
        }

        // Compare exact option text or index
        var isMatch = string.Equals(question.ExpectedAnswer.Trim(), answer.Trim(), StringComparison.OrdinalIgnoreCase);

        // Also allow comparing if answer is option index (e.g. "0", "1", "A", "B")
        if (!isMatch && int.TryParse(answer, out var index) && index >= 0 && index < question.Options.Count)
        {
            isMatch = string.Equals(question.ExpectedAnswer.Trim(), question.Options[index].Trim(), StringComparison.OrdinalIgnoreCase);
        }

        var earned = isMatch ? question.Weight : 0.0;
        var feedback = isMatch
            ? "Correct."
            : $"Incorrect. Expected: '{question.ExpectedAnswer}'. {question.RubricExplanation}";

        return new QuestionEvaluationResult(question.Id, question.QuestionType, earned, question.Weight, isMatch, feedback);
    }

    private static QuestionEvaluationResult EvaluateFillInTheBlank(AssessmentQuestion question, string answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            return new QuestionEvaluationResult(question.Id, question.QuestionType, 0, question.Weight, false, "No answer provided.");
        }

        var normalizedSubmission = NormalizeToken(answer);
        var normalizedExpected = NormalizeToken(question.ExpectedAnswer);

        var isMatch = string.Equals(normalizedSubmission, normalizedExpected, StringComparison.OrdinalIgnoreCase);
        var earned = isMatch ? question.Weight : 0.0;
        var feedback = isMatch
            ? "Correct."
            : $"Incorrect. Expected keyword: '{question.ExpectedAnswer}'. {question.RubricExplanation}";

        return new QuestionEvaluationResult(question.Id, question.QuestionType, earned, question.Weight, isMatch, feedback);
    }

    private static QuestionEvaluationResult EvaluateFreeText(AssessmentQuestion question, string answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            return new QuestionEvaluationResult(question.Id, question.QuestionType, 0, question.Weight, false, "No explanation provided.");
        }

        // Domain heuristic: Keyword density evaluation against question keywords
        var normalizedWords = TokenSanitizerRegex.Replace(answer.ToLowerInvariant(), " ")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet();

        if (question.Keywords.Count == 0)
        {
            // Fallback: minimal length check if no keywords provided
            var minLengthPassed = answer.Length >= 20;
            var points = minLengthPassed ? question.Weight : 0.0;
            return new QuestionEvaluationResult(question.Id, question.QuestionType, points, question.Weight, minLengthPassed, question.RubricExplanation);
        }

        var matchedKeywords = question.Keywords.Count(k => normalizedWords.Contains(k));
        var coverageRatio = (double)matchedKeywords / question.Keywords.Count;

        // Proportional grading with minimum threshold
        double earnedPoints;
        bool isCorrect;

        if (coverageRatio >= 0.75)
        {
            earnedPoints = question.Weight;
            isCorrect = true;
        }
        else if (coverageRatio >= 0.50)
        {
            earnedPoints = question.Weight * 0.70;
            isCorrect = false;
        }
        else if (coverageRatio >= 0.25)
        {
            earnedPoints = question.Weight * 0.35;
            isCorrect = false;
        }
        else
        {
            earnedPoints = 0.0;
            isCorrect = false;
        }

        var feedback = $"Keyword coverage: {matchedKeywords}/{question.Keywords.Count} concepts identified. {question.RubricExplanation}";
        return new QuestionEvaluationResult(question.Id, question.QuestionType, Math.Round(earnedPoints, 2), question.Weight, isCorrect, feedback);
    }

    private static QuestionEvaluationResult EvaluateCodeSnippet(AssessmentQuestion question, string answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            return new QuestionEvaluationResult(question.Id, question.QuestionType, 0, question.Weight, false, "No code provided.");
        }

        var normalizedAnswer = NormalizeWhitespace(answer);
        var normalizedExpected = NormalizeWhitespace(question.ExpectedAnswer);

        var isExact = string.Equals(normalizedAnswer, normalizedExpected, StringComparison.OrdinalIgnoreCase);
        if (isExact)
        {
            return new QuestionEvaluationResult(question.Id, question.QuestionType, question.Weight, question.Weight, true, "Code match verified.");
        }

        // Semantic keyword check for code constructs
        var answerLower = answer.ToLowerInvariant();
        var matched = question.Keywords.Count(k => answerLower.Contains(k));
        var ratio = question.Keywords.Count > 0 ? (double)matched / question.Keywords.Count : 0.0;

        var earned = ratio >= 0.80 ? question.Weight : question.Weight * ratio;
        var pass = ratio >= 0.80;

        return new QuestionEvaluationResult(
            question.Id,
            question.QuestionType,
            Math.Round(earned, 2),
            question.Weight,
            pass,
            $"Code constructs verified: {matched}/{question.Keywords.Count}. {question.RubricExplanation}");
    }

    private static string NormalizeToken(string input)
    {
        return TokenSanitizerRegex.Replace(input.Trim().ToLowerInvariant(), "");
    }

    private static string NormalizeWhitespace(string input)
    {
        return Regex.Replace(input.Trim(), @"\s+", " ");
    }
}
