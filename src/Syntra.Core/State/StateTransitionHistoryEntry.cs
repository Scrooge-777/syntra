using Syntra.Core.Enums;

namespace Syntra.Core.State;

/// <summary>
/// Immutable log record of an audited state transition.
/// </summary>
public record StateTransitionHistoryEntry(
    SessionState FromState,
    SessionState ToState,
    SessionTrigger Trigger,
    DateTimeOffset TimestampUtc,
    string? ContextNote = null);
