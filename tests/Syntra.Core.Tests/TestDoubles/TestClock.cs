using Syntra.Core.Ports;

namespace Syntra.Core.Tests.TestDoubles;

/// <summary>
/// Deterministic clock test double allowing programmatic time advances.
/// </summary>
public class TestClock : IClock
{
    public DateTimeOffset UtcNow { get; private set; }

    public TestClock(DateTimeOffset? initialTime = null)
    {
        UtcNow = initialTime ?? new DateTimeOffset(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);
    }

    public void Advance(TimeSpan delta)
    {
        UtcNow = UtcNow.Add(delta);
    }

    public void AdvanceSeconds(int seconds)
    {
        Advance(TimeSpan.FromSeconds(seconds));
    }

    public void AdvanceMinutes(int minutes)
    {
        Advance(TimeSpan.FromMinutes(minutes));
    }
}
