using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.Infrastructure.Services;

namespace WindowsSoftwareOrganizer.Tests.Services;

/// <summary>
/// Unit tests for FileSystemService.
/// </summary>
public class FileSystemServiceTests
{
    private readonly FileSystemService _service;

    public FileSystemServiceTests()
    {
        _service = new FileSystemService();
    }

    #region GetDrivesAsync Tests

    [Fact]
    public async Task GetDrivesAsync_ReturnsAtLeastOneDrive()
    {
        var drives = await _service.GetDrivesAsync();
        Assert.NotEmpty(drives);
    }

    [Fact]
    public async Task GetDrivesAsync_DriveHasName()
    {
        var drives = await _service.GetDrivesAsync();
        Assert.All(drives, d => Assert.False(string.IsNullOrEmpty(d.Name)));
    }

    [Fact]
    public async Task GetDrivesAsync_DriveHasRootPath()
    {
        var drives = await _service.GetDrivesAsync();
        Assert.All(drives, d => Assert.False(string.IsNullOrEmpty(d.RootPath)));
    }

    [Fact]
    public async Task GetDrivesAsync_SupportsCancellation()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // TaskCanceledException inherits from OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _service.GetDrivesAsync(cts.Token));
    }

    #endregion

    #region GetDirectoriesAsync Tests

    [Fact]
    public async Task GetDirectoriesAsync_ReturnsDirectories()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"FSTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, "SubDir1"));
        Directory.CreateDirectory(Path.Combine(tempDir, "SubDir2"));

        try
        {
            var directories = await _service.GetDirectoriesAsync(tempDir);
            Assert.Equal(2, directories.Count);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task GetDirectoriesAsync_EmptyDirectory_ReturnsEmpty()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"FSTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var directories = await _service.GetDirectoriesAsync(tempDir);
            Assert.Empty(directories);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task GetDirectoriesAsync_NonExistentPath_ReturnsEmpty()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"NonExistent_{Guid.NewGuid():N}");
        var directories = await _service.GetDirectoriesAsync(nonExistentPath);
        Assert.Empty(directories);
    }

    #endregion

    #region GetFilesAsync Tests

    [Fact]
    public async Task GetFilesAsync_ReturnsFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"FSTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "file1.txt"), "content1");
        await File.WriteAllTextAsync(Path.Combine(tempDir, "file2.txt"), "content2");

        try
        {
            var files = await _service.GetFilesAsync(tempDir);
            Assert.Equal(2, files.Count);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task GetFilesAsync_EmptyDirectory_ReturnsEmpty()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"FSTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var files = await _service.GetFilesAsync(tempDir);
            Assert.Empty(files);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task GetFilesAsync_FileHasCorrectProperties()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"FSTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, "test.txt");
        await File.WriteAllTextAsync(filePath, "test content");

        try
        {
            var files = await _service.GetFilesAsync(tempDir);
            var file = files.Single();

            Assert.Equal("test.txt", file.Name);
            Assert.Equal(filePath, file.FullPath);
            Assert.Equal(".txt", file.Extension);
            Assert.True(file.Size > 0);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region GetDirectoryContentAsync Tests

    [Fact]
    public async Task GetDirectoryContentAsync_ReturnsBothFilesAndDirectories()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"FSTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, "SubDir"));
        await File.WriteAllTextAsync(Path.Combine(tempDir, "file.txt"), "content");

        try
        {
            var content = await _service.GetDirectoryContentAsync(tempDir);

            Assert.Equal(tempDir, content.Path);
            Assert.Single(content.Directories);
            Assert.Single(content.Files);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task GetDirectoryContentAsync_WithFilter_FiltersFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"FSTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "visible.txt"), "content");
        var hiddenFile = Path.Combine(tempDir, "hidden.txt");
        await File.WriteAllTextAsync(hiddenFile, "hidden");
        File.SetAttributes(hiddenFile, FileAttributes.Hidden);

        try
        {
            var filter = new FileFilterOptions { ShowHiddenFiles = false };
            var content = await _service.GetDirectoryContentAsync(tempDir, filter);

            Assert.Single(content.Files);
            Assert.Equal("visible.txt", content.Files[0].Name);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region Exists Tests

    [Fact]
    public void Exists_ExistingFile_ReturnsTrue()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            Assert.True(_service.Exists(tempFile));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Exists_ExistingDirectory_ReturnsTrue()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"FSTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            Assert.True(_service.Exists(tempDir));
        }
        finally
        {
            Directory.Delete(tempDir);
        }
    }

    [Fact]
    public void Exists_NonExistent_ReturnsFalse()
    {
        var nonExistent = Path.Combine(Path.GetTempPath(), $"NonExistent_{Guid.NewGuid():N}");
        Assert.False(_service.Exists(nonExistent));
    }

    #endregion

    #region IsDirectory Tests

    [Fact]
    public void IsDirectory_Directory_ReturnsTrue()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"FSTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            Assert.True(_service.IsDirectory(tempDir));
        }
        finally
        {
            Directory.Delete(tempDir);
        }
    }

    [Fact]
    public void IsDirectory_File_ReturnsFalse()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            Assert.False(_service.IsDirectory(tempFile));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    #endregion

    #region CreateDirectoryAsync Tests

    [Fact]
    public async Task CreateDirectoryAsync_CreatesDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"FSTest_{Guid.NewGuid():N}");

        try
        {
            var result = await _service.CreateDirectoryAsync(tempDir);

            Assert.True(result.Success);
            Assert.True(Directory.Exists(tempDir));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir);
        }
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_DeletesFile()
    {
        var tempFile = Path.GetTempFileName();

        var result = await _service.DeleteAsync(tempFile, useRecycleBin: false);

        Assert.True(result.Success);
        Assert.False(File.Exists(tempFile));
    }

    [Fact]
    public async Task DeleteAsync_NonExistent_ReturnsFalse()
    {
        var nonExistent = Path.Combine(Path.GetTempPath(), $"NonExistent_{Guid.NewGuid():N}.txt");

        var result = await _service.DeleteAsync(nonExistent, useRecycleBin: false);

        Assert.False(result.Success);
    }

    #endregion

    #region CopyAsync Tests

    [Fact]
    public async Task CopyAsync_CopiesFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"FSTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var sourceFile = Path.Combine(tempDir, "source.txt");
        var destFile = Path.Combine(tempDir, "dest.txt");
        await File.WriteAllTextAsync(sourceFile, "content");

        try
        {
            var result = await _service.CopyAsync(sourceFile, destFile);

            Assert.True(result.Success);
            Assert.True(File.Exists(sourceFile));
            Assert.True(File.Exists(destFile));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region MoveAsync Tests

    [Fact]
    public async Task MoveAsync_MovesFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"FSTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var sourceFile = Path.Combine(tempDir, "source.txt");
        var destFile = Path.Combine(tempDir, "dest.txt");
        await File.WriteAllTextAsync(sourceFile, "content");

        try
        {
            var result = await _service.MoveAsync(sourceFile, destFile);

            Assert.True(result.Success);
            Assert.False(File.Exists(sourceFile));
            Assert.True(File.Exists(destFile));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region RenameAsync Tests

    [Fact]
    public async Task RenameAsync_RenamesFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"FSTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var originalFile = Path.Combine(tempDir, "original.txt");
        await File.WriteAllTextAsync(originalFile, "content");

        try
        {
            var result = await _service.RenameAsync(originalFile, "renamed.txt");

            Assert.True(result.Success);
            Assert.False(File.Exists(originalFile));
            Assert.True(File.Exists(Path.Combine(tempDir, "renamed.txt")));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region GetSizeAsync Tests

    [Fact]
    public async Task GetSizeAsync_ReturnsFileSize()
    {
        var tempFile = Path.GetTempFileName();
        await File.WriteAllBytesAsync(tempFile, new byte[100]);

        try
        {
            var size = await _service.GetSizeAsync(tempFile);
            Assert.Equal(100, size);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task GetSizeAsync_ReturnsDirectorySize()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"FSTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        await File.WriteAllBytesAsync(Path.Combine(tempDir, "file1.txt"), new byte[100]);
        await File.WriteAllBytesAsync(Path.Combine(tempDir, "file2.txt"), new byte[200]);

        try
        {
            var size = await _service.GetSizeAsync(tempDir);
            Assert.Equal(300, size);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region GetFileCountAsync Tests

    [Fact]
    public async Task GetFileCountAsync_ReturnsCorrectCount()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"FSTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "file1.txt"), "1");
        await File.WriteAllTextAsync(Path.Combine(tempDir, "file2.txt"), "2");
        await File.WriteAllTextAsync(Path.Combine(tempDir, "file3.txt"), "3");

        try
        {
            var count = await _service.GetFileCountAsync(tempDir);
            Assert.Equal(3, count);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task GetFileCountAsync_Recursive_IncludesSubdirectories()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"FSTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var subDir = Path.Combine(tempDir, "SubDir");
        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "root.txt"), "root");
        await File.WriteAllTextAsync(Path.Combine(subDir, "sub.txt"), "sub");

        try
        {
            var count = await _service.GetFileCountAsync(tempDir, recursive: true);
            Assert.Equal(2, count);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region Path Utility Tests

    [Fact]
    public void GetParentPath_ReturnsParent()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "Parent", "Child");
        var parent = _service.GetParentPath(tempDir);
        Assert.EndsWith("Parent", parent);
    }

    [Fact]
    public void NormalizePath_NormalizesPath()
    {
        var path = @"C:\Folder\..\Folder\SubFolder";
        var normalized = _service.NormalizePath(path);
        Assert.DoesNotContain("..", normalized);
    }

    [Fact]
    public void CombinePath_CombinesPaths()
    {
        var combined = _service.CombinePath("C:", "Folder", "SubFolder");
        Assert.Contains("Folder", combined);
        Assert.Contains("SubFolder", combined);
    }

    #endregion

    #region GetSpecialFolders Tests

    [Fact]
    public void GetSpecialFolders_ReturnsKnownFolders()
    {
        var folders = _service.GetSpecialFolders();
        Assert.NotEmpty(folders);
        Assert.Contains(folders, f => f.Type == SpecialFolderType.Desktop);
        Assert.Contains(folders, f => f.Type == SpecialFolderType.Documents);
    }

    [Fact]
    public void GetSpecialFolderPath_Desktop_ReturnsPath()
    {
        var path = _service.GetSpecialFolderPath(SpecialFolderType.Desktop);
        Assert.NotNull(path);
        Assert.True(Directory.Exists(path));
    }

    #endregion
}
