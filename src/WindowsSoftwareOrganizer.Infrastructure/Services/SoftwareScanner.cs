using Microsoft.Win32;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Infrastructure.Services;

/// <summary>
/// Implementation of ISoftwareScanner for scanning installed software.
/// Implements requirements 1.1, 1.2, 1.3, 1.4
/// </summary>
public class SoftwareScanner : ISoftwareScanner
{
    /// <summary>
    /// Registry paths for installed software (32-bit and 64-bit).
    /// </summary>
    private static readonly string[] RegistryPaths = new[]
    {
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
        @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
    };

    /// <summary>
    /// Executable file extensions to look for during directory scanning.
    /// </summary>
    private static readonly string[] ExecutableExtensions = new[] { ".exe" };

    /// <inheritdoc />
    public event EventHandler<ScanProgressEventArgs>? ProgressChanged;

    /// <inheritdoc />
    /// <remarks>
    /// Scans Windows registry for installed software.
    /// Requirement 1.1: Scan registry for installed software
    /// Requirement 1.2: Extract software information (name, version, vendor, path)
    /// Requirement 1.3: Handle inaccessible registry keys
    /// </remarks>
    public async Task<IReadOnlyList<SoftwareEntry>> ScanInstalledSoftwareAsync(
        CancellationToken cancellationToken = default)
    {
        var entries = new List<SoftwareEntry>();
        var processedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var processedNames = new Dictionary<string, SoftwareEntry>(StringComparer.OrdinalIgnoreCase);
        var totalKeys = 0;
        var scannedKeys = 0;

        // First pass: count total keys for progress reporting
        foreach (var registryPath in RegistryPaths)
        {
            totalKeys += CountRegistryKeys(Registry.LocalMachine, registryPath);
            totalKeys += CountRegistryKeys(Registry.CurrentUser, registryPath);
        }

        OnProgressChanged(new ScanProgressEventArgs
        {
            ProgressPercentage = 0,
            StatusMessage = "正在扫描注册表...",
            ItemsScanned = 0
        });

        // Scan HKLM (Local Machine)
        foreach (var registryPath in RegistryPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var hklmEntries = await ScanRegistryHiveAsync(
                Registry.LocalMachine,
                registryPath,
                processedIds,
                () =>
                {
                    scannedKeys++;
                    ReportProgress(scannedKeys, totalKeys, entries.Count);
                },
                cancellationToken);

            foreach (var entry in hklmEntries)
            {
                AddEntryWithDeduplication(entries, processedNames, entry);
            }
        }

        // Scan HKCU (Current User)
        foreach (var registryPath in RegistryPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var hkcuEntries = await ScanRegistryHiveAsync(
                Registry.CurrentUser,
                registryPath,
                processedIds,
                () =>
                {
                    scannedKeys++;
                    ReportProgress(scannedKeys, totalKeys, entries.Count);
                },
                cancellationToken);

            foreach (var entry in hkcuEntries)
            {
                AddEntryWithDeduplication(entries, processedNames, entry);
            }
        }

        OnProgressChanged(new ScanProgressEventArgs
        {
            ProgressPercentage = 100,
            StatusMessage = $"扫描完成，发现 {entries.Count} 个软件",
            ItemsScanned = entries.Count
        });

        return entries.AsReadOnly();
    }

    /// <summary>
    /// Adds an entry to the list with deduplication based on normalized name.
    /// Keeps the entry with more complete information (has executable path, larger size, etc.)
    /// </summary>
    private static void AddEntryWithDeduplication(
        List<SoftwareEntry> entries,
        Dictionary<string, SoftwareEntry> processedNames,
        SoftwareEntry entry)
    {
        // Normalize the name for comparison (remove version numbers, spaces, etc.)
        var normalizedName = NormalizeSoftwareName(entry.Name);
        
        if (processedNames.TryGetValue(normalizedName, out var existingEntry))
        {
            // Compare and keep the better entry
            if (IsBetterEntry(entry, existingEntry))
            {
                // Replace the existing entry
                entries.Remove(existingEntry);
                entries.Add(entry);
                processedNames[normalizedName] = entry;
            }
            // Otherwise keep the existing entry
        }
        else
        {
            entries.Add(entry);
            processedNames[normalizedName] = entry;
        }
    }

    /// <summary>
    /// Normalizes a software name for deduplication comparison.
    /// </summary>
    private static string NormalizeSoftwareName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var normalized = name.ToLowerInvariant();
        
        // Remove version numbers (e.g., "1.0.0", "v2.3")
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"[\s\-_]*v?\d+(\.\d+)*[\s\-_]*", " ");
        
        // Remove common suffixes
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s*(x64|x86|64-bit|32-bit|64bit|32bit)\s*", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        // Normalize WeChat/微信 variations
        if (normalized.Contains("wechat") || normalized.Contains("weixin") || normalized.Contains("微信"))
        {
            // Check if it's enterprise WeChat
            if (normalized.Contains("work") || normalized.Contains("企业"))
            {
                return "企业微信";
            }
            // Check if it's WeChat DevTools
            if (normalized.Contains("devtools") || normalized.Contains("开发者工具") || normalized.Contains("web开发"))
            {
                return "微信开发者工具";
            }
            return "微信";
        }
        
        // Normalize QQ variations
        if (normalized.Contains("qq") && !normalized.Contains("音乐") && !normalized.Contains("浏览器"))
        {
            if (normalized.Contains("tim"))
                return "tim";
            return "qq";
        }
        
        // Remove spaces and special characters for comparison
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"[\s\-_\.]+", "");
        
        return normalized.Trim();
    }

    /// <summary>
    /// Determines if the new entry is better than the existing one.
    /// </summary>
    private static bool IsBetterEntry(SoftwareEntry newEntry, SoftwareEntry existingEntry)
    {
        // Prefer entry with valid install path
        var newHasPath = !string.IsNullOrWhiteSpace(newEntry.InstallPath) && newEntry.InstallPath != "未知路径";
        var existingHasPath = !string.IsNullOrWhiteSpace(existingEntry.InstallPath) && existingEntry.InstallPath != "未知路径";
        
        if (newHasPath && !existingHasPath)
            return true;
        if (!newHasPath && existingHasPath)
            return false;

        // Prefer entry with executable path
        var newHasExe = !string.IsNullOrWhiteSpace(newEntry.ExecutablePath);
        var existingHasExe = !string.IsNullOrWhiteSpace(existingEntry.ExecutablePath);
        
        if (newHasExe && !existingHasExe)
            return true;
        if (!newHasExe && existingHasExe)
            return false;

        // Prefer entry with larger size (more complete installation)
        if (newEntry.TotalSizeBytes > existingEntry.TotalSizeBytes)
            return true;
        if (newEntry.TotalSizeBytes < existingEntry.TotalSizeBytes)
            return false;

        // Prefer entry with version info
        var newHasVersion = !string.IsNullOrWhiteSpace(newEntry.Version);
        var existingHasVersion = !string.IsNullOrWhiteSpace(existingEntry.Version);
        
        if (newHasVersion && !existingHasVersion)
            return true;

        return false;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Scans a directory for software installations by looking for executables.
    /// Requirement 1.4: Scan directories for software
    /// </remarks>
    public async Task<IReadOnlyList<SoftwareEntry>> ScanDirectoryAsync(
        string directoryPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException("目录路径不能为空", nameof(directoryPath));
        }

        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"目录不存在: {directoryPath}");
        }

        var entries = new List<SoftwareEntry>();
        var processedDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        OnProgressChanged(new ScanProgressEventArgs
        {
            ProgressPercentage = 0,
            StatusMessage = $"正在扫描目录: {directoryPath}",
            ItemsScanned = 0
        });

        await Task.Run(() =>
        {
            ScanDirectoryRecursive(directoryPath, entries, processedDirs, 0, cancellationToken);
        }, cancellationToken);

        OnProgressChanged(new ScanProgressEventArgs
        {
            ProgressPercentage = 100,
            StatusMessage = $"目录扫描完成，发现 {entries.Count} 个软件",
            ItemsScanned = entries.Count
        });

        return entries.AsReadOnly();
    }

    /// <summary>
    /// Scans a registry hive for installed software.
    /// </summary>
    private Task<List<SoftwareEntry>> ScanRegistryHiveAsync(
        RegistryKey rootKey,
        string subKeyPath,
        HashSet<string> processedIds,
        Action onKeyScanned,
        CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            var entries = new List<SoftwareEntry>();

            try
            {
                using var uninstallKey = rootKey.OpenSubKey(subKeyPath);
                if (uninstallKey == null)
                {
                    return entries;
                }

                var subKeyNames = uninstallKey.GetSubKeyNames();

                foreach (var subKeyName in subKeyNames)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        using var appKey = uninstallKey.OpenSubKey(subKeyName);
                        if (appKey == null)
                        {
                            onKeyScanned();
                            continue;
                        }

                        var entry = ParseRegistryEntry(appKey, subKeyName);
                        if (entry != null && !processedIds.Contains(entry.Id))
                        {
                            processedIds.Add(entry.Id);
                            entries.Add(entry);
                        }

                        onKeyScanned();
                    }
                    catch (System.Security.SecurityException)
                    {
                        // Requirement 1.3: Log warning and continue for inaccessible keys
                        onKeyScanned();
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Requirement 1.3: Log warning and continue for inaccessible keys
                        onKeyScanned();
                    }
                }
            }
            catch (System.Security.SecurityException)
            {
                // Cannot access the registry hive at all - continue silently
            }
            catch (UnauthorizedAccessException)
            {
                // Cannot access the registry hive at all - continue silently
            }

            return entries;
        }, cancellationToken);
    }

    /// <summary>
    /// Parses a registry key into a SoftwareEntry.
    /// </summary>
    private static SoftwareEntry? ParseRegistryEntry(RegistryKey appKey, string keyName)
    {
        var displayName = appKey.GetValue("DisplayName") as string;
        var installLocation = appKey.GetValue("InstallLocation") as string;
        var displayIcon = appKey.GetValue("DisplayIcon") as string;

        // Skip entries without a display name
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return null;
        }

        // Try to get executable path from DisplayIcon or UninstallString
        string? executablePath = null;
        string? iconPath = null;

        // First try DisplayIcon (usually contains the icon path)
        if (!string.IsNullOrWhiteSpace(displayIcon))
        {
            iconPath = ExtractIconPath(displayIcon);
            if (iconPath != null && iconPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                executablePath = iconPath;
            }
        }

        // Try to get install location from multiple sources
        if (string.IsNullOrWhiteSpace(installLocation))
        {
            // Method 1: Try UninstallString to extract path
            var uninstallString = appKey.GetValue("UninstallString") as string;
            if (!string.IsNullOrWhiteSpace(uninstallString))
            {
                var extractedPath = ExtractPathFromUninstallString(uninstallString);
                if (!string.IsNullOrWhiteSpace(extractedPath))
                {
                    installLocation = extractedPath;
                    
                    // Also try to get executable from uninstall string
                    if (executablePath == null)
                    {
                        executablePath = ExtractExecutableFromUninstallString(uninstallString);
                    }
                }
            }
        }

        // Method 2: Try to get path from DisplayIcon
        if (string.IsNullOrWhiteSpace(installLocation) && !string.IsNullOrWhiteSpace(iconPath))
        {
            try
            {
                var iconDir = Path.GetDirectoryName(iconPath);
                if (!string.IsNullOrWhiteSpace(iconDir) && Directory.Exists(iconDir))
                {
                    installLocation = iconDir;
                }
            }
            catch { }
        }

        // Method 3: Try ModifyPath
        if (string.IsNullOrWhiteSpace(installLocation))
        {
            var modifyPath = appKey.GetValue("ModifyPath") as string;
            if (!string.IsNullOrWhiteSpace(modifyPath))
            {
                var extractedPath = ExtractPathFromUninstallString(modifyPath);
                if (!string.IsNullOrWhiteSpace(extractedPath))
                {
                    installLocation = extractedPath;
                }
            }
        }

        // Method 4: Try InstallSource
        if (string.IsNullOrWhiteSpace(installLocation))
        {
            var installSource = appKey.GetValue("InstallSource") as string;
            if (!string.IsNullOrWhiteSpace(installSource) && Directory.Exists(installSource))
            {
                installLocation = installSource;
            }
        }

        // Method 5: Search common installation directories
        if (string.IsNullOrWhiteSpace(installLocation))
        {
            installLocation = SearchCommonInstallPaths(displayName);
        }

        // Method 6: Try App Paths registry lookup
        if (string.IsNullOrWhiteSpace(installLocation))
        {
            var appPathResult = SearchAppPathsRegistry(displayName);
            if (!string.IsNullOrWhiteSpace(appPathResult))
            {
                installLocation = appPathResult;
                if (executablePath == null)
                {
                    executablePath = GetExecutableFromAppPaths(displayName);
                }
            }
        }

        // Method 7: Search Start Menu shortcuts
        if (string.IsNullOrWhiteSpace(installLocation))
        {
            var shortcutResult = SearchStartMenuShortcuts(displayName);
            if (!string.IsNullOrWhiteSpace(shortcutResult.InstallPath))
            {
                installLocation = shortcutResult.InstallPath;
                if (executablePath == null && !string.IsNullOrWhiteSpace(shortcutResult.ExecutablePath))
                {
                    executablePath = shortcutResult.ExecutablePath;
                }
            }
        }

        // Method 8: Search Desktop shortcuts
        if (string.IsNullOrWhiteSpace(installLocation))
        {
            var desktopResult = SearchDesktopShortcuts(displayName);
            if (!string.IsNullOrWhiteSpace(desktopResult.InstallPath))
            {
                installLocation = desktopResult.InstallPath;
                if (executablePath == null && !string.IsNullOrWhiteSpace(desktopResult.ExecutablePath))
                {
                    executablePath = desktopResult.ExecutablePath;
                }
            }
        }

        // Method 9: Search PATH environment variable
        if (string.IsNullOrWhiteSpace(installLocation))
        {
            installLocation = SearchPathEnvironment(displayName);
        }

        // Method 10: Search Windows Store apps
        if (string.IsNullOrWhiteSpace(installLocation))
        {
            installLocation = SearchWindowsApps(displayName);
        }

        // Method 11: Search Steam games
        long steamSizeBytes = 0;
        if (string.IsNullOrWhiteSpace(installLocation))
        {
            var steamResult = SearchSteamGames(displayName);
            if (!string.IsNullOrWhiteSpace(steamResult.InstallPath))
            {
                installLocation = steamResult.InstallPath;
                steamSizeBytes = steamResult.SizeBytes;
                if (executablePath == null && !string.IsNullOrWhiteSpace(steamResult.ExecutablePath))
                {
                    executablePath = steamResult.ExecutablePath;
                }
            }
        }

        // Method 12: Search LocalAppData\Programs
        if (string.IsNullOrWhiteSpace(installLocation))
        {
            installLocation = SearchLocalAppDataPrograms(displayName);
        }

        // Method 13: Search user profile directories
        if (string.IsNullOrWhiteSpace(installLocation))
        {
            installLocation = SearchUserProfileDirectories(displayName);
        }

        // If we have install location but no executable, try to find main exe
        if (executablePath == null && !string.IsNullOrWhiteSpace(installLocation) && Directory.Exists(installLocation))
        {
            executablePath = FindMainExecutable(installLocation, displayName);
        }

        // If we found executable but no install location, derive from executable
        if (string.IsNullOrWhiteSpace(installLocation) && !string.IsNullOrWhiteSpace(executablePath))
        {
            try
            {
                installLocation = Path.GetDirectoryName(executablePath);
            }
            catch { }
        }

        // Use a placeholder if we still don't have an install location
        if (string.IsNullOrWhiteSpace(installLocation))
        {
            installLocation = "未知路径";
        }

        // Parse install date
        DateTime? installDate = null;
        var installDateStr = appKey.GetValue("InstallDate") as string;
        if (!string.IsNullOrWhiteSpace(installDateStr) && installDateStr.Length == 8)
        {
            // Format: YYYYMMDD
            if (int.TryParse(installDateStr.Substring(0, 4), out var year) &&
                int.TryParse(installDateStr.Substring(4, 2), out var month) &&
                int.TryParse(installDateStr.Substring(6, 2), out var day))
            {
                try
                {
                    installDate = new DateTime(year, month, day);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Invalid date, leave as null
                }
            }
        }

        // Calculate size if available
        long totalSize = 0;
        
        // First try Steam size (most accurate for Steam games)
        if (steamSizeBytes > 0)
        {
            totalSize = steamSizeBytes;
        }
        else
        {
            // Try EstimatedSize from registry
            var estimatedSize = appKey.GetValue("EstimatedSize");
            if (estimatedSize is int sizeKb && sizeKb > 0)
            {
                totalSize = sizeKb * 1024L; // Convert KB to bytes
            }
        }
        
        // If size is still 0, try to calculate actual directory size
        if (totalSize == 0 && !string.IsNullOrWhiteSpace(installLocation) && installLocation != "未知路径")
        {
            try
            {
                if (Directory.Exists(installLocation))
                {
                    totalSize = CalculateDirectorySizeQuick(installLocation);
                }
            }
            catch { /* Ignore size calculation errors */ }
        }

        return new SoftwareEntry
        {
            Id = keyName,
            Name = displayName,
            Version = appKey.GetValue("DisplayVersion") as string,
            Vendor = appKey.GetValue("Publisher") as string,
            InstallPath = installLocation,
            InstallDate = installDate,
            TotalSizeBytes = totalSize,
            ExecutablePath = executablePath,
            IconPath = iconPath ?? executablePath
        };
    }

    /// <summary>
    /// Extracts icon path from DisplayIcon registry value.
    /// </summary>
    private static string? ExtractIconPath(string displayIcon)
    {
        if (string.IsNullOrWhiteSpace(displayIcon))
            return null;

        var path = displayIcon.Trim();
        
        // Remove icon index (e.g., "C:\path\app.exe,0")
        var commaIndex = path.LastIndexOf(',');
        if (commaIndex > 0)
        {
            path = path.Substring(0, commaIndex);
        }

        // Remove quotes
        path = path.Trim('"');

        // Verify the file exists
        try
        {
            if (File.Exists(path))
            {
                return path;
            }
        }
        catch
        {
            // Invalid path
        }

        return null;
    }

    /// <summary>
    /// Extracts executable path from uninstall string.
    /// </summary>
    private static string? ExtractExecutableFromUninstallString(string uninstallString)
    {
        var path = uninstallString.Trim();
        
        // Handle quoted paths
        if (path.StartsWith('"'))
        {
            var endQuote = path.IndexOf('"', 1);
            if (endQuote > 1)
            {
                path = path.Substring(1, endQuote - 1);
            }
        }
        else
        {
            // Take everything before the first space
            var spaceIndex = path.IndexOf(' ');
            if (spaceIndex > 0)
            {
                path = path.Substring(0, spaceIndex);
            }
        }

        // Check if it's an executable
        try
        {
            if (File.Exists(path) && path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }
        }
        catch
        {
            // Invalid path
        }

        return null;
    }

    /// <summary>
    /// Finds the main executable in an installation directory.
    /// </summary>
    private static string? FindMainExecutable(string installLocation, string displayName)
    {
        try
        {
            var exeFiles = Directory.GetFiles(installLocation, "*.exe", SearchOption.TopDirectoryOnly);
            if (exeFiles.Length == 0)
            {
                return null;
            }

            // Try to find exe matching the display name
            var nameWithoutSpaces = displayName.Replace(" ", "").ToLowerInvariant();
            var matchingExe = exeFiles.FirstOrDefault(e =>
            {
                var exeName = Path.GetFileNameWithoutExtension(e).ToLowerInvariant();
                return exeName.Contains(nameWithoutSpaces) || nameWithoutSpaces.Contains(exeName);
            });

            if (matchingExe != null)
            {
                return matchingExe;
            }

            // Return the first exe that's not an uninstaller
            return exeFiles.FirstOrDefault(e =>
            {
                var name = Path.GetFileNameWithoutExtension(e).ToLowerInvariant();
                return !name.Contains("unins") && !name.Contains("uninst") && !name.Contains("remove");
            }) ?? exeFiles.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts the installation path from an uninstall string.
    /// </summary>
    private static string? ExtractPathFromUninstallString(string uninstallString)
    {
        // Remove quotes if present
        var path = uninstallString.Trim();
        if (path.StartsWith('"'))
        {
            var endQuote = path.IndexOf('"', 1);
            if (endQuote > 1)
            {
                path = path.Substring(1, endQuote - 1);
            }
        }
        else
        {
            // Take everything before the first space (likely the executable path)
            var spaceIndex = path.IndexOf(' ');
            if (spaceIndex > 0)
            {
                path = path.Substring(0, spaceIndex);
            }
        }

        // Get the directory containing the executable
        try
        {
            if (File.Exists(path))
            {
                return Path.GetDirectoryName(path);
            }
            if (Directory.Exists(path))
            {
                return path;
            }
        }
        catch
        {
            // Invalid path, return null
        }

        return null;
    }

    /// <summary>
    /// Searches common installation directories for a software by name.
    /// </summary>
    private static string? SearchCommonInstallPaths(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return null;

        // Common installation directories
        var searchPaths = new List<string>();
        
        // Program Files directories
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        
        if (!string.IsNullOrEmpty(programFiles)) searchPaths.Add(programFiles);
        if (!string.IsNullOrEmpty(programFilesX86)) searchPaths.Add(programFilesX86);
        if (!string.IsNullOrEmpty(localAppData)) searchPaths.Add(localAppData);
        if (!string.IsNullOrEmpty(appData)) searchPaths.Add(appData);
        
        // Add common custom paths
        searchPaths.AddRange(new[] { @"C:\Program Files", @"C:\Program Files (x86)", @"D:\Program Files", @"E:\Soft", @"F:\Soft" });

        // Normalize display name for matching
        var normalizedName = displayName.ToLowerInvariant()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("_", "");

        foreach (var basePath in searchPaths.Distinct())
        {
            if (!Directory.Exists(basePath))
                continue;

            try
            {
                // Search for directories matching the software name
                foreach (var dir in Directory.GetDirectories(basePath))
                {
                    var dirName = Path.GetFileName(dir)?.ToLowerInvariant()
                        .Replace(" ", "")
                        .Replace("-", "")
                        .Replace("_", "");
                    
                    if (string.IsNullOrEmpty(dirName))
                        continue;

                    // Check if directory name matches or contains the software name
                    if (dirName.Contains(normalizedName) || normalizedName.Contains(dirName))
                    {
                        // Verify it has executables
                        try
                        {
                            if (Directory.GetFiles(dir, "*.exe", SearchOption.TopDirectoryOnly).Length > 0)
                            {
                                return dir;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch
            {
                // Skip directories we can't access
            }
        }

        return null;
    }

    /// <summary>
    /// Recursively scans a directory for software installations.
    /// </summary>
    private void ScanDirectoryRecursive(
        string directoryPath,
        List<SoftwareEntry> entries,
        HashSet<string> processedDirs,
        int depth,
        CancellationToken cancellationToken)
    {
        // Limit recursion depth to prevent excessive scanning
        if (depth > 3)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Skip if already processed
        if (processedDirs.Contains(directoryPath))
        {
            return;
        }
        processedDirs.Add(directoryPath);

        try
        {
            // Look for executables in this directory
            var executables = Directory.GetFiles(directoryPath)
                .Where(f => ExecutableExtensions.Contains(
                    Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            if (executables.Count > 0)
            {
                // This directory contains executables - treat it as a software installation
                var entry = CreateEntryFromDirectory(directoryPath, executables);
                if (entry != null)
                {
                    entries.Add(entry);
                    OnProgressChanged(new ScanProgressEventArgs
                    {
                        StatusMessage = $"发现: {entry.Name}",
                        ItemsScanned = entries.Count
                    });
                }
            }

            // Scan subdirectories
            foreach (var subDir in Directory.GetDirectories(directoryPath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                ScanDirectoryRecursive(subDir, entries, processedDirs, depth + 1, cancellationToken);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip directories we can't access
        }
        catch (DirectoryNotFoundException)
        {
            // Directory was deleted during scan
        }
    }

    /// <summary>
    /// Creates a SoftwareEntry from a directory containing executables.
    /// </summary>
    private static SoftwareEntry? CreateEntryFromDirectory(string directoryPath, List<string> executables)
    {
        // Use the directory name as the software name
        var dirName = Path.GetFileName(directoryPath);
        if (string.IsNullOrWhiteSpace(dirName))
        {
            return null;
        }

        // Try to get version info from the main executable
        string? version = null;
        string? vendor = null;
        var mainExe = executables.FirstOrDefault(e =>
            Path.GetFileNameWithoutExtension(e).Equals(dirName, StringComparison.OrdinalIgnoreCase))
            ?? executables.FirstOrDefault();

        if (mainExe != null)
        {
            try
            {
                var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(mainExe);
                version = versionInfo.FileVersion;
                vendor = versionInfo.CompanyName;
            }
            catch
            {
                // Could not read version info
            }
        }

        // Calculate directory size
        long totalSize = 0;
        try
        {
            totalSize = CalculateDirectorySize(directoryPath);
        }
        catch
        {
            // Could not calculate size
        }

        return new SoftwareEntry
        {
            Id = $"dir_{directoryPath.GetHashCode():X8}",
            Name = dirName,
            Version = version,
            Vendor = vendor,
            InstallPath = directoryPath,
            TotalSizeBytes = totalSize
        };
    }

    /// <summary>
    /// Calculates the total size of a directory.
    /// </summary>
    private static long CalculateDirectorySize(string directoryPath)
    {
        long size = 0;

        try
        {
            foreach (var file in Directory.GetFiles(directoryPath))
            {
                try
                {
                    size += new FileInfo(file).Length;
                }
                catch
                {
                    // Skip files we can't access
                }
            }

            foreach (var subDir in Directory.GetDirectories(directoryPath))
            {
                size += CalculateDirectorySize(subDir);
            }
        }
        catch
        {
            // Skip directories we can't access
        }

        return size;
    }

    /// <summary>
    /// Counts the number of subkeys in a registry path.
    /// </summary>
    private static int CountRegistryKeys(RegistryKey rootKey, string subKeyPath)
    {
        try
        {
            using var key = rootKey.OpenSubKey(subKeyPath);
            return key?.SubKeyCount ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Reports scan progress.
    /// </summary>
    private void ReportProgress(int scanned, int total, int found)
    {
        var percentage = total > 0 ? (int)((scanned * 100.0) / total) : 0;
        OnProgressChanged(new ScanProgressEventArgs
        {
            ProgressPercentage = Math.Min(percentage, 99), // Reserve 100 for completion
            StatusMessage = $"已扫描 {scanned}/{total} 项，发现 {found} 个软件",
            ItemsScanned = found
        });
    }

    /// <summary>
    /// Raises the ProgressChanged event.
    /// </summary>
    protected virtual void OnProgressChanged(ScanProgressEventArgs e)
    {
        ProgressChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Searches App Paths registry for software installation path.
    /// App Paths registry: HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths
    /// </summary>
    private static string? SearchAppPathsRegistry(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return null;

        var appPathsKeys = new[]
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths"
        };

        var normalizedName = displayName.ToLowerInvariant()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("_", "");

        foreach (var appPathsKey in appPathsKeys)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(appPathsKey);
                if (key == null) continue;

                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    var exeName = Path.GetFileNameWithoutExtension(subKeyName).ToLowerInvariant()
                        .Replace(" ", "")
                        .Replace("-", "")
                        .Replace("_", "");

                    if (exeName.Contains(normalizedName) || normalizedName.Contains(exeName))
                    {
                        using var appKey = key.OpenSubKey(subKeyName);
                        if (appKey == null) continue;

                        // Try to get Path value first
                        var pathValue = appKey.GetValue("Path") as string;
                        if (!string.IsNullOrWhiteSpace(pathValue))
                        {
                            // Path value may contain multiple paths separated by semicolon
                            var firstPath = pathValue.Split(';').FirstOrDefault()?.Trim();
                            if (!string.IsNullOrWhiteSpace(firstPath) && Directory.Exists(firstPath))
                            {
                                return firstPath;
                            }
                        }

                        // Try default value (executable path)
                        var defaultValue = appKey.GetValue(null) as string;
                        if (!string.IsNullOrWhiteSpace(defaultValue))
                        {
                            var exePath = defaultValue.Trim('"');
                            if (File.Exists(exePath))
                            {
                                return Path.GetDirectoryName(exePath);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        return null;
    }

    /// <summary>
    /// Gets executable path from App Paths registry.
    /// </summary>
    private static string? GetExecutableFromAppPaths(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return null;

        var appPathsKeys = new[]
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths"
        };

        var normalizedName = displayName.ToLowerInvariant()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("_", "");

        foreach (var appPathsKey in appPathsKeys)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(appPathsKey);
                if (key == null) continue;

                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    var exeName = Path.GetFileNameWithoutExtension(subKeyName).ToLowerInvariant()
                        .Replace(" ", "")
                        .Replace("-", "")
                        .Replace("_", "");

                    if (exeName.Contains(normalizedName) || normalizedName.Contains(exeName))
                    {
                        using var appKey = key.OpenSubKey(subKeyName);
                        var defaultValue = appKey?.GetValue(null) as string;
                        if (!string.IsNullOrWhiteSpace(defaultValue))
                        {
                            var exePath = defaultValue.Trim('"');
                            if (File.Exists(exePath))
                            {
                                return exePath;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        return null;
    }

    /// <summary>
    /// Searches Start Menu shortcuts for software installation path.
    /// Uses Shell32 COM interop to read .lnk files.
    /// </summary>
    private static (string? InstallPath, string? ExecutablePath) SearchStartMenuShortcuts(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return (null, null);

        var startMenuPaths = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms)),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs))
        };

        var normalizedName = displayName.ToLowerInvariant()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("_", "");

        foreach (var startMenuPath in startMenuPaths)
        {
            if (!Directory.Exists(startMenuPath))
                continue;

            try
            {
                // Search for .lnk files
                foreach (var lnkFile in Directory.EnumerateFiles(startMenuPath, "*.lnk", SearchOption.AllDirectories))
                {
                    var lnkName = Path.GetFileNameWithoutExtension(lnkFile).ToLowerInvariant()
                        .Replace(" ", "")
                        .Replace("-", "")
                        .Replace("_", "");

                    if (lnkName.Contains(normalizedName) || normalizedName.Contains(lnkName))
                    {
                        var targetPath = GetShortcutTarget(lnkFile);
                        if (!string.IsNullOrWhiteSpace(targetPath) && File.Exists(targetPath))
                        {
                            var installPath = Path.GetDirectoryName(targetPath);
                            if (!string.IsNullOrWhiteSpace(installPath) && Directory.Exists(installPath))
                            {
                                return (installPath, targetPath);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        return (null, null);
    }

    /// <summary>
    /// Gets the target path of a Windows shortcut (.lnk) file.
    /// Uses Shell32 COM interop.
    /// </summary>
    private static string? GetShortcutTarget(string shortcutPath)
    {
        try
        {
            // Use Shell32 COM to read shortcut
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return null;

            dynamic? shell = Activator.CreateInstance(shellType);
            if (shell == null) return null;

            try
            {
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                string targetPath = shortcut.TargetPath;
                return targetPath;
            }
            finally
            {
                if (shell != null)
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(shell);
                }
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Searches desktop shortcuts for software installation path.
    /// </summary>
    private static (string? InstallPath, string? ExecutablePath) SearchDesktopShortcuts(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return (null, null);

        var desktopPaths = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)
        };

        var normalizedName = displayName.ToLowerInvariant()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("_", "");

        foreach (var desktopPath in desktopPaths)
        {
            if (!Directory.Exists(desktopPath))
                continue;

            try
            {
                foreach (var lnkFile in Directory.EnumerateFiles(desktopPath, "*.lnk"))
                {
                    var lnkName = Path.GetFileNameWithoutExtension(lnkFile).ToLowerInvariant()
                        .Replace(" ", "")
                        .Replace("-", "")
                        .Replace("_", "");

                    if (lnkName.Contains(normalizedName) || normalizedName.Contains(lnkName))
                    {
                        var targetPath = GetShortcutTarget(lnkFile);
                        if (!string.IsNullOrWhiteSpace(targetPath) && File.Exists(targetPath))
                        {
                            var installPath = Path.GetDirectoryName(targetPath);
                            if (!string.IsNullOrWhiteSpace(installPath) && Directory.Exists(installPath))
                            {
                                return (installPath, targetPath);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        return (null, null);
    }

    /// <summary>
    /// Searches PATH environment variable for software executable.
    /// </summary>
    private static string? SearchPathEnvironment(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return null;

        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathEnv))
            return null;

        var normalizedName = displayName.ToLowerInvariant()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("_", "");

        var paths = pathEnv.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var path in paths)
        {
            if (!Directory.Exists(path))
                continue;

            try
            {
                // Check if directory name matches
                var dirName = Path.GetFileName(path)?.ToLowerInvariant()
                    .Replace(" ", "")
                    .Replace("-", "")
                    .Replace("_", "");

                if (!string.IsNullOrEmpty(dirName) && 
                    (dirName.Contains(normalizedName) || normalizedName.Contains(dirName)))
                {
                    // Verify it has executables
                    if (Directory.GetFiles(path, "*.exe", SearchOption.TopDirectoryOnly).Length > 0)
                    {
                        return path;
                    }
                }

                // Also check parent directory
                var parentDir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(parentDir))
                {
                    var parentName = Path.GetFileName(parentDir)?.ToLowerInvariant()
                        .Replace(" ", "")
                        .Replace("-", "")
                        .Replace("_", "");

                    if (!string.IsNullOrEmpty(parentName) &&
                        (parentName.Contains(normalizedName) || normalizedName.Contains(parentName)))
                    {
                        if (Directory.Exists(parentDir))
                        {
                            return parentDir;
                        }
                    }
                }
            }
            catch { }
        }

        return null;
    }

    /// <summary>
    /// Searches Windows Store apps (UWP) for installation path.
    /// </summary>
    private static string? SearchWindowsApps(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return null;

        var windowsAppsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "WindowsApps");

        if (!Directory.Exists(windowsAppsPath))
            return null;

        var normalizedName = displayName.ToLowerInvariant()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("_", "");

        try
        {
            foreach (var dir in Directory.GetDirectories(windowsAppsPath))
            {
                var dirName = Path.GetFileName(dir)?.ToLowerInvariant()
                    .Replace(" ", "")
                    .Replace("-", "")
                    .Replace("_", "");

                if (!string.IsNullOrEmpty(dirName) &&
                    (dirName.Contains(normalizedName) || normalizedName.Contains(dirName)))
                {
                    return dir;
                }
            }
        }
        catch { /* WindowsApps may require special permissions */ }

        return null;
    }

    /// <summary>
    /// Searches Steam games for installation path and size.
    /// Parses Steam's libraryfolders.vdf and appmanifest_*.acf files.
    /// </summary>
    private static (string? InstallPath, string? ExecutablePath, long SizeBytes) SearchSteamGames(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return (null, null, 0);

        // Get Steam installation path from registry
        string? steamPath = null;
        var steamRegistryPaths = new[]
        {
            @"SOFTWARE\Valve\Steam",
            @"SOFTWARE\WOW6432Node\Valve\Steam"
        };

        foreach (var regPath in steamRegistryPaths)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(regPath);
                steamPath = key?.GetValue("InstallPath") as string;
                if (!string.IsNullOrWhiteSpace(steamPath) && Directory.Exists(steamPath))
                    break;
            }
            catch { }
        }

        // Also try current user
        if (string.IsNullOrWhiteSpace(steamPath) || !Directory.Exists(steamPath))
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam");
                steamPath = key?.GetValue("SteamPath") as string;
            }
            catch { }
        }

        if (string.IsNullOrWhiteSpace(steamPath) || !Directory.Exists(steamPath))
            return (null, null, 0);

        // Get all Steam library folders
        var libraryFolders = GetSteamLibraryFolders(steamPath);
        
        var normalizedName = displayName.ToLowerInvariant()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("_", "")
            .Replace(":", "")
            .Replace("：", "");

        // Search each library folder for the game
        foreach (var libraryPath in libraryFolders)
        {
            var steamAppsPath = Path.Combine(libraryPath, "steamapps");
            if (!Directory.Exists(steamAppsPath))
                continue;

            try
            {
                // Search appmanifest files
                foreach (var manifestFile in Directory.GetFiles(steamAppsPath, "appmanifest_*.acf"))
                {
                    var gameInfo = ParseSteamAppManifest(manifestFile);
                    if (gameInfo == null)
                        continue;

                    var gameNormalizedName = gameInfo.Value.Name.ToLowerInvariant()
                        .Replace(" ", "")
                        .Replace("-", "")
                        .Replace("_", "")
                        .Replace(":", "")
                        .Replace("：", "");

                    if (gameNormalizedName.Contains(normalizedName) || normalizedName.Contains(gameNormalizedName))
                    {
                        var gamePath = Path.Combine(steamAppsPath, "common", gameInfo.Value.InstallDir);
                        if (Directory.Exists(gamePath))
                        {
                            // Try to find main executable
                            string? exePath = FindMainExecutable(gamePath, displayName);
                            return (gamePath, exePath, gameInfo.Value.SizeOnDisk);
                        }
                    }
                }
            }
            catch { }
        }

        return (null, null, 0);
    }

    /// <summary>
    /// Gets all Steam library folders from libraryfolders.vdf.
    /// </summary>
    private static List<string> GetSteamLibraryFolders(string steamPath)
    {
        var folders = new List<string> { steamPath };
        
        var libraryFoldersPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(libraryFoldersPath))
            return folders;

        try
        {
            var content = File.ReadAllText(libraryFoldersPath);
            
            // Parse VDF format - look for "path" entries
            var pathMatches = System.Text.RegularExpressions.Regex.Matches(
                content, 
                @"""path""\s+""([^""]+)""",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            foreach (System.Text.RegularExpressions.Match match in pathMatches)
            {
                if (match.Groups.Count > 1)
                {
                    var path = match.Groups[1].Value.Replace(@"\\", @"\");
                    if (Directory.Exists(path) && !folders.Contains(path, StringComparer.OrdinalIgnoreCase))
                    {
                        folders.Add(path);
                    }
                }
            }
        }
        catch { }

        return folders;
    }

    /// <summary>
    /// Parses a Steam appmanifest_*.acf file to get game information.
    /// </summary>
    private static (string Name, string InstallDir, long SizeOnDisk)? ParseSteamAppManifest(string manifestPath)
    {
        try
        {
            var content = File.ReadAllText(manifestPath);
            
            // Parse name
            var nameMatch = System.Text.RegularExpressions.Regex.Match(
                content, @"""name""\s+""([^""]+)""");
            if (!nameMatch.Success)
                return null;
            var name = nameMatch.Groups[1].Value;

            // Parse installdir
            var installDirMatch = System.Text.RegularExpressions.Regex.Match(
                content, @"""installdir""\s+""([^""]+)""");
            if (!installDirMatch.Success)
                return null;
            var installDir = installDirMatch.Groups[1].Value;

            // Parse SizeOnDisk
            long sizeOnDisk = 0;
            var sizeMatch = System.Text.RegularExpressions.Regex.Match(
                content, @"""SizeOnDisk""\s+""(\d+)""");
            if (sizeMatch.Success)
            {
                long.TryParse(sizeMatch.Groups[1].Value, out sizeOnDisk);
            }

            return (name, installDir, sizeOnDisk);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Searches LocalAppData\Programs for software installation.
    /// Many modern apps install here (VS Code, Discord, etc.)
    /// </summary>
    private static string? SearchLocalAppDataPrograms(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return null;

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
            return null;

        var searchPaths = new[]
        {
            Path.Combine(localAppData, "Programs"),
            Path.Combine(localAppData, "Apps"),
            localAppData // Some apps install directly in LocalAppData
        };

        var normalizedName = displayName.ToLowerInvariant()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("_", "");

        foreach (var basePath in searchPaths)
        {
            if (!Directory.Exists(basePath))
                continue;

            try
            {
                foreach (var dir in Directory.GetDirectories(basePath))
                {
                    var dirName = Path.GetFileName(dir)?.ToLowerInvariant()
                        .Replace(" ", "")
                        .Replace("-", "")
                        .Replace("_", "");

                    if (string.IsNullOrEmpty(dirName))
                        continue;

                    if (dirName.Contains(normalizedName) || normalizedName.Contains(dirName))
                    {
                        // Verify it has executables
                        try
                        {
                            if (Directory.GetFiles(dir, "*.exe", SearchOption.TopDirectoryOnly).Length > 0)
                            {
                                return dir;
                            }
                            // Check one level deeper
                            foreach (var subDir in Directory.GetDirectories(dir))
                            {
                                if (Directory.GetFiles(subDir, "*.exe", SearchOption.TopDirectoryOnly).Length > 0)
                                {
                                    return subDir;
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        return null;
    }

    /// <summary>
    /// Searches user profile directories for software installation.
    /// </summary>
    private static string? SearchUserProfileDirectories(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return null;

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        var searchPaths = new List<string>();
        
        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            searchPaths.Add(userProfile);
            searchPaths.Add(Path.Combine(userProfile, "AppData", "Local"));
            searchPaths.Add(Path.Combine(userProfile, "AppData", "Roaming"));
        }
        
        if (!string.IsNullOrWhiteSpace(appData))
        {
            searchPaths.Add(appData);
        }

        var normalizedName = displayName.ToLowerInvariant()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("_", "");

        foreach (var basePath in searchPaths.Distinct())
        {
            if (!Directory.Exists(basePath))
                continue;

            try
            {
                foreach (var dir in Directory.GetDirectories(basePath))
                {
                    var dirName = Path.GetFileName(dir)?.ToLowerInvariant()
                        .Replace(" ", "")
                        .Replace("-", "")
                        .Replace("_", "");

                    if (string.IsNullOrEmpty(dirName))
                        continue;

                    // Skip system directories
                    if (dirName == "appdata" || dirName == "local" || dirName == "roaming" || 
                        dirName == "locallow" || dirName == "temp" || dirName == "cache")
                        continue;

                    if (dirName.Contains(normalizedName) || normalizedName.Contains(dirName))
                    {
                        // Verify it has executables
                        try
                        {
                            if (Directory.GetFiles(dir, "*.exe", SearchOption.TopDirectoryOnly).Length > 0)
                            {
                                return dir;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        return null;
    }

    /// <summary>
    /// Calculates directory size quickly with a depth limit for performance.
    /// </summary>
    private static long CalculateDirectorySizeQuick(string directoryPath, int maxDepth = 3)
    {
        if (maxDepth <= 0)
            return 0;

        long size = 0;

        try
        {
            // Get files in current directory
            foreach (var file in Directory.EnumerateFiles(directoryPath))
            {
                try
                {
                    size += new FileInfo(file).Length;
                }
                catch { /* Skip files we can't access */ }
            }

            // Recurse into subdirectories
            foreach (var subDir in Directory.EnumerateDirectories(directoryPath))
            {
                try
                {
                    size += CalculateDirectorySizeQuick(subDir, maxDepth - 1);
                }
                catch { /* Skip directories we can't access */ }
            }
        }
        catch { /* Skip on any error */ }

        return size;
    }
}
