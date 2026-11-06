using Syntra.Core.Entities;
using Syntra.Core.Security;
using Syntra.Core.Tests.TestDoubles;
using Xunit;

namespace Syntra.Core.Tests;

public class EmergencyExitHatchTests
{
    private readonly TestClock _clock;
    private readonly EmergencyExitHatch _sut;
    private readonly StudyProfile _profile;

    public EmergencyExitHatchTests()
    {
        _clock = new TestClock();
        _sut = new EmergencyExitHatch(_clock, cooldownSeconds: 180);
        _profile = new StudyProfile(
            Guid.NewGuid(),
            "Sreeharan",
            dailyGoalMinutes: 120,
            emergencyPassphrase: "I promise to return to my focused work after this crisis");
    }

    [Fact]
    public void AttemptPassphraseUnlock_WithCorrectPassphrase_ShouldUnlockImmediately()
    {
        var result = _sut.AttemptPassphraseUnlock(_profile, "I promise to return to my focused work after this crisis");

        Assert.True(result.Success);
        Assert.Equal("Passphrase", result.Method);
        Assert.Contains("Lockdown lifted", result.Message);
        Assert.False(_sut.IsCooldownActive);
    }

    [Fact]
    public void AttemptPassphraseUnlock_WithIncorrectPassphrase_ShouldFail()
    {
        var result = _sut.AttemptPassphraseUnlock(_profile, "wrong passphrase");

        Assert.False(result.Success);
        Assert.Equal("Passphrase", result.Method);
        Assert.Contains("Invalid emergency passphrase", result.Message);
    }

    [Fact]
    public void CooldownUnlock_ShouldRequireFull180Seconds()
    {
        // Initiate cooldown
        var initial = _sut.InitiateCooldownUnlock();
        Assert.False(initial.Success);
        Assert.True(_sut.IsCooldownActive);
        Assert.Equal(180, initial.RemainingCooldownSeconds);

        // Advance 100 seconds (still 80 seconds remaining)
        _clock.AdvanceSeconds(100);
        var midway = _sut.InitiateCooldownUnlock();
        Assert.False(midway.Success);
        Assert.Equal(80, midway.RemainingCooldownSeconds);

        // Advance remaining 80 seconds (total 180s)
        _clock.AdvanceSeconds(80);
        var completed = _sut.InitiateCooldownUnlock();
        Assert.True(completed.Success);
        Assert.Equal("CooldownTimer", completed.Method);
        Assert.Equal(0, completed.RemainingCooldownSeconds);
        Assert.False(_sut.IsCooldownActive);
    }

    [Fact]
    public void CancelCooldown_ShouldResetActiveCooldown()
    {
        _sut.InitiateCooldownUnlock();
        Assert.True(_sut.IsCooldownActive);

        _sut.CancelCooldown();
        Assert.False(_sut.IsCooldownActive);
        Assert.Equal(0, _sut.GetRemainingCooldownSeconds());
    }
}
