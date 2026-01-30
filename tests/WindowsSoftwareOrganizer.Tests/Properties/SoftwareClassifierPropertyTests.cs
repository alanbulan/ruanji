using FsCheck;
using FsCheck.Xunit;
using NSubstitute;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.Infrastructure.Services;

namespace WindowsSoftwareOrganizer.Tests.Properties;

/// <summary>
/// Property-based tests for SoftwareClassifier implementation.
/// Tests Properties 3, 4 from the design document.
/// </summary>
public class SoftwareClassifierPropertyTests
{
    #region Arbitraries

    /// <summary>
    /// Custom FsCheck generators for SoftwareClassifier property tests.
    /// </summary>
    public class ClassifierArbitraries
    {
        /// <summary>
        /// Generates arbitrary SoftwareEntry objects for testing.
        /// </summary>
        public static Arbitrary<SoftwareEntry> SoftwareEntryArb()
        {
            var names = new[]
            {
                "Visual Studio Code", "Chrome", "Firefox", "Git", "NodeJS",
                "Python 3.11", "Java SDK", "Docker Desktop", "MySQL Workbench",
                "VLC Media Player", "7-Zip", "Unknown App", "Random Software",
                "My Custom Tool", "Test Application"
            };
            var vendors = new[]
            {
                "Microsoft Corporation", "Google LLC", "Mozilla", "JetBrains",
                "Oracle Corporation", "Docker Inc", "Adobe Inc.", null, ""
            };
            var versions = new[] { "1.0.0", "2.5.3", "10.0.1", null };
            var paths = new[]
            {
                @"C:\Program Files\TestApp",
                @"C:\Program Files (x86)\TestApp",
                @"D:\Software\TestApp",
                @"C:\Users\Test\AppData\Local\TestApp"
            };

            return (from name in Gen.Elements(names)
                    from vendor in Gen.Elements(vendors)
                    from version in Gen.Elements(versions)
                    from path in Gen.Elements(paths)
                    select new SoftwareEntry
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = name,
                        Vendor = vendor,
                        Version = version,
                        InstallPath = path
                    }).ToArbitrary();
        }

        /// <summary>
        /// Generates arbitrary software IDs for testing.
        /// </summary>
        public static Arbitrary<string> SoftwareIdArb()
        {
            var ids = new[]
            {
                "vscode", "chrome", "firefox", "git", "nodejs",
                "python", "java-sdk", "docker", "mysql", "vlc",
                "test-app-1", "test-app-2", "custom-software"
            };
            return Gen.Elements(ids).ToArbitrary();
        }
    }

    #endregion

    #region Property 3: 分类结果有效性

    /// <summary>
    /// Property 3: 分类结果有效性
    /// 对于任意SoftwareEntry，分类器返回的Category必须是SoftwareCategory枚举的有效值之一。
    /// **Validates: Requirements 2.1**
    /// **Feature: windows-software-organizer, Property 3: 分类结果有效性**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ClassifierArbitraries) })]
    public Property Classify_AnyEntry_ReturnsValidCategory(SoftwareEntry entry)
    {
        // Arrange
        var mockConfigService = CreateMockConfigService();
        var classifier = new SoftwareClassifier(mockConfigService);

        // Act
        var category = classifier.Classify(entry);

        // Assert: Category must be a valid enum value
        var isValidCategory = Enum.IsDefined(typeof(SoftwareCategory), category);

        return isValidCategory.ToProperty()
            .Label($"Entry: {entry.Name}, Category: {category}");
    }

    /// <summary>
    /// Property 3 (Variant): Known software names should be classified correctly.
    /// **Validates: Requirements 2.1**
    /// </summary>
    [Theory]
    [InlineData("VS Code", SoftwareCategory.CodeEditor)]
    [InlineData("PyCharm", SoftwareCategory.IDE)]
    [InlineData("Chrome", SoftwareCategory.Browser)]
    [InlineData("Firefox", SoftwareCategory.Browser)]
    [InlineData("Git", SoftwareCategory.VersionControl)]
    [InlineData("Docker Desktop", SoftwareCategory.Virtualization)]
    [InlineData("MySQL Workbench", SoftwareCategory.Database)]
    [InlineData("VLC Media Player", SoftwareCategory.Video)]
    [InlineData("7-Zip", SoftwareCategory.Compression)]
    [InlineData("Java Runtime", SoftwareCategory.Runtime)]
    public void Classify_KnownSoftware_ReturnsExpectedCategory(string name, SoftwareCategory expected)
    {
        // Arrange
        var mockConfigService = CreateMockConfigService();
        var classifier = new SoftwareClassifier(mockConfigService);
        var entry = new SoftwareEntry
        {
            Id = "test",
            Name = name,
            InstallPath = @"C:\Program Files\Test"
        };

        // Act
        var category = classifier.Classify(entry);

        // Assert
        Assert.Equal(expected, category);
    }

    /// <summary>
    /// Property 3 (Variant): Unknown software should default to Other.
    /// **Validates: Requirements 2.3**
    /// </summary>
    [Fact]
    public void Classify_UnknownSoftware_ReturnsOther()
    {
        // Arrange
        var mockConfigService = CreateMockConfigService();
        var classifier = new SoftwareClassifier(mockConfigService);
        var entry = new SoftwareEntry
        {
            Id = "unknown",
            Name = "Completely Unknown Random Software XYZ",
            InstallPath = @"C:\Program Files\Unknown"
        };

        // Act
        var category = classifier.Classify(entry);

        // Assert
        Assert.Equal(SoftwareCategory.Other, category);
    }

    #endregion

    #region Property 4: 用户分类持久化往返

    /// <summary>
    /// Property 4: 用户分类持久化往返
    /// 对于任意软件ID和分类值，保存用户分类后再加载，应该得到相同的分类值。
    /// **Validates: Requirements 2.4**
    /// **Feature: windows-software-organizer, Property 4: 用户分类持久化往返**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ClassifierArbitraries) })]
    public Property UserClassification_SaveAndLoad_RoundTrip(string softwareId, SoftwareCategory category)
    {
        // Arrange
        var savedClassifications = new Dictionary<string, SoftwareCategory>();
        var mockConfigService = CreateMockConfigServiceWithStorage(savedClassifications);
        var classifier = new SoftwareClassifier(mockConfigService);

        // Act: Save classification
        classifier.SaveUserClassificationAsync(softwareId, category).Wait();

        // Act: Load classification
        var loadedCategory = classifier.GetUserClassificationAsync(softwareId).Result;

        // Assert: Loaded category should match saved category
        return (loadedCategory.HasValue && loadedCategory.Value == category)
            .ToProperty()
            .Label($"SoftwareId: {softwareId}, Saved: {category}, Loaded: {loadedCategory}");
    }

    /// <summary>
    /// Property 4 (Variant): Getting classification for non-existent ID returns null.
    /// **Validates: Requirements 2.4**
    /// </summary>
    [Fact]
    public async Task GetUserClassification_NonExistentId_ReturnsNull()
    {
        // Arrange
        var mockConfigService = CreateMockConfigService();
        var classifier = new SoftwareClassifier(mockConfigService);

        // Act
        var category = await classifier.GetUserClassificationAsync("non-existent-id");

        // Assert
        Assert.Null(category);
    }

    /// <summary>
    /// Property 4 (Variant): Saving classification overwrites previous value.
    /// **Validates: Requirements 2.4**
    /// </summary>
    [Fact]
    public async Task SaveUserClassification_OverwritesPreviousValue()
    {
        // Arrange
        var savedClassifications = new Dictionary<string, SoftwareCategory>();
        var mockConfigService = CreateMockConfigServiceWithStorage(savedClassifications);
        var classifier = new SoftwareClassifier(mockConfigService);
        var softwareId = "test-software";

        // Act: Save first classification
        await classifier.SaveUserClassificationAsync(softwareId, SoftwareCategory.IDE);
        var first = await classifier.GetUserClassificationAsync(softwareId);

        // Act: Save second classification (overwrite)
        await classifier.SaveUserClassificationAsync(softwareId, SoftwareCategory.DevTool);
        var second = await classifier.GetUserClassificationAsync(softwareId);

        // Assert
        Assert.Equal(SoftwareCategory.IDE, first);
        Assert.Equal(SoftwareCategory.DevTool, second);
    }

    #endregion

    #region Related Directories Tests

    /// <summary>
    /// Verifies that FindRelatedDirectories returns valid directory types.
    /// **Validates: Requirements 2.2**
    /// </summary>
    [Fact]
    public void FindRelatedDirectories_ReturnsValidDirectoryTypes()
    {
        // Arrange
        var mockConfigService = CreateMockConfigService();
        var classifier = new SoftwareClassifier(mockConfigService);
        var tempDir = Path.Combine(Path.GetTempPath(), $"ClassifierTest_{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(tempDir);
            var entry = new SoftwareEntry
            {
                Id = "test",
                Name = "TestApp",
                InstallPath = tempDir
            };

            // Act
            var directories = classifier.FindRelatedDirectories(entry);

            // Assert: All directory types should be valid enum values
            foreach (var dir in directories)
            {
                Assert.True(Enum.IsDefined(typeof(DirectoryType), dir.Type),
                    $"Invalid directory type: {dir.Type}");
                Assert.True(dir.SizeBytes >= 0,
                    $"Directory size should be non-negative: {dir.SizeBytes}");
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// Verifies that FindRelatedDirectories includes the install directory.
    /// **Validates: Requirements 2.2**
    /// </summary>
    [Fact]
    public void FindRelatedDirectories_IncludesInstallDirectory()
    {
        // Arrange
        var mockConfigService = CreateMockConfigService();
        var classifier = new SoftwareClassifier(mockConfigService);
        var tempDir = Path.Combine(Path.GetTempPath(), $"ClassifierTest_{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(tempDir);
            var entry = new SoftwareEntry
            {
                Id = "test",
                Name = "TestApp",
                InstallPath = tempDir
            };

            // Act
            var directories = classifier.FindRelatedDirectories(entry);

            // Assert: Should include the install directory
            Assert.Contains(directories, d =>
                d.Path.Equals(tempDir, StringComparison.OrdinalIgnoreCase) &&
                d.Type == DirectoryType.Install);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    #endregion

    #region Helper Methods

    private static IConfigurationService CreateMockConfigService()
    {
        var mock = Substitute.For<IConfigurationService>();
        mock.GetConfigurationAsync().Returns(Task.FromResult(new AppConfiguration()));
        return mock;
    }

    private static IConfigurationService CreateMockConfigServiceWithStorage(
        Dictionary<string, SoftwareCategory> storage)
    {
        var mock = Substitute.For<IConfigurationService>();

        mock.GetConfigurationAsync().Returns(callInfo =>
            Task.FromResult(new AppConfiguration
            {
                UserClassifications = new Dictionary<string, SoftwareCategory>(storage)
            }));

        mock.SaveConfigurationAsync(Arg.Any<AppConfiguration>())
            .Returns(callInfo =>
            {
                var config = callInfo.Arg<AppConfiguration>();
                storage.Clear();
                foreach (var kvp in config.UserClassifications)
                {
                    storage[kvp.Key] = kvp.Value;
                }
                return Task.CompletedTask;
            });

        return mock;
    }

    #endregion
}
