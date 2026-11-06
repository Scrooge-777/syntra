namespace Syntra.Core.Enums;

/// <summary>
/// Execution status of an individual study task within a study plan.
/// </summary>
public enum StudyTaskStatus
{
    /// <summary>Task is queued in the study plan.</summary>
    Pending = 0,

    /// <summary>Task is currently active in focus mode.</summary>
    InProgress = 1,

    /// <summary>Task focus timer has completed; comprehension assessment is required.</summary>
    AssessmentRequired = 2,

    /// <summary>Task assessment passed (>= 80%) and task is officially complete.</summary>
    Completed = 3,

    /// <summary>Task assessment failed maximum allowable attempts and requires remediation.</summary>
    Failed = 4
}
