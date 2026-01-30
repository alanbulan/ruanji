using FsCheck;
using FsCheck.Xunit;
using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.Infrastructure.Services;

namespace WindowsSoftwareOrganizer.Tests.Properties;

/// <summary>
/// Property-based tests for NamingEngine implementation.
/// Tests Properties 5, 6, 7, 8 from the design document.
/// </summary>
public class NamingEnginePropertyTests
{
    /// <summary>
    /// Windows file system illegal characters.
    /// </summary>
    private static readonly char[] IllegalFileNameChars = new[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

    /// <summary>
    /// Supported template variables.
    /// </summary>
    private static readonly string[] SupportedVariables = new[] { "Category", "Name", "Version", "Vendor", "Date" };

    #region Arbitraries

    /// <summary>
    /// Custom FsCheck generators for NamingEngine property tests.
    /// </summary>
    public class NamingEngineArbitraries
    {
        /// <summary>
        /// Generates valid SoftwareEntry objects for testing.
        /// </summary>
        public static Arbitrary<SoftwareEntry> SoftwareEntryArb()
        {
            var names = new[] { "VSCode", "Chrome", "Firefox", "Git", "NodeJS", "Python", "Java SDK", "Docker" };
            var vendors = new[] { "Microsoft", "Google", "Mozilla", "GitHub", "Oracle", "Docker Inc", null };
            var versions = new[] { "1.0.0", "2.5.3", "10.0.1", "3.14.159", null };

            return (from name in Gen.Elements(names)
                    from vendor in Gen.Elements(vendors)
                    from version in Gen.Elements(versions)
                    from category in Arb.Generate<SoftwareCategory>()
                    from hasDate in Arb.Generate<bool>()
                    select new SoftwareEntry
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = name,
                        Vendor = vendor,
                        Version = version,
                        InstallPath = @"C:\Program Files\" + name,
                        Category = category,
                        InstallDate = hasDate ? DateTime.Now.AddDays(-System.Random.Shared.Next(1, 365)) : null
                    }).ToArbitrary();
        }

        /// <summary>
        /// Generates valid template strings containing supported variables.
        /// </summary>
        public static Arbitrary<string> ValidTemplateArb()
        {
            var templates = new[]
            {
                "{Category}/{Name}",
                "{Category}/{Vendor}_{Name}_{Version}",
                "{Category}/{Date}_{Name}",
                "{Name}_{Version}",
                "{Vendor}/{Name}",
                "{Category}/{Name}_{Version}",
                "{Date}_{Vendor}_{Name}",
                "{Name}",
                "{Category}/{Vendor}/{Name}/{Version}",
                "{category}/{name}",  // lowercase
                "{CATEGORY}/{NAME}",  // uppercase
            };
            return Gen.Elements(templates).ToArbitrary();
        }

        /// <summary>
        /// Generates arbitrary strings for sanitization testing.
        /// </summary>
        public static Arbitrary<string> ArbitraryStringArb()
        {
            return (from length in Gen.Choose(1, 50)
                    from chars in Gen.ArrayOf(length, Gen.Choose(32, 126).Select(i => (char)i))
                    select new string(chars)).ToArbitrary();
        }
    }

    #endregion

    #region Property 5: 命名模板变量解析

    /// <summary>
    /// Property 5: 命名模板变量解析
    /// 对于任意包含支持变量（{Category}、{Name}、{Version}、{Vendor}、{Date}）的模板和有效的SoftwareEntry，
    /// 生成的名称中不应包含未解析的变量占位符。
    /// **Validates: Requirements 3.1**
    /// **Feature: windows-software-organizer, Property 5: 命名模板变量解析**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(NamingEngineArbitraries) })]
    public Property GenerateName_ValidTemplateAndEntry_NoUnresolvedVariables(
        SoftwareEntry entry, 
        string template)
    {
        // Arrange
        var engine = new NamingEngine();

        // Act
        var result = engine.GenerateName(entry, template);

        // Assert: No unresolved variable placeholders should remain
        var hasUnresolvedVariables = SupportedVariables.Any(v => 
            result.Contains($"{{{v}}}", StringComparison.OrdinalIgnoreCase));

        return (!hasUnresolvedVariables)
            .ToProperty()
            .Label($"Template: {template}, Result: {result}");
    }

    /// <summary>
    /// Property 5 (Variant): All supported variables should be replaced with non-empty values or "Unknown".
    /// **Validates: Requirements 3.1**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(NamingEngineArbitraries) })]
    public Property GenerateName_AllVariablesReplaced_ResultContainsNoPlaceholderBraces(
        SoftwareEntry entry,
        string template)
    {
        // Arrange
        var engine = new NamingEngine();

        // Act
        var result = engine.GenerateName(entry, template);

        // Assert: Result should not contain any {Variable} patterns
        var containsPlaceholder = result.Contains('{') && result.Contains('}');

        return (!containsPlaceholder)
            .ToProperty()
            .Label($"Template: {template}, Result: {result}, ContainsPlaceholder: {containsPlaceholder}");
    }

    #endregion

    #region Property 6: 模板验证一致性

    /// <summary>
    /// Property 6: 模板验证一致性
    /// 对于任意模板字符串，如果验证结果为有效，则使用该模板生成名称不应抛出异常；
    /// 如果验证结果为无效，则Errors列表必须非空。
    /// **Validates: Requirements 3.2**
    /// **Feature: windows-software-organizer, Property 6: 模板验证一致性**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(NamingEngineArbitraries) })]
    public Property ValidateTemplate_Consistency_ValidMeansNoException(string template)
    {
        // Arrange
        var engine = new NamingEngine();
        var entry = CreateTestEntry();

        // Act
        var validationResult = engine.ValidateTemplate(template);

        if (validationResult.IsValid)
        {
            // If valid, GenerateName should not throw
            try
            {
                var name = engine.GenerateName(entry, template);
                return (name != null)
                    .ToProperty()
                    .Label($"Valid template '{template}' generated name successfully");
            }
            catch (Exception ex)
            {
                return false.ToProperty()
                    .Label($"Valid template '{template}' threw exception: {ex.Message}");
            }
        }
        else
        {
            // If invalid, Errors must be non-empty
            return (validationResult.Errors.Count > 0)
                .ToProperty()
                .Label($"Invalid template '{template}' has {validationResult.Errors.Count} error(s)");
        }
    }

    /// <summary>
    /// Property 6 (Variant): Valid templates from preset list should always pass validation.
    /// **Validates: Requirements 3.2, 3.5**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ValidateTemplate_PresetTemplates_AlwaysValid()
    {
        // Arrange
        var engine = new NamingEngine();
        var presets = engine.GetPresetTemplates();

        // Assert: All preset templates should be valid
        var allValid = presets.All(t => engine.ValidateTemplate(t.Pattern).IsValid);

        return allValid.ToProperty()
            .Label($"All {presets.Count} preset templates should be valid");
    }

    #endregion

    #region Property 7: 文件名合法性

    /// <summary>
    /// Property 7: 文件名合法性
    /// 对于任意输入字符串，经过SanitizeFileName处理后的输出不应包含Windows文件系统禁止的字符（\ / : * ? " &lt; &gt; |）。
    /// **Validates: Requirements 3.3**
    /// **Feature: windows-software-organizer, Property 7: 文件名合法性**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(NamingEngineArbitraries) })]
    public Property SanitizeFileName_AnyInput_NoIllegalCharacters(string input)
    {
        // Skip null/empty inputs
        if (string.IsNullOrEmpty(input))
            return true.ToProperty().Label("Skipped null/empty input");

        // Arrange
        var engine = new NamingEngine();

        // Act
        var result = engine.SanitizeFileName(input);

        // Assert: Result should not contain any illegal characters
        var containsIllegal = IllegalFileNameChars.Any(c => result.Contains(c));

        return (!containsIllegal)
            .ToProperty()
            .Label($"Input: '{TruncateForLabel(input)}', Result: '{TruncateForLabel(result)}'");
    }

    /// <summary>
    /// Property 7 (Variant): Sanitized output should have same length as input (characters replaced, not removed).
    /// **Validates: Requirements 3.3**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(NamingEngineArbitraries) })]
    public Property SanitizeFileName_PreservesLength(string input)
    {
        // Skip null/empty inputs
        if (string.IsNullOrEmpty(input))
            return true.ToProperty().Label("Skipped null/empty input");

        // Arrange
        var engine = new NamingEngine();

        // Act
        var result = engine.SanitizeFileName(input);

        // Assert: Length should be preserved (characters are replaced, not removed)
        return (result.Length == input.Length)
            .ToProperty()
            .Label($"Input length: {input.Length}, Result length: {result.Length}");
    }

    /// <summary>
    /// Property 7 (Variant): Characters that are not illegal should be preserved.
    /// **Validates: Requirements 3.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property SanitizeFileName_LegalCharactersPreserved(NonEmptyString input)
    {
        // Arrange
        var engine = new NamingEngine();
        var inputStr = input.Get;

        // Act
        var result = engine.SanitizeFileName(inputStr);

        // Assert: Legal characters should be preserved at their positions
        for (int i = 0; i < inputStr.Length; i++)
        {
            var c = inputStr[i];
            if (!IllegalFileNameChars.Contains(c) && !char.IsControl(c))
            {
                if (result[i] != c)
                {
                    return false.ToProperty()
                        .Label($"Legal character '{c}' at position {i} was changed to '{result[i]}'");
                }
            }
        }

        return true.ToProperty()
            .Label($"All legal characters preserved in '{TruncateForLabel(inputStr)}'");
    }

    #endregion

    #region Property 8: 目录名唯一性

    /// <summary>
    /// Property 8: 目录名唯一性
    /// 对于任意基础路径和期望名称，ResolveConflict返回的路径在文件系统中必须不存在或与期望路径相同。
    /// **Validates: Requirements 3.4**
    /// **Feature: windows-software-organizer, Property 8: 目录名唯一性**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ResolveConflict_ReturnsUniqueOrOriginalPath(PositiveInt conflictCount)
    {
        // Arrange
        var mockFileSystem = new PropertyTestFileSystem();
        var engine = new NamingEngine(mockFileSystem);
        var basePath = @"C:\Software";
        var desiredName = "TestApp";

        // Setup conflicts
        var numConflicts = Math.Min(conflictCount.Get, 100); // Limit to reasonable number
        mockFileSystem.ExistingDirectories.Add(Path.Combine(basePath, desiredName));
        for (int i = 1; i < numConflicts; i++)
        {
            mockFileSystem.ExistingDirectories.Add(Path.Combine(basePath, $"{desiredName}_{i}"));
        }

        // Act
        var result = engine.ResolveConflict(basePath, desiredName);

        // Assert: Result should not exist in the mock file system
        var resultExists = mockFileSystem.DirectoryExists(result);

        return (!resultExists)
            .ToProperty()
            .Label($"Conflicts: {numConflicts}, Result: {result}, Exists: {resultExists}");
    }

    /// <summary>
    /// Property 8 (Variant): When no conflict exists, original path should be returned.
    /// **Validates: Requirements 3.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ResolveConflict_NoConflict_ReturnsOriginalPath(NonEmptyString desiredName)
    {
        // Arrange
        var mockFileSystem = new PropertyTestFileSystem(); // Empty - no conflicts
        var engine = new NamingEngine(mockFileSystem);
        var basePath = @"C:\Software";
        var name = SanitizeName(desiredName.Get);

        if (string.IsNullOrWhiteSpace(name))
            return true.ToProperty().Label("Skipped invalid name");

        // Act
        var result = engine.ResolveConflict(basePath, name);

        // Assert: Should return the original path
        var expectedPath = Path.Combine(basePath, name);

        return (result == expectedPath)
            .ToProperty()
            .Label($"Expected: {expectedPath}, Got: {result}");
    }

    /// <summary>
    /// Property 8 (Variant): Resolved path should always start with base path.
    /// **Validates: Requirements 3.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ResolveConflict_ResultStartsWithBasePath(PositiveInt conflictCount)
    {
        // Arrange
        var mockFileSystem = new PropertyTestFileSystem();
        var engine = new NamingEngine(mockFileSystem);
        var basePath = @"D:\Programs";
        var desiredName = "MyApp";

        // Setup some conflicts
        var numConflicts = Math.Min(conflictCount.Get, 50);
        for (int i = 0; i < numConflicts; i++)
        {
            var suffix = i == 0 ? "" : $"_{i}";
            mockFileSystem.ExistingDirectories.Add(Path.Combine(basePath, $"{desiredName}{suffix}"));
        }

        // Act
        var result = engine.ResolveConflict(basePath, desiredName);

        // Assert: Result should start with base path
        return result.StartsWith(basePath)
            .ToProperty()
            .Label($"BasePath: {basePath}, Result: {result}");
    }

    #endregion

    #region Helper Methods

    private static SoftwareEntry CreateTestEntry()
    {
        return new SoftwareEntry
        {
            Id = "test-id",
            Name = "TestApp",
            Version = "1.0.0",
            Vendor = "TestVendor",
            InstallPath = @"C:\Program Files\TestApp",
            InstallDate = DateTime.Now,
            Category = SoftwareCategory.DevTool
        };
    }

    private static string TruncateForLabel(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "<empty>";
        if (input.Length <= 30)
            return input.Replace("\n", "\\n").Replace("\t", "\\t");
        return input.Substring(0, 27).Replace("\n", "\\n").Replace("\t", "\\t") + "...";
    }

    private static string SanitizeName(string name)
    {
        // Remove characters that would cause path issues
        var result = name;
        foreach (var c in IllegalFileNameChars)
        {
            result = result.Replace(c.ToString(), "");
        }
        return result.Trim();
    }

    #endregion
}

/// <summary>
/// Mock file system for property-based testing of ResolveConflict.
/// </summary>
internal class PropertyTestFileSystem : IFileSystemAbstraction
{
    public HashSet<string> ExistingDirectories { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool DirectoryExists(string path)
    {
        return ExistingDirectories.Contains(path);
    }
}
