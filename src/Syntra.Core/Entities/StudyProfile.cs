using System.Security.Cryptography;
using System.Text;
using Syntra.Core.Common;

namespace Syntra.Core.Entities;

/// <summary>
/// User study profile maintaining study goals, passing thresholds, and emergency security settings.
/// </summary>
public class StudyProfile
{
    public Guid Id { get; }
    public string UserName { get; private set; }
    public int DailyGoalMinutes { get; private set; }
    public string EmergencyPassphraseHash { get; private set; }
    public double MinimumPassRatio { get; private set; }
    public int MaxAssessmentAttempts { get; private set; }
    public bool AutoLockdownEnabled { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public StudyProfile(
        Guid id,
        string userName,
        int dailyGoalMinutes,
        string emergencyPassphrase,
        double minimumPassRatio = 0.80,
        int maxAssessmentAttempts = 3,
        bool autoLockdownEnabled = true,
        DateTimeOffset? createdAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new DomainException("UserName cannot be empty.");
        if (dailyGoalMinutes <= 0)
            throw new DomainException("Daily goal must be greater than zero minutes.");
        if (string.IsNullOrWhiteSpace(emergencyPassphrase))
            throw new DomainException("Emergency passphrase must be defined.");
        if (minimumPassRatio is < 0.50 or > 1.00)
            throw new DomainException("Minimum pass ratio must be between 0.50 (50%) and 1.00 (100%).");
        if (maxAssessmentAttempts < 1)
            throw new DomainException("Max assessment attempts must be at least 1.");

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        UserName = userName.Trim();
        DailyGoalMinutes = dailyGoalMinutes;
        EmergencyPassphraseHash = HashPassphrase(emergencyPassphrase);
        MinimumPassRatio = minimumPassRatio;
        MaxAssessmentAttempts = maxAssessmentAttempts;
        AutoLockdownEnabled = autoLockdownEnabled;
        CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public void UpdateDailyGoal(int minutes, DateTimeOffset nowUtc)
    {
        if (minutes <= 0)
            throw new DomainException("Daily goal must be greater than zero minutes.");

        DailyGoalMinutes = minutes;
        UpdatedAtUtc = nowUtc;
    }

    public void SetEmergencyPassphrase(string newPassphrase, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(newPassphrase))
            throw new DomainException("Emergency passphrase cannot be empty.");

        EmergencyPassphraseHash = HashPassphrase(newPassphrase);
        UpdatedAtUtc = nowUtc;
    }

    public bool VerifyEmergencyPassphrase(string candidatePassphrase)
    {
        if (string.IsNullOrWhiteSpace(candidatePassphrase))
            return false;

        var candidateHash = HashPassphrase(candidatePassphrase);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(EmergencyPassphraseHash),
            Encoding.UTF8.GetBytes(candidateHash));
    }

    public void SetMinimumPassRatio(double ratio, DateTimeOffset nowUtc)
    {
        if (ratio is < 0.50 or > 1.00)
            throw new DomainException("Minimum pass ratio must be between 0.50 and 1.00.");

        MinimumPassRatio = ratio;
        UpdatedAtUtc = nowUtc;
    }

    public void SetAutoLockdown(bool enabled, DateTimeOffset nowUtc)
    {
        AutoLockdownEnabled = enabled;
        UpdatedAtUtc = nowUtc;
    }

    public static string HashPassphrase(string passphrase)
    {
        var normalized = passphrase.Trim().ToLowerInvariant();
        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
