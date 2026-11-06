using Syntra.Core.Common;
using Syntra.Core.Enums;

namespace Syntra.Core.Entities;

/// <summary>
/// A discrete question within an assessment evaluating specific learning objectives.
/// </summary>
public class AssessmentQuestion
{
    public Guid Id { get; private set; }
    public string Prompt { get; private set; } = string.Empty;
    public QuestionType QuestionType { get; private set; }
    public IReadOnlyList<string> Options { get; private set; } = new List<string>().AsReadOnly();
    public string ExpectedAnswer { get; private set; } = string.Empty;
    public string RubricExplanation { get; private set; } = string.Empty;
    public double Weight { get; private set; } = 1.0;
    public IReadOnlyList<string> Keywords { get; private set; } = new List<string>().AsReadOnly();

    protected AssessmentQuestion()
    {
    }

    public AssessmentQuestion(
        Guid id,
        string prompt,
        QuestionType questionType,
        string expectedAnswer,
        string rubricExplanation,
        IEnumerable<string>? options = null,
        double weight = 1.0,
        IEnumerable<string>? keywords = null)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new DomainException("Question prompt cannot be empty.");
        if (string.IsNullOrWhiteSpace(expectedAnswer))
            throw new DomainException("Expected answer cannot be empty.");
        if (weight <= 0)
            throw new DomainException("Question weight must be strictly positive.");

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Prompt = prompt.Trim();
        QuestionType = questionType;
        ExpectedAnswer = expectedAnswer.Trim();
        RubricExplanation = rubricExplanation?.Trim() ?? string.Empty;
        Weight = weight;
        Options = (options ?? Enumerable.Empty<string>()).Select(o => o.Trim()).ToList().AsReadOnly();
        Keywords = (keywords ?? Enumerable.Empty<string>()).Select(k => k.Trim().ToLowerInvariant()).ToList().AsReadOnly();

        if (QuestionType == QuestionType.MultipleChoice && Options.Count < 2)
        {
            throw new DomainException("Multiple choice questions must have at least 2 options.");
        }
    }
}
