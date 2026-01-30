using FsCheck;
using FsCheck.Xunit;
using WindowsSoftwareOrganizer.Infrastructure.Services;

namespace WindowsSoftwareOrganizer.Tests.Properties;

/// <summary>
/// Property-based tests for SizeAnalyzer.
/// **Feature: ai-file-manager**
/// </summary>
public class SizeAnalyzerPropertyTests
{
    private readonly SizeAnalyzer _analyzer;

    public SizeAnalyzerPropertyTests()
    {
        var fileSystemService = new FileSystemService();
        _analyzer = new SizeAnalyzer(fileSystemService);
    }

    #region Property 3: 大小分析正确性

    /// <summary>
    /// **Property 3**: TotalSize 等于所有子项 Size 之和
    /// **Validates: Requirements 3.1, 3.3, 3.4**
    /// </summary>
    [Property(MaxTest = 20)]
    public Property TotalSize_EqualsSum_OfChildSizes()
    {
        return Prop.ForAll(
            Gen.Choose(1, 5).ToArbitrary(),
            fileCount =>
            {
                var tempDir = Path.Combine(Path.GetTempPath(), $"SizeProp_{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    // Create files with random sizes
                    var random = new System.Random();
                    long expectedTotal = 0;
                    for (int i = 0; i < fileCount; i++)
                    {
                        var size = random.Next(10, 1000);
                        var filePath = Path.Combine(tempDir, $"file{i}.txt");
                        File.WriteAllBytes(filePath, new byte[size]);
                        expectedTotal += size;
                    }

                    var result = _analyzer.AnalyzeAsync(tempDir).Result;

                    // TotalSize should equal sum of all file sizes
                    var itemsSum = result.Items.Sum(i => i.Size);

                    return (result.TotalSize == expectedTotal && result.TotalSize == itemsSum)
                        .Label($"TotalSize ({result.TotalSize}) should equal expected ({expectedTotal}) and items sum ({itemsSum})");
                }
                finally
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            });
    }

    /// <summary>
    /// **Property 3**: 所有子项的 Percentage 之和约等于 100%（允许浮点误差）
    /// **Validates: Requirements 3.1, 3.3, 3.4**
    /// </summary>
    [Fact]
    public async Task Percentages_SumTo100()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SizeProp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create files
            await File.WriteAllBytesAsync(Path.Combine(tempDir, "file1.txt"), new byte[100]);
            await File.WriteAllBytesAsync(Path.Combine(tempDir, "file2.txt"), new byte[200]);
            await File.WriteAllBytesAsync(Path.Combine(tempDir, "file3.txt"), new byte[300]);

            var result = await _analyzer.AnalyzeAsync(tempDir);

            var percentageSum = result.Items.Sum(i => i.Percentage);

            // Allow for floating point errors
            Assert.True(Math.Abs(percentageSum - 100.0) < 0.1,
                $"Percentage sum ({percentageSum}) should be approximately 100%");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    /// **Property 3**: 按大小降序排列时，每个子项的 Size 应该大于或等于其后续子项
    /// **Validates: Requirements 3.1, 3.3, 3.4**
    /// </summary>
    [Property(MaxTest = 20)]
    public Property Items_AreOrderedBySize_Descending()
    {
        return Prop.ForAll(
            Gen.Choose(2, 6).ToArbitrary(),
            fileCount =>
            {
                var tempDir = Path.Combine(Path.GetTempPath(), $"SizeProp_{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    var random = new System.Random();
                    for (int i = 0; i < fileCount; i++)
                    {
                        var size = random.Next(10, 1000);
                        File.WriteAllBytes(Path.Combine(tempDir, $"file{i}.txt"), new byte[size]);
                    }

                    var result = _analyzer.AnalyzeAsync(tempDir).Result;

                    // Check descending order
                    var isOrdered = true;
                    for (int i = 0; i < result.Items.Count - 1; i++)
                    {
                        if (result.Items[i].Size < result.Items[i + 1].Size)
                        {
                            isOrdered = false;
                            break;
                        }
                    }

                    return isOrdered.Label("Items should be ordered by size descending");
                }
                finally
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            });
    }

    /// <summary>
    /// **Property 3**: 子目录的大小应该等于其包含的所有文件大小之和
    /// **Validates: Requirements 3.1, 3.3, 3.4**
    /// </summary>
    [Fact]
    public async Task SubdirectorySize_EqualsContainedFilesSum()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SizeProp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var subDir = Path.Combine(tempDir, "SubDir");
        Directory.CreateDirectory(subDir);

        try
        {
            // Create files in subdirectory
            await File.WriteAllBytesAsync(Path.Combine(subDir, "file1.txt"), new byte[100]);
            await File.WriteAllBytesAsync(Path.Combine(subDir, "file2.txt"), new byte[200]);
            var expectedSubDirSize = 300L;

            var result = await _analyzer.AnalyzeAsync(tempDir);

            var subDirItem = result.Items.FirstOrDefault(i => i.Name == "SubDir" && i.IsDirectory);
            Assert.NotNull(subDirItem);
            Assert.Equal(expectedSubDirSize, subDirItem.Size);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion
}
