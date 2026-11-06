using Syntra.Windows.Geometry;
using Syntra.Windows.Hooks;
using Syntra.Windows.Policies;
using Syntra.Windows.Services;
using Xunit;

namespace Syntra.Windows.Tests;

public class WindowsEnforcementTests
{
    [Fact]
    public void VirtualScreenBounds_ShouldReturnNonZeroCoordinates()
    {
        var bounds = VirtualScreenBounds.GetVirtualScreenBounds();

        Assert.True(bounds.Width > 0);
        Assert.True(bounds.Height > 0);
    }

    [Fact]
    public async Task WindowsEnforcementService_ShouldManageLockdownState()
    {
        using var hook = new LowLevelKeyboardHook();
        using var policy = new RegistryPolicyGuard();
        using var service = new WindowsEnforcementService(hook, policy);

        Assert.False(service.IsLockdownActive);

        await service.EngageLockdownAsync();
        Assert.True(service.IsLockdownActive);

        await service.ReleaseLockdownAsync();
        Assert.False(service.IsLockdownActive);
    }

    [Fact]
    public async Task ProcessWhitelist_ShouldAllowRegisteredProcesses()
    {
        using var service = new WindowsEnforcementService();

        var exception = await Record.ExceptionAsync(() => 
            service.AllowProcessWhitelistAsync(new[] { "code.exe", "devenv.exe", "wt.exe" }));

        Assert.Null(exception);
    }
}
