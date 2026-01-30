using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.Infrastructure.Services;

namespace WindowsSoftwareOrganizer.Tests.Services;

/// <summary>
/// Unit tests for NamingEngine implementation.
/// Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5
/// </summary>
public class NamingEngineTests
{
    private readonly NamingEngine _engine;
    private readonly TestFileSystem _mockFileSystem;

    public NamingEngineTests()
    {
        _mockFileSystem = new TestFileSystem();
        _engine = new NamingEngine(_mockFileSystem);
    }

    #region ValidateTemplate Tests (Requirement 3.2)

    [Fact]
    public void ValidateTemplate_ValidSimpleTemplate_ReturnsSuccess()
    {
        var result = _engine.ValidateTemplate("{Category}/{Name}");
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateTemplate_ValidDetailedTemplate_ReturnsSuccess()
    {
        var result = _engine.ValidateTemplate("{Category}/{Vendor}_{Name}_{Version}");
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateTemplate_ValidDateTemplate_ReturnsSuccess()
    {
        var result = _engine.ValidateTemplate("{Category}/{Date}_{Name}");
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateTemplate_EmptyTemplate_ReturnsFailure()
    {
        var result = _engine.ValidateTemplate("");
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void ValidateTemplate_NullTemplate_ReturnsFailure()
    {
        var result = _engine.ValidateTemplate(null!);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateTemplate_UnclosedBrace_ReturnsFailure()
    {
        var result = _engine.ValidateTemplate("{Category/{Name}");
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("未闭合"));
    }

    [Fact]
    public void ValidateTemplate_UnknownVariable_ReturnsFailure()
    {
        var result = _engine.ValidateTemplate("{Category}/{Unknown}");
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Unknown"));
    }

    [Fact]
    public void ValidateTemplate_EmptyPlaceholder_ReturnsFailure()
    {
        var result = _engine.ValidateTemplate("{Category}/{}");
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("空的变量占位符"));
    }

    [Fact]
    public void ValidateTemplate_CaseInsensitiveVariables_ReturnsSuccess()
    {
        var result = _engine.ValidateTemplate("{category}/{NAME}/{version}");
        Assert.True(result.IsValid);
    }

    #endregion

    #region GenerateName Tests (Requirement 3.1)

    [Fact]
    public void GenerateName_SimpleTemplate_ReplacesVariables()
    {
        var entry = CreateTestEntry();
        var result = _engine.GenerateName(entry, "{Category}/{Name}");
        Assert.Equal("DevTool/TestApp", result);
    }

    [Fact]
    public void GenerateName_DetailedTemplate_ReplacesAllVariables()
    {
        var entry = CreateTestEntry();
        var result = _engine.GenerateName(entry, "{Category}/{Vendor}_{Name}_{Version}");
        Assert.Equal("DevTool/TestVendor_TestApp_1.0.0", result);
    }

    [Fact]
    public void GenerateName_DateTemplate_FormatsDateCorrectly()
    {
        var entry = CreateTestEntry(installDate: new DateTime(2024, 6, 15));
        var result = _engine.GenerateName(entry, "{Date}_{Name}");
        Assert.Equal("2024-06-15_TestApp", result);
    }

    [Fact]
    public void GenerateName_MissingVersion_UsesUnknown()
    {
        var entry = CreateTestEntry(version: null);
        var result = _engine.GenerateName(entry, "{Name}_{Version}");
        Assert.Equal("TestApp_Unknown", result);
    }

    [Fact]
    public void GenerateName_MissingVendor_UsesUnknown()
    {
        var entry = CreateTestEntry(vendor: null);
        var result = _engine.GenerateName(entry, "{Vendor}_{Name}");
        Assert.Equal("Unknown_TestApp", result);
    }

    [Fact]
    public void GenerateName_MissingDate_UsesUnknown()
    {
        var entry = CreateTestEntry(installDate: null);
        var result = _engine.GenerateName(entry, "{Date}_{Name}");
        Assert.Equal("Unknown_TestApp", result);
    }

    [Fact]
    public void GenerateName_NullEntry_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _engine.GenerateName(null!, "{Name}"));
    }

    [Fact]
    public void GenerateName_EmptyTemplate_ThrowsArgumentException()
    {
        var entry = CreateTestEntry();
        Assert.Throws<ArgumentException>(() => _engine.GenerateName(entry, ""));
    }

    #endregion

    #region SanitizeFileName Tests (Requirement 3.3)

    [Theory]
    [InlineData("normal_name", "normal_name")]
    [InlineData("name/with/slashes", "name_with_slashes")]
    [InlineData("name\\with\\backslashes", "name_with_backslashes")]
    [InlineData("name:with:colons", "name_with_colons")]
    [InlineData("name*with*asterisks", "name_with_asterisks")]
    [InlineData("name?with?questions", "name_with_questions")]
    [InlineData("name\"with\"quotes", "name_with_quotes")]
    [InlineData("name<with>angles", "name_with_angles")]
    [InlineData("name|with|pipes", "name_with_pipes")]
    public void SanitizeFileName_IllegalCharacters_ReplacedWithUnderscore(string input, string expected)
    {
        var result = _engine.SanitizeFileName(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SanitizeFileName_EmptyString_ReturnsEmpty()
    {
        var result = _engine.SanitizeFileName("");
        Assert.Equal("", result);
    }

    [Fact]
    public void SanitizeFileName_NullString_ReturnsNull()
    {
        var result = _engine.SanitizeFileName(null!);
        Assert.Null(result);
    }

    [Fact]
    public void SanitizeFileName_MultipleIllegalChars_AllReplaced()
    {
        var result = _engine.SanitizeFileName("a/b\\c:d*e?f\"g<h>i|j");
        Assert.Equal("a_b_c_d_e_f_g_h_i_j", result);
    }

    #endregion

    #region ResolveConflict Tests (Requirement 3.4)

    [Fact]
    public void ResolveConflict_NoConflict_ReturnsOriginalPath()
    {
        _mockFileSystem.ExistingDirectories.Clear();
        var result = _engine.ResolveConflict(@"C:\Software", "TestApp");
        Assert.Equal(@"C:\Software\TestApp", result);
    }

    [Fact]
    public void ResolveConflict_SingleConflict_AddsSuffix1()
    {
        _mockFileSystem.ExistingDirectories.Add(@"C:\Software\TestApp");
        var result = _engine.ResolveConflict(@"C:\Software", "TestApp");
        Assert.Equal(@"C:\Software\TestApp_1", result);
    }

    [Fact]
    public void ResolveConflict_MultipleConflicts_AddsIncrementingSuffix()
    {
        _mockFileSystem.ExistingDirectories.Add(@"C:\Software\TestApp");
        _mockFileSystem.ExistingDirectories.Add(@"C:\Software\TestApp_1");
        _mockFileSystem.ExistingDirectories.Add(@"C:\Software\TestApp_2");
        var result = _engine.ResolveConflict(@"C:\Software", "TestApp");
        Assert.Equal(@"C:\Software\TestApp_3", result);
    }

    [Fact]
    public void ResolveConflict_EmptyBasePath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _engine.ResolveConflict("", "TestApp"));
    }

    [Fact]
    public void ResolveConflict_EmptyDesiredName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _engine.ResolveConflict(@"C:\Software", ""));
    }

    #endregion

    #region GetPresetTemplates Tests (Requirement 3.5)

    [Fact]
    public void GetPresetTemplates_ReturnsThreeTemplates()
    {
        var templates = _engine.GetPresetTemplates();
        Assert.Equal(3, templates.Count);
    }

    [Fact]
    public void GetPresetTemplates_ContainsSimpleTemplate()
    {
        var templates = _engine.GetPresetTemplates();
        var simple = templates.FirstOrDefault(t => t.Id == "simple");
        Assert.NotNull(simple);
        Assert.Equal("{Category}/{Name}", simple.Pattern);
        Assert.True(simple.IsPreset);
    }

    [Fact]
    public void GetPresetTemplates_ContainsDetailedTemplate()
    {
        var templates = _engine.GetPresetTemplates();
        var detailed = templates.FirstOrDefault(t => t.Id == "detailed");
        Assert.NotNull(detailed);
        Assert.Equal("{Category}/{Vendor}_{Name}_{Version}", detailed.Pattern);
        Assert.True(detailed.IsPreset);
    }

    [Fact]
    public void GetPresetTemplates_ContainsDatedTemplate()
    {
        var templates = _engine.GetPresetTemplates();
        var dated = templates.FirstOrDefault(t => t.Id == "dated");
        Assert.NotNull(dated);
        Assert.Equal("{Category}/{Date}_{Name}", dated.Pattern);
        Assert.True(dated.IsPreset);
    }

    [Fact]
    public void GetPresetTemplates_AllTemplatesAreValid()
    {
        var templates = _engine.GetPresetTemplates();
        foreach (var template in templates)
        {
            var result = _engine.ValidateTemplate(template.Pattern);
            Assert.True(result.IsValid, $"Template '{template.Id}' should be valid");
        }
    }

    #endregion

    #region Helper Methods

    private static SoftwareEntry CreateTestEntry(
        string name = "TestApp",
        string? version = "1.0.0",
        string? vendor = "TestVendor",
        DateTime? installDate = null,
        SoftwareCategory category = SoftwareCategory.DevTool)
    {
        return new SoftwareEntry
        {
            Id = "test-id",
            Name = name,
            Version = version,
            Vendor = vendor,
            InstallPath = @"C:\Program Files\TestApp",
            InstallDate = installDate ?? new DateTime(2024, 1, 1),
            Category = category
        };
    }

    #endregion
}

/// <summary>
/// Test file system for testing ResolveConflict.
/// </summary>
internal class TestFileSystem : IFileSystemAbstraction
{
    public HashSet<string> ExistingDirectories { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool DirectoryExists(string path)
    {
        return ExistingDirectories.Contains(path);
    }
}
