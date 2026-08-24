using System.Runtime.InteropServices;
using NoirvantaClipboard.Core.Models;

namespace NoirvantaClipboard.Core.Services;

/// <summary>
/// Monitors Windows clipboard for changes and saves entries
/// </summary>
public class ClipboardMonitor : IDisposable
{
    private readonly ClipboardDatabase _database;
    private IntPtr _windowHandle = IntPtr.Zero;
    private IntPtr _nextClipboardViewer = IntPtr.Zero;

    public event EventHandler<ClipboardEntry>? OnClipboardEntryAdded;

    [DllImport("user32.dll")]
    private static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

    [DllImport("user32.dll")]
    private static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

    [DllImport("user32.dll")]
    private static extern bool IsClipboardFormatAvailable(uint format);

    [DllImport("user32.dll")]
    private static extern IntPtr GetClipboardOwner();

    [DllImport("user32.dll")]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll")]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll")]
    private static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll")]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll")]
    private static extern uint GlobalSize(IntPtr hMem);

    private const uint CF_TEXT = 1;
    private const uint CF_UNICODETEXT = 13;

    public ClipboardMonitor(ClipboardDatabase database)
    {
        _database = database;
    }

    /// <summary>
    /// Start monitoring clipboard with a window handle
    /// </summary>
    public void StartMonitoring(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
        _nextClipboardViewer = SetClipboardViewer(_windowHandle);
    }

    /// <summary>
    /// Stop monitoring clipboard
    /// </summary>
    public void StopMonitoring()
    {
        if (_windowHandle != IntPtr.Zero && _nextClipboardViewer != IntPtr.Zero)
        {
            ChangeClipboardChain(_windowHandle, _nextClipboardViewer);
        }
    }

    /// <summary>
    /// Handle clipboard change notification from Windows message
    /// </summary>
    public async Task HandleClipboardChangeAsync()
    {
        try
        {
            var content = GetClipboardText();
            if (!string.IsNullOrWhiteSpace(content))
            {
                var entry = new ClipboardEntry
                {
                    Content = content,
                    Type = ClipboardEntryType.Text
                };

                await _database.SaveEntryAsync(entry);
                OnClipboardEntryAdded?.Invoke(this, entry);
            }
        }
        catch (Exception ex)
        {
            // Log error silently
            System.Diagnostics.Debug.WriteLine($"Clipboard monitor error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get text from clipboard
    /// </summary>
    private string GetClipboardText()
    {
        try
        {
            if (!OpenClipboard(IntPtr.Zero))
                return string.Empty;

            try
            {
                if (IsClipboardFormatAvailable(CF_UNICODETEXT))
                {
                    IntPtr hGlobal = GetClipboardData(CF_UNICODETEXT);
                    if (hGlobal == IntPtr.Zero)
                        return string.Empty;

                    IntPtr lpwcstr = GlobalLock(hGlobal);
                    if (lpwcstr == IntPtr.Zero)
                        return string.Empty;

                    try
                    {
                        return Marshal.PtrToStringUni(lpwcstr) ?? string.Empty;
                    }
                    finally
                    {
                        GlobalUnlock(hGlobal);
                    }
                }
                else if (IsClipboardFormatAvailable(CF_TEXT))
                {
                    IntPtr hGlobal = GetClipboardData(CF_TEXT);
                    if (hGlobal == IntPtr.Zero)
                        return string.Empty;

                    IntPtr lpstr = GlobalLock(hGlobal);
                    if (lpstr == IntPtr.Zero)
                        return string.Empty;

                    try
                    {
                        return Marshal.PtrToStringAnsi(lpstr) ?? string.Empty;
                    }
                    finally
                    {
                        GlobalUnlock(hGlobal);
                    }
                }
            }
            finally
            {
                CloseClipboard();
            }
        }
        catch
        {
            return string.Empty;
        }

        return string.Empty;
    }

    public void Dispose()
    {
        StopMonitoring();
    }
}