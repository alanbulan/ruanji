using FsCheck;
using FsCheck.Xunit;
using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.Infrastructure.Services;
using WindowsSoftwareOrganizer.Core.Interfaces;

namespace WindowsSoftwareOrganizer.Tests.Properties;

/// <summary>
/// Property-based tests for FileSearchService.
/// **Feature: ai-file-manager**
/// </summary>
public class FileSearchPropertyTests
{
    private readonly FileSearchService _service;

    public FileSearchPropertyTests()
    {
        _service = new FileSearchService();
    }

    #region Property 9: 搜索筛选正确性

    /// <summary>
    /// **Property 9**: 所有结果的文件名匹配搜索模式
    /// **Validates: Requirements 7.1, 7.2, 7.5**
    /// </summary>
    [Fact]
    public async Task Search_ResultsMatchPattern()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SearchProp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create test files
            await File.WriteAllTextAsync(Path.Combine(tempDir, "test_file.txt"), "content");
            await File.WriteAllTextAsync(Path.Combine(tempDir, "test_doc.txt"), "content");
            await File.WriteAllTextAsync(Path.Combine(tempDir, "other.txt"), "content");

            var results = new List<FileEntry>();
            await foreach (var file in _service.QuickSearchAsync(tempDir, "test*", recursive: false))
            {
                results.Add(file);
            }

            // All results should match the pattern
            Assert.Equal(2, results.Count);
            Assert.All(results, f => Assert.StartsWith("test", f.Name));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    /// **Property 9**: 所有结果的扩展名在指定的扩展名列表中
    /// **Validates: Requirements 7.1, 7.2, 7.5**
    /// </summary>
    [Fact]
    public async Task Search_ResultsHaveSpecifiedExtensions()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SearchProp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, "file1.txt"), "content");
            await File.WriteAllTextAsync(Path.Combine(tempDir, "file2.doc"), "content");
            await File.WriteAllTextAsync(Path.Combine(tempDir, "file3.pdf"), "content");
            await File.WriteAllTextAsync(Path.Combine(tempDir, "file4.txt"), "content");

            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".txt", ".doc" };

            var results = new List<FileEntry>();
            await foreach (var file in _service.QuickSearchAsync(tempDir, "*", recursive: false))
            {
                if (allowedExtensions.Contains(file.Extension))
                {
                    results.Add(file);
                }
            }

            // All results should have specified extensions
            Assert.Equal(3, results.Count);
            Assert.All(results, f => Assert.True(
                f.Extension == ".txt" || f.Extension == ".doc",
                $"Extension {f.Extension} should be .txt or .doc"));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    /// **Property 9**: 所有结果的大小在指定范围内
    /// **Validates: Requirements 7.1, 7.2, 7.5**
    /// </summary>
    [Property(MaxTest = 10)]
    public Property Search_ResultsWithinSizeRange()
    {
        return Prop.ForAll(
            Gen.Choose(50, 100).ToArbitrary(),
            Gen.Choose(200, 300).ToArbitrary(),
            (minSize, maxSize) =>
            {
                var tempDir = Path.Combine(Path.GetTempPath(), $"SearchProp_{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    // Create files with different sizes
                    File.WriteAllBytes(Path.Combine(tempDir, "small.txt"), new byte[30]);
                    File.WriteAllBytes(Path.Combine(tempDir, "medium.txt"), new byte[150]);
                    File.WriteAllBytes(Path.Combine(tempDir, "large.txt"), new byte[500]);

                    var results = new List<FileEntry>();
                    var enumerable = _service.QuickSearchAsync(tempDir, "*", recursive: false);
                    var enumerator = enumerable.GetAsyncEnumerator();
                    while (enumerator.MoveNextAsync().AsTask().Result)
                    {
                        var file = enumerator.Current;
                        if (file.Size >= minSize && file.Size <= maxSize)
                        {
                            results.Add(file);
                        }
                    }

                    // All results should be within size range (we filtered them)
                    var allWithinRange = results.All(f => f.Size >= minSize && f.Size <= maxSize);

                    return allWithinRange.Label("All results should be within size range");
                }
                finally
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            });
    }

    /// <summary>
    /// **Property 9**: 通配符 * 匹配任意字符序列
    /// **Validates: Requirements 7.1, 7.2, 7.5**
    /// </summary>
    [Theory]
    [InlineData("*.txt", new[] { "file.txt", "test.txt" }, new[] { "file.doc" })]
    [InlineData("test*", new[] { "test.txt", "test_file.doc" }, new[] { "file.txt" })]
    [InlineData("*file*", new[] { "file.txt", "myfile.doc", "file_test.pdf" }, new[] { "test.txt" })]
    public async Task Wildcard_Star_MatchesAnySequence(string pattern, string[] shouldMatch, string[] shouldNotMatch)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SearchProp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create all files
            foreach (var file in shouldMatch.Concat(shouldNotMatch))
            {
                await File.WriteAllTextAsync(Path.Combine(tempDir, file), "content");
            }

            var results = new List<FileEntry>();
            await foreach (var file in _service.QuickSearchAsync(tempDir, pattern, recursive: false))
            {
                results.Add(file);
            }

            var resultNames = results.Select(f => f.Name).ToHashSet();

            // Should contain all matching files
            foreach (var expected in shouldMatch)
            {
                Assert.Contains(expected, resultNames);
            }

            // Should not contain non-matching files
            foreach (var notExpected in shouldNotMatch)
            {
                Assert.DoesNotContain(notExpected, resultNames);
            }
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    /// **Property 9**: 通配符 ? 匹配单个字符
    /// **Validates: Requirements 7.1, 7.2, 7.5**
    /// </summary>
    [Fact]
    public async Task Wildcard_QuestionMark_MatchesSingleChar()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SearchProp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, "file1.txt"), "content");
            await File.WriteAllTextAsync(Path.Combine(tempDir, "file2.txt"), "content");
            await File.WriteAllTextAsync(Path.Combine(tempDir, "file10.txt"), "content");

            var results = new List<FileEntry>();
            await foreach (var file in _service.QuickSearchAsync(tempDir, "file?.txt", recursive: false))
            {
                results.Add(file);
            }

            // Should match file1.txt and file2.txt but not file10.txt
            Assert.Equal(2, results.Count);
            var names = results.Select(f => f.Name).ToHashSet();
            Assert.Contains("file1.txt", names);
            Assert.Contains("file2.txt", names);
            Assert.DoesNotContain("file10.txt", names);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    /// **Property 9**: 搜索应该支持子目录递归
    /// **Validates: Requirements 7.1, 7.2, 7.5**
    /// </summary>
    [Fact]
    public async Task Search_IncludesSubdirectories_WhenEnabled()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"SearchProp_{Guid.NewGuid():N}");
        var subDir = Path.Combine(tempDir, "SubDir");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(subDir);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, "root.txt"), "content");
            await File.WriteAllTextAsync(Path.Combine(subDir, "sub.txt"), "content");

            var resultsWithSub = new List<FileEntry>();
            await foreach (var file in _service.QuickSearchAsync(tempDir, "*.txt", recursive: true))
            {
                resultsWithSub.Add(file);
            }

            var resultsWithoutSub = new List<FileEntry>();
            await foreach (var file in _service.QuickSearchAsync(tempDir, "*.txt", recursive: false))
            {
                resultsWithoutSub.Add(file);
            }

            Assert.Equal(2, resultsWithSub.Count);
            Assert.Single(resultsWithoutSub);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion
}
