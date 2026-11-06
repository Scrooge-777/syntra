namespace Syntra.Core.Enums;

/// <summary>
/// Types of pedagogical questions evaluated by the SYNTRA assessment engine.
/// </summary>
public enum QuestionType
{
    /// <summary>Multiple choice question with discrete single or multi-select options.</summary>
    MultipleChoice = 0,

    /// <summary>Fill in the blank targeting exact keywords or normalized terminology.</summary>
    FillInTheBlank = 1,

    /// <summary>Free text conceptual explanation graded against a semantic rubric.</summary>
    FreeTextExplanation = 2,

    /// <summary>Code snippet analysis or bug identification.</summary>
    CodeSnippet = 3
}
