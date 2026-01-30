using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Infrastructure.Services;

/// <summary>
/// 文件操作 Agent 实现 - 支持 AI 自动调用文件操作（类似 LangChain Agent）。
/// </summary>
public class FileAgent : IFileAgent
{
    private readonly IOpenAIClient _openAIClient;
    private readonly IFileSystemService _fileSystemService;
    private readonly IBatchFileOperator _batchFileOperator;
    private readonly JsonSerializerOptions _jsonOptions;

    // 工具定义
    private static readonly List<AgentTool> _tools = new()
    {
        // 列出目录内容
        new AgentTool
        {
            Function = new AgentFunction
            {
                Name = "list_directory",
                Description = "列出指定目录下的文件和文件夹。用于了解目录结构。",
                Parameters = new AgentFunctionParameters
                {
                    Properties = new Dictionary<string, AgentParameterProperty>
                    {
                        ["path"] = new() { Type = "string", Description = "要列出的目录路径" }
                    },
                    Required = new[] { "path" }
                }
            }
        },
        // 创建文件夹
        new AgentTool
        {
            Function = new AgentFunction
            {
                Name = "create_folder",
                Description = "创建新文件夹。",
                Parameters = new AgentFunctionParameters
                {
                    Properties = new Dictionary<string, AgentParameterProperty>
                    {
                        ["path"] = new() { Type = "string", Description = "要创建的文件夹完整路径" }
                    },
                    Required = new[] { "path" }
                }
            }
        },
        // 移动文件/文件夹
        new AgentTool
        {
            Function = new AgentFunction
            {
                Name = "move_item",
                Description = "移动文件或文件夹到新位置。",
                Parameters = new AgentFunctionParameters
                {
                    Properties = new Dictionary<string, AgentParameterProperty>
                    {
                        ["source"] = new() { Type = "string", Description = "源文件或文件夹路径" },
                        ["destination"] = new() { Type = "string", Description = "目标路径（包含新名称）" }
                    },
                    Required = new[] { "source", "destination" }
                }
            }
        },
        // 复制文件/文件夹
        new AgentTool
        {
            Function = new AgentFunction
            {
                Name = "copy_item",
                Description = "复制文件或文件夹到新位置。",
                Parameters = new AgentFunctionParameters
                {
                    Properties = new Dictionary<string, AgentParameterProperty>
                    {
                        ["source"] = new() { Type = "string", Description = "源文件或文件夹路径" },
                        ["destination"] = new() { Type = "string", Description = "目标路径" }
                    },
                    Required = new[] { "source", "destination" }
                }
            }
        },
        // 删除文件/文件夹
        new AgentTool
        {
            Function = new AgentFunction
            {
                Name = "delete_item",
                Description = "删除文件或文件夹。谨慎使用！",
                Parameters = new AgentFunctionParameters
                {
                    Properties = new Dictionary<string, AgentParameterProperty>
                    {
                        ["path"] = new() { Type = "string", Description = "要删除的文件或文件夹路径" },
                        ["recursive"] = new() { Type = "boolean", Description = "是否递归删除（用于非空文件夹）" }
                    },
                    Required = new[] { "path" }
                }
            }
        },
        // 重命名
        new AgentTool
        {
            Function = new AgentFunction
            {
                Name = "rename_item",
                Description = "重命名文件或文件夹。",
                Parameters = new AgentFunctionParameters
                {
                    Properties = new Dictionary<string, AgentParameterProperty>
                    {
                        ["path"] = new() { Type = "string", Description = "要重命名的文件或文件夹路径" },
                        ["new_name"] = new() { Type = "string", Description = "新名称（不含路径）" }
                    },
                    Required = new[] { "path", "new_name" }
                }
            }
        },
        // 获取文件信息
        new AgentTool
        {
            Function = new AgentFunction
            {
                Name = "get_file_info",
                Description = "获取文件或文件夹的详细信息（大小、修改时间等）。",
                Parameters = new AgentFunctionParameters
                {
                    Properties = new Dictionary<string, AgentParameterProperty>
                    {
                        ["path"] = new() { Type = "string", Description = "文件或文件夹路径" }
                    },
                    Required = new[] { "path" }
                }
            }
        },
        // 搜索文件
        new AgentTool
        {
            Function = new AgentFunction
            {
                Name = "search_files",
                Description = "在目录中搜索匹配的文件。",
                Parameters = new AgentFunctionParameters
                {
                    Properties = new Dictionary<string, AgentParameterProperty>
                    {
                        ["directory"] = new() { Type = "string", Description = "搜索的目录" },
                        ["pattern"] = new() { Type = "string", Description = "搜索模式（如 *.txt, *报告*）" },
                        ["recursive"] = new() { Type = "boolean", Description = "是否递归搜索子目录" }
                    },
                    Required = new[] { "directory", "pattern" }
                }
            }
        },
        // 批量移动
        new AgentTool
        {
            Function = new AgentFunction
            {
                Name = "batch_move",
                Description = "批量移动多个文件到目标目录。",
                Parameters = new AgentFunctionParameters
                {
                    Properties = new Dictionary<string, AgentParameterProperty>
                    {
                        ["files"] = new() { Type = "string", Description = "要移动的文件路径列表，用逗号分隔" },
                        ["destination_folder"] = new() { Type = "string", Description = "目标文件夹路径" }
                    },
                    Required = new[] { "files", "destination_folder" }
                }
            }
        },
        // 完成任务
        new AgentTool
        {
            Function = new AgentFunction
            {
                Name = "task_complete",
                Description = "当所有整理任务完成时调用此函数，向用户报告结果。",
                Parameters = new AgentFunctionParameters
                {
                    Properties = new Dictionary<string, AgentParameterProperty>
                    {
                        ["summary"] = new() { Type = "string", Description = "任务完成摘要" }
                    },
                    Required = new[] { "summary" }
                }
            }
        }
    };

    public FileAgent(
        IOpenAIClient openAIClient,
        IFileSystemService fileSystemService,
        IBatchFileOperator batchFileOperator)
    {
        _openAIClient = openAIClient;
        _fileSystemService = fileSystemService;
        _batchFileOperator = batchFileOperator;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };
    }

    public IReadOnlyList<AgentTool> GetAvailableTools() => _tools;

    public async Task<ToolResult> ExecuteToolAsync(ToolCall toolCall, AgentContext context, CancellationToken cancellationToken = default)
    {
        var functionName = toolCall.Function?.Name ?? "";
        var argsJson = toolCall.Function?.Arguments ?? "{}";

        try
        {
            using var argsDoc = JsonDocument.Parse(argsJson);
            var args = argsDoc.RootElement;

            return functionName switch
            {
                "list_directory" => await ExecuteListDirectoryAsync(args, context, cancellationToken),
                "create_folder" => await ExecuteCreateFolderAsync(args, context, cancellationToken),
                "move_item" => await ExecuteMoveItemAsync(args, context, cancellationToken),
                "copy_item" => await ExecuteCopyItemAsync(args, context, cancellationToken),
                "delete_item" => await ExecuteDeleteItemAsync(args, context, cancellationToken),
                "rename_item" => await ExecuteRenameItemAsync(args, context, cancellationToken),
                "get_file_info" => await ExecuteGetFileInfoAsync(args, context, cancellationToken),
                "search_files" => await ExecuteSearchFilesAsync(args, context, cancellationToken),
                "batch_move" => await ExecuteBatchMoveAsync(args, context, cancellationToken),
                "task_complete" => ExecuteTaskComplete(args, toolCall.Id),
                _ => new ToolResult
                {
                    ToolCallId = toolCall.Id,
                    Name = functionName,
                    Success = false,
                    Result = "",
                    Error = $"未知的工具: {functionName}"
                }
            };
        }
        catch (Exception ex)
        {
            return new ToolResult
            {
                ToolCallId = toolCall.Id,
                Name = functionName,
                Success = false,
                Result = "",
                Error = ex.Message
            };
        }
    }

    #region 工具实现

    private async Task<ToolResult> ExecuteListDirectoryAsync(JsonElement args, AgentContext context, CancellationToken ct)
    {
        var path = args.GetProperty("path").GetString() ?? context.CurrentPath;
        
        var content = await _fileSystemService.GetDirectoryContentAsync(path, new FileFilterOptions());
        
        var sb = new StringBuilder();
        sb.AppendLine($"目录: {path}");
        sb.AppendLine($"文件夹 ({content.Directories.Count}):");
        foreach (var dir in content.Directories.Take(50))
        {
            sb.AppendLine($"  📁 {dir.Name}");
        }
        if (content.Directories.Count > 50)
            sb.AppendLine($"  ... 还有 {content.Directories.Count - 50} 个文件夹");

        sb.AppendLine($"文件 ({content.Files.Count}):");
        foreach (var file in content.Files.Take(50))
        {
            sb.AppendLine($"  📄 {file.Name} ({FormatSize(file.Size)}, {file.Extension})");
        }
        if (content.Files.Count > 50)
            sb.AppendLine($"  ... 还有 {content.Files.Count - 50} 个文件");

        return new ToolResult
        {
            ToolCallId = "",
            Name = "list_directory",
            Success = true,
            Result = sb.ToString()
        };
    }

    private async Task<ToolResult> ExecuteCreateFolderAsync(JsonElement args, AgentContext context, CancellationToken ct)
    {
        var path = args.GetProperty("path").GetString()!;
        
        var result = await _fileSystemService.CreateDirectoryAsync(path);
        context.ExecutedActions.Add($"创建文件夹: {path}");

        return new ToolResult
        {
            ToolCallId = "",
            Name = "create_folder",
            Success = result.Success,
            Result = result.Success ? $"已创建文件夹: {path}" : "",
            Error = result.ErrorMessage
        };
    }

    private async Task<ToolResult> ExecuteMoveItemAsync(JsonElement args, AgentContext context, CancellationToken ct)
    {
        var source = args.GetProperty("source").GetString()!;
        var destination = args.GetProperty("destination").GetString()!;

        var result = await _fileSystemService.MoveAsync(source, destination);
        context.ExecutedActions.Add($"移动: {source} → {destination}");

        return new ToolResult
        {
            ToolCallId = "",
            Name = "move_item",
            Success = result.Success,
            Result = result.Success ? $"已移动: {Path.GetFileName(source)} → {destination}" : "",
            Error = result.ErrorMessage
        };
    }

    private async Task<ToolResult> ExecuteCopyItemAsync(JsonElement args, AgentContext context, CancellationToken ct)
    {
        var source = args.GetProperty("source").GetString()!;
        var destination = args.GetProperty("destination").GetString()!;

        var result = await _fileSystemService.CopyAsync(source, destination);
        context.ExecutedActions.Add($"复制: {source} → {destination}");

        return new ToolResult
        {
            ToolCallId = "",
            Name = "copy_item",
            Success = result.Success,
            Result = result.Success ? $"已复制: {Path.GetFileName(source)} → {destination}" : "",
            Error = result.ErrorMessage
        };
    }

    private async Task<ToolResult> ExecuteDeleteItemAsync(JsonElement args, AgentContext context, CancellationToken ct)
    {
        var path = args.GetProperty("path").GetString()!;
        var recursive = args.TryGetProperty("recursive", out var r) && r.GetBoolean();

        var result = await _fileSystemService.DeleteAsync(path, recursive);
        context.ExecutedActions.Add($"删除: {path}");

        return new ToolResult
        {
            ToolCallId = "",
            Name = "delete_item",
            Success = result.Success,
            Result = result.Success ? $"已删除: {path}" : "",
            Error = result.ErrorMessage
        };
    }

    private async Task<ToolResult> ExecuteRenameItemAsync(JsonElement args, AgentContext context, CancellationToken ct)
    {
        var path = args.GetProperty("path").GetString()!;
        var newName = args.GetProperty("new_name").GetString()!;

        var result = await _fileSystemService.RenameAsync(path, newName);
        context.ExecutedActions.Add($"重命名: {Path.GetFileName(path)} → {newName}");

        return new ToolResult
        {
            ToolCallId = "",
            Name = "rename_item",
            Success = result.Success,
            Result = result.Success ? $"已重命名: {Path.GetFileName(path)} → {newName}" : "",
            Error = result.ErrorMessage
        };
    }

    private Task<ToolResult> ExecuteGetFileInfoAsync(JsonElement args, AgentContext context, CancellationToken ct)
    {
        var path = args.GetProperty("path").GetString()!;

        var sb = new StringBuilder();
        
        if (Directory.Exists(path))
        {
            var dirInfo = new DirectoryInfo(path);
            sb.AppendLine($"类型: 文件夹");
            sb.AppendLine($"名称: {dirInfo.Name}");
            sb.AppendLine($"路径: {dirInfo.FullName}");
            sb.AppendLine($"创建时间: {dirInfo.CreationTime}");
            sb.AppendLine($"修改时间: {dirInfo.LastWriteTime}");
            
            try
            {
                var files = dirInfo.GetFiles("*", SearchOption.AllDirectories);
                var totalSize = files.Sum(f => f.Length);
                sb.AppendLine($"包含文件数: {files.Length}");
                sb.AppendLine($"总大小: {FormatSize(totalSize)}");
            }
            catch (UnauthorizedAccessException)
            {
                sb.AppendLine("(部分子目录无权限访问)");
            }
        }
        else if (File.Exists(path))
        {
            var fileInfo = new FileInfo(path);
            sb.AppendLine($"类型: 文件");
            sb.AppendLine($"名称: {fileInfo.Name}");
            sb.AppendLine($"路径: {fileInfo.FullName}");
            sb.AppendLine($"大小: {FormatSize(fileInfo.Length)}");
            sb.AppendLine($"扩展名: {fileInfo.Extension}");
            sb.AppendLine($"创建时间: {fileInfo.CreationTime}");
            sb.AppendLine($"修改时间: {fileInfo.LastWriteTime}");
        }
        else
        {
            return Task.FromResult(new ToolResult
            {
                ToolCallId = "",
                Name = "get_file_info",
                Success = false,
                Result = "",
                Error = $"路径不存在: {path}"
            });
        }

        return Task.FromResult(new ToolResult
        {
            ToolCallId = "",
            Name = "get_file_info",
            Success = true,
            Result = sb.ToString()
        });
    }

    private Task<ToolResult> ExecuteSearchFilesAsync(JsonElement args, AgentContext context, CancellationToken ct)
    {
        var directory = args.GetProperty("directory").GetString()!;
        var pattern = args.GetProperty("pattern").GetString()!;
        var recursive = args.TryGetProperty("recursive", out var r) && r.GetBoolean();

        try
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(directory, pattern, searchOption);

            var sb = new StringBuilder();
            sb.AppendLine($"搜索结果 ({files.Length} 个文件):");
            foreach (var file in files.Take(100))
            {
                var info = new FileInfo(file);
                sb.AppendLine($"  {info.FullName} ({FormatSize(info.Length)})");
            }
            if (files.Length > 100)
                sb.AppendLine($"  ... 还有 {files.Length - 100} 个文件");

            return Task.FromResult(new ToolResult
            {
                ToolCallId = "",
                Name = "search_files",
                Success = true,
                Result = sb.ToString()
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ToolResult
            {
                ToolCallId = "",
                Name = "search_files",
                Success = false,
                Result = "",
                Error = ex.Message
            });
        }
    }

    private async Task<ToolResult> ExecuteBatchMoveAsync(JsonElement args, AgentContext context, CancellationToken ct)
    {
        var filesStr = args.GetProperty("files").GetString()!;
        var destFolder = args.GetProperty("destination_folder").GetString()!;

        // 确保目标文件夹存在
        if (!Directory.Exists(destFolder))
        {
            Directory.CreateDirectory(destFolder);
        }

        var files = filesStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .ToList();

        var operations = files.Select(f => (f, Path.Combine(destFolder, Path.GetFileName(f)))).ToList();
        
        var result = await _batchFileOperator.MoveAsync(operations, false, null, ct);
        
        context.ExecutedActions.Add($"批量移动 {result.SuccessCount} 个文件到 {destFolder}");

        return new ToolResult
        {
            ToolCallId = "",
            Name = "batch_move",
            Success = result.FailedCount == 0,
            Result = $"已移动 {result.SuccessCount} 个文件到 {destFolder}" + 
                     (result.FailedCount > 0 ? $"，{result.FailedCount} 个失败" : ""),
            Error = result.FailedCount > 0 ? $"有 {result.FailedCount} 个文件移动失败" : null
        };
    }

    private ToolResult ExecuteTaskComplete(JsonElement args, string toolCallId)
    {
        var summary = args.GetProperty("summary").GetString()!;

        return new ToolResult
        {
            ToolCallId = toolCallId,
            Name = "task_complete",
            Success = true,
            Result = summary
        };
    }

    #endregion

    public async IAsyncEnumerable<AgentEvent> RunAsync(
        string userRequest,
        AgentContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage>
        {
            ChatMessage.System(GetSystemPrompt(context)),
            ChatMessage.User(userRequest)
        };

        while (context.CurrentIteration < context.MaxIterations)
        {
            context.CurrentIteration++;
            cancellationToken.ThrowIfCancellationRequested();

            yield return new AgentEvent { Type = AgentEventType.Thinking, Message = "正在分析..." };

            // 发送请求（带工具定义）
            var request = new ChatCompletionRequestWithTools
            {
                Model = _openAIClient.Configuration.Model,
                Messages = messages,
                Tools = _tools,
                ToolChoice = "auto",
                Temperature = 0.2,
                MaxTokens = 4096
            };

            ChatCompletionResponseWithTools? response = null;
            Exception? requestError = null;
            
            try
            {
                response = await SendRequestWithToolsAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                requestError = ex;
            }

            if (requestError != null)
            {
                yield return new AgentEvent { Type = AgentEventType.Error, Error = requestError };
                yield break;
            }

            var choice = response?.Choices?.FirstOrDefault();
            if (choice == null)
            {
                yield return new AgentEvent { Type = AgentEventType.Error, Error = new Exception("AI 没有返回响应") };
                yield break;
            }

            // 如果有文本消息，输出它
            if (!string.IsNullOrEmpty(choice.Message?.Content))
            {
                yield return new AgentEvent { Type = AgentEventType.Message, Message = choice.Message.Content };
                messages.Add(ChatMessage.Assistant(choice.Message.Content));
            }

            // 检查是否有工具调用
            var toolCalls = choice.Message?.ToolCalls;
            if (toolCalls == null || toolCalls.Count == 0)
            {
                // 没有工具调用，任务完成
                yield return new AgentEvent { Type = AgentEventType.Complete };
                yield break;
            }

            // 执行工具调用
            foreach (var toolCall in toolCalls)
            {
                var functionName = toolCall.Function?.Name ?? "";
                
                yield return new AgentEvent 
                { 
                    Type = AgentEventType.ToolCall, 
                    ToolCall = toolCall,
                    Message = $"执行: {GetToolDisplayName(functionName)}"
                };

                var result = await ExecuteToolAsync(toolCall, context, cancellationToken);
                result = result with { ToolCallId = toolCall.Id };
                
                yield return new AgentEvent 
                { 
                    Type = AgentEventType.ToolResult, 
                    ToolResult = result,
                    Message = result.Success ? result.Result : $"❌ {result.Error}"
                };

                // 如果是 task_complete，结束循环
                if (functionName == "task_complete")
                {
                    yield return new AgentEvent { Type = AgentEventType.Complete, Message = result.Result };
                    yield break;
                }

                context.ToolResults.Add(result);
            }

            // 将工具结果添加到消息历史
            // 先添加 assistant 消息（包含 tool_calls）
            messages.Add(new ChatMessageWithToolCalls
            {
                Role = "assistant",
                Content = choice.Message?.Content,
                ToolCalls = toolCalls
            });

            // 再添加每个工具的结果
            foreach (var result in context.ToolResults.TakeLast(toolCalls.Count))
            {
                messages.Add(new ChatMessage
                {
                    Role = "tool",
                    Content = result.Success ? result.Result : $"错误: {result.Error}",
                    Name = result.Name
                } with { ToolCallId = result.ToolCallId });
            }
        }

        yield return new AgentEvent 
        { 
            Type = AgentEventType.Complete, 
            Message = $"已达到最大迭代次数 ({context.MaxIterations})，任务可能未完全完成。" 
        };
    }

    private async Task<ChatCompletionResponseWithTools> SendRequestWithToolsAsync(
        ChatCompletionRequestWithTools request, 
        CancellationToken cancellationToken)
    {
        // 检查配置是否有效
        if (!_openAIClient.IsConfigured)
        {
            throw new InvalidOperationException("AI 功能未配置。请先在设置页面配置 API 密钥和基础 URL。");
        }

        var config = _openAIClient.Configuration;
        var baseUrl = NormalizeBaseUrl(config.BaseUrl);
        var url = $"{baseUrl}/chat/completions";
        
        var json = JsonSerializer.Serialize(request, _jsonOptions);

        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(config.TimeoutSeconds, 120)); // 至少 120 秒
        
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
        httpRequest.Headers.Add("Authorization", $"Bearer {config.ApiKey}");

        // 使用更长的超时时间，因为 AI 响应可能较慢
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(Math.Max(config.TimeoutSeconds, 120)));

        try
        {
            var response = await httpClient.SendAsync(httpRequest, cts.Token);
            var responseContent = await response.Content.ReadAsStringAsync(cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"API 请求失败 ({response.StatusCode}): {responseContent}");
            }

            return JsonSerializer.Deserialize<ChatCompletionResponseWithTools>(responseContent, _jsonOptions)
                ?? throw new InvalidOperationException("无法解析 API 响应");
        }
        catch (TaskCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"API 请求超时（{config.TimeoutSeconds} 秒）。请检查网络连接或增加超时时间。");
        }
    }

    private static string NormalizeBaseUrl(string baseUrl)
    {
        var url = baseUrl.TrimEnd('/');
        if (!url.EndsWith("/v1") && !url.EndsWith("/v2") && !url.EndsWith("/v3"))
        {
            var knownApis = new[] { "api.openai.com", "api.siliconflow.cn", "api.deepseek.com", "api.groq.com" };
            if (knownApis.Any(api => url.Contains(api)))
            {
                url += "/v1";
            }
        }
        return url;
    }

    private string GetSystemPrompt(AgentContext context)
    {
        return $"""
            你是一个专业的文件整理助手 Agent。你可以通过调用工具来自动执行文件操作。

            当前工作目录: {context.CurrentPath}

            你的工作流程：
            1. 首先使用 list_directory 了解目录结构
            2. 分析文件，制定整理计划
            3. 逐步执行操作（创建文件夹、移动文件等）
            4. 完成后调用 task_complete 报告结果

            重要规则：
            - 在移动或删除文件前，先确认文件存在
            - 创建分类文件夹时使用清晰的中文名称
            - 按文件类型分类：图片、文档、视频、音乐、压缩包、程序等
            - 谨慎处理删除操作，只删除明显的临时文件
            - 每次只执行一个操作，等待结果后再继续
            - 操作完成后必须调用 task_complete

            常见文件分类：
            - 图片: .jpg, .jpeg, .png, .gif, .bmp, .webp, .svg
            - 文档: .doc, .docx, .pdf, .txt, .xlsx, .pptx, .md
            - 视频: .mp4, .avi, .mkv, .mov, .wmv
            - 音乐: .mp3, .wav, .flac, .aac, .ogg
            - 压缩包: .zip, .rar, .7z, .tar, .gz
            - 程序: .exe, .msi, .dmg, .apk
            """;
    }

    private static string GetToolDisplayName(string functionName)
    {
        return functionName switch
        {
            "list_directory" => "[列出目录]",
            "create_folder" => "[创建文件夹]",
            "move_item" => "[移动文件]",
            "copy_item" => "[复制文件]",
            "delete_item" => "[删除文件]",
            "rename_item" => "[重命名]",
            "get_file_info" => "[获取信息]",
            "search_files" => "[搜索文件]",
            "batch_move" => "[批量移动]",
            "task_complete" => "[完成任务]",
            _ => functionName
        };
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

    #region 扩展消息类型（支持 tool_calls）

    private record ChatCompletionRequestWithTools
    {
        [JsonPropertyName("model")]
        public required string Model { get; init; }

        [JsonPropertyName("messages")]
        public required IReadOnlyList<ChatMessage> Messages { get; init; }

        [JsonPropertyName("tools")]
        public IReadOnlyList<AgentTool>? Tools { get; init; }

        [JsonPropertyName("tool_choice")]
        public string? ToolChoice { get; init; }

        [JsonPropertyName("temperature")]
        public double? Temperature { get; init; }

        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; init; }
    }

    private record ChatCompletionResponseWithTools
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("choices")]
        public IReadOnlyList<ChatChoiceWithTools>? Choices { get; init; }

        [JsonPropertyName("usage")]
        public ChatUsage? Usage { get; init; }
    }

    private record ChatChoiceWithTools
    {
        [JsonPropertyName("index")]
        public int Index { get; init; }

        [JsonPropertyName("message")]
        public ChatMessageWithToolCalls? Message { get; init; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; init; }
    }

    private record ChatMessageWithToolCalls : ChatMessage
    {
        [JsonPropertyName("tool_calls")]
        public IReadOnlyList<ToolCall>? ToolCalls { get; init; }
    }

    #endregion
}

// 扩展 ChatMessage 以支持 tool 角色
public static class ChatMessageExtensions
{
    public static ChatMessage WithToolCallId(this ChatMessage message, string toolCallId)
    {
        return message with { ToolCallId = toolCallId };
    }
}
