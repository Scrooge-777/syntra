using System.Text.Json;
using Syntra.AI.Clients;
using Syntra.AI.Models;
using Syntra.AI.Security;
using Syntra.Core.Common;
using Syntra.Core.Entities;
using Syntra.Core.Ports;

namespace Syntra.AI.Services;

/// <summary>
/// AI-powered curriculum generation service enforcing prompt fencing and structured JSON schema parsing.
/// </summary>
public class AiCurriculumService : IAiCurriculumService
{
    private readonly ILlmClient _client;

    public AiCurriculumService(ILlmClient? client = null)
    {
        _client = client ?? new RuleBasedFallbackLlmClient();
    }

    public async Task<StudyPlan> GeneratePlanAsync(
        Guid profileId,
        string topicOrSyllabus,
        int targetTotalMinutes,
        int taskIntervalMinutes = 25,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(topicOrSyllabus))
            throw new DomainException("Topic or syllabus cannot be empty.");

        var fencedSyllabus = PromptFencing.Fence(topicOrSyllabus);

        var systemPrompt = """
        ROLE: Senior Pedagogical CurriculumArchitect.
        INSTRUCTION: Decompose the provided syllabus content into sequential, bite-sized study tasks.
        FORMAT REQUIREMENT: Output strictly valid JSON matching CurriculumPlanDto schema. No conversational preamble.
        INVARIANT: Each task must have clear, testable learning objectives.
        """;

        var userPrompt = $"""
        TARGET TOTAL MINUTES: {targetTotalMinutes}
        INTERVAL MINUTES PER TASK: {taskIntervalMinutes}
        SOURCE SYLLABUS:
        {fencedSyllabus}
        """;

        var rawJson = await _client.GenerateCompletionAsync(systemPrompt, userPrompt, cancellationToken);
        var planDto = JsonSerializer.Deserialize<CurriculumPlanDto>(rawJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new DomainException("Failed to deserialize generated curriculum plan from AI response.");

        var plan = new StudyPlan(
            Guid.NewGuid(),
            profileId,
            planDto.Title,
            planDto.Description,
            planDto.Subject);

        foreach (var taskDto in planDto.Tasks)
        {
            var task = new StudyTask(
                Guid.NewGuid(),
                plan.Id,
                taskDto.Title,
                taskDto.AllottedMinutes > 0 ? taskDto.AllottedMinutes : taskIntervalMinutes,
                taskDto.LearningObjectives);

            plan.AddTask(task);
        }

        return plan;
    }

    public async Task<IReadOnlyList<string>> GenerateRemediationObjectivesAsync(
        StudyTask failedTask,
        AssessmentAttempt failedAttempt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(failedTask);
        ArgumentNullException.ThrowIfNull(failedAttempt);

        var prompt = $"""
        The student failed the comprehension assessment for task '{failedTask.Title}' with a score of {failedAttempt.ScorePercentage:F1}%.
        Identified feedback: {failedAttempt.Feedback}
        Generate 3 focused remedial objectives the student must review before re-attempting the quiz.
        """;

        // Fallback / deterministic guidance
        var remedial = failedTask.LearningObjectives
            .Select(o => $"Review core mechanics of {o} focusing on common failure modes.")
            .ToList();

        return await Task.FromResult(remedial.AsReadOnly());
    }
}
