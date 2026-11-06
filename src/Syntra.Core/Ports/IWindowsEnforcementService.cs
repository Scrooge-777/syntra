namespace Syntra.Core.Ports;

/// <summary>
/// Port contract for platform-specific desktop lockdown and input restriction infrastructure.
/// </summary>
public interface IWindowsEnforcementService
{
    bool IsLockdownActive { get; }

    Task EngageLockdownAsync(CancellationToken cancellationToken = default);

    Task ReleaseLockdownAsync(CancellationToken cancellationToken = default);

    Task AllowProcessWhitelistAsync(IEnumerable<string> processNames, CancellationToken cancellationToken = default);

    Task AssertTopmostFocusAsync(CancellationToken cancellationToken = default);
}
