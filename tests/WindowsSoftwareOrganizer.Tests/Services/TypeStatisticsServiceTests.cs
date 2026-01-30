using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.Infrastructure.Services;

namespace WindowsSoftwareOrganizer.Tests.Services;

/// <summary>
/// Unit tests for TypeStatisticsService.
/// </summary>
public class TypeStatisticsServiceTests
{
    private readonly TypeStatisticsService _service;

    public TypeStatisticsServiceTests()
    {
        _service = new TypeStatisticsService();
    }

    #region FileTypeCategoryHelper.GetCategory Tests

    [Theory]
    [InlineData(".doc", FileTypeCategory.WordDocument)]
    [InlineData(".docx", FileTypeCategory.WordDocument)]
    [InlineData(".odt", FileTypeCategory.WordDocument)]
    public void GetCategory_WordDocumentExtensions_ReturnsWordDocument(string extension, FileTypeCategory expected)
    {
        var category = FileTypeCategoryHelper.GetCategory(extension);
        Assert.Equal(expected, category);
    }

    [Theory]
    [InlineData(".pdf", FileTypeCategory.PDF)]
    public void GetCategory_PdfExtensions_ReturnsPDF(string extension, FileTypeCategory expected)
    {
        var category = FileTypeCategoryHelper.GetCategory(extension);
        Assert.Equal(expected, category);
    }

    [Theory]
    [InlineData(".txt", FileTypeCategory.PlainText)]
    [InlineData(".text", FileTypeCategory.PlainText)]
    public void GetCategory_PlainTextExtensions_ReturnsPlainText(string extension, FileTypeCategory expected)
    {
        var category = FileTypeCategoryHelper.GetCategory(extension);
        Assert.Equal(expected, category);
    }

    [Theory]
    [InlineData(".xls", FileTypeCategory.Spreadsheet)]
    [InlineData(".xlsx", FileTypeCategory.Spreadsheet)]
    public void GetCategory_SpreadsheetExtensions_ReturnsSpreadsheet(string extension, FileTypeCategory expected)
    {
        var category = FileTypeCategoryHelper.GetCategory(extension);
        Assert.Equal(expected, category);
    }

    [Theory]
    [InlineData(".ppt", FileTypeCategory.Presentation)]
    [InlineData(".pptx", FileTypeCategory.Presentation)]
    public void GetCategory_PresentationExtensions_ReturnsPresentation(string extension, FileTypeCategory expected)
    {
        var category = FileTypeCategoryHelper.GetCategory(extension);
        Assert.Equal(expected, category);
    }

    [Theory]
    [InlineData(".jpg", FileTypeCategory.RasterImage)]
    [InlineData(".jpeg", FileTypeCategory.RasterImage)]
    [InlineData(".png", FileTypeCategory.RasterImage)]
    [InlineData(".gif", FileTypeCategory.RasterImage)]
    [InlineData(".bmp", FileTypeCategory.RasterImage)]
    [InlineData(".webp", FileTypeCategory.RasterImage)]
    public void GetCategory_RasterImageExtensions_ReturnsRasterImage(string extension, FileTypeCategory expected)
    {
        var category = FileTypeCategoryHelper.GetCategory(extension);
        Assert.Equal(expected, category);
    }

    [Theory]
    [InlineData(".svg", FileTypeCategory.VectorImage)]
    [InlineData(".eps", FileTypeCategory.VectorImage)]
    public void GetCategory_VectorImageExtensions_ReturnsVectorImage(string extension, FileTypeCategory expected)
    {
        var category = FileTypeCategoryHelper.GetCategory(extension);
        Assert.Equal(expected, category);
    }

    [Theory]
    [InlineData(".mp4", FileTypeCategory.Video)]
    [InlineData(".avi", FileTypeCategory.Video)]
    [InlineData(".mkv", FileTypeCategory.Video)]
    [InlineData(".mov", FileTypeCategory.Video)]
    [InlineData(".wmv", FileTypeCategory.Video)]
    public void GetCategory_VideoExtensions_ReturnsVideo(string extension, FileTypeCategory expected)
    {
        var category = FileTypeCategoryHelper.GetCategory(extension);
        Assert.Equal(expected, category);
    }

    [Theory]
    [InlineData(".mp3", FileTypeCategory.LossyAudio)]
    [InlineData(".aac", FileTypeCategory.LossyAudio)]
    [InlineData(".ogg", FileTypeCategory.LossyAudio)]
    public void GetCategory_LossyAudioExtensions_ReturnsLossyAudio(string extension, FileTypeCategory expected)
    {
        var category = FileTypeCategoryHelper.GetCategory(extension);
        Assert.Equal(expected, category);
    }

    [Theory]
    [InlineData(".wav", FileTypeCategory.LosslessAudio)]
    [InlineData(".flac", FileTypeCategory.LosslessAudio)]
    public void GetCategory_LosslessAudioExtensions_ReturnsLosslessAudio(string extension, FileTypeCategory expected)
    {
        var category = FileTypeCategoryHelper.GetCategory(extension);
        Assert.Equal(expected, category);
    }

    [Theory]
    [InlineData(".zip", FileTypeCategory.Archive)]
    [InlineData(".rar", FileTypeCategory.Archive)]
    [InlineData(".7z", FileTypeCategory.Archive)]
    [InlineData(".tar", FileTypeCategory.Archive)]
    [InlineData(".gz", FileTypeCategory.Archive)]
    public void GetCategory_ArchiveExtensions_ReturnsArchive(string extension, FileTypeCategory expected)
    {
        var category = FileTypeCategoryHelper.GetCategory(extension);
        Assert.Equal(expected, category);
    }

    [Theory]
    [InlineData(".cs", FileTypeCategory.CSharpSource)]
    public void GetCategory_CSharpExtensions_ReturnsCSharpSource(string extension, FileTypeCategory expected)
    {
        var category = FileTypeCategoryHelper.GetCategory(extension);
        Assert.Equal(expected, category);
    }

    [Theory]
    [InlineData(".js", FileTypeCategory.JavaScriptSource)]
    [InlineData(".ts", FileTypeCategory.JavaScriptSource)]
    public void GetCategory_JavaScriptExtensions_ReturnsJavaScriptSource(string extension, FileTypeCategory expected)
    {
        var category = FileTypeCategoryHelper.GetCategory(extension);
        Assert.Equal(expected, category);
    }

    [Theory]
    [InlineData(".py", FileTypeCategory.PythonSource)]
    public void GetCategory_PythonExtensions_ReturnsPythonSource(string extension, FileTypeCategory expected)
    {
        var category = FileTypeCategoryHelper.GetCategory(extension);
        Assert.Equal(expected, category);
    }

    [Theory]
    [InlineData(".java", FileTypeCategory.JavaSource)]
    public void GetCategory_JavaExtensions_ReturnsJavaSource(string extension, FileTypeCategory expected)
    {
        var category = FileTypeCategoryHelper.GetCategory(extension);
        Assert.Equal(expected, category);
    }

    [Theory]
    [InlineData(".cpp", FileTypeCategory.CppSource)]
    [InlineData(".c", FileTypeCategory.CppSource)]
    [InlineData(".h", FileTypeCategory.CppSource)]
    public void GetCategory_CppExtensions_ReturnsCppSource(string extension, FileTypeCategory expected)
    {
        var category = FileTypeCategoryHelper.GetCategory(extension);
        Assert.Equal(expected, category);
    }

    [Theory]
    [InlineData(".exe", FileTypeCategory.WindowsExecutable)]
    public void GetCategory_WindowsExecutableExtensions_ReturnsWindowsExecutable(string extension, FileTypeCategory expected)
    {
        var category = FileTypeCategoryHelper.GetCategory(extension);
        Assert.Equal(expected, category);
    }

    [Theory]
    [InlineData(".msi", FileTypeCategory.WindowsInstaller)]
    public void GetCategory_WindowsInstallerExtensions_ReturnsWindowsInstaller(string extension, FileTypeCategory expected)
    {
        var category = FileTypeCategoryHelper.GetCategory(extension);
        Assert.Equal(expected, category);
    }

    [Theory]
    [InlineData(".bat", FileTypeCategory.ShellScript)]
    [InlineData(".cmd", FileTypeCategory.ShellScript)]
    [InlineData(".ps1", FileTypeCategory.ShellScript)]
    public void GetCategory_ShellScriptExtensions_ReturnsShellScript(string extension, FileTypeCategory expected)
    {
        var category = FileTypeCategoryHelper.GetCategory(extension);
        Assert.Equal(expected, category);
    }

    [Theory]
    [InlineData(".json", FileTypeCategory.JSON)]
    public void GetCategory_JsonExtensions_ReturnsJSON(string extension, FileTypeCategory expected)
    {
        var category = FileTypeCategoryHelper.GetCategory(extension);
        Assert.Equal(expected, category);
    }

    [Theory]
    [InlineData(".xml", FileTypeCategory.XML)]
    public void GetCategory_XmlExtensions_ReturnsXML(string extension, FileTypeCategory expected)
    {
        var category = FileTypeCategoryHelper.GetCategory(extension);
        Assert.Equal(expected, category);
    }

    [Theory]
    [InlineData(".sql", FileTypeCategory.SQLSource)]
    public void GetCategory_SqlExtensions_ReturnsSQLSource(string extension, FileTypeCategory expected)
    {
        var category = FileTypeCategoryHelper.GetCategory(extension);
        Assert.Equal(expected, category);
    }

    [Theory]
    [InlineData(".qwerty")]
    [InlineData(".unknown")]
    [InlineData(".randomext")]
    public void GetCategory_UnknownExtensions_ReturnsUnknown(string extension)
    {
        var category = FileTypeCategoryHelper.GetCategory(extension);
        Assert.Equal(FileTypeCategory.Unknown, category);
    }

    [Fact]
    public void GetCategory_CaseInsensitive()
    {
        Assert.Equal(FileTypeCategoryHelper.GetCategory(".PDF"), FileTypeCategoryHelper.GetCategory(".pdf"));
        Assert.Equal(FileTypeCategoryHelper.GetCategory(".JPG"), FileTypeCategoryHelper.GetCategory(".jpg"));
        Assert.Equal(FileTypeCategoryHelper.GetCategory(".CS"), FileTypeCategoryHelper.GetCategory(".cs"));
    }

    #endregion

    #region AnalyzeAsync Tests

    [Fact]
    public async Task AnalyzeAsync_EmptyDirectory_ReturnsEmptyResult()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"TypeTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var result = await _service.AnalyzeAsync(tempDir);

            Assert.Equal(tempDir, result.RootPath);
            Assert.Equal(0, result.TotalFiles);
            Assert.Equal(0, result.TotalSize);
            Assert.Empty(result.Items);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task AnalyzeAsync_SingleFileType_ReturnsOneCategory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"TypeTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "doc1.txt"), "content1");
        await File.WriteAllTextAsync(Path.Combine(tempDir, "doc2.txt"), "content2");

        try
        {
            var result = await _service.AnalyzeAsync(tempDir);

            Assert.Single(result.Items);
            Assert.Equal(FileTypeCategory.PlainText, result.Items[0].Category);
            Assert.Equal(2, result.Items[0].FileCount);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task AnalyzeAsync_MultipleFileTypes_ReturnsMultipleCategories()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"TypeTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "doc.txt"), "text");
        await File.WriteAllBytesAsync(Path.Combine(tempDir, "image.png"), new byte[100]);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "code.cs"), "code");

        try
        {
            var result = await _service.AnalyzeAsync(tempDir);

            Assert.Equal(3, result.Items.Count);
            Assert.Equal(3, result.TotalFiles);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task AnalyzeAsync_TotalSizeMatchesSum()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"TypeTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        await File.WriteAllBytesAsync(Path.Combine(tempDir, "file1.txt"), new byte[100]);
        await File.WriteAllBytesAsync(Path.Combine(tempDir, "file2.png"), new byte[200]);

        try
        {
            var result = await _service.AnalyzeAsync(tempDir);

            var sumOfSizes = result.Items.Sum(i => i.TotalSize);
            Assert.Equal(result.TotalSize, sumOfSizes);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task AnalyzeAsync_TotalFilesMatchesSum()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"TypeTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "a.txt"), "a");
        await File.WriteAllTextAsync(Path.Combine(tempDir, "b.txt"), "b");
        await File.WriteAllTextAsync(Path.Combine(tempDir, "c.png"), "c");

        try
        {
            var result = await _service.AnalyzeAsync(tempDir);

            var sumOfCounts = result.Items.Sum(i => i.FileCount);
            Assert.Equal(result.TotalFiles, sumOfCounts);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task AnalyzeAsync_IncludesSubdirectories_WhenRecursiveTrue()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"TypeTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var subDir = Path.Combine(tempDir, "SubDir");
        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "root.txt"), "root");
        await File.WriteAllTextAsync(Path.Combine(subDir, "sub.txt"), "sub");

        try
        {
            var result = await _service.AnalyzeAsync(tempDir, recursive: true);

            Assert.Equal(2, result.TotalFiles);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task AnalyzeAsync_ExcludesSubdirectories_WhenRecursiveFalse()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"TypeTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var subDir = Path.Combine(tempDir, "SubDir");
        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "root.txt"), "root");
        await File.WriteAllTextAsync(Path.Combine(subDir, "sub.txt"), "sub");

        try
        {
            var result = await _service.AnalyzeAsync(tempDir, recursive: false);

            Assert.Equal(1, result.TotalFiles);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task AnalyzeAsync_SupportsCancellation()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"TypeTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => _service.AnalyzeAsync(tempDir, cancellationToken: cts.Token));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region GetExtensionStatisticsAsync Tests

    [Fact]
    public async Task GetExtensionStatisticsAsync_ReturnsExtensionBreakdown()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"TypeTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "a.txt"), "a");
        await File.WriteAllTextAsync(Path.Combine(tempDir, "b.txt"), "bb");
        await File.WriteAllTextAsync(Path.Combine(tempDir, "c.cs"), "ccc");

        try
        {
            var result = await _service.GetExtensionStatisticsAsync(tempDir);

            Assert.Equal(2, result.Count);
            Assert.Contains(result, e => e.Extension == ".txt" && e.Count == 2);
            Assert.Contains(result, e => e.Extension == ".cs" && e.Count == 1);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region GetFilesByCategoryAsync Tests

    [Fact]
    public async Task GetFilesByCategoryAsync_ReturnsFilesOfCategory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"TypeTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "doc.txt"), "text");
        await File.WriteAllBytesAsync(Path.Combine(tempDir, "image.png"), new byte[100]);

        try
        {
            var result = await _service.GetFilesByCategoryAsync(tempDir, FileTypeCategory.PlainText);

            Assert.Single(result);
            Assert.Equal("doc.txt", result[0].Name);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region GetFilesByExtensionAsync Tests

    [Fact]
    public async Task GetFilesByExtensionAsync_ReturnsFilesWithExtension()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"TypeTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "a.txt"), "a");
        await File.WriteAllTextAsync(Path.Combine(tempDir, "b.txt"), "b");
        await File.WriteAllTextAsync(Path.Combine(tempDir, "c.cs"), "c");

        try
        {
            var result = await _service.GetFilesByExtensionAsync(tempDir, ".txt");

            Assert.Equal(2, result.Count);
            Assert.All(result, f => Assert.Equal(".txt", f.Extension));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task GetFilesByExtensionAsync_WorksWithoutLeadingDot()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"TypeTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "file.txt"), "content");

        try
        {
            var result = await _service.GetFilesByExtensionAsync(tempDir, "txt");

            Assert.Single(result);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion
}
