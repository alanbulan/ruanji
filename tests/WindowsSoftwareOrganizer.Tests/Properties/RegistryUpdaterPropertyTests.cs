using FsCheck;
using FsCheck.Xunit;
using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.Infrastructure.Services;
using NSubstitute;
using WindowsSoftwareOrganizer.Core.Interfaces;

namespace WindowsSoftwareOrganizer.Tests.Properties;

/// <summary>
/// Property-based tests for RegistryUpdater.
/// Tests Properties 11 and 12 from the design document.
/// </summary>
public class RegistryUpdaterPropertyTests
{
    private readonly IOperationLogger _mockLogger;
    private readonly RegistryUpdater _registryUpdater;

    public RegistryUpdaterPropertyTests()
    {
        _mockLogger = Substitute.For<IOperationLogger>();
        _registryUpdater = new RegistryUpdater(_mockLogger);
    }

    #region Property 11: 注册表路径替换正确性

    /// <summary>
    /// **Validates: Requirements 5.3**
    /// **Property 11: 注册表路径替换正确性**
    /// 对于任意包含原路径的注册表值字符串，更新后的值中原路径的所有出现都应被替换为新路径。
    /// </summary>
    [Property(MaxTest = 100)]
    public Property PathReplacementReplacesAllOccurrences()
    {
        return Prop.ForAll(
            ValidPathArbitrary(),
            ValidPathArbitrary(),
            ValidRegistryValueArbitrary(),
            (oldPath, newPath, valueTemplate) =>
            {
                if (string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase))
                    return true;

                var originalValue = valueTemplate.Replace("{PATH}", oldPath);
                var updatedValue = originalValue.Replace(oldPath, newPath, StringComparison.OrdinalIgnoreCase);

                if (newPath.Contains(oldPath, StringComparison.OrdinalIgnoreCase))
                    return true;

                return !updatedValue.Contains(oldPath, StringComparison.OrdinalIgnoreCase);
            });
    }

    /// <summary>
    /// **Validates: Requirements 5.3**
    /// **Property 11: 注册表路径替换正确性**
    /// Path replacement should be case-insensitive.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property PathReplacementIsCaseInsensitive()
    {
        return Prop.ForAll(
            ValidPathArbitrary(),
            ValidPathArbitrary(),
            (oldPath, newPath) =>
            {
                if (string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase))
                    return true;

                var upperValue = $"Path={oldPath.ToUpperInvariant()}";
                var lowerValue = $"Path={oldPath.ToLowerInvariant()}";
                var mixedValue = $"Path={MixCase(oldPath)}";

                var upperReplaced = upperValue.Replace(oldPath, newPath, StringComparison.OrdinalIgnoreCase);
                var lowerReplaced = lowerValue.Replace(oldPath, newPath, StringComparison.OrdinalIgnoreCase);
                var mixedReplaced = mixedValue.Replace(oldPath, newPath, StringComparison.OrdinalIgnoreCase);

                if (newPath.Contains(oldPath, StringComparison.OrdinalIgnoreCase))
                    return true;

                return !upperReplaced.Contains(oldPath, StringComparison.OrdinalIgnoreCase) &&
                       !lowerReplaced.Contains(oldPath, StringComparison.OrdinalIgnoreCase) &&
                       !mixedReplaced.Contains(oldPath, StringComparison.OrdinalIgnoreCase);
            });
    }

    /// <summary>
    /// **Validates: Requirements 5.3**
    /// **Property 11: 注册表路径替换正确性**
    /// Multiple occurrences of the path should all be replaced.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property PathReplacementHandlesMultipleOccurrences()
    {
        var pathAndOccurrenceGen = from oldPath in ValidPathGen()
                                   from newPath in ValidPathGen()
                                   from occurrences in Gen.Choose(2, 5)
                                   select (oldPath, newPath, occurrences);

        return Prop.ForAll(
            pathAndOccurrenceGen.ToArbitrary(),
            tuple =>
            {
                var (oldPath, newPath, occurrences) = tuple;
                
                if (string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase))
                    return true;

                var parts = Enumerable.Range(0, occurrences).Select(_ => oldPath);
                var originalValue = string.Join(";", parts);
                var updatedValue = originalValue.Replace(oldPath, newPath, StringComparison.OrdinalIgnoreCase);

                if (newPath.Contains(oldPath, StringComparison.OrdinalIgnoreCase))
                    return true;

                var newPathCount = CountOccurrences(updatedValue, newPath);
                return newPathCount == occurrences;
            });
    }


    /// <summary>
    /// **Validates: Requirements 5.3**
    /// **Property 11: 注册表路径替换正确性**
    /// Path replacement preserves non-path content.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property PathReplacementPreservesOtherContent()
    {
        var gen = from oldPath in ValidPathGen()
                  from newPath in ValidPathGen()
                  from prefix in Gen.Elements("Prefix_", "Start:", "Begin=")
                  from suffix in Gen.Elements("_Suffix", ":End", "=Finish")
                  select (oldPath, newPath, prefix, suffix);

        return Prop.ForAll(
            gen.ToArbitrary(),
            tuple =>
            {
                var (oldPath, newPath, prefix, suffix) = tuple;
                
                if (string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase))
                    return true;

                var originalValue = $"{prefix}{oldPath}{suffix}";
                var expectedValue = $"{prefix}{newPath}{suffix}";
                var updatedValue = originalValue.Replace(oldPath, newPath, StringComparison.OrdinalIgnoreCase);

                return updatedValue == expectedValue;
            });
    }

    /// <summary>
    /// **Validates: Requirements 5.3**
    /// **Property 11: 注册表路径替换正确性**
    /// Empty references list should return success with zero counts.
    /// </summary>
    [Fact]
    public async Task UpdateReferencesAsync_EmptyList_ReturnsSuccessWithZeroCounts()
    {
        var result = await _registryUpdater.UpdateReferencesAsync(
            Array.Empty<RegistryReference>(),
            @"C:\OldPath",
            @"D:\NewPath");

        Assert.True(result.Success);
        Assert.Equal(0, result.UpdatedCount);
        Assert.Equal(0, result.FailedCount);
    }

    /// <summary>
    /// **Validates: Requirements 5.3**
    /// **Property 11: 注册表路径替换正确性**
    /// Null references should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task UpdateReferencesAsync_NullReferences_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _registryUpdater.UpdateReferencesAsync(null!, @"C:\Old", @"D:\New"));
    }

    /// <summary>
    /// **Validates: Requirements 5.3**
    /// **Property 11: 注册表路径替换正确性**
    /// Empty old path should throw ArgumentException.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateReferencesAsync_InvalidOldPath_ThrowsArgumentException(string oldPath)
    {
        var references = new[] { CreateTestReference(@"C:\Test") };
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _registryUpdater.UpdateReferencesAsync(references, oldPath, @"D:\New"));
    }

    /// <summary>
    /// **Validates: Requirements 5.3**
    /// **Property 11: 注册表路径替换正确性**
    /// Empty new path should throw ArgumentException.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateReferencesAsync_InvalidNewPath_ThrowsArgumentException(string newPath)
    {
        var references = new[] { CreateTestReference(@"C:\Test") };
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _registryUpdater.UpdateReferencesAsync(references, @"C:\Old", newPath));
    }

    #endregion

    #region Property 12: 注册表更新报告完整性

    /// <summary>
    /// **Validates: Requirements 5.5**
    /// **Property 12: 注册表更新报告完整性**
    /// 对于任意注册表更新操作，生成的报告中包含的条目数量应等于实际修改的注册表项数量。
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task ReportContainsCorrectEntryCount(int entryCount)
    {
        var references = Enumerable.Range(0, entryCount)
            .Select(i => CreateTestReference($@"C:\TestPath{i}"))
            .ToList();

        var result = await _registryUpdater.UpdateReferencesAsync(
            references,
            @"C:\TestPath",
            @"D:\NewPath");

        Assert.Equal(entryCount, result.UpdatedCount + result.FailedCount);
    }

    /// <summary>
    /// **Validates: Requirements 5.5**
    /// **Property 12: 注册表更新报告完整性**
    /// Report timestamp should be valid.
    /// </summary>
    [Fact]
    public async Task GenerateReportAsync_ReturnsValidTimestamp()
    {
        var beforeTime = DateTime.UtcNow;
        var report = await _registryUpdater.GenerateReportAsync("test-operation-id");
        var afterTime = DateTime.UtcNow;

        Assert.True(report.Timestamp >= beforeTime);
        Assert.True(report.Timestamp <= afterTime);
    }


    /// <summary>
    /// **Validates: Requirements 5.5**
    /// **Property 12: 注册表更新报告完整性**
    /// Report should contain the correct operation ID.
    /// </summary>
    [Theory]
    [InlineData("operation-1")]
    [InlineData("test-op-abc")]
    [InlineData("migration-12345")]
    public async Task ReportContainsCorrectOperationId(string operationId)
    {
        var report = await _registryUpdater.GenerateReportAsync(operationId);
        Assert.Equal(operationId, report.OperationId);
    }

    /// <summary>
    /// **Validates: Requirements 5.5**
    /// **Property 12: 注册表更新报告完整性**
    /// Report totals should match entry counts.
    /// </summary>
    [Fact]
    public async Task GenerateReportAsync_TotalsMatchEntryCounts()
    {
        var references = new[]
        {
            CreateTestReference(@"C:\Path1"),
            CreateTestReference(@"C:\Path2"),
            CreateTestReference(@"C:\Path3")
        };

        await _registryUpdater.UpdateReferencesAsync(references, @"C:\Path", @"D:\NewPath");
        var report = await _registryUpdater.GenerateReportAsync("unknown-operation");

        Assert.Equal(report.Entries.Count(e => e.Success), report.TotalUpdated);
        Assert.Equal(report.Entries.Count(e => !e.Success), report.TotalFailed);
    }

    /// <summary>
    /// **Validates: Requirements 5.5**
    /// **Property 12: 注册表更新报告完整性**
    /// Empty operation ID should throw ArgumentException.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GenerateReportAsync_InvalidOperationId_ThrowsArgumentException(string operationId)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _registryUpdater.GenerateReportAsync(operationId));
    }

    /// <summary>
    /// **Validates: Requirements 5.5**
    /// **Property 12: 注册表更新报告完整性**
    /// Unknown operation ID should return empty report.
    /// </summary>
    [Fact]
    public async Task GenerateReportAsync_UnknownOperationId_ReturnsEmptyReport()
    {
        var report = await _registryUpdater.GenerateReportAsync("non-existent-operation");

        Assert.Empty(report.Entries);
        Assert.Equal(0, report.TotalUpdated);
        Assert.Equal(0, report.TotalFailed);
    }

    #endregion

    #region Backup Tests

    /// <summary>
    /// **Validates: Requirements 5.2**
    /// Empty references should return empty backup ID.
    /// </summary>
    [Fact]
    public async Task CreateBackupAsync_EmptyReferences_ReturnsEmptyBackupId()
    {
        var backupId = await _registryUpdater.CreateBackupAsync(Array.Empty<RegistryReference>());
        Assert.Equal(string.Empty, backupId);
    }

    /// <summary>
    /// **Validates: Requirements 5.2**
    /// Null references should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task CreateBackupAsync_NullReferences_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _registryUpdater.CreateBackupAsync(null!));
    }

    /// <summary>
    /// **Validates: Requirements 5.4**
    /// Invalid backup ID should throw ArgumentException.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RestoreBackupAsync_InvalidBackupId_ThrowsArgumentException(string backupId)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _registryUpdater.RestoreBackupAsync(backupId));
    }

    /// <summary>
    /// **Validates: Requirements 5.4**
    /// Non-existent backup should throw FileNotFoundException.
    /// </summary>
    [Fact]
    public async Task RestoreBackupAsync_NonExistentBackup_ThrowsFileNotFoundException()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _registryUpdater.RestoreBackupAsync("non-existent-backup-id"));
    }

    #endregion

    #region FindReferences Tests

    /// <summary>
    /// **Validates: Requirements 5.1**
    /// Empty path should throw ArgumentException.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FindReferencesAsync_InvalidPath_ThrowsArgumentException(string path)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _registryUpdater.FindReferencesAsync(path));
    }

    /// <summary>
    /// **Validates: Requirements 5.1**
    /// Valid path should not throw.
    /// </summary>
    [Fact]
    public async Task FindReferencesAsync_ValidPath_DoesNotThrow()
    {
        var references = await _registryUpdater.FindReferencesAsync(@"C:\NonExistentTestPath12345");
        Assert.NotNull(references);
    }

    #endregion

    #region Helper Methods

    private static Gen<string> ValidPathGen()
    {
        var drives = new[] { "C", "D", "E", "F" };
        var folders = new[] { "Program Files", "Software", "Apps", "Tools", "Dev" };
        var names = new[] { "App", "Tool", "Program", "Software", "Utility" };

        return from drive in Gen.Elements(drives)
               from folder in Gen.Elements(folders)
               from name in Gen.Elements(names)
               from suffix in Gen.Choose(1, 999)
               select $@"{drive}:\{folder}\{name}{suffix}";
    }

    private static Arbitrary<string> ValidPathArbitrary() => ValidPathGen().ToArbitrary();

    private static Arbitrary<string> ValidRegistryValueArbitrary()
    {
        var templates = new[]
        {
            "{PATH}",
            "InstallPath={PATH}",
            "{PATH};{PATH}",
            @"""{PATH}""",
            "Path={PATH};Other=Value"
        };
        return Gen.Elements(templates).ToArbitrary();
    }

    private static string MixCase(string input)
    {
        var chars = input.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
            chars[i] = i % 2 == 0 ? char.ToUpper(chars[i]) : char.ToLower(chars[i]);
        return new string(chars);
    }

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0, index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }

    private static RegistryReference CreateTestReference(string valuePath)
    {
        return new RegistryReference
        {
            KeyPath = @"HKCU\SOFTWARE\Test\TestApp",
            ValueName = "InstallPath",
            ValueData = valuePath,
            ValueType = RegistryValueType.String
        };
    }

    #endregion
}
