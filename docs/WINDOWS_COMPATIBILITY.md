# SYNTRA — Windows 11 Compatibility & Display Matrix

**Target Operating System:** Windows 11 (Home / Pro / Enterprise)  
**Primary Verification Edition:** Windows 11 Home (64-bit)  

---

## 1. Windows OS Version Compatibility Matrix

| OS Version | Build Range | Architecture | Support Tier | Technical Notes |
|---|---|:---:|:---:|---|
| **Windows 11 24H2** | Build 26100+ | x64 / ARM64 | **Tier 1 (Target)** | Fully tested; supports native ARM64 compilation for Snapdragon X Elite devices. |
| **Windows 11 23H2** | Build 22631 | x64 / ARM64 | **Tier 1 (Target)** | Standard modern desktop baseline. |
| **Windows 11 22H2** | Build 22621 | x64 / ARM64 | **Tier 1 (Target)** | Minimum supported Windows App SDK baseline. |
| **Windows 10 22H2** | Build 19045 | x64 | **Tier 2 (Compatible)** | Functional, but visual presentation falls back from Mica Alt to Acrylic. |
| **Windows 11 SE / S Mode**| Build 22621+ | x64 / ARM64 | ❌ **Unsupported** | S Mode blocks Win32 hook registration (`SetWindowsHookEx`) outside packaged store apps. |

---

## 2. Windows App SDK Runtime Deployment

SYNTRA targets **Windows App SDK 1.5+**:
- **Self-Contained Deployment (`--self-contained true`):** Bundles the .NET 8 runtime and Windows App SDK binaries with the application. Recommended for initial private testing to eliminate prerequisite installer friction.
- **Framework-Dependent Deployment:** Leverages machine-wide Windows App SDK runtime packages installed via the official bootstrapper (`WindowsAppRuntimeInstall.exe`).

---

## 3. High-DPI & Per-Monitor Awareness

Desktop study sessions frequently occur on external 4K monitors paired with 1080p or 1440p laptop displays.

### Per-Monitor v2 Configuration
SYNTRA's application manifest specifies:
```xml
<application xmlns="urn:schemas-microsoft-com:asm.v3">
  <windowsSettings>
    <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true/pm</dpiAware>
    <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2</dpiAwareness>
  </windowsSettings>
</application>
```
- **Dynamic DPI Scaling:** Automatically handles `WM_DPICHANGED` messages without bitmap blurring or UI element misalignments.
- **Topmost Window Sizing:** When creating fullscreen overlay windows across multiple monitors, SYNTRA queries display coordinates using `GetMonitorInfo` with `MONITORINFOF_PRIMARY` to ensure exact pixel-boundary coverage.

---

## 4. Multi-Monitor Display Topology

On systems with multiple physical monitors:
1. **Primary Display:** Hosts the interactive study surface (reading view and assessment quiz runner).
2. **Secondary Displays:** During an active study session, secondary monitors are covered by dark, borderless companion overlay windows displaying a subtle ambient focus indicator. This eliminates peripheral video or chat distractions without disconnecting secondary monitors.
3. **Display Topology Changes:** Registered with `WM_DISPLAYCHANGE` to dynamically re-adjust overlays if a monitor is plugged in or disconnected mid-session.

---

## 5. Windows Virtual Desktops

In Windows 11, users can create multiple virtual desktops.
- **Interaction with Hooks:** Low-level keyboard hooks (`WH_KEYBOARD_LL`) suppress `Ctrl + Win + Left/Right` (the shortcut for cycling virtual desktops), ensuring the user cannot switch to a clean desktop to bypass study restrictions.
- **Topmost Behavior:** Extended style `WS_EX_TOPMOST` window ensures visibility across virtual desktop boundaries.
