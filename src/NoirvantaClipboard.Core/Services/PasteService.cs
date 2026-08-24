using System.Runtime.InteropServices;

namespace NoirvantaClipboard.Core.Services;

/// <summary>
/// Handles pasting content to clipboard and active window
/// </summary>
public class PasteService
{
    [DllImport("user32.dll")]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll")]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll")]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll")]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll")]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private const uint CF_UNICODETEXT = 13;
    private const uint GMEM_MOVEABLE = 0x0002;
    private const byte VK_CONTROL = 0x11;
    private const byte VK_V = 0x56;
    private const uint KEYEVENTF_KEYUP = 2;

    /// <summary>
    /// Copy text to Windows clipboard
    /// </summary>
    public static void CopyToClipboard(string text)
    {
        try
        {
            if (!OpenClipboard(IntPtr.Zero))
                return;

            try
            {
                EmptyClipboard();

                IntPtr hGlobal = GlobalAlloc(GMEM_MOVEABLE, new UIntPtr((uint)((text.Length + 1) * 2)));
                if (hGlobal == IntPtr.Zero)
                    return;

                IntPtr lpwcstr = GlobalLock(hGlobal);
                if (lpwcstr == IntPtr.Zero)
                    return;

                try
                {
                    Marshal.StringToHGlobalUni(text);
                    Marshal.Copy(text.ToCharArray(), 0, lpwcstr, text.Length);
                    Marshal.WriteInt16(lpwcstr, text.Length * 2, 0);
                }
                finally
                {
                    GlobalUnlock(hGlobal);
                }

                SetClipboardData(CF_UNICODETEXT, hGlobal);
            }
            finally
            {
                CloseClipboard();
            }
        }
        catch
        {
            // Silent fail
        }
    }

    /// <summary>
    /// Simulate Ctrl+V paste in the active application
    /// </summary>
    public static void SimulatePaste()
    {
        try
        {
            // Key down
            keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            keybd_event(VK_V, 0, 0, UIntPtr.Zero);

            // Key up
            keybd_event(VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
        catch
        {
            // Silent fail
        }
    }
}