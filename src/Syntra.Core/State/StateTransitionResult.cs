using Syntra.Core.Enums;

namespace Syntra.Core.State;

/// <summary>
/// Result of an attempted state transition.
/// </summary>
public record StateTransitionResult(
    bool Success,
    SessionState FromState,
    SessionState ToState,
    SessionTrigger Trigger,
    string? ErrorMessage = null);
