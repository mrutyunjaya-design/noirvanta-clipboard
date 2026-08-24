using System.Windows;
using System.Runtime.InteropServices;
using NoirvantaClipboard.Core.Database;
using NoirvantaClipboard.Core.Services;
using NoirvantaClipboard.Core.Models;

namespace NoirvantaClipboard.Wpf
{
    public partial class MainWindow : Window
    {
        private ClipboardDatabase _database;
        private ClipboardMonitor _monitor;
        private PasteService _pasteService;
        private HotKeyManager _hotKeyManager;

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private const int WM_CHANGECBCHAIN = 0x030D;
        private const int WM_DRAWCLIPBOARD = 0x0308;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize database
            _database = new ClipboardDatabase();
            await _database.InitializeAsync();

            // Initialize services
            _monitor = new ClipboardMonitor(_database);
            _pasteService = new PasteService();
            _hotKeyManager = new HotKeyManager(this);

            // Start clipboard monitoring
            var windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            _monitor.StartMonitoring(windowHandle);
            _monitor.OnClipboardEntryAdded += Monitor_OnClipboardEntryAdded;

            // Register hotkeys
            // Ctrl+Shift+V to show selector
            _hotKeyManager.RegisterHotKey(ModifierKeys.Control | ModifierKeys.Shift, System.Windows.Input.Key.V, ShowClipboardSelector);

            // Load initial entries
            await RefreshEntriesAsync();

            // Hide to system tray
            Hide();
        }

        private async void Monitor_OnClipboardEntryAdded(object sender, ClipboardEntry entry)
        {
            Dispatcher.Invoke(async () =>
            {
                await RefreshEntriesAsync();
                StatusText.Text = $"Saved: {entry.Content.Substring(0, Math.Min(30, entry.Content.Length))}...";
            });
        }

        private async Task RefreshEntriesAsync()
        {
            var entries = await _database.GetEntriesAsync(100);
            ClipboardListBox.ItemsSource = entries;
        }

        private void ShowClipboardSelector()
        {
            Dispatcher.Invoke(() =>
            {
                this.Show();
                this.Activate();
                this.Focus();
                ClipboardListBox.Focus();
            });
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int id)
            {
                await _database.DeleteEntryAsync(id);
                await RefreshEntriesAsync();
            }
        }

        private async void PinButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int id)
            {
                await _database.TogglePinAsync(id);
                await RefreshEntriesAsync();
            }
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear all entries
            MessageBoxResult result = MessageBox.Show(
                "Clear entire clipboard history?",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                var entries = await _database.GetEntriesAsync(1000);
                foreach (var entry in entries)
                {
                    await _database.DeleteEntryAsync(entry.Id);
                }
                await RefreshEntriesAsync();
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            var source = System.Windows.Interop.HwndSource.FromHwnd(hwnd);
            source?.AddHook(HwndHook);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_DRAWCLIPBOARD)
            {
                _monitor?.HandleClipboardChangeAsync();
            }
            return IntPtr.Zero;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        protected override void OnClosed(EventArgs e)
        {
            _monitor?.Dispose();
            _database?.Dispose();
            _hotKeyManager?.Dispose();
            base.OnClosed(e);
        }
    }
}