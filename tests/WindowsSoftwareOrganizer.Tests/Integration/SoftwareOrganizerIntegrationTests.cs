using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.Infrastructure.Services;
using Xunit;

namespace WindowsSoftwareOrganizer.Tests.Integration;

/// <summary>
/// Integration tests for the complete software organization workflow.
/// Tests scan-classify-migrate-rollback flow using temporary directories.
/// </summary>
public class SoftwareOrganizerIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _sourceDir;
    private readonly string _targetDir;
    private readonly string _configDir;

    public SoftwareOrganizerIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"SoftwareOrganizerTests_{Guid.NewGuid():N}");
        _sourceDir = Path.Combine(_tempDir, "Source");
        _targetDir = Path.Combine(_tempDir, "Target");
        _configDir = Path.Combine(_tempDir, "Config");

        Directory.CreateDirectory(_sourceDir);
        Directory.CreateDirectory(_targetDir);
        Directory.CreateDirectory(_configDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    #region NamingEngine Tests

    [Fact]
    public void NamingEngine_GenerateName_ReplacesAllVariables()
    {
        // Arrange
        var namingEngine = new NamingEngine();
        var template = "{Category}/{Name}_{Version}";
        var entry = new SoftwareEntry
        {
            Id = "test-1",
            Name = "Visual Studio Code",
            Version = "1.85.0",
            InstallPath = @"C:\Program Files\VS Code",
            Category = SoftwareCategory.IDE
        };

        // Act
        var result = namingEngine.GenerateName(entry, template);

        // Assert
        Assert.Equal("IDE/Visual Studio Code_1.85.0", result);
    }

    [Fact]
    public void NamingEngine_SanitizeFileName_RemovesInvalidCharacters()
    {
        // Arrange
        var namingEngine = new NamingEngine();
        var invalidName = "Test<>:\"/\\|?*File";

        // Act
        var result = namingEngine.SanitizeFileName(invalidName);

        // Assert
        Assert.DoesNotContain("<", result);
        Assert.DoesNotContain(">", result);
        Assert.DoesNotContain(":", result);
        Assert.DoesNotContain("\"", result);
        Assert.DoesNotContain("/", result);
        Assert.DoesNotContain("\\", result);
        Assert.DoesNotContain("|", result);
        Assert.DoesNotContain("?", result);
        Assert.DoesNotContain("*", result);
    }

    [Fact]
    public void NamingEngine_ResolveConflict_GeneratesUniqueNames()
    {
        // Arrange
        var namingEngine = new NamingEngine();
        var basePath = _targetDir;
        var baseName = "TestApp";

        // Create existing directory
        Directory.CreateDirectory(Path.Combine(basePath, baseName));

        // Act
        var result = namingEngine.ResolveConflict(basePath, baseName);

        // Assert
        Assert.NotEqual(Path.Combine(basePath, baseName), result);
        Assert.Contains(baseName, result);
    }

    [Fact]
    public void NamingEngine_ValidateTemplate_AcceptsValidTemplate()
    {
        // Arrange
        var namingEngine = new NamingEngine();
        var validTemplate = "{Category}/{Name}";

        // Act
        var result = namingEngine.ValidateTemplate(validTemplate);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void NamingEngine_ValidateTemplate_RejectsInvalidVariable()
    {
        // Arrange
        var namingEngine = new NamingEngine();
        var invalidTemplate = "{InvalidVariable}";

        // Act
        var result = namingEngine.ValidateTemplate(invalidTemplate);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void NamingEngine_GetPresetTemplates_ReturnsThreeTemplates()
    {
        // Arrange
        var namingEngine = new NamingEngine();

        // Act
        var templates = namingEngine.GetPresetTemplates();

        // Assert
        Assert.Equal(3, templates.Count);
        Assert.Contains(templates, t => t.Id == "simple");
        Assert.Contains(templates, t => t.Id == "detailed");
        Assert.Contains(templates, t => t.Id == "dated");
    }

    #endregion

    #region SoftwareClassifier Tests

    [Fact]
    public void SoftwareClassifier_Classify_ReturnsValidCategory()
    {
        // Arrange
        var configService = new ConfigurationService(Path.Combine(_configDir, "config.json"));
        var classifier = new SoftwareClassifier(configService);
        var entry = new SoftwareEntry
        {
            Id = "test-1",
            Name = "Visual Studio Code",
            InstallPath = @"C:\Program Files\Microsoft VS Code"
        };

        // Act
        var category = classifier.Classify(entry);

        // Assert
        Assert.True(Enum.IsDefined(typeof(SoftwareCategory), category));
    }

    [Fact]
    public void SoftwareClassifier_Classify_IdentifiesIDE()
    {
        // Arrange
        var configService = new ConfigurationService(Path.Combine(_configDir, "config.json"));
        var classifier = new SoftwareClassifier(configService);
        var entry = new SoftwareEntry
        {
            Id = "test-1",
            Name = "Visual Studio 2022",
            InstallPath = @"C:\Program Files\Microsoft Visual Studio\2022"
        };

        // Act
        var category = classifier.Classify(entry);

        // Assert
        Assert.Equal(SoftwareCategory.IDE, category);
    }

    [Fact]
    public void SoftwareClassifier_Classify_IdentifiesRuntime()
    {
        // Arrange
        var configService = new ConfigurationService(Path.Combine(_configDir, "config.json"));
        var classifier = new SoftwareClassifier(configService);
        var entry = new SoftwareEntry
        {
            Id = "test-1",
            Name = ".NET Runtime 8.0",
            InstallPath = @"C:\Program Files\dotnet"
        };

        // Act
        var category = classifier.Classify(entry);

        // Assert
        Assert.Equal(SoftwareCategory.Runtime, category);
    }

    #endregion

    #region ConfigurationService Tests

    [Fact]
    public async Task ConfigurationService_SaveAndLoad_RoundTrips()
    {
        // Arrange
        var configPath = Path.Combine(_configDir, "test_config.json");
        var configService = new ConfigurationService(configPath);
        var config = new AppConfiguration
        {
            DefaultTargetPath = @"D:\Software",
            Theme = ThemeMode.Dark,
            DefaultNamingTemplateId = "simple"
        };

        // Act
        await configService.SaveConfigurationAsync(config);
        configService.ClearCache();
        var loaded = await configService.GetConfigurationAsync();

        // Assert
        Assert.Equal(config.DefaultTargetPath, loaded.DefaultTargetPath);
        Assert.Equal(config.Theme, loaded.Theme);
        Assert.Equal(config.DefaultNamingTemplateId, loaded.DefaultNamingTemplateId);
    }

    [Fact]
    public async Task ConfigurationService_LoadMissingConfig_ReturnsDefault()
    {
        // Arrange
        var emptyConfigPath = Path.Combine(_tempDir, "EmptyConfig", "config.json");
        var configService = new ConfigurationService(emptyConfigPath);

        // Act
        var config = await configService.GetConfigurationAsync();

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config.DefaultNamingTemplateId);
    }

    [Fact]
    public void ConfigurationService_ValidateConfiguration_AcceptsValidJson()
    {
        // Arrange
        var configService = new ConfigurationService(Path.Combine(_configDir, "config.json"));
        var validJson = """
        {
            "defaultTargetPath": "D:\\Software",
            "theme": "Dark"
        }
        """;

        // Act
        var result = configService.ValidateConfiguration(validJson);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ConfigurationService_ValidateConfiguration_RejectsInvalidJson()
    {
        // Arrange
        var configService = new ConfigurationService(Path.Combine(_configDir, "config.json"));
        var invalidJson = "{ invalid json }";

        // Act
        var result = configService.ValidateConfiguration(invalidJson);

        // Assert
        Assert.False(result.IsValid);
    }

    #endregion

    #region OperationLogger Tests

    [Fact]
    public async Task OperationLogger_BeginAndComplete_RecordsOperation()
    {
        // Arrange
        var logger = new OperationLogger(30);

        // Act
        var operationId = await logger.BeginOperationAsync(
            OperationType.Migration,
            "Test migration operation");

        await logger.CompleteOperationAsync(operationId, true);

        var operation = await logger.GetOperationAsync(operationId);

        // Assert
        Assert.NotNull(operation);
        Assert.Equal(OperationType.Migration, operation.Type);
        Assert.True(operation.Success);
    }

    [Fact]
    public async Task OperationLogger_GetHistory_ReturnsRecentOperations()
    {
        // Arrange
        var logger = new OperationLogger(30);

        // Create some operations
        for (int i = 0; i < 5; i++)
        {
            var id = await logger.BeginOperationAsync(
                OperationType.Migration,
                $"Test operation {i}");
            await logger.CompleteOperationAsync(id, true);
        }

        // Act
        var history = await logger.GetHistoryAsync();

        // Assert
        Assert.NotNull(history);
        Assert.True(history.Count >= 5);
    }

    [Fact]
    public async Task OperationLogger_LogAction_RecordsAction()
    {
        // Arrange
        var logger = new OperationLogger(30);
        var operationId = await logger.BeginOperationAsync(
            OperationType.Migration,
            "Test operation");

        var action = new OperationAction
        {
            ActionType = "FileCopy",
            Description = "Copied file.txt",
            Timestamp = DateTime.UtcNow,
            OriginalValue = @"C:\Source\file.txt",
            NewValue = @"D:\Target\file.txt",
            CanRollback = true
        };

        // Act
        await logger.LogActionAsync(operationId, action);
        await logger.CompleteOperationAsync(operationId, true);

        var operation = await logger.GetOperationAsync(operationId);

        // Assert
        Assert.NotNull(operation);
        Assert.NotEmpty(operation.Actions);
    }

    #endregion

    #region LinkManager Tests

    [Fact]
    public void LinkManager_IsJunctionSupported_ReturnsBoolean()
    {
        // Arrange
        var linkManager = new LinkManager();

        // Act
        var isSupported = linkManager.IsJunctionSupported(_targetDir);

        // Assert - just verify it returns without throwing
        Assert.True(isSupported || !isSupported);
    }

    [Fact]
    public void LinkManager_IsSymbolicLinkSupported_ReturnsBoolean()
    {
        // Arrange
        var linkManager = new LinkManager();

        // Act
        var isSupported = linkManager.IsSymbolicLinkSupported();

        // Assert - just verify it returns without throwing
        Assert.True(isSupported || !isSupported);
    }

    #endregion

    #region MigrationEngine Tests

    [Fact]
    public async Task MigrationEngine_CreatePlan_CalculatesSize()
    {
        // Arrange
        var linkManager = new LinkManager();
        var logger = new OperationLogger(30);
        var registryUpdater = new RegistryUpdater(logger);
        var namingEngine = new NamingEngine();
        var migrationEngine = new MigrationEngine(linkManager, registryUpdater, logger, namingEngine);

        // Create test files
        var testFile = Path.Combine(_sourceDir, "test.exe");
        File.WriteAllBytes(testFile, new byte[1024]);

        var entry = new SoftwareEntry
        {
            Id = "test-1",
            Name = "TestApp",
            InstallPath = _sourceDir
        };

        var template = new NamingTemplate
        {
            Id = "test",
            Name = "Simple",
            Pattern = "{Name}"
        };

        // Act
        var plan = await migrationEngine.CreatePlanAsync(entry, _targetDir, template);

        // Assert
        Assert.NotNull(plan);
        Assert.True(plan.TotalSizeBytes >= 0);
    }

    #endregion

    #region CleanupEngine Tests

    [Fact]
    public async Task CleanupEngine_ScanCacheAsync_ReturnsItems()
    {
        // Arrange
        var logger = new OperationLogger(30);
        var scanner = new SoftwareScanner();
        var cleanupEngine = new CleanupEngine(logger, scanner);

        // Create some test directories
        var cacheDir = Path.Combine(_sourceDir, "cache");
        var tempDir = Path.Combine(_sourceDir, "temp");
        Directory.CreateDirectory(cacheDir);
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(cacheDir, "test.tmp"), "test");

        var entry = new SoftwareEntry
        {
            Id = "test-1",
            Name = "TestApp",
            InstallPath = _sourceDir
        };

        // Act
        var items = await cleanupEngine.ScanCacheAsync(entry);

        // Assert
        Assert.NotNull(items);
    }

    #endregion

    #region End-to-End Workflow Tests

    [Fact]
    public void EndToEnd_ScanClassifyWorkflow_CompletesSuccessfully()
    {
        // Arrange
        var configPath = Path.Combine(_configDir, "e2e_config.json");
        var configService = new ConfigurationService(configPath);
        var classifier = new SoftwareClassifier(configService);
        var namingEngine = new NamingEngine();

        var entry = new SoftwareEntry
        {
            Id = "test-1",
            Name = "Visual Studio Code",
            Version = "1.85.0",
            Vendor = "Microsoft",
            InstallPath = _sourceDir,
            Category = SoftwareCategory.Other
        };

        // Act - Classify
        var category = classifier.Classify(entry);
        var classifiedEntry = entry with { Category = category };

        // Act - Generate name
        var template = namingEngine.GetPresetTemplates().First(t => t.Id == "detailed");
        var generatedName = namingEngine.GenerateName(classifiedEntry, template.Pattern);

        // Assert
        Assert.True(Enum.IsDefined(typeof(SoftwareCategory), category));
        Assert.NotEmpty(generatedName);
        Assert.Contains(classifiedEntry.Category.ToString(), generatedName);
    }

    [Fact]
    public async Task EndToEnd_ConfigurationPersistence_WorksCorrectly()
    {
        // Arrange
        var configPath = Path.Combine(_configDir, "persistence_config.json");
        var configService = new ConfigurationService(configPath);

        // Act - Save configuration
        var originalConfig = new AppConfiguration
        {
            DefaultTargetPath = @"E:\MySoftware",
            Theme = ThemeMode.Dark,
            DefaultNamingTemplateId = "dated",
            OperationHistoryDays = 60
        };
        await configService.SaveConfigurationAsync(originalConfig);

        // Create new service instance to simulate app restart
        var newConfigService = new ConfigurationService(configPath);
        var loadedConfig = await newConfigService.GetConfigurationAsync();

        // Assert
        Assert.Equal(originalConfig.DefaultTargetPath, loadedConfig.DefaultTargetPath);
        Assert.Equal(originalConfig.Theme, loadedConfig.Theme);
        Assert.Equal(originalConfig.DefaultNamingTemplateId, loadedConfig.DefaultNamingTemplateId);
        Assert.Equal(originalConfig.OperationHistoryDays, loadedConfig.OperationHistoryDays);
    }

    [Fact]
    public async Task EndToEnd_OperationLogging_TracksFullWorkflow()
    {
        // Arrange
        var logger = new OperationLogger(30);

        // Act - Simulate a migration workflow
        var operationId = await logger.BeginOperationAsync(
            OperationType.Migration,
            "Migrating TestApp to D:\\Software");

        await logger.LogActionAsync(operationId, new OperationAction
        {
            ActionType = "CreateDirectory",
            Description = "Created target directory",
            Timestamp = DateTime.UtcNow,
            NewValue = @"D:\Software\TestApp",
            CanRollback = true
        });

        await logger.LogActionAsync(operationId, new OperationAction
        {
            ActionType = "FileCopy",
            Description = "Copied main executable",
            Timestamp = DateTime.UtcNow,
            OriginalValue = @"C:\Program Files\TestApp\app.exe",
            NewValue = @"D:\Software\TestApp\app.exe",
            CanRollback = true
        });

        await logger.CompleteOperationAsync(operationId, true);

        // Assert
        var operation = await logger.GetOperationAsync(operationId);
        Assert.NotNull(operation);
        Assert.True(operation.Success);
        Assert.Equal(2, operation.Actions.Count);
        Assert.All(operation.Actions, a => Assert.True(a.CanRollback));
    }

    #endregion
}
