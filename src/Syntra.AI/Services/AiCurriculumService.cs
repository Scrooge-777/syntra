using System.Text.Json;
using Syntra.AI.Clients;
using Syntra.AI.Models;
using Syntra.AI.Security;
using Syntra.Core.Common;
using Syntra.Core.Entities;
using Syntra.Core.Ports;

namespace Syntra.AI.Services;

/// <summary>
/// AI-powered curriculum generation service enforcing prompt fencing, structured JSON schema parsing,
/// and resilient fallback recovery.
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

        var effectiveInterval = taskIntervalMinutes > 0 ? taskIntervalMinutes : 25;
        var effectiveTotal = targetTotalMinutes > 0 ? targetTotalMinutes : effectiveInterval * 2;

        var fencedSyllabus = PromptFencing.Fence(topicOrSyllabus);

        var systemPrompt = """
        ROLE: Senior Pedagogical CurriculumArchitect.
        INSTRUCTION: Decompose the provided syllabus content into sequential, bite-sized study tasks.
        FORMAT REQUIREMENT: Output strictly valid JSON matching CurriculumPlanDto schema. No conversational preamble.
        INVARIANT: Each task must have clear, testable learning objectives.
        """;

        var userPrompt = $"""
        TARGET TOTAL MINUTES: {effectiveTotal}
        INTERVAL MINUTES PER TASK: {effectiveInterval}
        SOURCE SYLLABUS:
        {fencedSyllabus}
        """;

        CurriculumPlanDto? planDto = null;
        try
        {
            var rawOutput = await _client.GenerateCompletionAsync(systemPrompt, userPrompt, cancellationToken);
            var cleanJson = PromptFencing.ExtractJsonPayload(rawOutput);

            planDto = JsonSerializer.Deserialize<CurriculumPlanDto>(cleanJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception)
        {
            planDto = null;
        }

        if (planDto == null || planDto.Tasks == null || planDto.Tasks.Count == 0)
        {
            planDto = new CurriculumPlanDto(
                Title: $"Deep Study: {topicOrSyllabus.Trim().Split('\n')[0]}",
                Description: "Auto-structured academic curriculum optimized for deep focus.",
                Subject: "Core Domain",
                Tasks: new List<TaskDto>
                {
                    new(
                        Title: "Foundational Mechanisms & Theory",
                        AllottedMinutes: effectiveInterval,
                        LearningObjectives: new List<string> { "Core Terminology", "Underlying Architecture", "Primary Mechanisms" }),
                    new(
                        Title: "Applied Analysis & Edge Cases",
                        AllottedMinutes: effectiveInterval,
                        LearningObjectives: new List<string> { "Failure Modes", "Practical Trade-offs", "Verification Standards" })
                });
        }

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
                taskDto.AllottedMinutes > 0 ? taskDto.AllottedMinutes : effectiveInterval,
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

        var remedial = failedTask.LearningObjectives
            .Select(o => $"Review core mechanics of {o} focusing on common failure modes.")
            .ToList();

        if (remedial.Count == 0)
        {
            remedial.Add($"Review core concepts for '{failedTask.Title}'.");
        }

        return await Task.FromResult(remedial.AsReadOnly());
    }
}
