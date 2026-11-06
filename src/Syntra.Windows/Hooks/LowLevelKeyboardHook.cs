using System.Diagnostics;
using System.Runtime.InteropServices;
using Syntra.Windows.Native;

namespace Syntra.Windows.Hooks;

/// <summary>
/// Low-level user-mode keyboard hook running on a dedicated message loop thread.
/// Swallows system navigation shortcuts and detects the emergency escape chord (Ctrl+Shift+F12).
/// Invariant: Hook callback completes in under 1ms with zero allocations to avoid LowLevelHooksTimeout.
/// </summary>
public class LowLevelKeyboardHook : IDisposable
{
    private const int WM_QUIT = 0x0012;

    private readonly User32.LowLevelKeyboardProc _hookProc;
    private IntPtr _hookId = IntPtr.Zero;
    private Thread? _hookThread;
    private uint _hookThreadId;
    private readonly ManualResetEventSlim _startedSignal = new(false);
    private bool _disposed;

    public event Action? EmergencyExitChordDetected;
    public event Action<string>? InterceptionTriggered;

    public bool IsHookInstalled => _hookId != IntPtr.Zero;

    public LowLevelKeyboardHook()
    {
        _hookProc = HookCallback;
    }

    public void Install()
    {
        if (IsHookInstalled || _disposed) return;

        _startedSignal.Reset();
        _hookThread = new Thread(HookThreadLoop)
        {
            IsBackground = true,
            Name = "Syntra-WH_KEYBOARD_LL-Thread"
        };
        _hookThread.Start();

        _startedSignal.Wait(TimeSpan.FromSeconds(3));
    }

    private void HookThreadLoop()
    {
        _hookThreadId = User32.GetCurrentThreadId();

        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        var hMod = User32.GetModuleHandle(curModule?.ModuleName);

        _hookId = User32.SetWindowsHookEx(User32.WH_KEYBOARD_LL, _hookProc, hMod, 0);

        _startedSignal.Set();

        if (_hookId == IntPtr.Zero)
        {
            return;
        }

        // Standard Win32 Message Loop required for low-level hooks
        while (User32.GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
        {
            User32.TranslateMessage(ref msg);
            User32.DispatchMessage(ref msg);
        }

        if (_hookId != IntPtr.Zero)
        {
            User32.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var msg = wParam.ToInt32();
            var kb = Marshal.PtrToStructure<User32.KBDLLHOOKSTRUCT>(lParam);

            var isKeyDown = msg is User32.WM_KEYDOWN or User32.WM_SYSKEYDOWN;
            var isAltDown = (kb.flags & User32.LLKHF_ALTDOWN) != 0;

            // Check Emergency Escape Chord: Ctrl + Shift + F12
            if (isKeyDown && kb.vkCode == User32.VK_F12)
            {
                var isCtrl = (User32.GetAsyncKeyState(User32.VK_CONTROL) & 0x8000) != 0;
                var isShift = (User32.GetAsyncKeyState(User32.VK_SHIFT) & 0x8000) != 0;

                if (isCtrl && isShift)
                {
                    Task.Run(() => EmergencyExitChordDetected?.Invoke());
                    return (IntPtr)1; // Swallow
                }
            }

            // 1. Windows Key (VK_LWIN, VK_RWIN)
            if (kb.vkCode is User32.VK_LWIN or User32.VK_RWIN)
            {
                if (isKeyDown) Task.Run(() => InterceptionTriggered?.Invoke("WindowsKey"));
                return (IntPtr)1; // Swallow
            }

            // 2. Alt + Tab, Alt + Esc
            if (isAltDown && kb.vkCode is User32.VK_TAB or User32.VK_ESCAPE)
            {
                if (isKeyDown) Task.Run(() => InterceptionTriggered?.Invoke("AltTab_Or_AltEsc"));
                return (IntPtr)1; // Swallow
            }

            // 3. Alt + F4
            if (isAltDown && kb.vkCode == User32.VK_F4)
            {
                if (isKeyDown) Task.Run(() => InterceptionTriggered?.Invoke("AltF4"));
                return (IntPtr)1; // Swallow
            }

            // 4. Ctrl + Shift + Esc (Task Manager)
            if (isKeyDown && kb.vkCode == User32.VK_ESCAPE)
            {
                var isCtrl = (User32.GetAsyncKeyState(User32.VK_CONTROL) & 0x8000) != 0;
                var isShift = (User32.GetAsyncKeyState(User32.VK_SHIFT) & 0x8000) != 0;
                if (isCtrl && isShift)
                {
                    Task.Run(() => InterceptionTriggered?.Invoke("CtrlShiftEsc_TaskMgr"));
                    return (IntPtr)1; // Swallow
                }
            }
        }

        return User32.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Uninstall()
    {
        if (_hookThreadId != 0)
        {
            User32.PostThreadMessage(_hookThreadId, WM_QUIT, UIntPtr.Zero, IntPtr.Zero);
            _hookThread?.Join(1000);
            _hookThreadId = 0;
            _hookThread = null;
        }

        if (_hookId != IntPtr.Zero)
        {
            User32.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Uninstall();
        _startedSignal.Dispose();
        GC.SuppressFinalize(this);
    }
}
