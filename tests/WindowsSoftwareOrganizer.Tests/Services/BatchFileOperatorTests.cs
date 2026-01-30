using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.Infrastructure.Services;

namespace WindowsSoftwareOrganizer.Tests.Services;

/// <summary>
/// Unit tests for BatchFileOperator.
/// </summary>
public class BatchFileOperatorTests
{
    private readonly BatchFileOperator _operator;

    public BatchFileOperatorTests()
    {
        _operator = new BatchFileOperator();
    }

    #region MoveAsync Tests

    [Fact]
    public async Task MoveAsync_MovesFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"BatchTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var sourceFile = Path.Combine(tempDir, "source.txt");
        var destFile = Path.Combine(tempDir, "dest.txt");
        await File.WriteAllTextAsync(sourceFile, "content");

        try
        {
            var operations = new List<(string Source, string Destination)>
            {
                (sourceFile, destFile)
            };

            var result = await _operator.MoveAsync(operations);

            Assert.Equal(1, result.SuccessCount);
            Assert.False(File.Exists(sourceFile));
            Assert.True(File.Exists(destFile));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task MoveAsync_MultipleFiles_MovesAll()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"BatchTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var destDir = Path.Combine(tempDir, "Dest");
        Directory.CreateDirectory(destDir);

        var files = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            var file = Path.Combine(tempDir, $"file{i}.txt");
            await File.WriteAllTextAsync(file, $"content{i}");
            files.Add(file);
        }

        try
        {
            var operations = files.Select(f => (f, Path.Combine(destDir, Path.GetFileName(f)))).ToList();

            var result = await _operator.MoveAsync(operations);

            Assert.Equal(3, result.SuccessCount);
            Assert.Equal(0, result.FailedCount);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task MoveAsync_NonExistentSource_ReportsError()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"BatchTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var nonExistent = Path.Combine(tempDir, "nonexistent.txt");
        var destFile = Path.Combine(tempDir, "dest.txt");

        try
        {
            var operations = new List<(string Source, string Destination)>
            {
                (nonExistent, destFile)
            };

            var result = await _operator.MoveAsync(operations);

            Assert.Equal(0, result.SuccessCount);
            Assert.Equal(1, result.FailedCount);
            Assert.Single(result.Errors);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task MoveAsync_DestinationExists_WithoutOverwrite_ReportsError()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"BatchTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var sourceFile = Path.Combine(tempDir, "source.txt");
        var destFile = Path.Combine(tempDir, "dest.txt");
        await File.WriteAllTextAsync(sourceFile, "source");
        await File.WriteAllTextAsync(destFile, "dest");

        try
        {
            var operations = new List<(string Source, string Destination)>
            {
                (sourceFile, destFile)
            };

            var result = await _operator.MoveAsync(operations, overwrite: false);

            Assert.Equal(0, result.SuccessCount);
            Assert.Equal(1, result.FailedCount);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task MoveAsync_DestinationExists_WithOverwrite_Succeeds()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"BatchTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var sourceFile = Path.Combine(tempDir, "source.txt");
        var destFile = Path.Combine(tempDir, "dest.txt");
        await File.WriteAllTextAsync(sourceFile, "source content");
        await File.WriteAllTextAsync(destFile, "dest content");

        try
        {
            var operations = new List<(string Source, string Destination)>
            {
                (sourceFile, destFile)
            };

            var result = await _operator.MoveAsync(operations, overwrite: true);

            Assert.Equal(1, result.SuccessCount);
            Assert.Equal("source content", await File.ReadAllTextAsync(destFile));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region CopyAsync Tests

    [Fact]
    public async Task CopyAsync_CopiesFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"BatchTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var sourceFile = Path.Combine(tempDir, "source.txt");
        var destFile = Path.Combine(tempDir, "dest.txt");
        await File.WriteAllTextAsync(sourceFile, "content");

        try
        {
            var operations = new List<(string Source, string Destination)>
            {
                (sourceFile, destFile)
            };

            var result = await _operator.CopyAsync(operations);

            Assert.Equal(1, result.SuccessCount);
            Assert.True(File.Exists(sourceFile));
            Assert.True(File.Exists(destFile));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task CopyAsync_PreservesSourceFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"BatchTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var sourceFile = Path.Combine(tempDir, "source.txt");
        var destFile = Path.Combine(tempDir, "dest.txt");
        await File.WriteAllTextAsync(sourceFile, "content");

        try
        {
            var operations = new List<(string Source, string Destination)>
            {
                (sourceFile, destFile)
            };

            await _operator.CopyAsync(operations);

            Assert.True(File.Exists(sourceFile));
            Assert.Equal("content", await File.ReadAllTextAsync(sourceFile));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_DeletesFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"BatchTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var file1 = Path.Combine(tempDir, "file1.txt");
        var file2 = Path.Combine(tempDir, "file2.txt");
        await File.WriteAllTextAsync(file1, "content1");
        await File.WriteAllTextAsync(file2, "content2");

        try
        {
            var paths = new List<string> { file1, file2 };

            var result = await _operator.DeleteAsync(paths, useRecycleBin: false);

            Assert.Equal(2, result.SuccessCount);
            Assert.False(File.Exists(file1));
            Assert.False(File.Exists(file2));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task DeleteAsync_NonExistentFile_ReportsError()
    {
        var nonExistent = Path.Combine(Path.GetTempPath(), $"NonExistent_{Guid.NewGuid():N}.txt");

        var paths = new List<string> { nonExistent };

        var result = await _operator.DeleteAsync(paths, useRecycleBin: false);

        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
    }

    [Fact]
    public async Task DeleteAsync_DeletesDirectories()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"BatchTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var subDir = Path.Combine(tempDir, "SubDir");
        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(Path.Combine(subDir, "file.txt"), "content");

        try
        {
            var paths = new List<string> { subDir };

            var result = await _operator.DeleteAsync(paths, useRecycleBin: false);

            Assert.Equal(1, result.SuccessCount);
            Assert.False(Directory.Exists(subDir));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region RenameAsync Tests

    [Fact]
    public async Task RenameAsync_RenamesFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"BatchTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var originalFile = Path.Combine(tempDir, "original.txt");
        await File.WriteAllTextAsync(originalFile, "content");

        try
        {
            var operations = new List<RenameOperation>
            {
                new RenameOperation { SourcePath = originalFile, NewName = "renamed.txt" }
            };

            var result = await _operator.RenameAsync(operations);

            Assert.Equal(1, result.SuccessCount);
            Assert.False(File.Exists(originalFile));
            Assert.True(File.Exists(Path.Combine(tempDir, "renamed.txt")));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region UndoLastOperationAsync Tests

    [Fact]
    public async Task UndoLastOperationAsync_UndoesMove()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"BatchTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var sourceFile = Path.Combine(tempDir, "source.txt");
        var destFile = Path.Combine(tempDir, "dest.txt");
        await File.WriteAllTextAsync(sourceFile, "content");

        try
        {
            var operations = new List<(string Source, string Destination)>
            {
                (sourceFile, destFile)
            };

            await _operator.MoveAsync(operations);
            Assert.True(_operator.CanUndo);

            var undoResult = await _operator.UndoLastOperationAsync();

            Assert.True(undoResult);
            Assert.True(File.Exists(sourceFile));
            Assert.False(File.Exists(destFile));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task UndoLastOperationAsync_NoOperations_ReturnsFalse()
    {
        var result = await _operator.UndoLastOperationAsync();
        Assert.False(result);
    }

    [Fact]
    public void CanUndo_NoOperations_ReturnsFalse()
    {
        Assert.False(_operator.CanUndo);
    }

    #endregion

    #region Progress Reporting Tests

    [Fact]
    public async Task MoveAsync_ReportsProgress()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"BatchTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var destDir = Path.Combine(tempDir, "Dest");
        Directory.CreateDirectory(destDir);

        for (int i = 0; i < 5; i++)
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, $"file{i}.txt"), $"content{i}");
        }

        try
        {
            var progressReports = new List<BatchOperationProgress>();
            var progress = new Progress<BatchOperationProgress>(p => progressReports.Add(p));

            var operations = Directory.GetFiles(tempDir, "*.txt")
                .Select(f => (f, Path.Combine(destDir, Path.GetFileName(f))))
                .ToList();

            await _operator.MoveAsync(operations, progress: progress);

            // Progress should have been reported
            Assert.NotEmpty(progressReports);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region OrganizeByTypeAsync Tests

    [Fact]
    public async Task OrganizeByTypeAsync_OrganizesFilesByType()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"BatchTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "doc.pdf"), "pdf");
        await File.WriteAllBytesAsync(Path.Combine(tempDir, "image.png"), new byte[100]);

        try
        {
            var result = await _operator.OrganizeByTypeAsync(tempDir);

            Assert.True(result.SuccessCount > 0);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task OrganizeByTypeAsync_Preview_DoesNotMoveFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"BatchTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var pdfFile = Path.Combine(tempDir, "doc.pdf");
        await File.WriteAllTextAsync(pdfFile, "pdf");

        try
        {
            var result = await _operator.OrganizeByTypeAsync(tempDir, preview: true);

            Assert.True(File.Exists(pdfFile)); // File should still be in original location
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion
}
