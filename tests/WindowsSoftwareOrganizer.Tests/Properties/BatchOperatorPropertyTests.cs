using FsCheck;
using FsCheck.Xunit;
using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.Infrastructure.Services;

namespace WindowsSoftwareOrganizer.Tests.Properties;

/// <summary>
/// Property-based tests for BatchFileOperator.
/// **Feature: ai-file-manager**
/// </summary>
public class BatchOperatorPropertyTests
{
    private readonly BatchFileOperator _operator;

    public BatchOperatorPropertyTests()
    {
        _operator = new BatchFileOperator();
    }

    #region Property 8: 批量操作正确性

    /// <summary>
    /// **Property 8**: SuccessCount + FailedCount = TotalItems
    /// **Validates: Requirements 6.2, 6.7, 6.8**
    /// </summary>
    [Property(MaxTest = 20)]
    public Property BatchResult_CountsAddUp()
    {
        return Prop.ForAll(
            Gen.Choose(1, 5).ToArbitrary(),
            fileCount =>
            {
                var tempDir = Path.Combine(Path.GetTempPath(), $"BatchProp_{Guid.NewGuid():N}");
                var destDir = Path.Combine(Path.GetTempPath(), $"BatchDest_{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempDir);
                Directory.CreateDirectory(destDir);

                try
                {
                    var operations = new List<(string Source, string Destination)>();
                    for (int i = 0; i < fileCount; i++)
                    {
                        var filePath = Path.Combine(tempDir, $"file{i}.txt");
                        File.WriteAllText(filePath, "content");
                        operations.Add((filePath, Path.Combine(destDir, $"file{i}.txt")));
                    }

                    var result = _operator.CopyAsync(operations).Result;

                    return (result.SuccessCount + result.FailedCount == result.TotalItems)
                        .Label($"Success ({result.SuccessCount}) + Failed ({result.FailedCount}) should equal Total ({result.TotalItems})");
                }
                finally
                {
                    Directory.Delete(tempDir, recursive: true);
                    Directory.Delete(destDir, recursive: true);
                }
            });
    }

    /// <summary>
    /// **Property 8**: Errors 列表的长度等于 FailedCount
    /// **Validates: Requirements 6.2, 6.7, 6.8**
    /// </summary>
    [Fact]
    public async Task BatchResult_ErrorsCount_EqualsFailedCount()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"BatchProp_{Guid.NewGuid():N}");
        var destDir = Path.Combine(Path.GetTempPath(), $"BatchDest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(destDir);

        try
        {
            // Create some valid files and some invalid paths
            var validFile = Path.Combine(tempDir, "valid.txt");
            File.WriteAllText(validFile, "content");

            var operations = new List<(string Source, string Destination)>
            {
                (validFile, Path.Combine(destDir, "valid.txt")),
                (Path.Combine(tempDir, "nonexistent.txt"), Path.Combine(destDir, "nonexistent.txt")) // This will fail
            };

            var result = await _operator.CopyAsync(operations);

            Assert.Equal(result.FailedCount, result.Errors.Count);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
            Directory.Delete(destDir, recursive: true);
        }
    }

    /// <summary>
    /// **Property 8**: 对于包含无效路径的操作，应该继续处理其他有效路径
    /// **Validates: Requirements 6.2, 6.7, 6.8**
    /// </summary>
    [Fact]
    public async Task BatchOperation_ContinuesOnError()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"BatchProp_{Guid.NewGuid():N}");
        var destDir = Path.Combine(Path.GetTempPath(), $"BatchDest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(destDir);

        try
        {
            // Create valid files
            var file1 = Path.Combine(tempDir, "file1.txt");
            var file2 = Path.Combine(tempDir, "file2.txt");
            File.WriteAllText(file1, "content1");
            File.WriteAllText(file2, "content2");

            var operations = new List<(string Source, string Destination)>
            {
                (file1, Path.Combine(destDir, "file1.txt")),
                (Path.Combine(tempDir, "nonexistent.txt"), Path.Combine(destDir, "nonexistent.txt")), // Invalid
                (file2, Path.Combine(destDir, "file2.txt"))
            };

            var result = await _operator.CopyAsync(operations);

            // Should have processed all items
            Assert.Equal(3, result.TotalItems);
            // Should have succeeded for valid files
            Assert.Equal(2, result.SuccessCount);
            // Should have failed for invalid file
            Assert.Equal(1, result.FailedCount);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
            Directory.Delete(destDir, recursive: true);
        }
    }

    /// <summary>
    /// **Property 8**: 批量移动后，源文件不存在，目标文件存在
    /// **Validates: Requirements 6.2, 6.7, 6.8**
    /// </summary>
    [Property(MaxTest = 10)]
    public Property BatchMove_SourceRemoved_DestinationExists()
    {
        return Prop.ForAll(
            Gen.Choose(1, 3).ToArbitrary(),
            fileCount =>
            {
                var tempDir = Path.Combine(Path.GetTempPath(), $"BatchProp_{Guid.NewGuid():N}");
                var destDir = Path.Combine(Path.GetTempPath(), $"BatchDest_{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempDir);
                Directory.CreateDirectory(destDir);

                try
                {
                    var operations = new List<(string Source, string Destination)>();
                    var sourceFiles = new List<string>();
                    for (int i = 0; i < fileCount; i++)
                    {
                        var filePath = Path.Combine(tempDir, $"file{i}.txt");
                        File.WriteAllText(filePath, $"content{i}");
                        sourceFiles.Add(filePath);
                        operations.Add((filePath, Path.Combine(destDir, $"file{i}.txt")));
                    }

                    var result = _operator.MoveAsync(operations).Result;

                    // Check all files were moved
                    var allSourcesRemoved = sourceFiles.All(f => !File.Exists(f));
                    var allDestinationsExist = operations.All(op => File.Exists(op.Destination));

                    return (result.SuccessCount == fileCount && allSourcesRemoved && allDestinationsExist)
                        .Label("All files should be moved successfully");
                }
                finally
                {
                    if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
                    if (Directory.Exists(destDir)) Directory.Delete(destDir, recursive: true);
                }
            });
    }

    /// <summary>
    /// **Property 8**: 批量复制后，源文件和目标文件都存在
    /// **Validates: Requirements 6.2, 6.7, 6.8**
    /// </summary>
    [Property(MaxTest = 10)]
    public Property BatchCopy_BothSourceAndDestinationExist()
    {
        return Prop.ForAll(
            Gen.Choose(1, 3).ToArbitrary(),
            fileCount =>
            {
                var tempDir = Path.Combine(Path.GetTempPath(), $"BatchProp_{Guid.NewGuid():N}");
                var destDir = Path.Combine(Path.GetTempPath(), $"BatchDest_{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempDir);
                Directory.CreateDirectory(destDir);

                try
                {
                    var operations = new List<(string Source, string Destination)>();
                    for (int i = 0; i < fileCount; i++)
                    {
                        var filePath = Path.Combine(tempDir, $"file{i}.txt");
                        File.WriteAllText(filePath, $"content{i}");
                        operations.Add((filePath, Path.Combine(destDir, $"file{i}.txt")));
                    }

                    var result = _operator.CopyAsync(operations).Result;

                    // Check all files still exist at source and destination
                    var allSourcesExist = operations.All(op => File.Exists(op.Source));
                    var allDestinationsExist = operations.All(op => File.Exists(op.Destination));

                    return (result.SuccessCount == fileCount && allSourcesExist && allDestinationsExist)
                        .Label("All files should exist at both source and destination");
                }
                finally
                {
                    Directory.Delete(tempDir, recursive: true);
                    Directory.Delete(destDir, recursive: true);
                }
            });
    }

    /// <summary>
    /// **Property 8**: 批量删除后，文件不存在
    /// **Validates: Requirements 6.2, 6.7, 6.8**
    /// </summary>
    [Property(MaxTest = 10)]
    public Property BatchDelete_FilesRemoved()
    {
        return Prop.ForAll(
            Gen.Choose(1, 3).ToArbitrary(),
            fileCount =>
            {
                var tempDir = Path.Combine(Path.GetTempPath(), $"BatchProp_{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    var files = new List<string>();
                    for (int i = 0; i < fileCount; i++)
                    {
                        var filePath = Path.Combine(tempDir, $"file{i}.txt");
                        File.WriteAllText(filePath, $"content{i}");
                        files.Add(filePath);
                    }

                    // Delete without moving to recycle bin for test
                    var result = _operator.DeleteAsync(files, useRecycleBin: false).Result;

                    // Check all files are deleted
                    var allDeleted = files.All(f => !File.Exists(f));

                    return (result.SuccessCount == fileCount && allDeleted)
                        .Label("All files should be deleted");
                }
                finally
                {
                    if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
                }
            });
    }

    /// <summary>
    /// **Property 8**: 批量重命名后，原文件名不存在，新文件名存在
    /// **Validates: Requirements 6.2, 6.7, 6.8**
    /// </summary>
    [Fact]
    public async Task BatchRename_OldNameRemoved_NewNameExists()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"BatchProp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var oldPath = Path.Combine(tempDir, "old_name.txt");
            File.WriteAllText(oldPath, "content");

            var operations = new List<RenameOperation>
            {
                new RenameOperation { SourcePath = oldPath, NewName = "new_name.txt" }
            };

            var result = await _operator.RenameAsync(operations);

            Assert.Equal(1, result.SuccessCount);
            Assert.False(File.Exists(oldPath));
            Assert.True(File.Exists(Path.Combine(tempDir, "new_name.txt")));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion
}
