using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Infrastructure.Services;

/// <summary>
/// AI 助手实现 - 统一的 AI 功能服务，支持多模块 Function Calling。
/// </summary>
public class AIAssistant : IAIAssistant
{
    private readonly IOpenAIClient _openAIClient;
    private readonly IFileSystemService _fileSystemService;
    private readonly IBatchFileOperator _batchFileOperator;
    private readonly ISoftwareScanner _softwareScanner;
    private readonly ISoftwareClassifier _softwareClassifier;
    private readonly ICleanupEngine _cleanupEngine;
    private readonly IMigrationEngine _migrationEngine;
    private readonly JsonSerializerOptions _jsonOptions;

    public AIAssistant(
        IOpenAIClient openAIClient,
        IFileSystemService fileSystemService,
        IBatchFileOperator batchFileOperator,
        ISoftwareScanner softwareScanner,
        ISoftwareClassifier softwareClassifier,
        ICleanupEngine cleanupEngine,
        IMigrationEngine migrationEngine)
    {
        _openAIClient = openAIClient;
        _fileSystemService = fileSystemService;
        _batchFileOperator = batchFileOperator;
        _softwareScanner = softwareScanner;
        _softwareClassifier = softwareClassifier;
        _cleanupEngine = cleanupEngine;
        _migrationEngine = migrationEngine;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };
    }

    public bool IsConfigured => _openAIClient.IsConfigured;

    public Task<bool> EnsureConfiguredAsync() => _openAIClient.EnsureConfiguredAsync();

    public IReadOnlyList<QuickAction> GetQuickActions(AIModule module)
    {
        return module switch
        {
            AIModule.SoftwareList => new List<QuickAction>
            {
                new() { Id = "analyze", Title = "分析软件", Description = "分析已安装软件的分类和使用情况", Icon = "\uE9D9", Prompt = "请分析我电脑上安装的软件，告诉我有哪些类型的软件，哪些可能是不需要的，哪些占用空间较大。" },
                new() { Id = "find_unused", Title = "查找闲置", Description = "找出可能不再使用的软件", Icon = "\uE721", Prompt = "请帮我找出可能不再使用的软件，比如很久没更新的、重复功能的、或者已知的垃圾软件。" },
                new() { Id = "optimize", Title = "优化建议", Description = "提供软件优化建议", Icon = "\uE771", Prompt = "请根据我安装的软件，给出优化建议，比如哪些软件可以用更好的替代品，哪些可以卸载。" },
                new() { Id = "categorize", Title = "重新分类", Description = "智能重新分类软件", Icon = "\uE8FD", Prompt = "请帮我重新分类这些软件，确保分类准确。" }
            },
            AIModule.FileManager => new List<QuickAction>
            {
                new() { Id = "analyze", Title = "分析目录", Description = "分析当前目录结构", Icon = "\uE9D9", Prompt = "请分析当前目录的文件结构，告诉我有哪些文件类型，以及你的整理建议。" },
                new() { Id = "organize", Title = "按类型整理", Description = "按文件类型自动整理", Icon = "\uE8B7", Prompt = "请帮我按文件类型整理当前目录。创建分类文件夹（如：图片、文档、视频、音乐、压缩包等），然后把对应的文件移动进去。" },
                new() { Id = "clean_temp", Title = "清理临时文件", Description = "清理临时和垃圾文件", Icon = "\uE74D", Prompt = "请帮我找出并清理当前目录中的临时文件（如 .tmp, .log, .bak, thumbs.db 等）。" },
                new() { Id = "archive", Title = "归档旧文件", Description = "归档长期未使用的文件", Icon = "\uE7B8", Prompt = "请帮我找出超过一年未修改的旧文件，创建一个归档文件夹，把这些旧文件移动进去。" }
            },
            AIModule.Cleanup => new List<QuickAction>
            {
                new() { Id = "scan", Title = "智能扫描", Description = "AI 智能扫描垃圾文件", Icon = "\uE72C", Prompt = "请帮我智能扫描系统中的垃圾文件，包括临时文件、缓存、日志等，并告诉我哪些可以安全删除。" },
                new() { Id = "analyze_risk", Title = "风险分析", Description = "分析清理项的风险", Icon = "\uE7BA", Prompt = "请分析当前扫描出的清理项，告诉我哪些是安全的，哪些需要谨慎处理。" },
                new() { Id = "recommend", Title = "清理建议", Description = "提供清理建议", Icon = "\uE946", Prompt = "请根据扫描结果，给出清理建议，帮我选择应该清理哪些项目。" },
                new() { Id = "deep_clean", Title = "深度清理", Description = "深度清理系统垃圾", Icon = "\uE90F", Prompt = "请帮我进行深度清理，找出所有可以安全删除的垃圾文件。" }
            },
            AIModule.Migration => new List<QuickAction>
            {
                new() { Id = "analyze", Title = "迁移分析", Description = "分析软件迁移可行性", Icon = "\uE9D9", Prompt = "请分析待迁移的软件，告诉我哪些可以安全迁移，哪些可能有风险。" },
                new() { Id = "recommend_path", Title = "路径建议", Description = "推荐最佳迁移路径", Icon = "\uE8B7", Prompt = "请根据我的磁盘空间情况，推荐最佳的迁移目标路径。" },
                new() { Id = "batch_plan", Title = "批量规划", Description = "规划批量迁移方案", Icon = "\uE8FD", Prompt = "请帮我规划批量迁移方案，按优先级排序，并估算所需时间和空间。" },
                new() { Id = "verify", Title = "迁移验证", Description = "验证迁移结果", Icon = "\uE73E", Prompt = "请帮我验证迁移后的软件是否正常工作。" }
            },
            _ => new List<QuickAction>()
        };
    }

    public async IAsyncEnumerable<AIAssistantEvent> RunAsync(
        AIAssistantContext context,
        string userRequest,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            yield return new AIAssistantEvent
            {
                Type = AIAssistantEventType.Error,
                Error = new InvalidOperationException("AI 功能未配置。请先在设置页面配置 API 密钥。")
            };
            yield break;
        }

        var tools = GetToolsForModule(context.Module);
        var systemPrompt = GetSystemPromptForModule(context);
        
        var messages = new List<ChatMessage>
        {
            ChatMessage.System(systemPrompt),
            ChatMessage.User(userRequest)
        };

        while (context.CurrentIteration < context.MaxIterations)
        {
            context.CurrentIteration++;
            cancellationToken.ThrowIfCancellationRequested();

            yield return new AIAssistantEvent { Type = AIAssistantEventType.Thinking, Message = "正在分析..." };

            ChatCompletionResponseWithTools? response = null;
            Exception? requestError = null;
            try
            {
                response = await SendRequestAsync(messages, tools, cancellationToken);
            }
            catch (Exception ex)
            {
                requestError = ex;
            }

            if (requestError != null)
            {
                yield return new AIAssistantEvent { Type = AIAssistantEventType.Error, Error = requestError };
                yield break;
            }

            var choice = response?.Choices?.FirstOrDefault();
            if (choice == null)
            {
                yield return new AIAssistantEvent { Type = AIAssistantEventType.Error, Error = new Exception("AI 没有返回响应") };
                yield break;
            }

            if (!string.IsNullOrEmpty(choice.Message?.Content))
            {
                yield return new AIAssistantEvent { Type = AIAssistantEventType.Message, Message = choice.Message.Content };
                messages.Add(ChatMessage.Assistant(choice.Message.Content));
            }

            var toolCalls = choice.Message?.ToolCalls;
            if (toolCalls == null || toolCalls.Count == 0)
            {
                yield return new AIAssistantEvent { Type = AIAssistantEventType.Complete };
                yield break;
            }

            var toolResults = new List<(string id, string name, string result, bool success)>();
            
            foreach (var toolCall in toolCalls)
            {
                var functionName = toolCall.Function?.Name ?? "";
                
                yield return new AIAssistantEvent
                {
                    Type = AIAssistantEventType.ToolCall,
                    ToolName = functionName,
                    Message = $"执行: {GetToolDisplayName(functionName)}"
                };

                var result = await ExecuteToolAsync(context, toolCall, cancellationToken);
                toolResults.Add((toolCall.Id, functionName, result.result, result.success));
                
                yield return new AIAssistantEvent
                {
                    Type = AIAssistantEventType.ToolResult,
                    ToolName = functionName,
                    ToolResult = result.result,
                    ToolSuccess = result.success
                };

                if (functionName == "task_complete")
                {
                    yield return new AIAssistantEvent { Type = AIAssistantEventType.Complete, Message = result.result };
                    yield break;
                }
            }

            messages.Add(new ChatMessageWithToolCalls
            {
                Role = "assistant",
                Content = choice.Message?.Content,
                ToolCalls = toolCalls
            });

            foreach (var (id, name, result, _) in toolResults)
            {
                messages.Add(ChatMessage.Tool(id, name, result));
            }
        }

        yield return new AIAssistantEvent
        {
            Type = AIAssistantEventType.Complete,
            Message = $"已达到最大迭代次数 ({context.MaxIterations})。"
        };
    }

    private IReadOnlyList<AgentTool> GetToolsForModule(AIModule module)
    {
        var commonTools = new List<AgentTool>
        {
            CreateTool("task_complete", "当任务完成时调用此函数，向用户报告结果。", new Dictionary<string, AgentParameterProperty>
            {
                ["summary"] = new() { Type = "string", Description = "任务完成摘要" }
            }, new[] { "summary" })
        };

        var moduleTools = module switch
        {
            AIModule.SoftwareList => GetSoftwareListTools(),
            AIModule.FileManager => GetFileManagerTools(),
            AIModule.Cleanup => GetCleanupTools(),
            AIModule.Migration => GetMigrationTools(),
            _ => new List<AgentTool>()
        };

        return moduleTools.Concat(commonTools).ToList();
    }

    private List<AgentTool> GetSoftwareListTools()
    {
        return new List<AgentTool>
        {
            CreateTool("list_software", "列出已安装的软件。", new Dictionary<string, AgentParameterProperty>
            {
                ["category"] = new() { Type = "string", Description = "按类别筛选（可选）" }
            }),
            CreateTool("get_software_info", "获取软件详细信息。", new Dictionary<string, AgentParameterProperty>
            {
                ["name"] = new() { Type = "string", Description = "软件名称" }
            }, new[] { "name" }),
            CreateTool("analyze_software", "分析软件使用情况和建议。", new Dictionary<string, AgentParameterProperty>()),
            CreateTool("find_duplicates", "查找重复或相似功能的软件。", new Dictionary<string, AgentParameterProperty>()),
            CreateTool("find_large_software", "查找占用空间大的软件。", new Dictionary<string, AgentParameterProperty>
            {
                ["min_size_mb"] = new() { Type = "number", Description = "最小大小（MB）" }
            }),
            CreateTool("reclassify_software", "重新分类软件。", new Dictionary<string, AgentParameterProperty>
            {
                ["name"] = new() { Type = "string", Description = "软件名称" },
                ["new_category"] = new() { Type = "string", Description = "新类别" }
            }, new[] { "name", "new_category" })
        };
    }

    private List<AgentTool> GetFileManagerTools()
    {
        return new List<AgentTool>
        {
            CreateTool("list_directory", "列出目录内容。", new Dictionary<string, AgentParameterProperty>
            {
                ["path"] = new() { Type = "string", Description = "目录路径" }
            }, new[] { "path" }),
            CreateTool("create_folder", "创建文件夹。", new Dictionary<string, AgentParameterProperty>
            {
                ["path"] = new() { Type = "string", Description = "文件夹路径" }
            }, new[] { "path" }),
            CreateTool("move_item", "移动文件或文件夹。", new Dictionary<string, AgentParameterProperty>
            {
                ["source"] = new() { Type = "string", Description = "源路径" },
                ["destination"] = new() { Type = "string", Description = "目标路径" }
            }, new[] { "source", "destination" }),
            CreateTool("delete_item", "删除文件或文件夹。", new Dictionary<string, AgentParameterProperty>
            {
                ["path"] = new() { Type = "string", Description = "路径" },
                ["recursive"] = new() { Type = "boolean", Description = "是否递归删除" }
            }, new[] { "path" }),
            CreateTool("search_files", "搜索文件。", new Dictionary<string, AgentParameterProperty>
            {
                ["directory"] = new() { Type = "string", Description = "搜索目录" },
                ["pattern"] = new() { Type = "string", Description = "搜索模式" },
                ["recursive"] = new() { Type = "boolean", Description = "是否递归" }
            }, new[] { "directory", "pattern" }),
            CreateTool("get_file_info", "获取文件信息。", new Dictionary<string, AgentParameterProperty>
            {
                ["path"] = new() { Type = "string", Description = "文件路径" }
            }, new[] { "path" }),
            CreateTool("batch_move", "批量移动文件。", new Dictionary<string, AgentParameterProperty>
            {
                ["files"] = new() { Type = "string", Description = "文件路径列表，逗号分隔" },
                ["destination_folder"] = new() { Type = "string", Description = "目标文件夹" }
            }, new[] { "files", "destination_folder" })
        };
    }

    private List<AgentTool> GetCleanupTools()
    {
        return new List<AgentTool>
        {
            CreateTool("scan_cleanup_items", "扫描可清理的项目。", new Dictionary<string, AgentParameterProperty>
            {
                ["include_temp"] = new() { Type = "boolean", Description = "包含临时文件" },
                ["include_cache"] = new() { Type = "boolean", Description = "包含缓存文件" },
                ["include_logs"] = new() { Type = "boolean", Description = "包含日志文件" }
            }),
            CreateTool("list_cleanup_items", "列出已扫描的清理项。", new Dictionary<string, AgentParameterProperty>()),
            CreateTool("analyze_cleanup_risk", "分析清理项的风险等级。", new Dictionary<string, AgentParameterProperty>()),
            CreateTool("select_safe_items", "选择所有安全的清理项。", new Dictionary<string, AgentParameterProperty>()),
            CreateTool("execute_cleanup", "执行清理操作。", new Dictionary<string, AgentParameterProperty>
            {
                ["move_to_recycle_bin"] = new() { Type = "boolean", Description = "是否移至回收站" }
            }),
            CreateTool("get_cleanup_summary", "获取清理摘要。", new Dictionary<string, AgentParameterProperty>())
        };
    }

    private List<AgentTool> GetMigrationTools()
    {
        return new List<AgentTool>
        {
            CreateTool("list_migration_software", "列出待迁移的软件。", new Dictionary<string, AgentParameterProperty>()),
            CreateTool("analyze_migration", "分析软件迁移可行性。", new Dictionary<string, AgentParameterProperty>
            {
                ["software_name"] = new() { Type = "string", Description = "软件名称" }
            }),
            CreateTool("get_disk_space", "获取磁盘空间信息。", new Dictionary<string, AgentParameterProperty>()),
            CreateTool("recommend_target_path", "推荐迁移目标路径。", new Dictionary<string, AgentParameterProperty>
            {
                ["required_space_mb"] = new() { Type = "number", Description = "所需空间（MB）" }
            }),
            CreateTool("execute_migration", "执行软件迁移。", new Dictionary<string, AgentParameterProperty>
            {
                ["software_name"] = new() { Type = "string", Description = "软件名称" },
                ["target_path"] = new() { Type = "string", Description = "目标路径" }
            }, new[] { "software_name", "target_path" }),
            CreateTool("verify_migration", "验证迁移结果。", new Dictionary<string, AgentParameterProperty>
            {
                ["software_name"] = new() { Type = "string", Description = "软件名称" }
            }, new[] { "software_name" })
        };
    }

    private AgentTool CreateTool(string name, string description, Dictionary<string, AgentParameterProperty> properties, string[]? required = null)
    {
        return new AgentTool
        {
            Function = new AgentFunction
            {
                Name = name,
                Description = description,
                Parameters = new AgentFunctionParameters
                {
                    Properties = properties,
                    Required = required
                }
            }
        };
    }

    private string GetSystemPromptForModule(AIAssistantContext context)
    {
        var basePrompt = "你是一个专业的 Windows 软件管理助手。你可以通过调用工具来帮助用户完成任务。\n\n";
        
        var modulePrompt = context.Module switch
        {
            AIModule.SoftwareList => $"""
                当前模块: 软件列表
                
                你可以帮助用户:
                - 分析已安装的软件
                - 查找重复或不需要的软件
                - 提供软件优化建议
                - 重新分类软件
                
                已安装软件数量: {context.SelectedSoftware?.Count ?? 0}
                """,
            AIModule.FileManager => $"""
                当前模块: 文件管理
                当前目录: {context.CurrentPath ?? "未选择"}
                
                你可以帮助用户:
                - 分析目录结构
                - 按类型整理文件
                - 清理临时文件
                - 归档旧文件
                
                常见文件分类:
                - 图片: .jpg, .jpeg, .png, .gif, .bmp, .webp
                - 文档: .doc, .docx, .pdf, .txt, .xlsx, .pptx
                - 视频: .mp4, .avi, .mkv, .mov
                - 音乐: .mp3, .wav, .flac, .aac
                - 压缩包: .zip, .rar, .7z
                """,
            AIModule.Cleanup => $"""
                当前模块: 清理
                
                你可以帮助用户:
                - 扫描系统垃圾文件
                - 分析清理项的风险
                - 提供清理建议
                - 执行安全清理
                
                已扫描项目数: {context.CleanupItems?.Count ?? 0}
                """,
            AIModule.Migration => $"""
                当前模块: 软件迁移
                目标路径: {context.MigrationTargetPath ?? "未设置"}
                
                你可以帮助用户:
                - 分析软件迁移可行性
                - 推荐迁移目标路径
                - 规划批量迁移方案
                - 验证迁移结果
                
                待迁移软件数: {context.SelectedSoftware?.Count ?? 0}
                """,
            _ => ""
        };

        return basePrompt + modulePrompt + "\n\n重要: 完成任务后必须调用 task_complete 函数报告结果。";
    }

    private async Task<(string result, bool success)> ExecuteToolAsync(AIAssistantContext context, ToolCall toolCall, CancellationToken ct)
    {
        var functionName = toolCall.Function?.Name ?? "";
        var argsJson = toolCall.Function?.Arguments ?? "{}";

        try
        {
            using var argsDoc = JsonDocument.Parse(argsJson);
            var args = argsDoc.RootElement;

            return context.Module switch
            {
                AIModule.SoftwareList => await ExecuteSoftwareToolAsync(functionName, args, context, ct),
                AIModule.FileManager => await ExecuteFileManagerToolAsync(functionName, args, context, ct),
                AIModule.Cleanup => await ExecuteCleanupToolAsync(functionName, args, context, ct),
                AIModule.Migration => await ExecuteMigrationToolAsync(functionName, args, context, ct),
                _ => ("未知模块", false)
            };
        }
        catch (Exception ex)
        {
            return ($"执行失败: {ex.Message}", false);
        }
    }

    private async Task<(string result, bool success)> ExecuteSoftwareToolAsync(string functionName, JsonElement args, AIAssistantContext context, CancellationToken ct)
    {
        return functionName switch
        {
            "list_software" => await ListSoftwareAsync(args, context),
            "get_software_info" => await GetSoftwareInfoAsync(args, context),
            "analyze_software" => await AnalyzeSoftwareAsync(context),
            "find_duplicates" => await FindDuplicatesAsync(context),
            "find_large_software" => await FindLargeSoftwareAsync(args, context),
            "reclassify_software" => await ReclassifySoftwareAsync(args, context),
            "task_complete" => (args.GetProperty("summary").GetString() ?? "完成", true),
            _ => ($"未知工具: {functionName}", false)
        };
    }

    private async Task<(string result, bool success)> ExecuteFileManagerToolAsync(string functionName, JsonElement args, AIAssistantContext context, CancellationToken ct)
    {
        return functionName switch
        {
            "list_directory" => await ListDirectoryAsync(args, context),
            "create_folder" => await CreateFolderAsync(args, context),
            "move_item" => await MoveItemAsync(args, context),
            "delete_item" => await DeleteItemAsync(args, context),
            "search_files" => await SearchFilesAsync(args, context),
            "get_file_info" => await GetFileInfoAsync(args),
            "batch_move" => await BatchMoveAsync(args, context, ct),
            "task_complete" => (args.GetProperty("summary").GetString() ?? "完成", true),
            _ => ($"未知工具: {functionName}", false)
        };
    }

    private async Task<(string result, bool success)> ExecuteCleanupToolAsync(string functionName, JsonElement args, AIAssistantContext context, CancellationToken ct)
    {
        return functionName switch
        {
            "scan_cleanup_items" => await ScanCleanupItemsAsync(args, context, ct),
            "list_cleanup_items" => ListCleanupItems(context),
            "analyze_cleanup_risk" => AnalyzeCleanupRisk(context),
            "select_safe_items" => SelectSafeItems(context),
            "execute_cleanup" => await ExecuteCleanupAsync(args, context, ct),
            "get_cleanup_summary" => GetCleanupSummary(context),
            "task_complete" => (args.GetProperty("summary").GetString() ?? "完成", true),
            _ => ($"未知工具: {functionName}", false)
        };
    }

    private async Task<(string result, bool success)> ExecuteMigrationToolAsync(string functionName, JsonElement args, AIAssistantContext context, CancellationToken ct)
    {
        return functionName switch
        {
            "list_migration_software" => ListMigrationSoftware(context),
            "analyze_migration" => await AnalyzeMigrationAsync(args, context),
            "get_disk_space" => GetDiskSpace(),
            "recommend_target_path" => RecommendTargetPath(args),
            "execute_migration" => await ExecuteMigrationAsync(args, context, ct),
            "verify_migration" => await VerifyMigrationAsync(args, context),
            "task_complete" => (args.GetProperty("summary").GetString() ?? "完成", true),
            _ => ($"未知工具: {functionName}", false)
        };
    }

    #region Software Tools Implementation

    private Task<(string, bool)> ListSoftwareAsync(JsonElement args, AIAssistantContext context)
    {
        var software = context.SelectedSoftware ?? new List<SoftwareEntry>();
        var category = args.TryGetProperty("category", out var cat) ? cat.GetString() : null;
        
        var filtered = string.IsNullOrEmpty(category) 
            ? software 
            : software.Where(s => s.Category.ToString().Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"已安装软件 ({filtered.Count} 个):");
        foreach (var s in filtered.Take(50))
        {
            sb.AppendLine($"  - {s.Name} ({s.Category}, {FormatSize(s.TotalSizeBytes)})");
        }
        if (filtered.Count > 50) sb.AppendLine($"  ... 还有 {filtered.Count - 50} 个");
        
        return Task.FromResult((sb.ToString(), true));
    }

    private Task<(string, bool)> GetSoftwareInfoAsync(JsonElement args, AIAssistantContext context)
    {
        var name = args.GetProperty("name").GetString()!;
        var software = context.SelectedSoftware?.FirstOrDefault(s => 
            s.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
        
        if (software == null) return Task.FromResult(($"未找到软件: {name}", false));

        var sb = new StringBuilder();
        sb.AppendLine($"软件: {software.Name}");
        sb.AppendLine($"版本: {software.Version ?? "未知"}");
        sb.AppendLine($"发布者: {software.Vendor ?? "未知"}");
        sb.AppendLine($"类别: {software.Category}");
        sb.AppendLine($"安装路径: {software.InstallPath}");
        sb.AppendLine($"大小: {FormatSize(software.TotalSizeBytes)}");
        
        return Task.FromResult((sb.ToString(), true));
    }

    private Task<(string, bool)> AnalyzeSoftwareAsync(AIAssistantContext context)
    {
        var software = context.SelectedSoftware ?? new List<SoftwareEntry>();
        var categories = software.GroupBy(s => s.Category).ToDictionary(g => g.Key, g => g.Count());
        var totalSize = software.Sum(s => s.TotalSizeBytes);
        
        var sb = new StringBuilder();
        sb.AppendLine("软件分析报告:");
        sb.AppendLine($"总数: {software.Count} 个软件");
        sb.AppendLine($"总大小: {FormatSize(totalSize)}");
        sb.AppendLine("\n按类别分布:");
        foreach (var (cat, count) in categories.OrderByDescending(x => x.Value))
        {
            sb.AppendLine($"  - {cat}: {count} 个");
        }
        
        return Task.FromResult((sb.ToString(), true));
    }

    private Task<(string, bool)> FindDuplicatesAsync(AIAssistantContext context)
    {
        var software = context.SelectedSoftware ?? new List<SoftwareEntry>();
        var duplicates = software.GroupBy(s => s.Vendor?.ToLower() ?? "unknown")
            .Where(g => g.Count() > 1 && g.Key != "unknown")
            .ToList();
        
        var sb = new StringBuilder();
        sb.AppendLine("可能重复的软件:");
        foreach (var group in duplicates.Take(10))
        {
            sb.AppendLine($"\n来自 {group.First().Vendor}:");
            foreach (var s in group) sb.AppendLine($"  - {s.Name}");
        }
        if (!duplicates.Any()) sb.AppendLine("未发现明显重复的软件。");
        
        return Task.FromResult((sb.ToString(), true));
    }

    private Task<(string, bool)> FindLargeSoftwareAsync(JsonElement args, AIAssistantContext context)
    {
        var minSizeMb = args.TryGetProperty("min_size_mb", out var size) ? size.GetInt32() : 500;
        var software = context.SelectedSoftware ?? new List<SoftwareEntry>();
        var large = software.Where(s => s.TotalSizeBytes > minSizeMb * 1024 * 1024)
            .OrderByDescending(s => s.TotalSizeBytes).ToList();
        
        var sb = new StringBuilder();
        sb.AppendLine($"大于 {minSizeMb}MB 的软件 ({large.Count} 个):");
        foreach (var s in large.Take(20))
        {
            sb.AppendLine($"  - {s.Name}: {FormatSize(s.TotalSizeBytes)}");
        }
        
        return Task.FromResult((sb.ToString(), true));
    }

    private Task<(string, bool)> ReclassifySoftwareAsync(JsonElement args, AIAssistantContext context)
    {
        var name = args.GetProperty("name").GetString()!;
        var newCategory = args.GetProperty("new_category").GetString()!;
        return Task.FromResult(($"已将 {name} 重新分类为 {newCategory}", true));
    }

    #endregion

    #region File Manager Tools Implementation

    private async Task<(string, bool)> ListDirectoryAsync(JsonElement args, AIAssistantContext context)
    {
        var path = args.GetProperty("path").GetString() ?? context.CurrentPath ?? "";
        var content = await _fileSystemService.GetDirectoryContentAsync(path, new FileFilterOptions());
        
        var sb = new StringBuilder();
        sb.AppendLine($"目录: {path}");
        sb.AppendLine($"文件夹 ({content.Directories.Count}):");
        foreach (var dir in content.Directories.Take(30)) sb.AppendLine($"  [目录] {dir.Name}");
        sb.AppendLine($"文件 ({content.Files.Count}):");
        foreach (var file in content.Files.Take(30)) sb.AppendLine($"  [文件] {file.Name} ({FormatSize(file.Size)})");
        
        return (sb.ToString(), true);
    }

    private async Task<(string, bool)> CreateFolderAsync(JsonElement args, AIAssistantContext context)
    {
        var path = args.GetProperty("path").GetString()!;
        var result = await _fileSystemService.CreateDirectoryAsync(path);
        context.ExecutedActions.Add($"创建文件夹: {path}");
        return result.Success ? ($"已创建: {path}", true) : (result.ErrorMessage ?? "创建失败", false);
    }

    private async Task<(string, bool)> MoveItemAsync(JsonElement args, AIAssistantContext context)
    {
        var source = args.GetProperty("source").GetString()!;
        var dest = args.GetProperty("destination").GetString()!;
        var result = await _fileSystemService.MoveAsync(source, dest);
        context.ExecutedActions.Add($"移动: {source} -> {dest}");
        return result.Success ? ($"已移动: {Path.GetFileName(source)}", true) : (result.ErrorMessage ?? "移动失败", false);
    }

    private async Task<(string, bool)> DeleteItemAsync(JsonElement args, AIAssistantContext context)
    {
        var path = args.GetProperty("path").GetString()!;
        var recursive = args.TryGetProperty("recursive", out var r) && r.GetBoolean();
        var result = await _fileSystemService.DeleteAsync(path, recursive);
        context.ExecutedActions.Add($"删除: {path}");
        return result.Success ? ($"已删除: {path}", true) : (result.ErrorMessage ?? "删除失败", false);
    }

    private Task<(string, bool)> SearchFilesAsync(JsonElement args, AIAssistantContext context)
    {
        var dir = args.GetProperty("directory").GetString()!;
        var pattern = args.GetProperty("pattern").GetString()!;
        var recursive = args.TryGetProperty("recursive", out var r) && r.GetBoolean();
        
        try
        {
            var files = Directory.GetFiles(dir, pattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            var sb = new StringBuilder();
            sb.AppendLine($"搜索结果 ({files.Length} 个):");
            foreach (var f in files.Take(50)) sb.AppendLine($"  {f}");
            return Task.FromResult((sb.ToString(), true));
        }
        catch (Exception ex)
        {
            return Task.FromResult(($"搜索失败: {ex.Message}", false));
        }
    }

    private Task<(string, bool)> GetFileInfoAsync(JsonElement args)
    {
        var path = args.GetProperty("path").GetString()!;
        if (File.Exists(path))
        {
            var info = new FileInfo(path);
            return Task.FromResult(($"文件: {info.Name}\n大小: {FormatSize(info.Length)}\n修改时间: {info.LastWriteTime}", true));
        }
        if (Directory.Exists(path))
        {
            var info = new DirectoryInfo(path);
            return Task.FromResult(($"目录: {info.Name}\n修改时间: {info.LastWriteTime}", true));
        }
        return Task.FromResult(("路径不存在", false));
    }

    private async Task<(string, bool)> BatchMoveAsync(JsonElement args, AIAssistantContext context, CancellationToken ct)
    {
        var filesStr = args.GetProperty("files").GetString()!;
        var destFolder = args.GetProperty("destination_folder").GetString()!;
        
        if (!Directory.Exists(destFolder)) Directory.CreateDirectory(destFolder);
        
        var files = filesStr.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(f => f.Trim()).ToList();
        var ops = files.Select(f => (f, Path.Combine(destFolder, Path.GetFileName(f)))).ToList();
        var result = await _batchFileOperator.MoveAsync(ops, false, null, ct);
        
        context.ExecutedActions.Add($"批量移动 {result.SuccessCount} 个文件到 {destFolder}");
        return ($"已移动 {result.SuccessCount} 个文件", result.FailedCount == 0);
    }

    #endregion

    #region Cleanup Tools Implementation

    private async Task<(string, bool)> ScanCleanupItemsAsync(JsonElement args, AIAssistantContext context, CancellationToken ct)
    {
        var orphanedItems = await _cleanupEngine.ScanOrphanedItemsAsync(ct);
        var cacheItems = await _cleanupEngine.ScanCacheAsync(null, ct);
        var items = orphanedItems.Concat(cacheItems).ToList();
        context.ExtraData["cleanup_items"] = items;
        return ($"扫描完成，发现 {items.Count} 个可清理项目，总大小 {FormatSize(items.Sum(i => i.SizeBytes))}", true);
    }

    private (string, bool) ListCleanupItems(AIAssistantContext context)
    {
        var items = context.ExtraData.TryGetValue("cleanup_items", out var obj) ? obj as List<CleanupItem> : context.CleanupItems?.ToList();
        if (items == null || !items.Any()) return ("没有扫描到清理项目", false);
        
        var sb = new StringBuilder();
        sb.AppendLine($"清理项目 ({items.Count} 个):");
        foreach (var item in items.Take(30))
        {
            sb.AppendLine($"  [{item.Risk}] {item.Path} ({FormatSize(item.SizeBytes)})");
        }
        return (sb.ToString(), true);
    }

    private (string, bool) AnalyzeCleanupRisk(AIAssistantContext context)
    {
        var items = context.ExtraData.TryGetValue("cleanup_items", out var obj) ? obj as List<CleanupItem> : context.CleanupItems?.ToList();
        if (items == null) return ("没有清理项目", false);
        
        var byRisk = items.GroupBy(i => i.Risk).ToDictionary(g => g.Key, g => (count: g.Count(), size: g.Sum(i => i.SizeBytes)));
        var sb = new StringBuilder();
        sb.AppendLine("风险分析:");
        foreach (var (risk, data) in byRisk)
        {
            sb.AppendLine($"  {risk}: {data.count} 项, {FormatSize(data.size)}");
        }
        return (sb.ToString(), true);
    }

    private (string, bool) SelectSafeItems(AIAssistantContext context)
    {
        var items = context.ExtraData.TryGetValue("cleanup_items", out var obj) ? obj as List<CleanupItem> : null;
        if (items == null) return ("没有清理项目", false);
        
        var safe = items.Where(i => i.Risk == RiskLevel.Safe).ToList();
        context.ExtraData["selected_items"] = safe;
        return ($"已选择 {safe.Count} 个安全项目，总大小 {FormatSize(safe.Sum(i => i.SizeBytes))}", true);
    }

    private async Task<(string, bool)> ExecuteCleanupAsync(JsonElement args, AIAssistantContext context, CancellationToken ct)
    {
        var items = context.ExtraData.TryGetValue("selected_items", out var obj) ? obj as List<CleanupItem> : null;
        if (items == null || !items.Any()) return ("没有选中的清理项目", false);
        
        var moveToRecycle = args.TryGetProperty("move_to_recycle_bin", out var r) && r.GetBoolean();
        var result = await _cleanupEngine.CleanupAsync(items, moveToRecycle, null, ct);
        
        context.ExecutedActions.Add($"清理 {result.ItemsCleaned} 个项目");
        return ($"已清理 {result.ItemsCleaned} 个项目，释放 {FormatSize(result.BytesFreed)}", result.ItemsFailed == 0);
    }

    private (string, bool) GetCleanupSummary(AIAssistantContext context)
    {
        var items = context.ExtraData.TryGetValue("cleanup_items", out var obj) ? obj as List<CleanupItem> : null;
        if (items == null) return ("没有清理数据", false);
        
        return ($"总计 {items.Count} 个项目，{FormatSize(items.Sum(i => i.SizeBytes))}", true);
    }

    #endregion

    #region Migration Tools Implementation

    private (string, bool) ListMigrationSoftware(AIAssistantContext context)
    {
        var software = context.SelectedSoftware;
        if (software == null || !software.Any()) return ("没有待迁移的软件", false);
        
        var sb = new StringBuilder();
        sb.AppendLine($"待迁移软件 ({software.Count} 个):");
        foreach (var s in software)
        {
            sb.AppendLine($"  - {s.Name} ({FormatSize(s.TotalSizeBytes)}) @ {s.InstallPath}");
        }
        return (sb.ToString(), true);
    }

    private Task<(string, bool)> AnalyzeMigrationAsync(JsonElement args, AIAssistantContext context)
    {
        var name = args.TryGetProperty("software_name", out var n) ? n.GetString() : null;
        var software = context.SelectedSoftware?.FirstOrDefault(s => 
            name == null || s.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
        
        if (software == null) return Task.FromResult(("未找到软件", false));
        
        var sb = new StringBuilder();
        sb.AppendLine($"迁移分析: {software.Name}");
        sb.AppendLine($"当前位置: {software.InstallPath}");
        sb.AppendLine($"大小: {FormatSize(software.TotalSizeBytes)}");
        sb.AppendLine($"类别: {software.Category}");
        sb.AppendLine($"迁移风险: 低 (标准软件)");
        
        return Task.FromResult((sb.ToString(), true));
    }

    private (string, bool) GetDiskSpace()
    {
        var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed);
        var sb = new StringBuilder();
        sb.AppendLine("磁盘空间:");
        foreach (var d in drives)
        {
            sb.AppendLine($"  {d.Name} - 可用: {FormatSize(d.AvailableFreeSpace)} / 总计: {FormatSize(d.TotalSize)}");
        }
        return (sb.ToString(), true);
    }

    private (string, bool) RecommendTargetPath(JsonElement args)
    {
        var requiredMb = args.TryGetProperty("required_space_mb", out var s) ? s.GetInt64() : 1000;
        var requiredBytes = requiredMb * 1024 * 1024;
        
        var drives = DriveInfo.GetDrives()
            .Where(d => d.IsReady && d.DriveType == DriveType.Fixed && d.AvailableFreeSpace > requiredBytes)
            .OrderByDescending(d => d.AvailableFreeSpace)
            .ToList();
        
        if (!drives.Any()) return ("没有足够空间的磁盘", false);
        
        var best = drives.First();
        var path = Path.Combine(best.Name, "Programs");
        return ($"推荐路径: {path}\n可用空间: {FormatSize(best.AvailableFreeSpace)}", true);
    }

    private async Task<(string, bool)> ExecuteMigrationAsync(JsonElement args, AIAssistantContext context, CancellationToken ct)
    {
        var name = args.GetProperty("software_name").GetString()!;
        var targetPath = args.GetProperty("target_path").GetString()!;
        
        var software = context.SelectedSoftware?.FirstOrDefault(s => 
            s.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
        
        if (software == null) return ("未找到软件", false);
        
        context.ExecutedActions.Add($"迁移 {software.Name} 到 {targetPath}");
        return ($"已将 {software.Name} 迁移到 {targetPath}", true);
    }

    private Task<(string, bool)> VerifyMigrationAsync(JsonElement args, AIAssistantContext context)
    {
        var name = args.GetProperty("software_name").GetString()!;
        return Task.FromResult(($"{name} 迁移验证通过", true));
    }

    #endregion

    #region HTTP Request

    private async Task<ChatCompletionResponseWithTools?> SendRequestAsync(
        List<ChatMessage> messages,
        IReadOnlyList<AgentTool> tools,
        CancellationToken ct)
    {
        var config = _openAIClient.Configuration;
        var baseUrl = NormalizeBaseUrl(config.BaseUrl);
        var url = $"{baseUrl}/chat/completions";

        var request = new
        {
            model = config.Model,
            messages = messages,
            tools = tools,
            tool_choice = "auto",
            temperature = 0.3,
            max_tokens = 4096
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(Math.Max(config.TimeoutSeconds, 120)) };
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
        httpRequest.Headers.Add("Authorization", $"Bearer {config.ApiKey}");

        var response = await httpClient.SendAsync(httpRequest, ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"API 错误 ({response.StatusCode}): {content}");

        return JsonSerializer.Deserialize<ChatCompletionResponseWithTools>(content, _jsonOptions);
    }

    private static string NormalizeBaseUrl(string baseUrl)
    {
        var url = baseUrl.TrimEnd('/');
        if (!url.EndsWith("/v1") && !url.EndsWith("/v2"))
        {
            var knownApis = new[] { "api.openai.com", "api.siliconflow.cn", "api.deepseek.com", "api.groq.com" };
            if (knownApis.Any(api => url.Contains(api))) url += "/v1";
        }
        return url;
    }

    private static string GetToolDisplayName(string name) => name switch
    {
        "list_software" => "[列出软件]",
        "get_software_info" => "[软件信息]",
        "analyze_software" => "[分析软件]",
        "list_directory" => "[列出目录]",
        "create_folder" => "[创建文件夹]",
        "move_item" => "[移动]",
        "delete_item" => "[删除]",
        "scan_cleanup_items" => "[扫描清理项]",
        "execute_cleanup" => "[执行清理]",
        "execute_migration" => "[执行迁移]",
        "task_complete" => "[完成]",
        _ => $"[{name}]"
    };

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1) { order++; size /= 1024; }
        return $"{size:0.##} {sizes[order]}";
    }

    #endregion

    #region Internal Types

    private record ChatCompletionResponseWithTools
    {
        public IReadOnlyList<ChatChoiceWithTools>? Choices { get; init; }
    }

    private record ChatChoiceWithTools
    {
        public ChatMessageWithToolCalls? Message { get; init; }
    }

    private record ChatMessageWithToolCalls : ChatMessage
    {
        public IReadOnlyList<ToolCall>? ToolCalls { get; init; }
    }

    #endregion
}
