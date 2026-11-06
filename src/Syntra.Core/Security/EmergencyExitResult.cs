namespace Syntra.Core.Security;

/// <summary>
/// Status of an emergency exit attempt.
/// </summary>
public record EmergencyExitResult(
    bool Success,
    string Method,
    DateTimeOffset TimestampUtc,
    string Message,
    int? RemainingCooldownSeconds = null);
