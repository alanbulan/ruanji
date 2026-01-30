using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Core.Interfaces;

/// <summary>
/// Interface for generating standardized directory names based on templates.
/// </summary>
public interface INamingEngine
{
    /// <summary>
    /// Validates a naming template for correct syntax.
    /// </summary>
    /// <param name="template">The template string to validate.</param>
    /// <returns>The validation result.</returns>
    ValidationResult ValidateTemplate(string template);

    /// <summary>
    /// Generates a directory name based on a software entry and template.
    /// </summary>
    /// <param name="entry">The software entry.</param>
    /// <param name="template">The naming template pattern.</param>
    /// <returns>The generated directory name.</returns>
    string GenerateName(SoftwareEntry entry, string template);

    /// <summary>
    /// Sanitizes a file name by replacing illegal characters.
    /// </summary>
    /// <param name="name">The name to sanitize.</param>
    /// <returns>The sanitized name.</returns>
    string SanitizeFileName(string name);

    /// <summary>
    /// Resolves naming conflicts by generating a unique name.
    /// </summary>
    /// <param name="basePath">The base directory path.</param>
    /// <param name="desiredName">The desired directory name.</param>
    /// <returns>A unique path that doesn't conflict with existing directories.</returns>
    string ResolveConflict(string basePath, string desiredName);

    /// <summary>
    /// Gets the list of preset naming templates.
    /// </summary>
    /// <returns>A list of preset templates.</returns>
    IReadOnlyList<NamingTemplate> GetPresetTemplates();
}
