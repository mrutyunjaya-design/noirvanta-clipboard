namespace NoirvantaClipboard.Core.Models;

/// <summary>
/// Represents a single clipboard entry
/// </summary>
public class ClipboardEntry
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public ClipboardEntryType Type { get; set; } = ClipboardEntryType.Text;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsPinned { get; set; } = false;
    public string? Tags { get; set; }
}

public enum ClipboardEntryType
{
    Text = 0,
    Image = 1,
    File = 2
}