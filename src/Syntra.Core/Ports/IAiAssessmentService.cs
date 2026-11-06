using Syntra.Core.Entities;

namespace Syntra.Core.Ports;

/// <summary>
/// Port contract for AI-powered assessment generation and semantic grading of free-text / synthesis responses.
/// </summary>
public interface IAiAssessmentService
{
    Task<Assessment> GenerateAssessmentAsync(
        StudyTask task,
        int questionCount = 3,
        double minimumPassRatio = 0.80,
        CancellationToken cancellationToken = default);

    Task<string> EvaluateFreeTextSemanticsAsync(
        AssessmentQuestion question,
        string studentAnswer,
        CancellationToken cancellationToken = default);
}
