using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;

namespace NoirvantaClipboard.Wpf
{
    /// <summary>
    /// Manages global hotkeys for the application
    /// </summary>
    public class HotKeyManager : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const uint MOD_CONTROL = 2;
        private const uint MOD_SHIFT = 4;
        private const uint MOD_ALT = 1;
        private const uint MOD_WIN = 8;

        private readonly IntPtr _windowHandle;
        private readonly Dictionary<int, Action> _hotkeys = new();
        private int _hotKeyId = 0;
        private System.Windows.Interop.HwndSource _source;

        public HotKeyManager(Window window)
        {
            _windowHandle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
            
            if (_windowHandle != IntPtr.Zero)
            {
                _source = System.Windows.Interop.HwndSource.FromHwnd(_windowHandle);
                _source?.AddHook(HwndHook);
            }
        }

        public void RegisterHotKey(ModifierKeys modifiers, Key key, Action callback)
        {
            uint mod = 0;
            if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                mod |= MOD_CONTROL;
            if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                mod |= MOD_SHIFT;
            if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
                mod |= MOD_ALT;
            if ((modifiers & ModifierKeys.Windows) == ModifierKeys.Windows)
                mod |= MOD_WIN;

            uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
            int id = ++_hotKeyId;

            if (RegisterHotKey(_windowHandle, id, mod, vk))
            {
                _hotkeys[id] = callback;
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (_hotkeys.TryGetValue(id, out var callback))
                {
                    callback?.Invoke();
                    handled = true;
                }
            }

            return IntPtr.Zero;
        }

        public void Dispose()
        {
            foreach (var id in _hotkeys.Keys)
            {
                UnregisterHotKey(_windowHandle, id);
            }
            _source?.RemoveHook(HwndHook);
        }
    }
}