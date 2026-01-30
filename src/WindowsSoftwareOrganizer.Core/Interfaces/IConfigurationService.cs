using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Core.Interfaces;

/// <summary>
/// Event arguments for configuration corruption events.
/// </summary>
public class ConfigurationCorruptedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the error message describing the corruption.
    /// </summary>
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// Gets the path to the corrupted configuration file.
    /// </summary>
    public required string ConfigFilePath { get; init; }
}

/// <summary>
/// Interface for managing application configuration.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Event raised when a corrupted configuration file is detected and default values are used.
    /// </summary>
    event EventHandler<ConfigurationCorruptedEventArgs>? ConfigurationCorrupted;

    /// <summary>
    /// Gets the current application configuration.
    /// </summary>
    /// <returns>The current configuration.</returns>
    Task<AppConfiguration> GetConfigurationAsync();

    /// <summary>
    /// Saves the application configuration.
    /// </summary>
    /// <param name="configuration">The configuration to save.</param>
    Task SaveConfigurationAsync(AppConfiguration configuration);

    /// <summary>
    /// Resets the configuration to default values.
    /// </summary>
    /// <returns>The default configuration.</returns>
    Task<AppConfiguration> ResetToDefaultAsync();

    /// <summary>
    /// Validates a JSON configuration string.
    /// </summary>
    /// <param name="json">The JSON string to validate.</param>
    /// <returns>The validation result.</returns>
    ValidationResult ValidateConfiguration(string json);

    /// <summary>
    /// Gets the path to the configuration file.
    /// </summary>
    string ConfigFilePath { get; }
}
