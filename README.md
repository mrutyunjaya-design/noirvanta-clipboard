# Noirvanta Clipboard

A native Windows clipboard manager built with C# + WPF + .NET 10 + SQLite.

**Status**: Core development in progress

## Features (Planned)

- 🖱️ **Global Hotkeys**: Ctrl+C to copy, Ctrl+Shift+V to select and paste
- 💾 **Persistent Storage**: SQLite-based clipboard history
- 📌 **Pin Important Items**: Mark frequently used clipboard entries
- 🎯 **Quick Selection**: Keyboard/mouse-friendly clipboard selector UI
- 🗑️ **Management**: Delete individual entries or clear entire history
- ⚡ **Lightweight**: Minimal resource footprint

## Architecture

```
NoirvantaClipboard/
├── src/
│   ├── NoirvantaClipboard.Core/
│   │   ├── Models/
│   │   ├── Database/
│   │   └── Services/
│   │       ├── ClipboardMonitor.cs  (Windows clipboard monitoring)
│   │       └── PasteService.cs      (Clipboard write & paste simulation)
│   └── NoirvantaClipboard.Wpf/
│       ├── MainWindow.xaml          (Clipboard selector UI)
│       └── HotKeyManager.cs         (Global hotkey registration)
```

## Core Workflow

1. **Copy** (Ctrl+C): ClipboardMonitor listens for clipboard changes via Windows API
2. **Save**: Entry stored in SQLite database with timestamp
3. **Select** (Ctrl+Shift+V): ClipboardSelector UI appears
4. **Paste** (Enter): Selected entry copied to clipboard & pasted via Ctrl+V simulation

## Technology Stack

- **Runtime**: .NET 10
- **UI**: WPF (Windows Presentation Foundation)
- **Database**: SQLite with System.Data.SQLite
- **Interop**: P/Invoke for Windows API (clipboard, hotkeys, window management)

## Building

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run application
dotnet run --project src/NoirvantaClipboard.Wpf
```

## Development

Default branch: `main`
Development branch: `develop`

For features and fixes, create a branch from `develop`.

## License

MIT
