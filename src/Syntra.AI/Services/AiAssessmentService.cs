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
/// AI-powered assessment generation and semantic grading service with hardened JSON extraction
/// and deterministic fallback resilience.
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

        var count = Math.Max(1, questionCount);
        var ratio = Math.Clamp(minimumPassRatio, 0.01, 1.0);

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
        QUESTION COUNT: {count}
        LEARNING OBJECTIVES:
        {fencedObjectives}
        """;

        AssessmentDto? assessmentDto = null;
        try
        {
            var rawOutput = await _client.GenerateCompletionAsync(systemPrompt, userPrompt, cancellationToken);
            var cleanJson = PromptFencing.ExtractJsonPayload(rawOutput);

            assessmentDto = JsonSerializer.Deserialize<AssessmentDto>(cleanJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception)
        {
            assessmentDto = null;
        }

        // Resilient fallback if remote LLM produced unparseable response
        if (assessmentDto == null || assessmentDto.Questions == null || assessmentDto.Questions.Count == 0)
        {
            assessmentDto = new AssessmentDto(new List<QuestionDto>
            {
                new(
                    Prompt: $"Identify the central architectural purpose of '{task.Title}'.",
                    QuestionType: "MultipleChoice",
                    ExpectedAnswer: "Active comprehension verification",
                    RubricExplanation: "The system enforces objective retention over passive browsing.",
                    Weight: 2.0,
                    Options: new List<string> { "Timer bypass", "Passive browsing", "Active comprehension verification", "Random guessing" },
                    Keywords: new List<string> { "comprehension", "verification" }),
                new(
                    Prompt: $"Which software component governs task progression for '{task.Title}'?",
                    QuestionType: "FillInTheBlank",
                    ExpectedAnswer: "SessionStateMachine",
                    RubricExplanation: "The deterministic state machine regulates valid state transitions.",
                    Weight: 1.0,
                    Options: new List<string>(),
                    Keywords: new List<string> { "statemachine" }),
                new(
                    Prompt: $"Explain how mastering the learning objectives of '{task.Title}' prevents operational failure.",
                    QuestionType: "FreeTextExplanation",
                    ExpectedAnswer: "Understanding underlying mechanics enables rapid diagnosis and prevention of edge case failure modes.",
                    RubricExplanation: "Must explain failure prevention, mechanics, or architectural invariants.",
                    Weight: 5.0,
                    Options: new List<string>(),
                    Keywords: new List<string> { "failure", "mechanics", "prevention" })
            });
        }

        var assessment = new Assessment(Guid.NewGuid(), task.Id, ratio);

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
        return PromptFencing.ExtractJsonPayload(rawJson);
    }
}
