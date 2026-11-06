using System.Text.Json.Serialization;

namespace Syntra.AI.Models;

public record TaskDto(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("allottedMinutes")] int AllottedMinutes,
    [property: JsonPropertyName("learningObjectives")] List<string> LearningObjectives);

public record CurriculumPlanDto(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("tasks")] List<TaskDto> Tasks);

public record QuestionDto(
    [property: JsonPropertyName("prompt")] string Prompt,
    [property: JsonPropertyName("questionType")] string QuestionType,
    [property: JsonPropertyName("expectedAnswer")] string ExpectedAnswer,
    [property: JsonPropertyName("rubricExplanation")] string RubricExplanation,
    [property: JsonPropertyName("weight")] double Weight,
    [property: JsonPropertyName("options")] List<string>? Options,
    [property: JsonPropertyName("keywords")] List<string>? Keywords);

public record AssessmentDto(
    [property: JsonPropertyName("questions")] List<QuestionDto> Questions);

public record SemanticEvaluationDto(
    [property: JsonPropertyName("scorePercentage")] double ScorePercentage,
    [property: JsonPropertyName("passed")] bool Passed,
    [property: JsonPropertyName("feedback")] string Feedback,
    [property: JsonPropertyName("identifiedConcepts")] List<string> IdentifiedConcepts);
