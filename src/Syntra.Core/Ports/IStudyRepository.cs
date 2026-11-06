using Syntra.Core.Entities;

namespace Syntra.Core.Ports;

/// <summary>
/// Port contract for local persistence of study profiles, plans, tasks, and historical sessions.
/// </summary>
public interface IStudyRepository
{
    Task<StudyProfile?> GetProfileAsync(Guid profileId, CancellationToken cancellationToken = default);
    Task SaveProfileAsync(StudyProfile profile, CancellationToken cancellationToken = default);

    Task<StudyPlan?> GetPlanAsync(Guid planId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudyPlan>> GetPlansForProfileAsync(Guid profileId, CancellationToken cancellationToken = default);
    Task SavePlanAsync(StudyPlan plan, CancellationToken cancellationToken = default);

    Task<StudySession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task SaveSessionAsync(StudySession session, CancellationToken cancellationToken = default);

    Task<Assessment?> GetAssessmentForTaskAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task SaveAssessmentAsync(Assessment assessment, CancellationToken cancellationToken = default);
}
