using System.Text.Json;
using Microsoft.Win32;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Infrastructure.Services;

/// <summary>
/// Implementation of IRegistryUpdater for updating registry path references.
/// Implements requirements 5.1, 5.2, 5.3, 5.4, 5.5
/// </summary>
public class RegistryUpdater : IRegistryUpdater
{
    private readonly IOperationLogger _operationLogger;
    private readonly string _backupDirectory;
    private readonly Dictionary<string, List<RegistryUpdateEntry>> _operationEntries = new();

    /// <summary>
    /// Registry hives to search for path references.
    /// </summary>
    private static readonly (RegistryKey Root, string Name)[] SearchHives = new[]
    {
        (Registry.LocalMachine, "HKLM"),
        (Registry.CurrentUser, "HKCU")
    };

    /// <summary>
    /// Registry paths to search for software references.
    /// </summary>
    private static readonly string[] SearchPaths = new[]
    {
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
        @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths",
        @"SOFTWARE\Classes\Applications",
        @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment"
    };

    /// <summary>
    /// Initializes a new instance of the RegistryUpdater class.
    /// </summary>
    public RegistryUpdater(IOperationLogger operationLogger)
    {
        _operationLogger = operationLogger ?? throw new ArgumentNullException(nameof(operationLogger));
        _backupDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WindowsSoftwareOrganizer",
            "RegistryBackups");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RegistryReference>> FindReferencesAsync(
        string oldPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(oldPath))
            throw new ArgumentException("Path cannot be null or empty.", nameof(oldPath));

        var references = new List<RegistryReference>();
        var normalizedPath = NormalizePath(oldPath);

        await Task.Run(() =>
        {
            foreach (var (rootKey, hiveName) in SearchHives)
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var searchPath in SearchPaths)
                {
                    try
                    {
                        SearchRegistryKey(rootKey, searchPath, hiveName, normalizedPath, references, cancellationToken);
                    }
                    catch { /* Skip inaccessible keys */ }
                }
            }
        }, cancellationToken);

        return references;
    }


    /// <inheritdoc />
    public async Task<string> CreateBackupAsync(IEnumerable<RegistryReference> references)
    {
        if (references == null)
            throw new ArgumentNullException(nameof(references));

        var referenceList = references.ToList();
        if (referenceList.Count == 0)
            return string.Empty;

        var backupId = $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}";
        var backupPath = Path.Combine(_backupDirectory, $"{backupId}.json");

        Directory.CreateDirectory(_backupDirectory);

        var backupData = referenceList.Select(r => new RegistryBackupEntry
        {
            KeyPath = r.KeyPath,
            ValueName = r.ValueName,
            ValueData = r.ValueData,
            ValueType = r.ValueType
        }).ToList();

        var json = JsonSerializer.Serialize(backupData, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(backupPath, json);

        return backupId;
    }

    /// <inheritdoc />
    public async Task<RegistryUpdateResult> UpdateReferencesAsync(
        IEnumerable<RegistryReference> references,
        string oldPath,
        string newPath)
    {
        if (references == null)
            throw new ArgumentNullException(nameof(references));
        if (string.IsNullOrWhiteSpace(oldPath))
            throw new ArgumentException("Old path cannot be null or empty.", nameof(oldPath));
        if (string.IsNullOrWhiteSpace(newPath))
            throw new ArgumentException("New path cannot be null or empty.", nameof(newPath));

        var referenceList = references.ToList();
        if (referenceList.Count == 0)
        {
            return new RegistryUpdateResult { Success = true, UpdatedCount = 0, FailedCount = 0 };
        }

        var backupId = await CreateBackupAsync(referenceList);
        var operationId = Guid.NewGuid().ToString("N");
        var entries = new List<RegistryUpdateEntry>();
        var updatedCount = 0;
        var failedCount = 0;

        var normalizedOldPath = NormalizePath(oldPath);
        var normalizedNewPath = NormalizePath(newPath);

        await Task.Run(() =>
        {
            foreach (var reference in referenceList)
            {
                var entry = UpdateSingleReference(reference, normalizedOldPath, normalizedNewPath);
                entries.Add(entry);
                if (entry.Success) updatedCount++;
                else failedCount++;
            }
        });

        _operationEntries[operationId] = entries;

        return new RegistryUpdateResult
        {
            Success = failedCount == 0,
            UpdatedCount = updatedCount,
            FailedCount = failedCount,
            BackupId = backupId,
            ErrorMessage = failedCount > 0 ? $"{failedCount} reference(s) failed to update." : null
        };
    }


    /// <inheritdoc />
    public async Task RestoreBackupAsync(string backupId)
    {
        if (string.IsNullOrWhiteSpace(backupId))
            throw new ArgumentException("Backup ID cannot be null or empty.", nameof(backupId));

        var backupPath = Path.Combine(_backupDirectory, $"{backupId}.json");
        if (!File.Exists(backupPath))
            throw new FileNotFoundException($"Backup file not found: {backupId}");

        var json = await File.ReadAllTextAsync(backupPath);
        var backupData = JsonSerializer.Deserialize<List<RegistryBackupEntry>>(json);

        if (backupData == null || backupData.Count == 0)
            return;

        await Task.Run(() =>
        {
            foreach (var entry in backupData)
            {
                try { RestoreSingleEntry(entry); }
                catch { /* Log but continue */ }
            }
        });
    }

    /// <inheritdoc />
    public Task<RegistryUpdateReport> GenerateReportAsync(string operationId)
    {
        if (string.IsNullOrWhiteSpace(operationId))
            throw new ArgumentException("Operation ID cannot be null or empty.", nameof(operationId));

        var entries = _operationEntries.TryGetValue(operationId, out var storedEntries)
            ? storedEntries
            : new List<RegistryUpdateEntry>();

        var report = new RegistryUpdateReport
        {
            OperationId = operationId,
            Timestamp = DateTime.UtcNow,
            Entries = entries,
            TotalUpdated = entries.Count(e => e.Success),
            TotalFailed = entries.Count(e => !e.Success)
        };

        return Task.FromResult(report);
    }

    private void SearchRegistryKey(
        RegistryKey rootKey,
        string subKeyPath,
        string hiveName,
        string searchPath,
        List<RegistryReference> references,
        CancellationToken cancellationToken)
    {
        try
        {
            using var key = rootKey.OpenSubKey(subKeyPath, false);
            if (key == null) return;

            foreach (var valueName in key.GetValueNames())
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var value = key.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                    if (value == null) continue;

                    var valueKind = key.GetValueKind(valueName);
                    var reference = CheckValueForPath($"{hiveName}\\{subKeyPath}", valueName, value, valueKind, searchPath);
                    if (reference != null)
                        references.Add(reference);
                }
                catch { /* Skip inaccessible values */ }
            }

            foreach (var subKeyName in key.GetSubKeyNames())
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    SearchRegistryKey(rootKey, $"{subKeyPath}\\{subKeyName}", hiveName, searchPath, references, cancellationToken);
                }
                catch { /* Skip inaccessible subkeys */ }
            }
        }
        catch { /* Skip inaccessible keys */ }
    }


    private RegistryReference? CheckValueForPath(
        string keyPath, string valueName, object value, RegistryValueKind valueKind, string searchPath)
    {
        var valueType = ConvertValueKind(valueKind);
        string? valueData = null;

        switch (valueKind)
        {
            case RegistryValueKind.String:
            case RegistryValueKind.ExpandString:
                valueData = value as string;
                break;
            case RegistryValueKind.MultiString:
                var strings = value as string[];
                if (strings != null && strings.Any(s => ContainsPath(s, searchPath)))
                    valueData = string.Join("\0", strings);
                break;
        }

        if (valueData != null && ContainsPath(valueData, searchPath))
        {
            return new RegistryReference
            {
                KeyPath = keyPath,
                ValueName = valueName,
                ValueData = valueData,
                ValueType = valueType
            };
        }
        return null;
    }

    private bool ContainsPath(string value, string searchPath)
    {
        if (string.IsNullOrEmpty(value)) return false;
        return value.Contains(searchPath, StringComparison.OrdinalIgnoreCase);
    }

    private RegistryUpdateEntry UpdateSingleReference(RegistryReference reference, string oldPath, string newPath)
    {
        try
        {
            var (rootKey, subKeyPath) = ParseKeyPath(reference.KeyPath);
            if (rootKey == null)
            {
                return new RegistryUpdateEntry
                {
                    KeyPath = reference.KeyPath, ValueName = reference.ValueName,
                    OldValue = reference.ValueData, NewValue = reference.ValueData,
                    Success = false, ErrorMessage = "Invalid registry key path"
                };
            }

            using var key = rootKey.OpenSubKey(subKeyPath, true);
            if (key == null)
            {
                return new RegistryUpdateEntry
                {
                    KeyPath = reference.KeyPath, ValueName = reference.ValueName,
                    OldValue = reference.ValueData, NewValue = reference.ValueData,
                    Success = false, ErrorMessage = "Cannot open registry key for writing"
                };
            }

            var newValue = reference.ValueData.Replace(oldPath, newPath, StringComparison.OrdinalIgnoreCase);
            var valueKind = ConvertToValueKind(reference.ValueType);

            if (reference.ValueType == RegistryValueType.MultiString)
            {
                var strings = newValue.Split('\0');
                key.SetValue(reference.ValueName, strings, valueKind);
            }
            else
            {
                key.SetValue(reference.ValueName, newValue, valueKind);
            }

            return new RegistryUpdateEntry
            {
                KeyPath = reference.KeyPath, ValueName = reference.ValueName,
                OldValue = reference.ValueData, NewValue = newValue, Success = true
            };
        }
        catch (Exception ex)
        {
            return new RegistryUpdateEntry
            {
                KeyPath = reference.KeyPath, ValueName = reference.ValueName,
                OldValue = reference.ValueData, NewValue = reference.ValueData,
                Success = false, ErrorMessage = ex.Message
            };
        }
    }


    private void RestoreSingleEntry(RegistryBackupEntry entry)
    {
        var (rootKey, subKeyPath) = ParseKeyPath(entry.KeyPath);
        if (rootKey == null) return;

        using var key = rootKey.OpenSubKey(subKeyPath, true);
        if (key == null) return;

        var valueKind = ConvertToValueKind(entry.ValueType);

        if (entry.ValueType == RegistryValueType.MultiString)
        {
            var strings = entry.ValueData.Split('\0');
            key.SetValue(entry.ValueName, strings, valueKind);
        }
        else
        {
            key.SetValue(entry.ValueName, entry.ValueData, valueKind);
        }
    }

    private (RegistryKey? Root, string SubKeyPath) ParseKeyPath(string keyPath)
    {
        var parts = keyPath.Split('\\', 2);
        if (parts.Length < 2) return (null, string.Empty);

        var hiveName = parts[0].ToUpperInvariant();
        var subKeyPath = parts[1];

        RegistryKey? rootKey = hiveName switch
        {
            "HKLM" or "HKEY_LOCAL_MACHINE" => Registry.LocalMachine,
            "HKCU" or "HKEY_CURRENT_USER" => Registry.CurrentUser,
            "HKCR" or "HKEY_CLASSES_ROOT" => Registry.ClassesRoot,
            "HKU" or "HKEY_USERS" => Registry.Users,
            "HKCC" or "HKEY_CURRENT_CONFIG" => Registry.CurrentConfig,
            _ => null
        };

        return (rootKey, subKeyPath);
    }

    private static string NormalizePath(string path) => path.TrimEnd('\\');

    private static RegistryValueType ConvertValueKind(RegistryValueKind kind) => kind switch
    {
        RegistryValueKind.String => RegistryValueType.String,
        RegistryValueKind.ExpandString => RegistryValueType.ExpandString,
        RegistryValueKind.MultiString => RegistryValueType.MultiString,
        RegistryValueKind.Binary => RegistryValueType.Binary,
        RegistryValueKind.DWord => RegistryValueType.DWord,
        RegistryValueKind.QWord => RegistryValueType.QWord,
        _ => RegistryValueType.String
    };

    private static RegistryValueKind ConvertToValueKind(RegistryValueType type) => type switch
    {
        RegistryValueType.String => RegistryValueKind.String,
        RegistryValueType.ExpandString => RegistryValueKind.ExpandString,
        RegistryValueType.MultiString => RegistryValueKind.MultiString,
        RegistryValueType.Binary => RegistryValueKind.Binary,
        RegistryValueType.DWord => RegistryValueKind.DWord,
        RegistryValueType.QWord => RegistryValueKind.QWord,
        _ => RegistryValueKind.String
    };

    private class RegistryBackupEntry
    {
        public string KeyPath { get; set; } = string.Empty;
        public string ValueName { get; set; } = string.Empty;
        public string ValueData { get; set; } = string.Empty;
        public RegistryValueType ValueType { get; set; }
    }
}
