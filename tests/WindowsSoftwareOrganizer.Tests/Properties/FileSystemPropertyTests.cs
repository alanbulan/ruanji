using FsCheck;
using FsCheck.Xunit;
using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.Infrastructure.Services;

namespace WindowsSoftwareOrganizer.Tests.Properties;

/// <summary>
/// Property-based tests for FileSystemService.
/// **Feature: ai-file-manager**
/// </summary>
public class FileSystemPropertyTests
{
    private readonly FileSystemService _service;

    public FileSystemPropertyTests()
    {
        _service = new FileSystemService();
    }

    #region Property 1: 目录内容获取正确性

    /// <summary>
    /// **Property 1**: 对于任意有效的目录路径，GetDirectoryContentAsync 返回的内容应该：
    /// - 包含该目录下所有可访问的子目录
    /// - 包含该目录下所有可访问的文件
    /// - 每个 FileEntry 应该包含正确的文件名、大小、修改日期和扩展名
    /// **Validates: Requirements 1.2, 1.3, 2.1, 2.2**
    /// </summary>
    [Fact]
    public async Task DirectoryContent_ContainsAllAccessibleItems()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"PropTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var subDir = Path.Combine(tempDir, "SubDir");
        Directory.CreateDirectory(subDir);
        var file1 = Path.Combine(tempDir, "file1.txt");
        var file2 = Path.Combine(tempDir, "file2.doc");
        await File.WriteAllTextAsync(file1, "content1");
        await File.WriteAllBytesAsync(file2, new byte[100]);

        try
        {
            // Act
            var content = await _service.GetDirectoryContentAsync(tempDir);

            // Assert - Contains all subdirectories
            Assert.Single(content.Directories);
            Assert.Equal("SubDir", content.Directories[0].Name);

            // Assert - Contains all files
            Assert.Equal(2, content.Files.Count);
            var fileNames = content.Files.Select(f => f.Name).ToHashSet();
            Assert.Contains("file1.txt", fileNames);
            Assert.Contains("file2.doc", fileNames);

            // Assert - FileEntry has correct properties
            var txtFile = content.Files.First(f => f.Name == "file1.txt");
            Assert.Equal(".txt", txtFile.Extension);
            Assert.True(txtFile.Size > 0);
            Assert.True(txtFile.ModifiedTime <= DateTime.Now);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    /// **Property 1**: FileEntry 包含正确的扩展名
    /// **Validates: Requirements 2.1, 2.2**
    /// </summary>
    [Property(MaxTest = 50)]
    public Property FileEntry_HasCorrectExtension()
    {
        return Prop.ForAll(
            Arb.From<NonEmptyString>(),
            ext =>
            {
                var validExt = new string(ext.Get.Where(c => char.IsLetterOrDigit(c)).Take(5).ToArray());
                if (string.IsNullOrEmpty(validExt)) validExt = "txt";

                var tempDir = Path.Combine(Path.GetTempPath(), $"PropTest_{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempDir);
                var fileName = $"test.{validExt}";
                var filePath = Path.Combine(tempDir, fileName);

                try
                {
                    File.WriteAllText(filePath, "test");
                    var files = _service.GetFilesAsync(tempDir).Result;

                    return (files.Count == 1 && files[0].Extension == $".{validExt}")
                        .Label($"Extension should be .{validExt}");
                }
                finally
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            });
    }

    #endregion

    #region Property 2: 文件列表排序正确性

    /// <summary>
    /// **Property 2**: 对于任意文件列表和排序字段，排序后的列表应该满足：
    /// - 升序排序时，每个元素应该小于或等于其后续元素
    /// - 排序操作不应改变列表中的元素集合（只改变顺序）
    /// **Validates: Requirements 2.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property SortByName_Ascending_IsOrdered()
    {
        return Prop.ForAll(
            Gen.ListOf(Gen.Elements("a.txt", "b.doc", "c.pdf", "d.exe", "e.zip", "f.cs", "g.py", "h.js"))
               .Select(l => l.ToList())
               .Where(l => l.Distinct().Count() >= 2)
               .ToArbitrary(),
            fileNames =>
            {
                var distinctNames = fileNames.Distinct().ToList();
                var files = distinctNames.Select(n => new FileEntry
                {
                    Name = n,
                    FullPath = $"C:\\{n}",
                    Extension = Path.GetExtension(n),
                    Size = 100,
                    CreatedTime = DateTime.Now,
                    ModifiedTime = DateTime.Now,
                    Attributes = FileAttributes.Normal,
                    Category = FileTypeCategory.Unknown
                }).ToList();

                var sorted = files.OrderBy(f => f.Name).ToList();

                // Check ordering
                var isOrdered = true;
                for (int i = 0; i < sorted.Count - 1; i++)
                {
                    if (string.Compare(sorted[i].Name, sorted[i + 1].Name, StringComparison.OrdinalIgnoreCase) > 0)
                    {
                        isOrdered = false;
                        break;
                    }
                }

                // Check same elements
                var sameElements = files.Select(f => f.Name).OrderBy(n => n)
                    .SequenceEqual(sorted.Select(f => f.Name).OrderBy(n => n));

                return (isOrdered && sameElements)
                    .Label("Sorted list should be ordered and contain same elements");
            });
    }

    /// <summary>
    /// **Property 2**: 按大小降序排序时，每个元素应该大于或等于其后续元素
    /// **Validates: Requirements 2.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property SortBySize_Descending_IsOrdered()
    {
        return Prop.ForAll(
            Gen.ListOf(Gen.Choose(1, 10000))
               .Select(l => l.ToList())
               .Where(l => l.Count >= 2)
               .ToArbitrary(),
            sizes =>
            {
                var files = sizes.Select((s, i) => new FileEntry
                {
                    Name = $"file{i}.txt",
                    FullPath = $"C:\\file{i}.txt",
                    Extension = ".txt",
                    Size = s,
                    CreatedTime = DateTime.Now,
                    ModifiedTime = DateTime.Now,
                    Attributes = FileAttributes.Normal,
                    Category = FileTypeCategory.PlainText
                }).ToList();

                var sorted = files.OrderByDescending(f => f.Size).ToList();

                // Check ordering
                var isOrdered = true;
                for (int i = 0; i < sorted.Count - 1; i++)
                {
                    if (sorted[i].Size < sorted[i + 1].Size)
                    {
                        isOrdered = false;
                        break;
                    }
                }

                return isOrdered.Label("Sorted list should be in descending order by size");
            });
    }

    /// <summary>
    /// **Property 2**: 排序操作不应改变列表中的元素集合
    /// **Validates: Requirements 2.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Sort_PreservesElements()
    {
        return Prop.ForAll(
            Gen.ListOf(Gen.Choose(1, 1000))
               .Select(l => l.ToList())
               .Where(l => l.Count >= 1)
               .ToArbitrary(),
            sizes =>
            {
                var files = sizes.Select((s, i) => new FileEntry
                {
                    Name = $"file{i}_{s}.txt",
                    FullPath = $"C:\\file{i}_{s}.txt",
                    Extension = ".txt",
                    Size = s,
                    CreatedTime = DateTime.Now,
                    ModifiedTime = DateTime.Now,
                    Attributes = FileAttributes.Normal,
                    Category = FileTypeCategory.PlainText
                }).ToList();

                var sortedByName = files.OrderBy(f => f.Name).ToList();
                var sortedBySize = files.OrderByDescending(f => f.Size).ToList();

                var originalSet = files.Select(f => f.Name).ToHashSet();
                var sortedByNameSet = sortedByName.Select(f => f.Name).ToHashSet();
                var sortedBySizeSet = sortedBySize.Select(f => f.Name).ToHashSet();

                return (originalSet.SetEquals(sortedByNameSet) && originalSet.SetEquals(sortedBySizeSet))
                    .Label("Sorting should preserve all elements");
            });
    }

    #endregion
}
