using Syntra.Windows.Native;

namespace Syntra.Windows.Geometry;

public record VirtualScreenRect(int X, int Y, int Width, int Height);

/// <summary>
/// Computes multi-monitor virtual desktop coordinates to guarantee complete screen occlusion across all monitors.
/// </summary>
public static class VirtualScreenBounds
{
    public static VirtualScreenRect GetVirtualScreenBounds()
    {
        var x = User32.GetSystemMetrics(User32.SM_XVIRTUALSCREEN);
        var y = User32.GetSystemMetrics(User32.SM_YVIRTUALSCREEN);
        var width = User32.GetSystemMetrics(User32.SM_CXVIRTUALSCREEN);
        var height = User32.GetSystemMetrics(User32.SM_CYVIRTUALSCREEN);

        // Fallback if virtual metrics return zero (single monitor)
        if (width <= 0) width = 1920;
        if (height <= 0) height = 1080;

        return new VirtualScreenRect(x, y, width, height);
    }
}
