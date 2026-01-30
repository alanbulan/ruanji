using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.Infrastructure.Services;

namespace WindowsSoftwareOrganizer.Tests.Services;

/// <summary>
/// Unit tests for FileSearchService.
/// </summary>
public class FileSearchServiceTests
{
    private readonly FileSearchService _service;

    public FileSearchServiceTests()
    {
        _service = new FileSearchService();
    }

    #region QuickSearchAsync Tests

    [Fact]
    public async Task QuickSearchAsync_FindsMatchingFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SearchTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "test1.txt"), "content1");
        await File.WriteAllTextAsync(Path.Combine(tempDir, "test2.txt"), "content2");
        await File.WriteAllTextAsync(Path.Combine(tempDir, "other.doc"), "other");

        try
        {
            var results = new List<FileEntry>();
            await foreach (var file in _service.QuickSearchAsync(tempDir, "*.txt"))
            {
                results.Add(file);
            }

            Assert.Equal(2, results.Count);
            Assert.All(results, f => Assert.EndsWith(".txt", f.Name));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task QuickSearchAsync_EmptyDirectory_ReturnsEmpty()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SearchTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var results = new List<FileEntry>();
            await foreach (var file in _service.QuickSearchAsync(tempDir, "*.*"))
            {
                results.Add(file);
            }

            Assert.Empty(results);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task QuickSearchAsync_NoMatches_ReturnsEmpty()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SearchTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "file.txt"), "content");

        try
        {
            var results = new List<FileEntry>();
            await foreach (var file in _service.QuickSearchAsync(tempDir, "*.xyz"))
            {
                results.Add(file);
            }

            Assert.Empty(results);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task QuickSearchAsync_Recursive_SearchesSubdirectories()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SearchTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var subDir = Path.Combine(tempDir, "SubDir");
        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "root.txt"), "root");
        await File.WriteAllTextAsync(Path.Combine(subDir, "sub.txt"), "sub");

        try
        {
            var results = new List<FileEntry>();
            await foreach (var file in _service.QuickSearchAsync(tempDir, "*.txt", recursive: true))
            {
                results.Add(file);
            }

            Assert.Equal(2, results.Count);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task QuickSearchAsync_NonRecursive_OnlySearchesTopLevel()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SearchTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var subDir = Path.Combine(tempDir, "SubDir");
        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "root.txt"), "root");
        await File.WriteAllTextAsync(Path.Combine(subDir, "sub.txt"), "sub");

        try
        {
            var results = new List<FileEntry>();
            await foreach (var file in _service.QuickSearchAsync(tempDir, "*.txt", recursive: false))
            {
                results.Add(file);
            }

            Assert.Single(results);
            Assert.Equal("root.txt", results[0].Name);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task QuickSearchAsync_NonExistentDirectory_ReturnsEmpty()
    {
        var nonExistent = Path.Combine(Path.GetTempPath(), $"NonExistent_{Guid.NewGuid():N}");

        var results = new List<FileEntry>();
        await foreach (var file in _service.QuickSearchAsync(nonExistent, "*.*"))
        {
            results.Add(file);
        }

        Assert.Empty(results);
    }

    #endregion

    #region SearchDirectoriesAsync Tests

    [Fact]
    public async Task SearchDirectoriesAsync_FindsMatchingDirectories()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SearchTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, "TestDir1"));
        Directory.CreateDirectory(Path.Combine(tempDir, "TestDir2"));
        Directory.CreateDirectory(Path.Combine(tempDir, "OtherDir"));

        try
        {
            var results = new List<DirectoryEntry>();
            await foreach (var dir in _service.SearchDirectoriesAsync(tempDir, "Test*"))
            {
                results.Add(dir);
            }

            Assert.Equal(2, results.Count);
            Assert.All(results, d => Assert.StartsWith("Test", d.Name));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region GetRecentFilesAsync Tests

    [Fact]
    public async Task GetRecentFilesAsync_ReturnsRecentFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SearchTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var oldFile = Path.Combine(tempDir, "old.txt");
        var newFile = Path.Combine(tempDir, "new.txt");
        await File.WriteAllTextAsync(oldFile, "old");
        File.SetLastWriteTime(oldFile, DateTime.Now.AddDays(-10));
        await File.WriteAllTextAsync(newFile, "new");

        try
        {
            var files = await _service.GetRecentFilesAsync(tempDir, count: 10);

            Assert.Equal(2, files.Count);
            Assert.Equal("new.txt", files[0].Name);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task GetRecentFilesAsync_LimitsCount()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SearchTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        for (int i = 0; i < 10; i++)
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, $"file{i}.txt"), $"content{i}");
        }

        try
        {
            var files = await _service.GetRecentFilesAsync(tempDir, count: 5);

            Assert.Equal(5, files.Count);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region SearchAllAsync Tests

    [Fact]
    public async Task SearchAllAsync_ReturnsSearchResult()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SearchTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "test.txt"), "content");

        // Change current directory temporarily
        var originalDir = Environment.CurrentDirectory;
        Environment.CurrentDirectory = tempDir;

        try
        {
            var criteria = new FileSearchCriteria
            {
                Pattern = "*.txt",
                UseWildcard = true,
                IncludeSubdirectories = false
            };

            var result = await _service.SearchAllAsync(criteria);

            Assert.NotNull(result);
            Assert.Single(result.Files);
            Assert.Equal(criteria, result.Criteria);
        }
        finally
        {
            Environment.CurrentDirectory = originalDir;
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task SearchAllAsync_RespectsMaxResults()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SearchTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        for (int i = 0; i < 20; i++)
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, $"file{i}.txt"), $"content{i}");
        }

        var originalDir = Environment.CurrentDirectory;
        Environment.CurrentDirectory = tempDir;

        try
        {
            var criteria = new FileSearchCriteria
            {
                Pattern = "*.txt",
                UseWildcard = true,
                IncludeSubdirectories = false
            };

            var result = await _service.SearchAllAsync(criteria, maxResults: 5);

            Assert.Equal(5, result.Files.Count);
            Assert.True(result.ReachedLimit);
        }
        finally
        {
            Environment.CurrentDirectory = originalDir;
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region SearchContentAsync Tests

    [Fact]
    public async Task SearchContentAsync_FindsContentInFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SearchTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "file1.txt"), "Hello World");
        await File.WriteAllTextAsync(Path.Combine(tempDir, "file2.txt"), "Goodbye World");
        await File.WriteAllTextAsync(Path.Combine(tempDir, "file3.txt"), "No match here");

        try
        {
            var results = new List<FileContentMatch>();
            await foreach (var match in _service.SearchContentAsync(tempDir, "World"))
            {
                results.Add(match);
            }

            Assert.Equal(2, results.Count);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task SearchContentAsync_CaseSensitive_MatchesExactCase()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SearchTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "file1.txt"), "Hello World");
        await File.WriteAllTextAsync(Path.Combine(tempDir, "file2.txt"), "hello world");

        try
        {
            var results = new List<FileContentMatch>();
            await foreach (var match in _service.SearchContentAsync(tempDir, "Hello", caseSensitive: true))
            {
                results.Add(match);
            }

            Assert.Single(results);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion
}
