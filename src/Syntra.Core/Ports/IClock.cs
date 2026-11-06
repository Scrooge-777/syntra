namespace Syntra.Core.Ports;

/// <summary>
/// Port for abstracting system time, enabling 100% deterministic unit testing and simulation.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

/// <summary>
/// Default system wall-clock provider.
/// </summary>
public class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
