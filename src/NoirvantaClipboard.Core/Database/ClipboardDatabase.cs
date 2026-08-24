using System.Data.SQLite;
using NoirvantaClipboard.Core.Models;

namespace NoirvantaClipboard.Core.Database;

/// <summary>
/// Manages SQLite database for clipboard entries
/// </summary>
public class ClipboardDatabase : IDisposable
{
    private readonly string _connectionString;
    private SQLiteConnection? _connection;

    public ClipboardDatabase(string dbPath = null)
    {
        dbPath ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NoirvantaClipboard",
            "clipboard.db"
        );

        // Ensure directory exists
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = $"Data Source={dbPath};Version=3;";
    }

    /// <summary>
    /// Initialize database schema
    /// </summary>
    public async Task InitializeAsync()
    {
        using var connection = new SQLiteConnection(_connectionString);
        await connection.OpenAsync();

        string createTableQuery = @"
            CREATE TABLE IF NOT EXISTS ClipboardEntries (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Content TEXT NOT NULL,
                Type INTEGER NOT NULL DEFAULT 0,
                CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                IsPinned BOOLEAN NOT NULL DEFAULT 0,
                Tags TEXT
            );

            CREATE INDEX IF NOT EXISTS idx_created_at ON ClipboardEntries(CreatedAt DESC);
            CREATE INDEX IF NOT EXISTS idx_pinned ON ClipboardEntries(IsPinned DESC);
        ";

        using var command = new SQLiteCommand(createTableQuery, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Save a new clipboard entry
    /// </summary>
    public async Task<ClipboardEntry> SaveEntryAsync(ClipboardEntry entry)
    {
        using var connection = new SQLiteConnection(_connectionString);
        await connection.OpenAsync();

        string query = @"
            INSERT INTO ClipboardEntries (Content, Type, CreatedAt, IsPinned, Tags)
            VALUES (@content, @type, @createdAt, @isPinned, @tags);
            SELECT last_insert_rowid();
        ";

        using var command = new SQLiteCommand(query, connection);
        command.Parameters.AddWithValue("@content", entry.Content ?? string.Empty);
        command.Parameters.AddWithValue("@type", (int)entry.Type);
        command.Parameters.AddWithValue("@createdAt", entry.CreatedAt);
        command.Parameters.AddWithValue("@isPinned", entry.IsPinned);
        command.Parameters.AddWithValue("@tags", entry.Tags ?? string.Empty);

        var result = await command.ExecuteScalarAsync();
        entry.Id = Convert.ToInt32(result);

        return entry;
    }

    /// <summary>
    /// Get all clipboard entries, pinned first, then by most recent
    /// </summary>
    public async Task<List<ClipboardEntry>> GetEntriesAsync(int limit = 100)
    {
        using var connection = new SQLiteConnection(_connectionString);
        await connection.OpenAsync();

        string query = @"
            SELECT Id, Content, Type, CreatedAt, IsPinned, Tags
            FROM ClipboardEntries
            ORDER BY IsPinned DESC, CreatedAt DESC
            LIMIT @limit
        ";

        using var command = new SQLiteCommand(query, connection);
        command.Parameters.AddWithValue("@limit", limit);

        var entries = new List<ClipboardEntry>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            entries.Add(new ClipboardEntry
            {
                Id = reader.GetInt32(0),
                Content = reader.GetString(1),
                Type = (ClipboardEntryType)reader.GetInt32(2),
                CreatedAt = reader.GetDateTime(3),
                IsPinned = reader.GetBoolean(4),
                Tags = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }

        return entries;
    }

    /// <summary>
    /// Delete an entry by ID
    /// </summary>
    public async Task DeleteEntryAsync(int id)
    {
        using var connection = new SQLiteConnection(_connectionString);
        await connection.OpenAsync();

        string query = "DELETE FROM ClipboardEntries WHERE Id = @id";
        using var command = new SQLiteCommand(query, connection);
        command.Parameters.AddWithValue("@id", id);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Toggle pin status of an entry
    /// </summary>
    public async Task TogglePinAsync(int id)
    {
        using var connection = new SQLiteConnection(_connectionString);
        await connection.OpenAsync();

        string query = "UPDATE ClipboardEntries SET IsPinned = NOT IsPinned WHERE Id = @id";
        using var command = new SQLiteCommand(query, connection);
        command.Parameters.AddWithValue("@id", id);
        await command.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}