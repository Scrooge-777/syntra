namespace Syntra.Core.Enums;

/// <summary>
/// Deterministic states of a SYNTRA study session.
/// </summary>
public enum SessionState
{
    /// <summary>Initial state prior to session configuration or load.</summary>
    NotStarted = 0,

    /// <summary>Curriculum and plan loaded, ready for the user to initiate lockdown.</summary>
    Ready = 1,

    /// <summary>Active focus lockdown mode. Windows hooks engaged, timers running.</summary>
    Active = 2,

    /// <summary>Temporary, audited break or interruption window.</summary>
    Paused = 3,

    /// <summary>
    /// Active comprehension assessment mode. 
    /// Non-negotiable gate: Timer has completed; user must prove >=80% comprehension.
    /// </summary>
    TaskAssessment = 4,

    /// <summary>The active task's assessment has passed. Preparing next task or wrap-up.</summary>
    TaskCompleted = 5,

    /// <summary>All planned study tasks completed and verified with comprehension passing scores.</summary>
    SessionCompleted = 6,

    /// <summary>User unlocked the environment via emergency passphrase or 3-minute cooldown.</summary>
    EmergencyExit = 7,

    /// <summary>The session was interrupted abnormally (crash, process termination) and requires recovery.</summary>
    RecoveryRequired = 8
}
