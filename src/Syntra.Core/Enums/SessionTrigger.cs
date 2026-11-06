namespace Syntra.Core.Enums;

/// <summary>
/// Triggers that drive deterministic state transitions within the SYNTRA session engine.
/// </summary>
public enum SessionTrigger
{
    /// <summary>Loads curriculum and initializes profile.</summary>
    Initialize = 0,

    /// <summary>Engages full-screen lockdown and focus timer.</summary>
    StartSession = 1,

    /// <summary>Pauses the active timer for an allowable break.</summary>
    Pause = 2,

    /// <summary>Resumes active focus mode.</summary>
    Resume = 3,

    /// <summary>
    /// Triggered when the focus timer elapses. 
    /// INVARIANT: Can ONLY transition from Active to TaskAssessment. Never to TaskCompleted.
    /// </summary>
    TriggerAssessment = 4,

    /// <summary>Triggered when assessment achieves >= 80% passing score.</summary>
    PassAssessment = 5,

    /// <summary>Triggered when assessment score falls below passing threshold.</summary>
    FailAssessment = 6,

    /// <summary>Starts the next scheduled task in the study plan.</summary>
    AdvanceToNextTask = 7,

    /// <summary>Finalizes session once all tasks are completed.</summary>
    CompleteSession = 8,

    /// <summary>Invoked when user successfully satisfies emergency hatch requirements.</summary>
    TriggerEmergencyExit = 9,

    /// <summary>Recovers state after an unexpected power outage or crash.</summary>
    Recover = 10
}
