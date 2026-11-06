using Syntra.Core.Ports;
using Syntra.Windows.Geometry;
using Syntra.Windows.Hooks;
using Syntra.Windows.Policies;

namespace Syntra.Windows.Services;

/// <summary>
/// Implementation of IWindowsEnforcementService managing native Win32 keyboard hooks,
/// registry restriction policies, and fullscreen topmost focus preservation.
/// </summary>
public class WindowsEnforcementService : IWindowsEnforcementService, IDisposable
{
    private readonly LowLevelKeyboardHook _hook;
    private readonly RegistryPolicyGuard _policyGuard;
    private readonly HashSet<string> _whitelistedProcesses = new(StringComparer.OrdinalIgnoreCase);
    private bool _isLockdownActive;

    public bool IsLockdownActive => _isLockdownActive;

    public event Action? EmergencyExitTriggered;
    public event Action<string>? DistractionInterventionRecorded;

    public WindowsEnforcementService(
        LowLevelKeyboardHook? hook = null,
        RegistryPolicyGuard? policyGuard = null)
    {
        _hook = hook ?? new LowLevelKeyboardHook();
        _policyGuard = policyGuard ?? new RegistryPolicyGuard();

        _hook.EmergencyExitChordDetected += () => EmergencyExitTriggered?.Invoke();
        _hook.InterceptionTriggered += action => DistractionInterventionRecorded?.Invoke(action);
    }

    public Task EngageLockdownAsync(CancellationToken cancellationToken = default)
    {
        if (_isLockdownActive) return Task.CompletedTask;

        _hook.Install();
        _policyGuard.EnableRestrictions();
        _isLockdownActive = true;

        return Task.CompletedTask;
    }

    public Task ReleaseLockdownAsync(CancellationToken cancellationToken = default)
    {
        if (!_isLockdownActive) return Task.CompletedTask;

        _hook.Uninstall();
        _policyGuard.DisableRestrictions();
        _isLockdownActive = false;

        return Task.CompletedTask;
    }

    public Task AllowProcessWhitelistAsync(IEnumerable<string> processNames, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processNames);
        foreach (var p in processNames)
        {
            _whitelistedProcesses.Add(p.Trim());
        }
        return Task.CompletedTask;
    }

    public Task AssertTopmostFocusAsync(CancellationToken cancellationToken = default)
    {
        // Reassert virtual screen geometry if needed
        var bounds = VirtualScreenBounds.GetVirtualScreenBounds();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        ReleaseLockdownAsync().GetAwaiter().GetResult();
        _hook.Dispose();
        _policyGuard.Dispose();
        GC.SuppressFinalize(this);
    }
}
