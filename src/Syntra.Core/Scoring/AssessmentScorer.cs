using System.Text.RegularExpressions;
using Syntra.Core.Common;
using Syntra.Core.Entities;
using Syntra.Core.Enums;

namespace Syntra.Core.Scoring;

/// <summary>
/// Domain scoring authority enforcing comprehension standards.
/// Evaluates question types (MCQ, Fill-in-the-blank, Free Text, Code) and calculates
/// weighted scores against the minimum passing threshold (default 80%).
/// Hardened with defensive boundary checks, token sanitization, and multi-word phrase matching.
/// </summary>
public class AssessmentScorer
{
    private static readonly Regex TokenSanitizerRegex = new(@"[^\w\s]", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    /// <summary>
    /// Scores submitted answers against an assessment and produces an official ScoringResult.
    /// </summary>
    public ScoringResult Score(Assessment assessment, IDictionary<Guid, string>? submittedAnswers)
    {
        ArgumentNullException.ThrowIfNull(assessment);
        var answers = submittedAnswers ?? new Dictionary<Guid, string>();

        if (assessment.Questions == null || assessment.Questions.Count == 0)
        {
            throw new DomainException("Cannot score an assessment with zero questions.");
        }

        var results = new List<QuestionEvaluationResult>();
        double totalEarned = 0.0;
        double totalPossible = 0.0;

        foreach (var question in assessment.Questions)
        {
            // Defensive: ensure positive weight
            var maxPoints = question.Weight > 0 ? question.Weight : 1.0;
            totalPossible += maxPoints;

            answers.TryGetValue(question.Id, out var rawAnswer);
            var answer = rawAnswer?.Trim() ?? string.Empty;

            var evaluation = EvaluateQuestion(question, answer, maxPoints);
            results.Add(evaluation);
            totalEarned += evaluation.EarnedPoints;
        }

        var scorePercentage = totalPossible > 0 ? (totalEarned / totalPossible) * 100.0 : 0.0;
        
        // Defensive clamping: ensure minimum pass ratio is within [0.01, 1.0]
        var clampedRatio = Math.Clamp(assessment.MinimumPassRatio, 0.01, 1.0);
        var requiredPercentage = clampedRatio * 100.0;
        var passed = scorePercentage >= requiredPercentage;

        var summary = passed
            ? $"Assessment PASSED with {scorePercentage:F1}% (Required: {requiredPercentage:F1}%). High comprehension demonstrated."
            : $"Assessment FAILED with {scorePercentage:F1}% (Required: {requiredPercentage:F1}%). Further remediation required.";

        return new ScoringResult(
            EarnedPoints: Math.Round(totalEarned, 2),
            TotalPossiblePoints: Math.Round(totalPossible, 2),
            ScorePercentage: Math.Round(scorePercentage, 2),
            Passed: passed,
            MinimumPassRatio: clampedRatio,
            QuestionResults: results.AsReadOnly(),
            SummaryFeedback: summary);
    }

    private static QuestionEvaluationResult EvaluateQuestion(AssessmentQuestion question, string answer, double effectiveWeight)
    {
        return question.QuestionType switch
        {
            QuestionType.MultipleChoice => EvaluateMultipleChoice(question, answer, effectiveWeight),
            QuestionType.FillInTheBlank => EvaluateFillInTheBlank(question, answer, effectiveWeight),
            QuestionType.FreeTextExplanation => EvaluateFreeText(question, answer, effectiveWeight),
            QuestionType.CodeSnippet => EvaluateCodeSnippet(question, answer, effectiveWeight),
            _ => throw new DomainException($"Unsupported question type: {question.QuestionType}")
        };
    }

    private static QuestionEvaluationResult EvaluateMultipleChoice(AssessmentQuestion question, string answer, double weight)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            return new QuestionEvaluationResult(question.Id, question.QuestionType, 0, weight, false, "No answer provided.");
        }

        // Compare exact option text or index
        var isMatch = string.Equals(question.ExpectedAnswer?.Trim(), answer.Trim(), StringComparison.OrdinalIgnoreCase);

        // Also allow comparing if answer is option index (e.g. "0", "1", "A", "B")
        if (!isMatch && question.Options != null && int.TryParse(answer, out var index) && index >= 0 && index < question.Options.Count)
        {
            isMatch = string.Equals(question.ExpectedAnswer?.Trim(), question.Options[index]?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        var earned = isMatch ? weight : 0.0;
        var feedback = isMatch
            ? "Correct."
            : $"Incorrect. Expected: '{question.ExpectedAnswer}'. {question.RubricExplanation}";

        return new QuestionEvaluationResult(question.Id, question.QuestionType, earned, weight, isMatch, feedback);
    }

    private static QuestionEvaluationResult EvaluateFillInTheBlank(AssessmentQuestion question, string answer, double weight)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            return new QuestionEvaluationResult(question.Id, question.QuestionType, 0, weight, false, "No answer provided.");
        }

        var normalizedSubmission = NormalizeToken(answer);
        var normalizedExpected = NormalizeToken(question.ExpectedAnswer ?? string.Empty);

        var isMatch = string.Equals(normalizedSubmission, normalizedExpected, StringComparison.OrdinalIgnoreCase);
        var earned = isMatch ? weight : 0.0;
        var feedback = isMatch
            ? "Correct."
            : $"Incorrect. Expected keyword: '{question.ExpectedAnswer}'. {question.RubricExplanation}";

        return new QuestionEvaluationResult(question.Id, question.QuestionType, earned, weight, isMatch, feedback);
    }

    private static QuestionEvaluationResult EvaluateFreeText(AssessmentQuestion question, string answer, double weight)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            return new QuestionEvaluationResult(question.Id, question.QuestionType, 0, weight, false, "No explanation provided.");
        }

        var answerLower = answer.ToLowerInvariant();
        var normalizedWords = TokenSanitizerRegex.Replace(answerLower, " ")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet();

        var validKeywords = question.Keywords?
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim().ToLowerInvariant())
            .Distinct()
            .ToList() ?? new List<string>();

        if (validKeywords.Count == 0)
        {
            // Fallback: minimal length check if no keywords provided
            var minLengthPassed = answer.Trim().Length >= 20;
            var points = minLengthPassed ? weight : 0.0;
            return new QuestionEvaluationResult(question.Id, question.QuestionType, points, weight, minLengthPassed, question.RubricExplanation);
        }

        // Hardened: Supports both individual tokens and compound multi-word key phrases
        var matchedCount = 0;
        foreach (var kw in validKeywords)
        {
            if (kw.Contains(' '))
            {
                if (answerLower.Contains(kw))
                {
                    matchedCount++;
                }
            }
            else
            {
                if (normalizedWords.Contains(kw) || answerLower.Contains(kw))
                {
                    matchedCount++;
                }
            }
        }

        var coverageRatio = (double)matchedCount / validKeywords.Count;

        // Proportional grading with minimum threshold
        double earnedPoints;
        bool isCorrect;

        if (coverageRatio >= 0.75)
        {
            earnedPoints = weight;
            isCorrect = true;
        }
        else if (coverageRatio >= 0.50)
        {
            earnedPoints = weight * 0.70;
            isCorrect = false;
        }
        else if (coverageRatio >= 0.25)
        {
            earnedPoints = weight * 0.35;
            isCorrect = false;
        }
        else
        {
            earnedPoints = 0.0;
            isCorrect = false;
        }

        var feedback = $"Keyword coverage: {matchedCount}/{validKeywords.Count} concepts identified. {question.RubricExplanation}";
        return new QuestionEvaluationResult(question.Id, question.QuestionType, Math.Round(earnedPoints, 2), weight, isCorrect, feedback);
    }

    private static QuestionEvaluationResult EvaluateCodeSnippet(AssessmentQuestion question, string answer, double weight)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            return new QuestionEvaluationResult(question.Id, question.QuestionType, 0, weight, false, "No code provided.");
        }

        var normalizedAnswer = NormalizeWhitespace(answer);
        var normalizedExpected = NormalizeWhitespace(question.ExpectedAnswer ?? string.Empty);

        var isExact = string.Equals(normalizedAnswer, normalizedExpected, StringComparison.OrdinalIgnoreCase);
        if (isExact)
        {
            return new QuestionEvaluationResult(question.Id, question.QuestionType, weight, weight, true, "Code match verified.");
        }

        var answerLower = answer.ToLowerInvariant();
        var validKeywords = question.Keywords?
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim().ToLowerInvariant())
            .ToList() ?? new List<string>();

        var matched = validKeywords.Count(k => answerLower.Contains(k));
        var ratio = validKeywords.Count > 0 ? (double)matched / validKeywords.Count : 0.0;

        var earned = ratio >= 0.80 ? weight : weight * ratio;
        var pass = ratio >= 0.80;

        return new QuestionEvaluationResult(
            question.Id,
            question.QuestionType,
            Math.Round(earned, 2),
            weight,
            pass,
            $"Code constructs verified: {matched}/{validKeywords.Count}. {question.RubricExplanation}");
    }

    private static string NormalizeToken(string input)
    {
        return TokenSanitizerRegex.Replace(input.Trim().ToLowerInvariant(), "");
    }

    private static string NormalizeWhitespace(string input)
    {
        return WhitespaceRegex.Replace(input.Trim(), " ");
    }
}
