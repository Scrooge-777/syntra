using System.Text.Json;
using Syntra.AI.Clients;
using Syntra.AI.Models;
using Syntra.AI.Security;
using Syntra.Core.Common;
using Syntra.Core.Entities;
using Syntra.Core.Enums;
using Syntra.Core.Ports;

namespace Syntra.AI.Services;

/// <summary>
/// AI-powered assessment generation and semantic grading service.
/// </summary>
public class AiAssessmentService : IAiAssessmentService
{
    private readonly ILlmClient _client;

    public AiAssessmentService(ILlmClient? client = null)
    {
        _client = client ?? new RuleBasedFallbackLlmClient();
    }

    public async Task<Assessment> GenerateAssessmentAsync(
        StudyTask task,
        int questionCount = 3,
        double minimumPassRatio = 0.80,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        var objectives = string.Join("; ", task.LearningObjectives);
        var fencedObjectives = PromptFencing.Fence(objectives);

        var systemPrompt = """
        ROLE: Senior Pedagogical AssessmentGenerator.
        INSTRUCTION: Generate an active comprehension quiz evaluating the provided learning objectives.
        FORMAT REQUIREMENT: Output strictly valid JSON matching AssessmentDto schema.
        INVARIANT: Include a mix of MultipleChoice, FillInTheBlank, and FreeTextExplanation questions.
        """;

        var userPrompt = $"""
        TASK TITLE: {task.Title}
        QUESTION COUNT: {questionCount}
        LEARNING OBJECTIVES:
        {fencedObjectives}
        """;

        var rawJson = await _client.GenerateCompletionAsync(systemPrompt, userPrompt, cancellationToken);
        var assessmentDto = JsonSerializer.Deserialize<AssessmentDto>(rawJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new DomainException("Failed to deserialize generated assessment from AI response.");

        var assessment = new Assessment(Guid.NewGuid(), task.Id, minimumPassRatio);

        foreach (var qDto in assessmentDto.Questions)
        {
            var qType = Enum.TryParse<QuestionType>(qDto.QuestionType, ignoreCase: true, out var parsedType)
                ? parsedType
                : QuestionType.FreeTextExplanation;

            var question = new AssessmentQuestion(
                Guid.NewGuid(),
                qDto.Prompt,
                qType,
                qDto.ExpectedAnswer,
                qDto.RubricExplanation,
                qDto.Options,
                qDto.Weight > 0 ? qDto.Weight : 1.0,
                qDto.Keywords);

            assessment.AddQuestion(question);
        }

        return assessment;
    }

    public async Task<string> EvaluateFreeTextSemanticsAsync(
        AssessmentQuestion question,
        string studentAnswer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(question);

        var fencedAnswer = PromptFencing.Fence(studentAnswer);

        var systemPrompt = """
        ROLE: Academic Grader.
        INSTRUCTION: Grade the student's answer against the expected rubric and return a JSON matching SemanticEvaluationDto.
        """;

        var userPrompt = $"""
        QUESTION PROMPT: {question.Prompt}
        EXPECTED ANSWER: {question.ExpectedAnswer}
        RUBRIC EXPLANATION: {question.RubricExplanation}
        STUDENT SUBMISSION:
        {fencedAnswer}
        """;

        var rawJson = await _client.GenerateCompletionAsync(systemPrompt, userPrompt, cancellationToken);
        return rawJson;
    }
}
