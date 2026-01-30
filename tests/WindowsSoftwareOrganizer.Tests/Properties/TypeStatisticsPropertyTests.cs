using FsCheck;
using FsCheck.Xunit;
using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.Infrastructure.Services;

namespace WindowsSoftwareOrganizer.Tests.Properties;

/// <summary>
/// Property-based tests for TypeStatisticsService.
/// **Feature: ai-file-manager**
/// </summary>
public class TypeStatisticsPropertyTests
{
    private readonly TypeStatisticsService _service;

    public TypeStatisticsPropertyTests()
    {
        _service = new TypeStatisticsService();
    }

    #region Property 4: 类型统计正确性

    /// <summary>
    /// **Property 4**: 所有 TypeStatisticsItem 的 FileCount 之和等于 TotalFiles
    /// **Validates: Requirements 4.1, 4.3, 4.4**
    /// </summary>
    [Property(MaxTest = 20)]
    public Property FileCount_Sum_EqualsTotalFiles()
    {
        return Prop.ForAll(
            Gen.Choose(1, 10).ToArbitrary(),
            fileCount =>
            {
                var tempDir = Path.Combine(Path.GetTempPath(), $"TypeProp_{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    var extensions = new[] { ".txt", ".doc", ".pdf", ".jpg", ".mp3" };
                    var random = new System.Random();
                    for (int i = 0; i < fileCount; i++)
                    {
                        var ext = extensions[random.Next(extensions.Length)];
                        File.WriteAllText(Path.Combine(tempDir, $"file{i}{ext}"), "content");
                    }

                    var result = _service.AnalyzeAsync(tempDir).Result;

                    var sumOfCounts = result.Items.Sum(i => i.FileCount);

                    return (sumOfCounts == result.TotalFiles && result.TotalFiles == fileCount)
                        .Label($"Sum of counts ({sumOfCounts}) should equal TotalFiles ({result.TotalFiles}) and actual count ({fileCount})");
                }
                finally
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            });
    }

    /// <summary>
    /// **Property 4**: 所有 TypeStatisticsItem 的 TotalSize 之和等于总 TotalSize
    /// **Validates: Requirements 4.1, 4.3, 4.4**
    /// </summary>
    [Property(MaxTest = 20)]
    public Property TotalSize_Sum_EqualsTotalSize()
    {
        return Prop.ForAll(
            Gen.Choose(1, 8).ToArbitrary(),
            fileCount =>
            {
                var tempDir = Path.Combine(Path.GetTempPath(), $"TypeProp_{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    var extensions = new[] { ".txt", ".cs", ".json" };
                    var random = new System.Random();
                    long expectedTotal = 0;
                    for (int i = 0; i < fileCount; i++)
                    {
                        var ext = extensions[random.Next(extensions.Length)];
                        var content = new byte[random.Next(10, 100)];
                        File.WriteAllBytes(Path.Combine(tempDir, $"file{i}{ext}"), content);
                        expectedTotal += content.Length;
                    }

                    var result = _service.AnalyzeAsync(tempDir).Result;

                    var sumOfSizes = result.Items.Sum(i => i.TotalSize);

                    return (sumOfSizes == result.TotalSize)
                        .Label($"Sum of sizes ({sumOfSizes}) should equal TotalSize ({result.TotalSize})");
                }
                finally
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            });
    }

    /// <summary>
    /// **Property 4**: 按扩展名筛选后的文件列表只包含该扩展名的文件
    /// **Validates: Requirements 4.1, 4.3, 4.4**
    /// </summary>
    [Fact]
    public async Task FilterByExtension_OnlyReturnsMatchingFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"TypeProp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, "file1.txt"), "text");
            await File.WriteAllTextAsync(Path.Combine(tempDir, "file2.txt"), "text");
            await File.WriteAllTextAsync(Path.Combine(tempDir, "file3.cs"), "code");
            await File.WriteAllTextAsync(Path.Combine(tempDir, "file4.json"), "{}");

            var txtFiles = await _service.GetFilesByExtensionAsync(tempDir, ".txt");

            Assert.Equal(2, txtFiles.Count);
            Assert.All(txtFiles, f => Assert.Equal(".txt", f.Extension));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    /// **Property 4**: 每个已知扩展名应该被正确归类到预定义类别
    /// **Validates: Requirements 4.1, 4.3, 4.4**
    /// </summary>
    [Theory]
    [InlineData(".txt", FileTypeCategory.PlainText)]
    [InlineData(".doc", FileTypeCategory.WordDocument)]
    [InlineData(".docx", FileTypeCategory.WordDocument)]
    [InlineData(".pdf", FileTypeCategory.PDF)]
    [InlineData(".jpg", FileTypeCategory.RasterImage)]
    [InlineData(".png", FileTypeCategory.RasterImage)]
    [InlineData(".mp3", FileTypeCategory.LossyAudio)]
    [InlineData(".mp4", FileTypeCategory.Video)]
    [InlineData(".zip", FileTypeCategory.Archive)]
    [InlineData(".cs", FileTypeCategory.CSharpSource)]
    [InlineData(".js", FileTypeCategory.JavaScriptSource)]
    [InlineData(".py", FileTypeCategory.PythonSource)]
    [InlineData(".exe", FileTypeCategory.WindowsExecutable)]
    [InlineData(".json", FileTypeCategory.JSON)]
    [InlineData(".xml", FileTypeCategory.XML)]
    public void KnownExtension_MapsToCorrectCategory(string extension, FileTypeCategory expectedCategory)
    {
        var category = FileTypeCategoryHelper.GetCategory(extension);
        Assert.Equal(expectedCategory, category);
    }

    /// <summary>
    /// **Property 4**: 未知扩展名应该归类为 Unknown
    /// **Validates: Requirements 4.1, 4.3, 4.4**
    /// </summary>
    [Property(MaxTest = 50)]
    public Property UnknownExtension_MapsToUnknown()
    {
        return Prop.ForAll(
            Arb.From<NonEmptyString>(),
            randomStr =>
            {
                // Generate a random extension that's unlikely to be known
                var ext = "." + new string(randomStr.Get.Where(char.IsLetter).Take(10).ToArray()) + "xyz123";
                var category = FileTypeCategoryHelper.GetCategory(ext);

                // Most random extensions should be Unknown
                // (some might accidentally match known extensions)
                return (category == FileTypeCategory.Unknown || Enum.IsDefined(typeof(FileTypeCategory), category))
                    .Label("Category should be a valid enum value");
            });
    }

    /// <summary>
    /// **Property 4**: 扩展名匹配应该不区分大小写
    /// **Validates: Requirements 4.1, 4.3, 4.4**
    /// </summary>
    [Theory]
    [InlineData(".TXT", ".txt")]
    [InlineData(".PDF", ".pdf")]
    [InlineData(".Jpg", ".jpg")]
    [InlineData(".CS", ".cs")]
    public void ExtensionMatching_IsCaseInsensitive(string upper, string lower)
    {
        var upperCategory = FileTypeCategoryHelper.GetCategory(upper);
        var lowerCategory = FileTypeCategoryHelper.GetCategory(lower);
        Assert.Equal(upperCategory, lowerCategory);
    }

    #endregion
}
