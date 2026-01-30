using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Infrastructure.Services;

/// <summary>
/// AI 文件分析器实现 - 使用 OpenAI API 分析文件并提供整理建议。
/// </summary>
public class AIFileAnalyzer : IAIFileAnalyzer
{
    private readonly IOpenAIClient _openAIClient;
    private readonly IConfigurationService _configurationService;
    private readonly IFileSystemService _fileSystemService;

    public AIFileAnalyzer(
        IOpenAIClient openAIClient,
        IConfigurationService configurationService,
        IFileSystemService fileSystemService)
    {
        _openAIClient = openAIClient;
        _configurationService = configurationService;
        _fileSystemService = fileSystemService;
    }

    public bool IsConfigured
    {
        get
        {
            var config = _configurationService.GetConfigurationAsync().GetAwaiter().GetResult();
            return !string.IsNullOrEmpty(config.OpenAIConfiguration?.ApiKey);
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        if (!IsConfigured) return false;
        
        var result = await _openAIClient.TestConnectionAsync();
        return result.Success;
    }

    public async Task<AIAnalysisResult> AnalyzeAsync(
        string path,
        AIAnalysisOptions? options = null,
        IProgress<AIAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new AIAnalysisOptions();
        var stopwatch = Stopwatch.StartNew();

        // 阶段 1: 收集文件信息
        progress?.Report(new AIAnalysisProgress
        {
            Phase = AIAnalysisPhase.CollectingData,
            ProgressPercentage = 10,
            StatusMessage = "正在收集文件信息..."
        });

        var files = await CollectFilesAsync(path, options, cancellationToken);
        
        if (files.Count == 0)
        {
            return new AIAnalysisResult
            {
                RootPath = path,
                Suggestions = Array.Empty<OrganizationSuggestion>(),
                Summary = "目录为空或无法访问",
                TokensUsed = 0,
                AnalysisDuration = stopwatch.Elapsed
            };
        }

        // 阶段 2: 准备 AI 请求
        progress?.Report(new AIAnalysisProgress
        {
            Phase = AIAnalysisPhase.PreparingRequest,
            ProgressPercentage = 30,
            StatusMessage = "正在准备分析请求..."
        });

        var prompt = BuildAnalysisPrompt(path, files, options);

        // 阶段 3: 发送请求并等待响应
        progress?.Report(new AIAnalysisProgress
        {
            Phase = AIAnalysisPhase.WaitingForResponse,
            ProgressPercentage = 50,
            StatusMessage = "正在等待 AI 分析..."
        });

        var response = await _openAIClient.SendChatCompletionRequestAsync(
            new ChatCompletionRequest
            {
                Model = "gpt-4o-mini",
                Messages = new[]
                {
                    ChatMessage.System(GetSystemPrompt()),
                    ChatMessage.User(prompt)
                },
                Temperature = 0.3,
                MaxTokens = 4000
            },
            cancellationToken);

        // 阶段 4: 解析响应
        progress?.Report(new AIAnalysisProgress
        {
            Phase = AIAnalysisPhase.ParsingResponse,
            ProgressPercentage = 80,
            StatusMessage = "正在解析分析结果..."
        });

        var suggestions = ParseSuggestions(response.FirstContent ?? "", path, files);

        // 完成
        progress?.Report(new AIAnalysisProgress
        {
            Phase = AIAnalysisPhase.Complete,
            ProgressPercentage = 100,
            StatusMessage = "分析完成"
        });

        stopwatch.Stop();

        return new AIAnalysisResult
        {
            RootPath = path,
            Suggestions = suggestions,
            Summary = ExtractSummary(response.FirstContent ?? ""),
            TokensUsed = response.Usage?.TotalTokens ?? 0,
            AnalysisDuration = stopwatch.Elapsed
        };
    }

    public async Task<AIAnalysisResult> AnalyzeFilesAsync(
        IReadOnlyList<FileEntry> files,
        AIAnalysisOptions? options = null,
        IProgress<AIAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new AIAnalysisOptions();
        var stopwatch = Stopwatch.StartNew();

        if (files.Count == 0)
        {
            return new AIAnalysisResult
            {
                RootPath = "",
                Suggestions = Array.Empty<OrganizationSuggestion>(),
                Summary = "没有文件需要分析",
                TokensUsed = 0,
                AnalysisDuration = stopwatch.Elapsed
            };
        }

        var rootPath = files[0].ParentPath ?? "";

        progress?.Report(new AIAnalysisProgress
        {
            Phase = AIAnalysisPhase.PreparingRequest,
            ProgressPercentage = 20,
            StatusMessage = "正在准备分析请求..."
        });

        var prompt = BuildAnalysisPrompt(rootPath, files.ToList(), options);

        progress?.Report(new AIAnalysisProgress
        {
            Phase = AIAnalysisPhase.WaitingForResponse,
            ProgressPercentage = 50,
            StatusMessage = "正在等待 AI 分析..."
        });

        var response = await _openAIClient.SendChatCompletionRequestAsync(
            new ChatCompletionRequest
            {
                Model = "gpt-4o-mini",
                Messages = new[]
                {
                    ChatMessage.System(GetSystemPrompt()),
                    ChatMessage.User(prompt)
                },
                Temperature = 0.3,
                MaxTokens = 4000
            },
            cancellationToken);

        progress?.Report(new AIAnalysisProgress
        {
            Phase = AIAnalysisPhase.ParsingResponse,
            ProgressPercentage = 80,
            StatusMessage = "正在解析分析结果..."
        });

        var suggestions = ParseSuggestions(response.FirstContent ?? "", rootPath, files.ToList());

        progress?.Report(new AIAnalysisProgress
        {
            Phase = AIAnalysisPhase.Complete,
            ProgressPercentage = 100,
            StatusMessage = "分析完成"
        });

        stopwatch.Stop();

        return new AIAnalysisResult
        {
            RootPath = rootPath,
            Suggestions = suggestions,
            Summary = ExtractSummary(response.FirstContent ?? ""),
            TokensUsed = response.Usage?.TotalTokens ?? 0,
            AnalysisDuration = stopwatch.Elapsed
        };
    }

    public async Task<IReadOnlyList<string>> SuggestFileNameAsync(
        FileEntry file,
        string? context = null,
        CancellationToken cancellationToken = default)
    {
        var prompt = $"""
            请为以下文件建议更好的文件名（保持扩展名不变）：
            
            当前文件名: {file.Name}
            文件类型: {FileTypeCategoryHelper.GetDisplayName(file.Category)}
            文件大小: {FormatSize(file.Size)}
            修改时间: {file.ModifiedTime:yyyy-MM-dd}
            {(string.IsNullOrEmpty(context) ? "" : $"上下文: {context}")}
            
            请提供 3 个建议的新文件名，每行一个，只输出文件名，不要其他解释。
            """;

        var response = await _openAIClient.SendChatCompletionRequestAsync(
            new ChatCompletionRequest
            {
                Model = "gpt-4o-mini",
                Messages = new[]
                {
                    ChatMessage.System("你是一个文件命名专家。请根据文件信息建议清晰、规范的文件名。"),
                    ChatMessage.User(prompt)
                },
                Temperature = 0.5,
                MaxTokens = 200
            },
            cancellationToken);

        return (response.FirstContent ?? "")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s) && !s.StartsWith('-') && !s.StartsWith('*'))
            .Take(3)
            .ToList();
    }

    public async Task<string?> SuggestCategoryDirectoryAsync(
        FileEntry file,
        IReadOnlyList<string> availableDirectories,
        CancellationToken cancellationToken = default)
    {
        if (availableDirectories.Count == 0) return null;

        var prompt = $"""
            请为以下文件选择最合适的目标目录：
            
            文件名: {file.Name}
            文件类型: {FileTypeCategoryHelper.GetDisplayName(file.Category)}
            
            可用目录:
            {string.Join("\n", availableDirectories.Select((d, i) => $"{i + 1}. {d}"))}
            
            请只输出最合适的目录名称（从上面的列表中选择），不要其他解释。
            """;

        var response = await _openAIClient.SendChatCompletionRequestAsync(
            new ChatCompletionRequest
            {
                Model = "gpt-4o-mini",
                Messages = new[]
                {
                    ChatMessage.System("你是一个文件分类专家。请根据文件信息选择最合适的目标目录。"),
                    ChatMessage.User(prompt)
                },
                Temperature = 0.2,
                MaxTokens = 100
            },
            cancellationToken);

        var suggestion = (response.FirstContent ?? "").Trim();
        
        // 尝试匹配可用目录
        return availableDirectories.FirstOrDefault(d => 
            d.Equals(suggestion, StringComparison.OrdinalIgnoreCase) ||
            suggestion.Contains(d, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<List<FileEntry>> CollectFilesAsync(
        string path,
        AIAnalysisOptions options,
        CancellationToken cancellationToken)
    {
        var files = new List<FileEntry>();
        
        await CollectFilesRecursiveAsync(path, 0, options, files, cancellationToken);
        
        return files;
    }

    private async Task CollectFilesRecursiveAsync(
        string path,
        int currentDepth,
        AIAnalysisOptions options,
        List<FileEntry> files,
        CancellationToken cancellationToken)
    {
        if (currentDepth > options.MaxDepth) return;
        if (!Directory.Exists(path)) return;

        try
        {
            var dirInfo = new DirectoryInfo(path);
            var fileCount = 0;

            foreach (var file in dirInfo.EnumerateFiles())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!options.IncludeHiddenFiles && (file.Attributes & FileAttributes.Hidden) != 0)
                    continue;

                if (fileCount >= options.MaxFilesPerDirectory)
                    break;

                files.Add(new FileEntry
                {
                    Name = file.Name,
                    FullPath = file.FullName,
                    Extension = file.Extension,
                    Size = file.Length,
                    CreatedTime = file.CreationTime,
                    ModifiedTime = file.LastWriteTime,
                    AccessedTime = file.LastAccessTime,
                    Attributes = file.Attributes,
                    Category = FileTypeCategoryHelper.GetCategory(file.Extension),
                    ParentPath = file.DirectoryName
                });

                fileCount++;
            }

            // 递归子目录
            foreach (var subDir in dirInfo.EnumerateDirectories())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!options.IncludeHiddenFiles && (subDir.Attributes & FileAttributes.Hidden) != 0)
                    continue;

                await CollectFilesRecursiveAsync(subDir.FullName, currentDepth + 1, options, files, cancellationToken);
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
    }

    private string BuildAnalysisPrompt(string rootPath, List<FileEntry> files, AIAnalysisOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"请分析以下目录结构并提供整理建议：");
        sb.AppendLine($"根目录: {(options.AnonymizeFilenames ? AnonymizePath(rootPath) : rootPath)}");
        sb.AppendLine();
        sb.AppendLine("文件列表:");

        // 按目录分组
        var groupedFiles = files.GroupBy(f => f.ParentPath ?? "");
        
        foreach (var group in groupedFiles)
        {
            var dirPath = options.AnonymizeFilenames ? AnonymizePath(group.Key) : group.Key;
            sb.AppendLine($"\n[{dirPath}]");
            
            foreach (var file in group.Take(options.MaxFilesPerDirectory))
            {
                var fileName = options.AnonymizeFilenames ? AnonymizeFileName(file.Name) : file.Name;
                var category = FileTypeCategoryHelper.GetDisplayName(file.Category);
                sb.AppendLine($"  - {fileName} ({category}, {FormatSize(file.Size)}, {file.ModifiedTime:yyyy-MM-dd})");
            }
        }

        if (!string.IsNullOrEmpty(options.CustomPrompt))
        {
            sb.AppendLine();
            sb.AppendLine($"额外要求: {options.CustomPrompt}");
        }

        return sb.ToString();
    }

    private static string GetSystemPrompt()
    {
        return """
            你是一个专业的文件整理助手。请分析用户提供的文件列表，并提供整理建议。

            请以 JSON 格式返回建议，格式如下：
            {
                "summary": "整体分析摘要",
                "suggestions": [
                    {
                        "type": "move|rename|delete|create_folder|merge|archive",
                        "source": "源文件/目录路径",
                        "destination": "目标路径（如适用）",
                        "new_name": "新名称（如适用）",
                        "reason": "建议原因",
                        "priority": "high|medium|low"
                    }
                ]
            }

            整理原则：
            1. 按文件类型分类（图片、文档、视频、音乐等）
            2. 识别并建议删除临时文件、重复文件
            3. 建议规范化文件命名
            4. 建议创建合理的目录结构
            5. 识别可以归档的旧文件

            注意：
            - 只返回 JSON，不要其他解释
            - 建议数量控制在 20 个以内
            - 优先提供高价值的建议
            """;
    }

    private List<OrganizationSuggestion> ParseSuggestions(string content, string rootPath, List<FileEntry> files)
    {
        var suggestions = new List<OrganizationSuggestion>();

        try
        {
            // 提取 JSON 部分
            var jsonMatch = Regex.Match(content, @"\{[\s\S]*\}", RegexOptions.Multiline);
            if (!jsonMatch.Success) return suggestions;

            using var doc = JsonDocument.Parse(jsonMatch.Value);
            var root = doc.RootElement;

            if (root.TryGetProperty("suggestions", out var suggestionsArray))
            {
                int id = 1;
                foreach (var item in suggestionsArray.EnumerateArray())
                {
                    var suggestion = ParseSuggestionItem(item, rootPath, files, id++);
                    if (suggestion != null)
                    {
                        suggestions.Add(suggestion);
                    }
                }
            }
        }
        catch (JsonException)
        {
            // JSON 解析失败，尝试简单文本解析
            suggestions = ParseSuggestionsFromText(content, rootPath, files);
        }

        return suggestions;
    }

    private OrganizationSuggestion? ParseSuggestionItem(JsonElement item, string rootPath, List<FileEntry> files, int id)
    {
        try
        {
            var typeStr = item.GetProperty("type").GetString() ?? "";
            var source = item.TryGetProperty("source", out var s) ? s.GetString() ?? "" : "";
            var destination = item.TryGetProperty("destination", out var d) ? d.GetString() : null;
            var newName = item.TryGetProperty("new_name", out var n) ? n.GetString() : null;
            var reason = item.TryGetProperty("reason", out var r) ? r.GetString() ?? "" : "";
            var priorityStr = item.TryGetProperty("priority", out var p) ? p.GetString() ?? "medium" : "medium";

            var type = typeStr.ToLowerInvariant() switch
            {
                "move" => SuggestionType.Move,
                "rename" => SuggestionType.Rename,
                "delete" => SuggestionType.Delete,
                "create_folder" => SuggestionType.CreateFolder,
                "merge" => SuggestionType.Merge,
                "archive" => SuggestionType.Archive,
                _ => SuggestionType.Move
            };

            var priority = priorityStr.ToLowerInvariant() switch
            {
                "high" => SuggestionPriority.High,
                "low" => SuggestionPriority.Low,
                _ => SuggestionPriority.Medium
            };

            // 尝试匹配实际文件路径
            var actualSource = ResolveFilePath(source, rootPath, files);

            return new OrganizationSuggestion
            {
                Id = $"suggestion_{id}",
                Type = type,
                SourcePath = actualSource ?? source,
                DestinationPath = destination != null ? ResolvePath(destination, rootPath) : null,
                NewName = newName,
                Reason = reason,
                Priority = priority,
                IsSelected = priority == SuggestionPriority.High
            };
        }
        catch
        {
            return null;
        }
    }

    private List<OrganizationSuggestion> ParseSuggestionsFromText(string content, string rootPath, List<FileEntry> files)
    {
        // 简单的文本解析作为后备方案
        var suggestions = new List<OrganizationSuggestion>();
        var lines = content.Split('\n');
        int id = 1;

        foreach (var line in lines)
        {
            if (line.Contains("移动") || line.Contains("move", StringComparison.OrdinalIgnoreCase))
            {
                suggestions.Add(new OrganizationSuggestion
                {
                    Id = $"suggestion_{id++}",
                    Type = SuggestionType.Move,
                    SourcePath = rootPath,
                    Reason = line.Trim(),
                    Priority = SuggestionPriority.Medium
                });
            }
            else if (line.Contains("删除") || line.Contains("delete", StringComparison.OrdinalIgnoreCase))
            {
                suggestions.Add(new OrganizationSuggestion
                {
                    Id = $"suggestion_{id++}",
                    Type = SuggestionType.Delete,
                    SourcePath = rootPath,
                    Reason = line.Trim(),
                    Priority = SuggestionPriority.Low
                });
            }
        }

        return suggestions.Take(10).ToList();
    }

    private string? ResolveFilePath(string path, string rootPath, List<FileEntry> files)
    {
        // 尝试直接匹配
        var match = files.FirstOrDefault(f => 
            f.FullPath.Equals(path, StringComparison.OrdinalIgnoreCase) ||
            f.Name.Equals(path, StringComparison.OrdinalIgnoreCase) ||
            f.FullPath.EndsWith(path, StringComparison.OrdinalIgnoreCase));

        return match?.FullPath;
    }

    private string ResolvePath(string path, string rootPath)
    {
        if (Path.IsPathRooted(path)) return path;
        return Path.Combine(rootPath, path);
    }

    private string? ExtractSummary(string content)
    {
        try
        {
            var jsonMatch = Regex.Match(content, @"\{[\s\S]*\}", RegexOptions.Multiline);
            if (!jsonMatch.Success) return null;

            using var doc = JsonDocument.Parse(jsonMatch.Value);
            if (doc.RootElement.TryGetProperty("summary", out var summary))
            {
                return summary.GetString();
            }
        }
        catch { }

        return null;
    }

    private static string AnonymizePath(string path)
    {
        // 保留目录结构但匿名化敏感部分
        var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var anonymized = parts.Select((p, i) => i < 2 ? p : $"dir_{i}");
        return string.Join(Path.DirectorySeparatorChar, anonymized);
    }

    private static string AnonymizeFileName(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        
        // 保留扩展名，匿名化文件名但保留一些特征
        if (nameWithoutExt.Length <= 3) return fileName;
        
        return $"{nameWithoutExt[..3]}***{ext}";
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}
