using Microsoft.Win32;

namespace Syntra.Windows.Policies;

/// <summary>
/// Manages Windows user policies via HKCU registry to suppress Task Manager
/// and Lock Workstation (Win+L) during active study sessions.
/// </summary>
public class RegistryPolicyGuard : IDisposable
{
    private const string PolicySubKey = @"Software\Microsoft\Windows\CurrentVersion\Policies\System";
    private const string DisableTaskMgrKey = "DisableTaskMgr";
    private const string DisableLockWorkstationKey = "DisableLockWorkstation";

    private bool _policiesEngaged;

    public bool ArePoliciesEngaged => _policiesEngaged;

    public void EnableRestrictions()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(PolicySubKey, writable: true);
            if (key != null)
            {
                key.SetValue(DisableTaskMgrKey, 1, RegistryValueKind.DWord);
                key.SetValue(DisableLockWorkstationKey, 1, RegistryValueKind.DWord);
                _policiesEngaged = true;
            }
        }
        catch
        {
            // Fail safely if user permissions restrict registry manipulation
        }
    }

    public void DisableRestrictions()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PolicySubKey, writable: true);
            if (key != null)
            {
                key.SetValue(DisableTaskMgrKey, 0, RegistryValueKind.DWord);
                key.SetValue(DisableLockWorkstationKey, 0, RegistryValueKind.DWord);
                _policiesEngaged = false;
            }
        }
        catch
        {
            // Ignore cleanup failures
        }
    }

    public void Dispose()
    {
        if (_policiesEngaged)
        {
            DisableRestrictions();
        }
        GC.SuppressFinalize(this);
    }
}
