using System.Text.Json;
using System.Text.Json.Serialization;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Infrastructure.Services;

/// <summary>
/// Implementation of IConfigurationService for managing application configuration.
/// Validates: Requirements 9.1, 9.2, 9.3, 9.4, 9.5
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _configFilePath;
    private AppConfiguration? _cachedConfiguration;

    /// <inheritdoc />
    public event EventHandler<ConfigurationCorruptedEventArgs>? ConfigurationCorrupted;

    /// <inheritdoc />
    public string ConfigFilePath => _configFilePath;

    /// <summary>
    /// Initializes a new instance of the ConfigurationService class.
    /// </summary>
    /// <param name="configFilePath">The path to the configuration file. If null, uses the default path.</param>
    public ConfigurationService(string? configFilePath = null)
    {
        _configFilePath = configFilePath ?? GetDefaultConfigPath();
    }

    /// <inheritdoc />
    public async Task<AppConfiguration> GetConfigurationAsync()
    {
        if (_cachedConfiguration != null)
        {
            return _cachedConfiguration;
        }

        if (!File.Exists(_configFilePath))
        {
            _cachedConfiguration = new AppConfiguration();
            return _cachedConfiguration;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_configFilePath);
            var validation = ValidateConfiguration(json);
            
            if (!validation.IsValid)
            {
                // Raise event to notify about corrupted configuration
                OnConfigurationCorrupted(string.Join("; ", validation.Errors));
                _cachedConfiguration = new AppConfiguration();
                return _cachedConfiguration;
            }

            var config = JsonSerializer.Deserialize<AppConfiguration>(json, JsonOptions);
            if (config == null)
            {
                OnConfigurationCorrupted("配置反序列化返回空值");
                _cachedConfiguration = new AppConfiguration();
                return _cachedConfiguration;
            }

            _cachedConfiguration = config;
            return _cachedConfiguration;
        }
        catch (JsonException ex)
        {
            // Raise event to notify about corrupted configuration
            OnConfigurationCorrupted($"JSON解析错误: {ex.Message}");
            _cachedConfiguration = new AppConfiguration();
            return _cachedConfiguration;
        }
        catch (IOException ex)
        {
            // Raise event to notify about file read error
            OnConfigurationCorrupted($"文件读取错误: {ex.Message}");
            _cachedConfiguration = new AppConfiguration();
            return _cachedConfiguration;
        }
    }

    /// <inheritdoc />
    public async Task SaveConfigurationAsync(AppConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var directory = Path.GetDirectoryName(_configFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(configuration, JsonOptions);
        await File.WriteAllTextAsync(_configFilePath, json);
        _cachedConfiguration = configuration;
    }

    /// <inheritdoc />
    public async Task<AppConfiguration> ResetToDefaultAsync()
    {
        var defaultConfig = new AppConfiguration();
        await SaveConfigurationAsync(defaultConfig);
        return defaultConfig;
    }

    /// <inheritdoc />
    public ValidationResult ValidateConfiguration(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return ValidationResult.Failure("配置内容不能为空");
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            
            // Basic structure validation - must be a JSON object
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return ValidationResult.Failure("配置必须是一个JSON对象");
            }

            // Try to deserialize to validate structure and types
            var config = JsonSerializer.Deserialize<AppConfiguration>(json, JsonOptions);
            if (config == null)
            {
                return ValidationResult.Failure("配置反序列化失败");
            }

            // Validate specific fields
            var errors = new List<string>();

            // Validate OperationHistoryDays is positive
            if (config.OperationHistoryDays < 0)
            {
                errors.Add("操作历史天数不能为负数");
            }

            // Validate DefaultTargetPath is not empty if set
            if (config.DefaultTargetPath != null && string.IsNullOrWhiteSpace(config.DefaultTargetPath))
            {
                errors.Add("默认目标路径不能为空白字符串");
            }

            if (errors.Count > 0)
            {
                return ValidationResult.Failure(errors.ToArray());
            }

            return ValidationResult.Success();
        }
        catch (JsonException ex)
        {
            return ValidationResult.Failure($"JSON格式错误: {ex.Message}");
        }
    }

    /// <summary>
    /// Clears the cached configuration, forcing a reload on next access.
    /// </summary>
    public void ClearCache()
    {
        _cachedConfiguration = null;
    }

    /// <summary>
    /// Raises the ConfigurationCorrupted event.
    /// </summary>
    /// <param name="errorMessage">The error message describing the corruption.</param>
    protected virtual void OnConfigurationCorrupted(string errorMessage)
    {
        ConfigurationCorrupted?.Invoke(this, new ConfigurationCorruptedEventArgs
        {
            ErrorMessage = errorMessage,
            ConfigFilePath = _configFilePath
        });
    }

    private static string GetDefaultConfigPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "WindowsSoftwareOrganizer", "config.json");
    }
}
