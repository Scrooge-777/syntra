using Syntra.Core.Entities;
using Syntra.Core.Ports;

namespace Syntra.Core.Security;

/// <summary>
/// Cryptographically verified emergency exit hatch providing safety and recovery paths
/// from full-screen lockdown while preventing impulsive bypasses.
/// Supports immediate unlock via pre-configured passphrase or an enforced 3-minute cooldown.
/// </summary>
public class EmergencyExitHatch
{
    public const int DefaultCooldownSeconds = 180; // 3 minutes

    private readonly IClock _clock;
    private readonly int _cooldownSeconds;
    private DateTimeOffset? _cooldownInitiatedAtUtc;

    public bool IsCooldownActive => _cooldownInitiatedAtUtc.HasValue && !IsCooldownExpired;
    public bool IsCooldownExpired => _cooldownInitiatedAtUtc.HasValue && 
        (_clock.UtcNow - _cooldownInitiatedAtUtc.Value).TotalSeconds >= _cooldownSeconds;

    public EmergencyExitHatch(IClock? clock = null, int cooldownSeconds = DefaultCooldownSeconds)
    {
        _clock = clock ?? new SystemClock();
        _cooldownSeconds = Math.Max(10, cooldownSeconds); // Minimum 10 seconds for testing
    }

    /// <summary>
    /// Attempts immediate emergency unlock via user-configured passphrase.
    /// </summary>
    public EmergencyExitResult AttemptPassphraseUnlock(StudyProfile profile, string candidatePassphrase)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var now = _clock.UtcNow;

        if (string.IsNullOrWhiteSpace(candidatePassphrase))
        {
            return new EmergencyExitResult(
                Success: false,
                Method: "Passphrase",
                TimestampUtc: now,
                Message: "Passphrase cannot be empty.");
        }

        var isMatch = profile.VerifyEmergencyPassphrase(candidatePassphrase);

        if (isMatch)
        {
            _cooldownInitiatedAtUtc = null;
            return new EmergencyExitResult(
                Success: true,
                Method: "Passphrase",
                TimestampUtc: now,
                Message: "Emergency passphrase verified. Lockdown lifted.");
        }

        return new EmergencyExitResult(
            Success: false,
            Method: "Passphrase",
            TimestampUtc: now,
            Message: "Invalid emergency passphrase. Verification failed.");
    }

    /// <summary>
    /// Initiates the mandatory 3-minute emergency cooldown timer when passphrase is unknown.
    /// </summary>
    public EmergencyExitResult InitiateCooldownUnlock()
    {
        var now = _clock.UtcNow;

        if (!_cooldownInitiatedAtUtc.HasValue)
        {
            _cooldownInitiatedAtUtc = now;
        }

        var elapsedSeconds = (int)(now - _cooldownInitiatedAtUtc.Value).TotalSeconds;
        var remainingSeconds = Math.Max(0, _cooldownSeconds - elapsedSeconds);

        if (remainingSeconds == 0)
        {
            _cooldownInitiatedAtUtc = null;
            return new EmergencyExitResult(
                Success: true,
                Method: "CooldownTimer",
                TimestampUtc: now,
                Message: "Emergency cooldown period completed. Lockdown lifted.",
                RemainingCooldownSeconds: 0);
        }

        return new EmergencyExitResult(
            Success: false,
            Method: "CooldownTimer",
            TimestampUtc: now,
            Message: $"Emergency cooldown active. Must wait {remainingSeconds} seconds before unlock.",
            RemainingCooldownSeconds: remainingSeconds);
    }

    /// <summary>
    /// Checks the remaining seconds on an active cooldown.
    /// </summary>
    public int GetRemainingCooldownSeconds()
    {
        if (!_cooldownInitiatedAtUtc.HasValue)
            return 0;

        var elapsedSeconds = (int)(_clock.UtcNow - _cooldownInitiatedAtUtc.Value).TotalSeconds;
        return Math.Max(0, _cooldownSeconds - elapsedSeconds);
    }

    /// <summary>
    /// Cancels an in-progress cooldown if the student decides to resume studying.
    /// </summary>
    public void CancelCooldown()
    {
        _cooldownInitiatedAtUtc = null;
    }
}
