using Syntra.Core.Entities;

namespace Syntra.Core.Ports;

/// <summary>
/// Port contract for AI-driven curriculum generation from user topics, syllabus documents, or books.
/// </summary>
public interface IAiCurriculumService
{
    Task<StudyPlan> GeneratePlanAsync(
        Guid profileId,
        string topicOrSyllabus,
        int targetTotalMinutes,
        int taskIntervalMinutes = 25,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GenerateRemediationObjectivesAsync(
        StudyTask failedTask,
        AssessmentAttempt failedAttempt,
        CancellationToken cancellationToken = default);
}
