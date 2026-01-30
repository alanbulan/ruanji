using FsCheck;
using FsCheck.Xunit;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.Infrastructure.Services;

namespace WindowsSoftwareOrganizer.Tests.Properties;

/// <summary>
/// Property-based tests for SoftwareScanner implementation.
/// Tests Properties 1, 2 from the design document.
/// </summary>
public class SoftwareScannerPropertyTests
{
    #region Property 1: 扫描结果完整性

    /// <summary>
    /// Property 1: 扫描结果完整性
    /// 对于任意注册表软件条目集合，扫描器返回的每个SoftwareEntry都必须包含非空的Id、Name和InstallPath字段。
    /// **Validates: Requirements 1.1, 1.2**
    /// **Feature: windows-software-organizer, Property 1: 扫描结果完整性**
    /// </summary>
    [Fact]
    public async Task ScanInstalledSoftware_AllEntriesHaveRequiredFields()
    {
        // Arrange
        var scanner = new SoftwareScanner();

        // Act
        var entries = await scanner.ScanInstalledSoftwareAsync();

        // Assert: Every entry must have non-empty Id, Name, and InstallPath
        foreach (var entry in entries)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Id),
                $"Entry has empty Id: Name={entry.Name}");
            Assert.False(string.IsNullOrWhiteSpace(entry.Name),
                $"Entry has empty Name: Id={entry.Id}");
            Assert.False(string.IsNullOrWhiteSpace(entry.InstallPath),
                $"Entry has empty InstallPath: Name={entry.Name}");
        }
    }

    /// <summary>
    /// Property 1 (Variant): All entries should have unique IDs.
    /// **Validates: Requirements 1.1, 1.2**
    /// </summary>
    [Fact]
    public async Task ScanInstalledSoftware_AllEntriesHaveUniqueIds()
    {
        // Arrange
        var scanner = new SoftwareScanner();

        // Act
        var entries = await scanner.ScanInstalledSoftwareAsync();

        // Assert: All IDs should be unique
        var ids = entries.Select(e => e.Id).ToList();
        var uniqueIds = ids.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        Assert.Equal(uniqueIds.Count, ids.Count);
    }

    /// <summary>
    /// Property 1 (Variant): TotalSizeBytes should be non-negative.
    /// **Validates: Requirements 1.2**
    /// </summary>
    [Fact]
    public async Task ScanInstalledSoftware_SizeIsNonNegative()
    {
        // Arrange
        var scanner = new SoftwareScanner();

        // Act
        var entries = await scanner.ScanInstalledSoftwareAsync();

        // Assert: All sizes should be non-negative
        foreach (var entry in entries)
        {
            Assert.True(entry.TotalSizeBytes >= 0,
                $"Entry '{entry.Name}' has negative size: {entry.TotalSizeBytes}");
        }
    }

    /// <summary>
    /// Property 1 (Variant): Category should be a valid enum value.
    /// **Validates: Requirements 1.2**
    /// </summary>
    [Fact]
    public async Task ScanInstalledSoftware_CategoryIsValidEnum()
    {
        // Arrange
        var scanner = new SoftwareScanner();

        // Act
        var entries = await scanner.ScanInstalledSoftwareAsync();

        // Assert: All categories should be valid enum values
        foreach (var entry in entries)
        {
            Assert.True(Enum.IsDefined(typeof(SoftwareCategory), entry.Category),
                $"Entry '{entry.Name}' has invalid category: {entry.Category}");
        }
    }

    #endregion

    #region Property 2: 目录扫描覆盖性

    /// <summary>
    /// Property 2: 目录扫描覆盖性
    /// 对于任意包含可执行文件的目录结构，扫描器必须识别出所有.exe文件所在的目录。
    /// **Validates: Requirements 1.4**
    /// **Feature: windows-software-organizer, Property 2: 目录扫描覆盖性**
    /// </summary>
    [Fact]
    public async Task ScanDirectory_FindsAllExecutableDirectories()
    {
        // Arrange
        var scanner = new SoftwareScanner();
        var tempDir = Path.Combine(Path.GetTempPath(), $"ScanTest_{Guid.NewGuid():N}");

        try
        {
            // Create test directory structure with executables
            Directory.CreateDirectory(tempDir);
            var app1Dir = Path.Combine(tempDir, "App1");
            var app2Dir = Path.Combine(tempDir, "App2");
            var subDir = Path.Combine(app1Dir, "SubApp");

            Directory.CreateDirectory(app1Dir);
            Directory.CreateDirectory(app2Dir);
            Directory.CreateDirectory(subDir);

            // Create dummy exe files (just empty files with .exe extension)
            File.WriteAllText(Path.Combine(app1Dir, "app1.exe"), "");
            File.WriteAllText(Path.Combine(app2Dir, "app2.exe"), "");
            File.WriteAllText(Path.Combine(subDir, "subapp.exe"), "");

            // Act
            var entries = await scanner.ScanDirectoryAsync(tempDir);

            // Assert: Should find all directories containing executables
            var foundPaths = entries.Select(e => e.InstallPath).ToHashSet(StringComparer.OrdinalIgnoreCase);

            Assert.Contains(app1Dir, foundPaths);
            Assert.Contains(app2Dir, foundPaths);
            Assert.Contains(subDir, foundPaths);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// Property 2 (Variant): Directory scan results should have valid required fields.
    /// **Validates: Requirements 1.4**
    /// </summary>
    [Fact]
    public async Task ScanDirectory_ResultsHaveRequiredFields()
    {
        // Arrange
        var scanner = new SoftwareScanner();
        var tempDir = Path.Combine(Path.GetTempPath(), $"ScanTest_{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(tempDir);
            var appDir = Path.Combine(tempDir, "TestApp");
            Directory.CreateDirectory(appDir);
            File.WriteAllText(Path.Combine(appDir, "test.exe"), "");

            // Act
            var entries = await scanner.ScanDirectoryAsync(tempDir);

            // Assert: All entries should have required fields
            foreach (var entry in entries)
            {
                Assert.False(string.IsNullOrWhiteSpace(entry.Id),
                    "Directory scan entry has empty Id");
                Assert.False(string.IsNullOrWhiteSpace(entry.Name),
                    "Directory scan entry has empty Name");
                Assert.False(string.IsNullOrWhiteSpace(entry.InstallPath),
                    "Directory scan entry has empty InstallPath");
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// Property 2 (Variant): Empty directory should return empty results.
    /// **Validates: Requirements 1.4**
    /// </summary>
    [Fact]
    public async Task ScanDirectory_EmptyDirectory_ReturnsEmpty()
    {
        // Arrange
        var scanner = new SoftwareScanner();
        var tempDir = Path.Combine(Path.GetTempPath(), $"ScanTest_{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(tempDir);

            // Act
            var entries = await scanner.ScanDirectoryAsync(tempDir);

            // Assert: Empty directory should return no entries
            Assert.Empty(entries);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// Property 2 (Variant): Non-existent directory should throw.
    /// **Validates: Requirements 1.4**
    /// </summary>
    [Fact]
    public async Task ScanDirectory_NonExistentDirectory_Throws()
    {
        // Arrange
        var scanner = new SoftwareScanner();
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"NonExistent_{Guid.NewGuid():N}");

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => scanner.ScanDirectoryAsync(nonExistentPath));
    }

    #endregion

    #region Progress Event Tests

    /// <summary>
    /// Verifies that progress events are raised during scanning.
    /// **Validates: Requirements 1.3**
    /// </summary>
    [Fact]
    public async Task ScanInstalledSoftware_RaisesProgressEvents()
    {
        // Arrange
        var scanner = new SoftwareScanner();
        var progressEvents = new List<ScanProgressEventArgs>();
        scanner.ProgressChanged += (sender, e) => progressEvents.Add(e);

        // Act
        await scanner.ScanInstalledSoftwareAsync();

        // Assert: Should have at least start and end progress events
        Assert.NotEmpty(progressEvents);
        Assert.Contains(progressEvents, e => e.ProgressPercentage == 0);
        Assert.Contains(progressEvents, e => e.ProgressPercentage == 100);
    }

    /// <summary>
    /// Verifies that progress percentage is always in valid range.
    /// **Validates: Requirements 1.3**
    /// </summary>
    [Fact]
    public async Task ScanInstalledSoftware_ProgressPercentageInValidRange()
    {
        // Arrange
        var scanner = new SoftwareScanner();
        var progressEvents = new List<ScanProgressEventArgs>();
        scanner.ProgressChanged += (sender, e) => progressEvents.Add(e);

        // Act
        await scanner.ScanInstalledSoftwareAsync();

        // Assert: All progress percentages should be 0-100
        foreach (var e in progressEvents)
        {
            Assert.InRange(e.ProgressPercentage, 0, 100);
        }
    }

    #endregion

    #region Cancellation Tests

    /// <summary>
    /// Verifies that scanning can be cancelled.
    /// **Validates: Requirements 1.3**
    /// </summary>
    [Fact]
    public async Task ScanInstalledSoftware_CanBeCancelled()
    {
        // Arrange
        var scanner = new SoftwareScanner();
        using var cts = new CancellationTokenSource();

        // Cancel immediately
        cts.Cancel();

        // Act & Assert - TaskCanceledException is a subclass of OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => scanner.ScanInstalledSoftwareAsync(cts.Token));
    }

    /// <summary>
    /// Verifies that directory scanning can be cancelled.
    /// **Validates: Requirements 1.4**
    /// </summary>
    [Fact]
    public async Task ScanDirectory_CanBeCancelled()
    {
        // Arrange
        var scanner = new SoftwareScanner();
        var tempDir = Path.Combine(Path.GetTempPath(), $"ScanTest_{Guid.NewGuid():N}");
        using var cts = new CancellationTokenSource();

        try
        {
            Directory.CreateDirectory(tempDir);
            cts.Cancel();

            // Act & Assert - TaskCanceledException is a subclass of OperationCanceledException
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => scanner.ScanDirectoryAsync(tempDir, cts.Token));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    #endregion
}
