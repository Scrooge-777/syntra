using Syntra.Core.Ports;

namespace Syntra.App.Tests.TestDoubles;

public class TestClock : IClock
{
    public DateTimeOffset UtcNow { get; private set; }

    public TestClock(DateTimeOffset? initial = null)
    {
        UtcNow = initial ?? new DateTimeOffset(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);
    }

    public void AdvanceMinutes(int minutes)
    {
        UtcNow = UtcNow.AddMinutes(minutes);
    }

    public void AdvanceSeconds(int seconds)
    {
        UtcNow = UtcNow.AddSeconds(seconds);
    }
}
