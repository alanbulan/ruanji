using System.Text.RegularExpressions;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Infrastructure.Services;

/// <summary>
/// Implementation of INamingEngine for generating standardized directory names.
/// Implements requirements 3.1, 3.2, 3.3, 3.4, 3.5
/// </summary>
public class NamingEngine : INamingEngine
{
    /// <summary>
    /// Windows file system illegal characters: \ / : * ? " &lt; &gt; |
    /// </summary>
    private static readonly char[] IllegalFileNameChars = new[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

    /// <summary>
    /// Supported template variables.
    /// </summary>
    private static readonly string[] SupportedVariables = new[] { "Category", "Name", "Version", "Vendor", "Date" };

    /// <summary>
    /// Regex pattern to match template variables like {Category}, {Name}, etc.
    /// </summary>
    private static readonly Regex VariablePattern = new(@"\{([^}]+)\}", RegexOptions.Compiled);

    /// <summary>
    /// Preset naming templates as defined in requirement 3.5.
    /// </summary>
    private static readonly IReadOnlyList<NamingTemplate> PresetTemplates = new[]
    {
        new NamingTemplate
        {
            Id = "simple",
            Name = "简洁模式",
            Pattern = "{Category}/{Name}",
            Description = "按类别分组，仅使用软件名称",
            IsPreset = true
        },
        new NamingTemplate
        {
            Id = "detailed",
            Name = "详细模式",
            Pattern = "{Category}/{Vendor}_{Name}_{Version}",
            Description = "包含厂商、名称和版本信息",
            IsPreset = true
        },
        new NamingTemplate
        {
            Id = "dated",
            Name = "日期模式",
            Pattern = "{Category}/{Date}_{Name}",
            Description = "按安装日期和名称组织",
            IsPreset = true
        }
    };

    /// <summary>
    /// File system abstraction for testing purposes.
    /// </summary>
    private readonly IFileSystemAbstraction _fileSystem;

    /// <summary>
    /// Initializes a new instance of the NamingEngine class.
    /// </summary>
    public NamingEngine() : this(new DefaultFileSystemAbstraction())
    {
    }

    /// <summary>
    /// Initializes a new instance of the NamingEngine class with a custom file system abstraction.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction to use.</param>
    public NamingEngine(IFileSystemAbstraction fileSystem)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    /// <inheritdoc />
    /// <remarks>
    /// Validates template syntax according to requirement 3.2.
    /// Checks for:
    /// - Empty templates
    /// - Unclosed braces
    /// - Unknown variables
    /// </remarks>
    public ValidationResult ValidateTemplate(string template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return ValidationResult.Failure("模板不能为空");
        }

        var errors = new List<string>();

        // Check for unclosed braces
        var openBraceCount = 0;
        var closeBraceCount = 0;
        var inVariable = false;

        for (int i = 0; i < template.Length; i++)
        {
            var c = template[i];
            if (c == '{')
            {
                openBraceCount++;
                if (inVariable)
                {
                    errors.Add("模板中存在嵌套的大括号");
                }
                inVariable = true;
            }
            else if (c == '}')
            {
                closeBraceCount++;
                inVariable = false;
            }
        }

        if (openBraceCount != closeBraceCount)
        {
            errors.Add("模板中存在未闭合的大括号");
        }

        // Check for unknown variables
        var matches = VariablePattern.Matches(template);
        foreach (Match match in matches)
        {
            var variableName = match.Groups[1].Value;
            if (!SupportedVariables.Contains(variableName, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add($"未知的模板变量: {{{variableName}}}");
            }
        }

        // Check for empty variable placeholders
        if (template.Contains("{}"))
        {
            errors.Add("模板中存在空的变量占位符");
        }

        return errors.Count > 0
            ? ValidationResult.Failure(errors.ToArray())
            : ValidationResult.Success();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Generates directory name by replacing template variables according to requirement 3.1.
    /// Supported variables: {Category}, {Name}, {Version}, {Vendor}, {Date}
    /// </remarks>
    public string GenerateName(SoftwareEntry entry, string template)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        if (string.IsNullOrWhiteSpace(template))
        {
            throw new ArgumentException("模板不能为空", nameof(template));
        }

        // Replace all supported variables (case-insensitive)
        var result = VariablePattern.Replace(template, match =>
        {
            var variableName = match.Groups[1].Value;
            return GetVariableValue(entry, variableName);
        });

        // Sanitize each path segment separately to preserve path structure
        var segments = result.Split('/');
        var sanitizedSegments = segments.Select(SanitizeFileName).ToArray();
        return string.Join("/", sanitizedSegments);
    }

    /// <summary>
    /// Gets the value for a template variable from a software entry.
    /// </summary>
    private static string GetVariableValue(SoftwareEntry entry, string variableName)
    {
        return variableName.ToUpperInvariant() switch
        {
            "CATEGORY" => entry.Category.ToString(),
            "NAME" => entry.Name,
            "VERSION" => !string.IsNullOrWhiteSpace(entry.Version) ? entry.Version : "Unknown",
            "VENDOR" => !string.IsNullOrWhiteSpace(entry.Vendor) ? entry.Vendor : "Unknown",
            "DATE" => entry.InstallDate?.ToString("yyyy-MM-dd") ?? "Unknown",
            _ => $"{{{variableName}}}" // Return original if unknown (shouldn't happen after validation)
        };
    }

    /// <inheritdoc />
    /// <remarks>
    /// Sanitizes file names by replacing illegal characters with underscores according to requirement 3.3.
    /// Windows illegal characters: \ / : * ? " &lt; &gt; |
    /// </remarks>
    public string SanitizeFileName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var result = name;

        // Replace all illegal Windows file name characters with underscore
        foreach (var illegalChar in IllegalFileNameChars)
        {
            result = result.Replace(illegalChar, '_');
        }

        // Also handle control characters and other problematic characters
        var sanitized = new char[result.Length];
        for (int i = 0; i < result.Length; i++)
        {
            var c = result[i];
            // Replace control characters (0-31) with underscore
            sanitized[i] = char.IsControl(c) ? '_' : c;
        }

        return new string(sanitized);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Resolves naming conflicts by adding numeric suffixes according to requirement 3.4.
    /// Returns a unique path that doesn't exist in the file system.
    /// </remarks>
    public string ResolveConflict(string basePath, string desiredName)
    {
        if (string.IsNullOrWhiteSpace(basePath))
        {
            throw new ArgumentException("基础路径不能为空", nameof(basePath));
        }

        if (string.IsNullOrWhiteSpace(desiredName))
        {
            throw new ArgumentException("期望名称不能为空", nameof(desiredName));
        }

        var fullPath = Path.Combine(basePath, desiredName);

        // If the path doesn't exist, return it as-is
        if (!_fileSystem.DirectoryExists(fullPath))
        {
            return fullPath;
        }

        // Add numeric suffix to resolve conflict
        var counter = 1;
        string newPath;
        do
        {
            newPath = Path.Combine(basePath, $"{desiredName}_{counter}");
            counter++;

            // Safety limit to prevent infinite loops
            if (counter > 10000)
            {
                throw new InvalidOperationException("无法解决目录名称冲突，已尝试超过10000个后缀");
            }
        } while (_fileSystem.DirectoryExists(newPath));

        return newPath;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Returns preset templates as defined in requirement 3.5:
    /// - 简洁模式: {Category}/{Name}
    /// - 详细模式: {Category}/{Vendor}_{Name}_{Version}
    /// - 日期模式: {Category}/{Date}_{Name}
    /// </remarks>
    public IReadOnlyList<NamingTemplate> GetPresetTemplates()
    {
        return PresetTemplates;
    }
}

/// <summary>
/// Abstraction for file system operations to enable testing.
/// </summary>
public interface IFileSystemAbstraction
{
    /// <summary>
    /// Checks if a directory exists at the specified path.
    /// </summary>
    bool DirectoryExists(string path);
}

/// <summary>
/// Default implementation using the actual file system.
/// </summary>
public class DefaultFileSystemAbstraction : IFileSystemAbstraction
{
    /// <inheritdoc />
    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }
}
