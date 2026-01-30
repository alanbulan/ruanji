using System.Text.Json;
using Microsoft.Data.Sqlite;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Infrastructure.Services;

/// <summary>
/// Implementation of IOperationLogger for logging operations to SQLite.
/// Implements requirements 7.1, 7.2, 7.3
/// </summary>
public class OperationLogger : IOperationLogger
{
    private readonly string _connectionString;
    private readonly int _historyDays;
    private readonly object _lock = new();
    private bool _initialized;

    // In-memory cache for active operations
    private readonly Dictionary<string, OperationRecord> _activeOperations = new();
    private readonly Dictionary<string, List<OperationAction>> _activeActions = new();

    public OperationLogger(int historyDays = 30)
    {
        _historyDays = historyDays;
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WindowsSoftwareOrganizer",
            "operations.db");
        
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _connectionString = $"Data Source={dbPath}";
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;
        
        lock (_lock)
        {
            if (_initialized) return;
            
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Operations (
                    Id TEXT PRIMARY KEY,
                    Type INTEGER NOT NULL,
                    Description TEXT NOT NULL,
                    StartTime TEXT NOT NULL,
                    EndTime TEXT,
                    Success INTEGER
                );
                
                CREATE TABLE IF NOT EXISTS OperationActions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OperationId TEXT NOT NULL,
                    ActionType TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    Timestamp TEXT NOT NULL,
                    OriginalValue TEXT,
                    NewValue TEXT,
                    CanRollback INTEGER NOT NULL,
                    FOREIGN KEY (OperationId) REFERENCES Operations(Id)
                );
                
                CREATE INDEX IF NOT EXISTS IX_Operations_StartTime ON Operations(StartTime);
                CREATE INDEX IF NOT EXISTS IX_OperationActions_OperationId ON OperationActions(OperationId);
            ";
            command.ExecuteNonQuery();
            
            _initialized = true;
        }
    }

    /// <inheritdoc />
    public async Task<string> BeginOperationAsync(OperationType type, string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be null or empty.", nameof(description));

        EnsureInitialized();

        var operationId = Guid.NewGuid().ToString("N");
        var startTime = DateTime.UtcNow;

        var record = new OperationRecord
        {
            Id = operationId,
            Type = type,
            Description = description,
            StartTime = startTime
        };

        // Cache for active operation
        lock (_lock)
        {
            _activeOperations[operationId] = record;
            _activeActions[operationId] = new List<OperationAction>();
        }

        // Persist to database
        await Task.Run(() =>
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Operations (Id, Type, Description, StartTime)
                VALUES ($id, $type, $description, $startTime)";
            command.Parameters.AddWithValue("$id", operationId);
            command.Parameters.AddWithValue("$type", (int)type);
            command.Parameters.AddWithValue("$description", description);
            command.Parameters.AddWithValue("$startTime", startTime.ToString("O"));
            command.ExecuteNonQuery();
        });

        return operationId;
    }


    /// <inheritdoc />
    public async Task LogActionAsync(string operationId, OperationAction action)
    {
        if (string.IsNullOrWhiteSpace(operationId))
            throw new ArgumentException("Operation ID cannot be null or empty.", nameof(operationId));
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        EnsureInitialized();

        // Add to cache
        lock (_lock)
        {
            if (_activeActions.TryGetValue(operationId, out var actions))
            {
                actions.Add(action);
            }
        }

        // Persist to database
        await Task.Run(() =>
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO OperationActions (OperationId, ActionType, Description, Timestamp, OriginalValue, NewValue, CanRollback)
                VALUES ($operationId, $actionType, $description, $timestamp, $originalValue, $newValue, $canRollback)";
            command.Parameters.AddWithValue("$operationId", operationId);
            command.Parameters.AddWithValue("$actionType", action.ActionType);
            command.Parameters.AddWithValue("$description", action.Description);
            command.Parameters.AddWithValue("$timestamp", action.Timestamp.ToString("O"));
            command.Parameters.AddWithValue("$originalValue", (object?)action.OriginalValue ?? DBNull.Value);
            command.Parameters.AddWithValue("$newValue", (object?)action.NewValue ?? DBNull.Value);
            command.Parameters.AddWithValue("$canRollback", action.CanRollback ? 1 : 0);
            command.ExecuteNonQuery();
        });
    }

    /// <inheritdoc />
    public async Task CompleteOperationAsync(string operationId, bool success)
    {
        if (string.IsNullOrWhiteSpace(operationId))
            throw new ArgumentException("Operation ID cannot be null or empty.", nameof(operationId));

        EnsureInitialized();

        var endTime = DateTime.UtcNow;

        // Update cache
        lock (_lock)
        {
            if (_activeOperations.TryGetValue(operationId, out var record))
            {
                IReadOnlyList<OperationAction> actions = _activeActions.TryGetValue(operationId, out var actionList)
                    ? actionList.ToList()
                    : Array.Empty<OperationAction>();

                _activeOperations[operationId] = record with
                {
                    EndTime = endTime,
                    Success = success,
                    Actions = actions
                };
            }
        }

        // Persist to database
        await Task.Run(() =>
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Operations 
                SET EndTime = $endTime, Success = $success
                WHERE Id = $id";
            command.Parameters.AddWithValue("$id", operationId);
            command.Parameters.AddWithValue("$endTime", endTime.ToString("O"));
            command.Parameters.AddWithValue("$success", success ? 1 : 0);
            command.ExecuteNonQuery();
        });

        // Remove from active cache after completion
        lock (_lock)
        {
            _activeOperations.Remove(operationId);
            _activeActions.Remove(operationId);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OperationRecord>> GetHistoryAsync(
        DateTime? since = null,
        int? limit = null)
    {
        EnsureInitialized();

        // Property 16: 操作历史时间范围 - 30天限制
        var cutoffDate = DateTime.UtcNow.AddDays(-_historyDays);
        var effectiveSince = since ?? cutoffDate;
        
        // Ensure we don't return records older than history limit
        if (effectiveSince < cutoffDate)
            effectiveSince = cutoffDate;

        return await Task.Run(() =>
        {
            var results = new List<OperationRecord>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Type, Description, StartTime, EndTime, Success
                FROM Operations
                WHERE StartTime >= $since
                ORDER BY StartTime DESC
                LIMIT $limit";
            command.Parameters.AddWithValue("$since", effectiveSince.ToString("O"));
            command.Parameters.AddWithValue("$limit", limit ?? 1000);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var record = new OperationRecord
                {
                    Id = reader.GetString(0),
                    Type = (OperationType)reader.GetInt32(1),
                    Description = reader.GetString(2),
                    StartTime = DateTime.Parse(reader.GetString(3)),
                    EndTime = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4)),
                    Success = reader.IsDBNull(5) ? null : reader.GetInt32(5) == 1,
                    Actions = GetActionsForOperation(connection, reader.GetString(0))
                };
                results.Add(record);
            }

            return results;
        });
    }


    /// <inheritdoc />
    public async Task<OperationRecord?> GetOperationAsync(string operationId)
    {
        if (string.IsNullOrWhiteSpace(operationId))
            throw new ArgumentException("Operation ID cannot be null or empty.", nameof(operationId));

        EnsureInitialized();

        // Check active cache first
        lock (_lock)
        {
            if (_activeOperations.TryGetValue(operationId, out var cached))
            {
                return cached;
            }
        }

        return await Task.Run(() =>
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Type, Description, StartTime, EndTime, Success
                FROM Operations
                WHERE Id = $id";
            command.Parameters.AddWithValue("$id", operationId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new OperationRecord
                {
                    Id = reader.GetString(0),
                    Type = (OperationType)reader.GetInt32(1),
                    Description = reader.GetString(2),
                    StartTime = DateTime.Parse(reader.GetString(3)),
                    EndTime = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4)),
                    Success = reader.IsDBNull(5) ? null : reader.GetInt32(5) == 1,
                    Actions = GetActionsForOperation(connection, reader.GetString(0))
                };
            }

            return null;
        });
    }

    private static IReadOnlyList<OperationAction> GetActionsForOperation(SqliteConnection connection, string operationId)
    {
        var actions = new List<OperationAction>();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT ActionType, Description, Timestamp, OriginalValue, NewValue, CanRollback
            FROM OperationActions
            WHERE OperationId = $operationId
            ORDER BY Id";
        command.Parameters.AddWithValue("$operationId", operationId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            actions.Add(new OperationAction
            {
                ActionType = reader.GetString(0),
                Description = reader.GetString(1),
                Timestamp = DateTime.Parse(reader.GetString(2)),
                OriginalValue = reader.IsDBNull(3) ? null : reader.GetString(3),
                NewValue = reader.IsDBNull(4) ? null : reader.GetString(4),
                CanRollback = reader.GetInt32(5) == 1
            });
        }

        return actions;
    }

    /// <summary>
    /// Cleans up old operation records beyond the history limit.
    /// </summary>
    public async Task CleanupOldRecordsAsync()
    {
        EnsureInitialized();

        var cutoffDate = DateTime.UtcNow.AddDays(-_historyDays);

        await Task.Run(() =>
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Delete actions first (foreign key constraint)
            using var deleteActions = connection.CreateCommand();
            deleteActions.CommandText = @"
                DELETE FROM OperationActions 
                WHERE OperationId IN (
                    SELECT Id FROM Operations WHERE StartTime < $cutoff
                )";
            deleteActions.Parameters.AddWithValue("$cutoff", cutoffDate.ToString("O"));
            deleteActions.ExecuteNonQuery();

            // Delete operations
            using var deleteOps = connection.CreateCommand();
            deleteOps.CommandText = "DELETE FROM Operations WHERE StartTime < $cutoff";
            deleteOps.Parameters.AddWithValue("$cutoff", cutoffDate.ToString("O"));
            deleteOps.ExecuteNonQuery();
        });
    }
}
