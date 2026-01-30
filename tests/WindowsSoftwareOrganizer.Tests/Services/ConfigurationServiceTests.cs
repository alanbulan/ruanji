using System.Text.Json;
using System.Text.Json.Serialization;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.Infrastructure.Services;

namespace WindowsSoftwareOrganizer.Tests.Services;

/// <summary>
/// Unit tests for ConfigurationService.
/// Validates: Requirements 9.1, 9.2, 9.3, 9.4, 9.5
/// </summary>
public class ConfigurationServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _testConfigPath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public ConfigurationServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "ConfigServiceTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
        _testConfigPath = Path.Combine(_testDirectory, "config.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    #region GetConfigurationAsync Tests

    [Fact]
    public async Task GetConfigurationAsync_WhenFileDoesNotExist_ReturnsDefaultConfiguration()
    {
        // Arrange
        var service = new ConfigurationService(_testConfigPath);

        // Act
        var config = await service.GetConfigurationAsync();

        // Assert
        Assert.NotNull(config);
        Assert.Equal(@"D:\Software", config.DefaultTargetPath);
        Assert.Equal("detailed", config.DefaultNamingTemplateId);
        Assert.Equal(LinkType.Junction, config.PreferredLinkType);
        Assert.True(config.AutoUpdateRegistry);
        Assert.True(config.MoveToRecycleBin);
        Assert.Equal(30, config.OperationHistoryDays);
        Assert.Equal(ThemeMode.System, config.Theme);
    }

    [Fact]
    public async Task GetConfigurationAsync_WhenValidFileExists_ReturnsDeserializedConfiguration()
    {
        // Arrange
        var expectedConfig = new AppConfiguration
        {
            DefaultTargetPath = @"E:\MyApps",
            DefaultNamingTemplateId = "simple",
            PreferredLinkType = LinkType.SymbolicLink,
            AutoUpdateRegistry = false,
            MoveToRecycleBin = false,
            OperationHistoryDays = 60,
            Theme = ThemeMode.Dark
        };
        var json = JsonSerializer.Serialize(expectedConfig, JsonOptions);
        await File.WriteAllTextAsync(_testConfigPath, json);

        var service = new ConfigurationService(_testConfigPath);

        // Act
        var config = await service.GetConfigurationAsync();

        // Assert
        Assert.Equal(@"E:\MyApps", config.DefaultTargetPath);
        Assert.Equal("simple", config.DefaultNamingTemplateId);
        Assert.Equal(LinkType.SymbolicLink, config.PreferredLinkType);
        Assert.False(config.AutoUpdateRegistry);
        Assert.False(config.MoveToRecycleBin);
        Assert.Equal(60, config.OperationHistoryDays);
        Assert.Equal(ThemeMode.Dark, config.Theme);
    }

    [Fact]
    public async Task GetConfigurationAsync_CachesConfiguration()
    {
        // Arrange
        var config = new AppConfiguration { DefaultTargetPath = @"C:\Test" };
        var json = JsonSerializer.Serialize(config, JsonOptions);
        await File.WriteAllTextAsync(_testConfigPath, json);

        var service = new ConfigurationService(_testConfigPath);

        // Act
        var config1 = await service.GetConfigurationAsync();
        
        // Modify file after first read
        var newConfig = new AppConfiguration { DefaultTargetPath = @"D:\Modified" };
        await File.WriteAllTextAsync(_testConfigPath, JsonSerializer.Serialize(newConfig, JsonOptions));
        
        var config2 = await service.GetConfigurationAsync();

        // Assert - should return cached value, not the modified file
        Assert.Same(config1, config2);
        Assert.Equal(@"C:\Test", config2.DefaultTargetPath);
    }

    [Fact]
    public async Task GetConfigurationAsync_WhenCorruptedJson_ReturnsDefaultAndRaisesEvent()
    {
        // Arrange
        await File.WriteAllTextAsync(_testConfigPath, "{ invalid json }");
        var service = new ConfigurationService(_testConfigPath);
        
        ConfigurationCorruptedEventArgs? eventArgs = null;
        service.ConfigurationCorrupted += (sender, args) => eventArgs = args;

        // Act
        var config = await service.GetConfigurationAsync();

        // Assert
        Assert.NotNull(config);
        Assert.Equal(@"D:\Software", config.DefaultTargetPath); // Default value
        Assert.NotNull(eventArgs);
        Assert.Contains("JSON", eventArgs.ErrorMessage);
        Assert.Equal(_testConfigPath, eventArgs.ConfigFilePath);
    }

    [Fact]
    public async Task GetConfigurationAsync_WhenEmptyFile_ReturnsDefaultAndRaisesEvent()
    {
        // Arrange
        await File.WriteAllTextAsync(_testConfigPath, "");
        var service = new ConfigurationService(_testConfigPath);
        
        ConfigurationCorruptedEventArgs? eventArgs = null;
        service.ConfigurationCorrupted += (sender, args) => eventArgs = args;

        // Act
        var config = await service.GetConfigurationAsync();

        // Assert
        Assert.NotNull(config);
        Assert.Equal(@"D:\Software", config.DefaultTargetPath);
        Assert.NotNull(eventArgs);
    }

    [Fact]
    public async Task GetConfigurationAsync_WhenJsonArray_ReturnsDefaultAndRaisesEvent()
    {
        // Arrange
        await File.WriteAllTextAsync(_testConfigPath, "[]");
        var service = new ConfigurationService(_testConfigPath);
        
        ConfigurationCorruptedEventArgs? eventArgs = null;
        service.ConfigurationCorrupted += (sender, args) => eventArgs = args;

        // Act
        var config = await service.GetConfigurationAsync();

        // Assert
        Assert.NotNull(config);
        Assert.Equal(@"D:\Software", config.DefaultTargetPath);
        Assert.NotNull(eventArgs);
        Assert.Contains("JSON对象", eventArgs.ErrorMessage);
    }

    #endregion

    #region SaveConfigurationAsync Tests

    [Fact]
    public async Task SaveConfigurationAsync_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var nestedPath = Path.Combine(_testDirectory, "nested", "folder", "config.json");
        var service = new ConfigurationService(nestedPath);
        var config = new AppConfiguration();

        // Act
        await service.SaveConfigurationAsync(config);

        // Assert
        Assert.True(File.Exists(nestedPath));
    }

    [Fact]
    public async Task SaveConfigurationAsync_WritesValidJson()
    {
        // Arrange
        var service = new ConfigurationService(_testConfigPath);
        var config = new AppConfiguration
        {
            DefaultTargetPath = @"F:\Software",
            Theme = ThemeMode.Light,
            OperationHistoryDays = 45
        };

        // Act
        await service.SaveConfigurationAsync(config);

        // Assert
        var json = await File.ReadAllTextAsync(_testConfigPath);
        var validation = service.ValidateConfiguration(json);
        Assert.True(validation.IsValid);
    }

    [Fact]
    public async Task SaveConfigurationAsync_UpdatesCache()
    {
        // Arrange
        var service = new ConfigurationService(_testConfigPath);
        var config1 = new AppConfiguration { DefaultTargetPath = @"C:\First" };
        var config2 = new AppConfiguration { DefaultTargetPath = @"D:\Second" };

        // Act
        await service.SaveConfigurationAsync(config1);
        var retrieved1 = await service.GetConfigurationAsync();
        
        await service.SaveConfigurationAsync(config2);
        var retrieved2 = await service.GetConfigurationAsync();

        // Assert
        Assert.Equal(@"C:\First", retrieved1.DefaultTargetPath);
        Assert.Equal(@"D:\Second", retrieved2.DefaultTargetPath);
    }

    [Fact]
    public async Task SaveConfigurationAsync_ThrowsOnNullConfiguration()
    {
        // Arrange
        var service = new ConfigurationService(_testConfigPath);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.SaveConfigurationAsync(null!));
    }

    [Fact]
    public async Task SaveConfigurationAsync_PreservesUserClassifications()
    {
        // Arrange
        var service = new ConfigurationService(_testConfigPath);
        var config = new AppConfiguration
        {
            UserClassifications = new Dictionary<string, SoftwareCategory>
            {
                ["vscode"] = SoftwareCategory.IDE,
                ["chrome"] = SoftwareCategory.Browser
            }
        };

        // Act
        await service.SaveConfigurationAsync(config);
        service.ClearCache();
        var loaded = await service.GetConfigurationAsync();

        // Assert
        Assert.Equal(2, loaded.UserClassifications.Count);
        Assert.Equal(SoftwareCategory.IDE, loaded.UserClassifications["vscode"]);
        Assert.Equal(SoftwareCategory.Browser, loaded.UserClassifications["chrome"]);
    }

    [Fact]
    public async Task SaveConfigurationAsync_PreservesCustomTemplates()
    {
        // Arrange
        var service = new ConfigurationService(_testConfigPath);
        var config = new AppConfiguration
        {
            CustomTemplates = new List<NamingTemplate>
            {
                new NamingTemplate
                {
                    Id = "custom1",
                    Name = "My Template",
                    Pattern = "{Name}_{Version}",
                    Description = "Custom template",
                    IsPreset = false
                }
            }
        };

        // Act
        await service.SaveConfigurationAsync(config);
        service.ClearCache();
        var loaded = await service.GetConfigurationAsync();

        // Assert
        Assert.Single(loaded.CustomTemplates);
        Assert.Equal("custom1", loaded.CustomTemplates[0].Id);
        Assert.Equal("My Template", loaded.CustomTemplates[0].Name);
        Assert.Equal("{Name}_{Version}", loaded.CustomTemplates[0].Pattern);
    }

    #endregion

    #region ResetToDefaultAsync Tests

    [Fact]
    public async Task ResetToDefaultAsync_ReturnsDefaultConfiguration()
    {
        // Arrange
        var service = new ConfigurationService(_testConfigPath);
        var customConfig = new AppConfiguration { DefaultTargetPath = @"X:\Custom" };
        await service.SaveConfigurationAsync(customConfig);

        // Act
        var defaultConfig = await service.ResetToDefaultAsync();

        // Assert
        Assert.Equal(@"D:\Software", defaultConfig.DefaultTargetPath);
        Assert.Equal("detailed", defaultConfig.DefaultNamingTemplateId);
    }

    [Fact]
    public async Task ResetToDefaultAsync_SavesDefaultToFile()
    {
        // Arrange
        var service = new ConfigurationService(_testConfigPath);
        var customConfig = new AppConfiguration { DefaultTargetPath = @"X:\Custom" };
        await service.SaveConfigurationAsync(customConfig);

        // Act
        await service.ResetToDefaultAsync();
        service.ClearCache();
        var loaded = await service.GetConfigurationAsync();

        // Assert
        Assert.Equal(@"D:\Software", loaded.DefaultTargetPath);
    }

    #endregion

    #region ValidateConfiguration Tests

    [Fact]
    public void ValidateConfiguration_ValidJson_ReturnsSuccess()
    {
        // Arrange
        var service = new ConfigurationService(_testConfigPath);
        var config = new AppConfiguration();
        var json = JsonSerializer.Serialize(config, JsonOptions);

        // Act
        var result = service.ValidateConfiguration(json);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateConfiguration_EmptyString_ReturnsFailure()
    {
        // Arrange
        var service = new ConfigurationService(_testConfigPath);

        // Act
        var result = service.ValidateConfiguration("");

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void ValidateConfiguration_WhitespaceOnly_ReturnsFailure()
    {
        // Arrange
        var service = new ConfigurationService(_testConfigPath);

        // Act
        var result = service.ValidateConfiguration("   ");

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("不能为空"));
    }

    [Fact]
    public void ValidateConfiguration_InvalidJson_ReturnsFailure()
    {
        // Arrange
        var service = new ConfigurationService(_testConfigPath);

        // Act
        var result = service.ValidateConfiguration("{ not valid json }");

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("JSON格式错误"));
    }

    [Fact]
    public void ValidateConfiguration_JsonArray_ReturnsFailure()
    {
        // Arrange
        var service = new ConfigurationService(_testConfigPath);

        // Act
        var result = service.ValidateConfiguration("[1, 2, 3]");

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("JSON对象"));
    }

    [Fact]
    public void ValidateConfiguration_NegativeOperationHistoryDays_ReturnsFailure()
    {
        // Arrange
        var service = new ConfigurationService(_testConfigPath);
        var json = """{"operationHistoryDays": -5}""";

        // Act
        var result = service.ValidateConfiguration(json);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("操作历史天数"));
    }

    [Fact]
    public void ValidateConfiguration_EmptyObject_ReturnsSuccess()
    {
        // Arrange
        var service = new ConfigurationService(_testConfigPath);

        // Act
        var result = service.ValidateConfiguration("{}");

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateConfiguration_PartialConfig_ReturnsSuccess()
    {
        // Arrange
        var service = new ConfigurationService(_testConfigPath);
        var json = """{"defaultTargetPath": "C:\\Test", "theme": "Dark"}""";

        // Act
        var result = service.ValidateConfiguration(json);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateConfiguration_UnknownProperties_ReturnsSuccess()
    {
        // Arrange
        var service = new ConfigurationService(_testConfigPath);
        var json = """{"unknownProperty": "value", "defaultTargetPath": "C:\\Test"}""";

        // Act
        var result = service.ValidateConfiguration(json);

        // Assert
        Assert.True(result.IsValid); // Unknown properties should be ignored
    }

    #endregion

    #region ConfigFilePath Tests

    [Fact]
    public void ConfigFilePath_ReturnsProvidedPath()
    {
        // Arrange
        var customPath = @"C:\Custom\config.json";
        var service = new ConfigurationService(customPath);

        // Act & Assert
        Assert.Equal(customPath, service.ConfigFilePath);
    }

    [Fact]
    public void ConfigFilePath_WhenNullProvided_ReturnsDefaultPath()
    {
        // Arrange
        var service = new ConfigurationService(null);

        // Act
        var path = service.ConfigFilePath;

        // Assert
        Assert.Contains("WindowsSoftwareOrganizer", path);
        Assert.EndsWith("config.json", path);
    }

    #endregion

    #region ClearCache Tests

    [Fact]
    public async Task ClearCache_ForcesReloadFromFile()
    {
        // Arrange
        var config1 = new AppConfiguration { DefaultTargetPath = @"C:\First" };
        await File.WriteAllTextAsync(_testConfigPath, JsonSerializer.Serialize(config1, JsonOptions));
        
        var service = new ConfigurationService(_testConfigPath);
        var loaded1 = await service.GetConfigurationAsync();
        
        // Modify file
        var config2 = new AppConfiguration { DefaultTargetPath = @"D:\Second" };
        await File.WriteAllTextAsync(_testConfigPath, JsonSerializer.Serialize(config2, JsonOptions));

        // Act
        service.ClearCache();
        var loaded2 = await service.GetConfigurationAsync();

        // Assert
        Assert.Equal(@"C:\First", loaded1.DefaultTargetPath);
        Assert.Equal(@"D:\Second", loaded2.DefaultTargetPath);
    }

    #endregion

    #region Enum Serialization Tests

    [Fact]
    public async Task SaveAndLoad_PreservesLinkTypeEnum()
    {
        // Arrange
        var service = new ConfigurationService(_testConfigPath);
        var config = new AppConfiguration { PreferredLinkType = LinkType.SymbolicLink };

        // Act
        await service.SaveConfigurationAsync(config);
        service.ClearCache();
        var loaded = await service.GetConfigurationAsync();

        // Assert
        Assert.Equal(LinkType.SymbolicLink, loaded.PreferredLinkType);
    }

    [Fact]
    public async Task SaveAndLoad_PreservesThemeModeEnum()
    {
        // Arrange
        var service = new ConfigurationService(_testConfigPath);
        var config = new AppConfiguration { Theme = ThemeMode.Dark };

        // Act
        await service.SaveConfigurationAsync(config);
        service.ClearCache();
        var loaded = await service.GetConfigurationAsync();

        // Assert
        Assert.Equal(ThemeMode.Dark, loaded.Theme);
    }

    [Fact]
    public async Task SaveAndLoad_PreservesSoftwareCategoryEnum()
    {
        // Arrange
        var service = new ConfigurationService(_testConfigPath);
        var config = new AppConfiguration
        {
            UserClassifications = new Dictionary<string, SoftwareCategory>
            {
                ["test"] = SoftwareCategory.Database
            }
        };

        // Act
        await service.SaveConfigurationAsync(config);
        service.ClearCache();
        var loaded = await service.GetConfigurationAsync();

        // Assert
        Assert.Equal(SoftwareCategory.Database, loaded.UserClassifications["test"]);
    }

    #endregion
}
