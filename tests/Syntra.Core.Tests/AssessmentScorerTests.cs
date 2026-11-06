using Syntra.Core.Entities;
using Syntra.Core.Enums;
using Syntra.Core.Scoring;
using Xunit;

namespace Syntra.Core.Tests;

public class AssessmentScorerTests
{
    private readonly AssessmentScorer _scorer = new();

    [Fact]
    public void Score_ExactPassingBoundary_80Percent_ShouldPass()
    {
        var taskId = Guid.NewGuid();
        var assessment = new Assessment(Guid.NewGuid(), taskId, minimumPassRatio: 0.80);

        // 5 questions, each weight 1.0 -> 4 correct = 80.0%
        var q1 = new AssessmentQuestion(Guid.NewGuid(), "Q1", QuestionType.FillInTheBlank, "A", "R1", weight: 1.0);
        var q2 = new AssessmentQuestion(Guid.NewGuid(), "Q2", QuestionType.FillInTheBlank, "B", "R2", weight: 1.0);
        var q3 = new AssessmentQuestion(Guid.NewGuid(), "Q3", QuestionType.FillInTheBlank, "C", "R3", weight: 1.0);
        var q4 = new AssessmentQuestion(Guid.NewGuid(), "Q4", QuestionType.FillInTheBlank, "D", "R4", weight: 1.0);
        var q5 = new AssessmentQuestion(Guid.NewGuid(), "Q5", QuestionType.FillInTheBlank, "E", "R5", weight: 1.0);

        assessment.AddQuestion(q1);
        assessment.AddQuestion(q2);
        assessment.AddQuestion(q3);
        assessment.AddQuestion(q4);
        assessment.AddQuestion(q5);

        var answers = new Dictionary<Guid, string>
        {
            [q1.Id] = "A",
            [q2.Id] = "B",
            [q3.Id] = "C",
            [q4.Id] = "D",
            [q5.Id] = "Wrong"
        };

        var result = _scorer.Score(assessment, answers);

        Assert.Equal(4.0, result.EarnedPoints);
        Assert.Equal(5.0, result.TotalPossiblePoints);
        Assert.Equal(80.0, result.ScorePercentage);
        Assert.True(result.Passed);
    }

    [Fact]
    public void Score_BelowPassingBoundary_75Percent_ShouldFail()
    {
        var taskId = Guid.NewGuid();
        var assessment = new Assessment(Guid.NewGuid(), taskId, minimumPassRatio: 0.80);

        // 4 questions, each weight 1.0 -> 3 correct = 75.0%
        var q1 = new AssessmentQuestion(Guid.NewGuid(), "Q1", QuestionType.FillInTheBlank, "A", "R1", weight: 1.0);
        var q2 = new AssessmentQuestion(Guid.NewGuid(), "Q2", QuestionType.FillInTheBlank, "B", "R2", weight: 1.0);
        var q3 = new AssessmentQuestion(Guid.NewGuid(), "Q3", QuestionType.FillInTheBlank, "C", "R3", weight: 1.0);
        var q4 = new AssessmentQuestion(Guid.NewGuid(), "Q4", QuestionType.FillInTheBlank, "D", "R4", weight: 1.0);

        assessment.AddQuestion(q1);
        assessment.AddQuestion(q2);
        assessment.AddQuestion(q3);
        assessment.AddQuestion(q4);

        var answers = new Dictionary<Guid, string>
        {
            [q1.Id] = "A",
            [q2.Id] = "B",
            [q3.Id] = "C",
            [q4.Id] = "Wrong"
        };

        var result = _scorer.Score(assessment, answers);

        Assert.Equal(3.0, result.EarnedPoints);
        Assert.Equal(4.0, result.TotalPossiblePoints);
        Assert.Equal(75.0, result.ScorePercentage);
        Assert.False(result.Passed);
    }

    [Fact]
    public void MultipleChoice_ShouldSupportBothTextAndIndexMatching()
    {
        var q = new AssessmentQuestion(
            Guid.NewGuid(),
            "Which pattern decouples domain from persistence?",
            QuestionType.MultipleChoice,
            "Repository Pattern",
            "Repository isolates domain models from data access layers.",
            options: new[] { "Singleton", "Repository Pattern", "Active Record" },
            weight: 2.0);

        var assessment = new Assessment(Guid.NewGuid(), Guid.NewGuid(), 0.80, questions: new[] { q });

        // Test matching via text
        var textResult = _scorer.Score(assessment, new Dictionary<Guid, string> { [q.Id] = "Repository Pattern" });
        Assert.Equal(2.0, textResult.EarnedPoints);
        Assert.True(textResult.Passed);

        // Test matching via 0-based index string ("1")
        var indexResult = _scorer.Score(assessment, new Dictionary<Guid, string> { [q.Id] = "1" });
        Assert.Equal(2.0, indexResult.EarnedPoints);
        Assert.True(indexResult.Passed);
    }

    [Fact]
    public void FillInTheBlank_ShouldNormalizeWhitespaceAndPunctuation()
    {
        var q = new AssessmentQuestion(
            Guid.NewGuid(),
            "What hook intercepts low-level keystrokes?",
            QuestionType.FillInTheBlank,
            "WH_KEYBOARD_LL",
            "Low-level keyboard hook identifier.",
            weight: 1.0);

        var assessment = new Assessment(Guid.NewGuid(), Guid.NewGuid(), 0.80, questions: new[] { q });

        // Submitted with extra whitespace and lower casing
        var result = _scorer.Score(assessment, new Dictionary<Guid, string> { [q.Id] = "  wh_keyboard_ll  " });
        Assert.Equal(1.0, result.EarnedPoints);
        Assert.True(result.Passed);
    }

    [Fact]
    public void FreeText_ShouldEvaluateKeywordCoverage()
    {
        var q = new AssessmentQuestion(
            Guid.NewGuid(),
            "Explain the testing effect in cognitive psychology.",
            QuestionType.FreeTextExplanation,
            "Retrieval practice strengthens long-term memory consolidation.",
            "Must mention retrieval, memory, and consolidation.",
            weight: 10.0,
            keywords: new[] { "retrieval", "memory", "consolidation" });

        var assessment = new Assessment(Guid.NewGuid(), Guid.NewGuid(), 0.80, questions: new[] { q });

        var answer = "Active retrieval practice significantly enhances memory consolidation over passive review.";
        var result = _scorer.Score(assessment, new Dictionary<Guid, string> { [q.Id] = answer });

        Assert.Equal(10.0, result.EarnedPoints);
        Assert.True(result.Passed);
    }

    [Fact]
    public void WeightedScoring_ShouldCalculateAccurateProportions()
    {
        var qEasy = new AssessmentQuestion(Guid.NewGuid(), "Easy", QuestionType.FillInTheBlank, "A", "R1", weight: 1.0);
        var qHard = new AssessmentQuestion(Guid.NewGuid(), "Hard", QuestionType.FillInTheBlank, "B", "R2", weight: 9.0);

        var assessment = new Assessment(Guid.NewGuid(), Guid.NewGuid(), 0.80, questions: new[] { qEasy, qHard });

        // Getting only the hard question right = 9 / 10 = 90% (Passes)
        var hardCorrectAnswers = new Dictionary<Guid, string>
        {
            [qEasy.Id] = "Wrong",
            [qHard.Id] = "B"
        };
        var hardResult = _scorer.Score(assessment, hardCorrectAnswers);
        Assert.Equal(90.0, hardResult.ScorePercentage);
        Assert.True(hardResult.Passed);

        // Getting only the easy question right = 1 / 10 = 10% (Fails)
        var easyCorrectAnswers = new Dictionary<Guid, string>
        {
            [qEasy.Id] = "A",
            [qHard.Id] = "Wrong"
        };
        var easyResult = _scorer.Score(assessment, easyCorrectAnswers);
        Assert.Equal(10.0, easyResult.ScorePercentage);
        Assert.False(easyResult.Passed);
    }
}
