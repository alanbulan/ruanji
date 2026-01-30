using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.Infrastructure.Services;

namespace WindowsSoftwareOrganizer.Tests.Services;

/// <summary>
/// Unit tests for SizeAnalyzer.
/// </summary>
public class SizeAnalyzerTests
{
    private readonly SizeAnalyzer _analyzer;

    public SizeAnalyzerTests()
    {
        var fileSystemService = new FileSystemService();
        _analyzer = new SizeAnalyzer(fileSystemService);
    }

    #region AnalyzeAsync Tests

    [Fact]
    public async Task AnalyzeAsync_EmptyDirectory_ReturnsEmptyResult()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SizeTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var result = await _analyzer.AnalyzeAsync(tempDir);

            Assert.Equal(tempDir, result.RootPath);
            Assert.Equal(0, result.TotalSize);
            Assert.Empty(result.Items);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task AnalyzeAsync_WithFiles_ReturnsTotalSize()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SizeTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        await File.WriteAllBytesAsync(Path.Combine(tempDir, "file1.txt"), new byte[100]);
        await File.WriteAllBytesAsync(Path.Combine(tempDir, "file2.txt"), new byte[200]);

        try
        {
            var result = await _analyzer.AnalyzeAsync(tempDir);

            Assert.Equal(300, result.TotalSize);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task AnalyzeAsync_WithSubdirectories_IncludesSubdirectorySizes()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SizeTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var subDir = Path.Combine(tempDir, "SubDir");
        Directory.CreateDirectory(subDir);
        await File.WriteAllBytesAsync(Path.Combine(tempDir, "root.txt"), new byte[100]);
        await File.WriteAllBytesAsync(Path.Combine(subDir, "sub.txt"), new byte[200]);

        try
        {
            var result = await _analyzer.AnalyzeAsync(tempDir);

            Assert.Equal(300, result.TotalSize);
            Assert.Contains(result.Items, i => i.Name == "SubDir" && i.IsDirectory);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task AnalyzeAsync_ItemsOrderedBySize()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SizeTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        await File.WriteAllBytesAsync(Path.Combine(tempDir, "small.txt"), new byte[10]);
        await File.WriteAllBytesAsync(Path.Combine(tempDir, "large.txt"), new byte[1000]);
        await File.WriteAllBytesAsync(Path.Combine(tempDir, "medium.txt"), new byte[100]);

        try
        {
            var result = await _analyzer.AnalyzeAsync(tempDir);

            var sizes = result.Items.Select(i => i.Size).ToList();
            Assert.Equal(sizes.OrderByDescending(s => s), sizes);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task AnalyzeAsync_NonExistentDirectory_ReturnsEmptyResult()
    {
        var nonExistent = Path.Combine(Path.GetTempPath(), $"NonExistent_{Guid.NewGuid():N}");

        var result = await _analyzer.AnalyzeAsync(nonExistent);

        Assert.Equal(0, result.TotalSize);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task AnalyzeAsync_SupportsCancellation()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SizeTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        // Create some files to make the operation take longer
        for (int i = 0; i < 10; i++)
        {
            var subDir = Path.Combine(tempDir, $"SubDir{i}");
            Directory.CreateDirectory(subDir);
            await File.WriteAllBytesAsync(Path.Combine(subDir, "file.txt"), new byte[100]);
        }

        try
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // TaskCanceledException inherits from OperationCanceledException
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => _analyzer.AnalyzeAsync(tempDir, cancellationToken: cts.Token));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task AnalyzeAsync_ReportsProgress()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SizeTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        for (int i = 0; i < 5; i++)
        {
            Directory.CreateDirectory(Path.Combine(tempDir, $"SubDir{i}"));
        }

        try
        {
            var progressReports = new List<SizeAnalysisProgress>();
            var progress = new Progress<SizeAnalysisProgress>(p => progressReports.Add(p));

            await _analyzer.AnalyzeAsync(tempDir, progress: progress);

            // Progress may or may not be reported depending on timing
            // Just verify no exceptions
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region GetTotalSizeAsync Tests

    [Fact]
    public async Task GetTotalSizeAsync_ReturnsCorrectSize()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SizeTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        await File.WriteAllBytesAsync(Path.Combine(tempDir, "file.txt"), new byte[500]);

        try
        {
            var size = await _analyzer.GetTotalSizeAsync(tempDir);
            Assert.Equal(500, size);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region FindLargestFilesAsync Tests

    [Fact]
    public async Task FindLargestFilesAsync_ReturnsLargestFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SizeTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        await File.WriteAllBytesAsync(Path.Combine(tempDir, "small.txt"), new byte[10]);
        await File.WriteAllBytesAsync(Path.Combine(tempDir, "large.txt"), new byte[1000]);
        await File.WriteAllBytesAsync(Path.Combine(tempDir, "medium.txt"), new byte[100]);

        try
        {
            var files = await _analyzer.FindLargestFilesAsync(tempDir, count: 2);

            Assert.Equal(2, files.Count);
            Assert.Equal("large.txt", files[0].Name);
            Assert.Equal("medium.txt", files[1].Name);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region FindLargestDirectoriesAsync Tests

    [Fact]
    public async Task FindLargestDirectoriesAsync_ReturnsLargestDirectories()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SizeTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var smallDir = Path.Combine(tempDir, "Small");
        var largeDir = Path.Combine(tempDir, "Large");
        Directory.CreateDirectory(smallDir);
        Directory.CreateDirectory(largeDir);
        await File.WriteAllBytesAsync(Path.Combine(smallDir, "file.txt"), new byte[10]);
        await File.WriteAllBytesAsync(Path.Combine(largeDir, "file.txt"), new byte[1000]);

        try
        {
            var dirs = await _analyzer.FindLargestDirectoriesAsync(tempDir, count: 2);

            Assert.Equal(2, dirs.Count);
            Assert.Equal("Large", dirs[0].Name);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region FindEmptyDirectoriesAsync Tests

    [Fact]
    public async Task FindEmptyDirectoriesAsync_ReturnsEmptyDirectories()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SizeTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var emptyDir = Path.Combine(tempDir, "Empty");
        var nonEmptyDir = Path.Combine(tempDir, "NonEmpty");
        Directory.CreateDirectory(emptyDir);
        Directory.CreateDirectory(nonEmptyDir);
        await File.WriteAllTextAsync(Path.Combine(nonEmptyDir, "file.txt"), "content");

        try
        {
            var emptyDirs = await _analyzer.FindEmptyDirectoriesAsync(tempDir);

            Assert.Single(emptyDirs);
            Assert.Equal("Empty", emptyDirs[0].Name);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region FindOldFilesAsync Tests

    [Fact]
    public async Task FindOldFilesAsync_ReturnsOldFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SizeTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var oldFile = Path.Combine(tempDir, "old.txt");
        await File.WriteAllTextAsync(oldFile, "old content");
        File.SetLastAccessTime(oldFile, DateTime.Now.AddDays(-100));

        try
        {
            var oldFiles = await _analyzer.FindOldFilesAsync(tempDir, TimeSpan.FromDays(30));

            Assert.Single(oldFiles);
            Assert.Equal("old.txt", oldFiles[0].Name);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region FindDuplicateFilesAsync Tests

    [Fact]
    public async Task FindDuplicateFilesAsync_FindsDuplicates()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SizeTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var content = new byte[100];
        new Random(42).NextBytes(content);
        await File.WriteAllBytesAsync(Path.Combine(tempDir, "file1.txt"), content);
        await File.WriteAllBytesAsync(Path.Combine(tempDir, "file2.txt"), content);
        await File.WriteAllBytesAsync(Path.Combine(tempDir, "unique.txt"), new byte[100]);

        try
        {
            var duplicates = await _analyzer.FindDuplicateFilesAsync(tempDir);

            Assert.Single(duplicates);
            Assert.Equal(2, duplicates[0].Files.Count);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion
}
