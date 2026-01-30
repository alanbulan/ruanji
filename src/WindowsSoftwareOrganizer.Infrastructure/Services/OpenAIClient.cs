using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Infrastructure.Services;

/// <summary>
/// OpenAI 客户端实现。
/// </summary>
public class OpenAIClient : IOpenAIClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IConfigurationService _configService;
    private OpenAIConfiguration _configuration;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;
    private bool _initialized;

    public OpenAIClient(IConfigurationService configService)
    {
        _configService = configService;
        _configuration = new OpenAIConfiguration();
        _httpClient = new HttpClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        
        // 异步加载配置
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        if (_initialized) return;
        
        try
        {
            var config = await _configService.GetConfigurationAsync();
            if (config.OpenAIConfiguration != null && config.OpenAIConfiguration.IsValid)
            {
                _configuration = config.OpenAIConfiguration;
                Debug.WriteLine($"OpenAIClient: 已从配置加载 API 设置，BaseUrl={_configuration.BaseUrl}, Model={_configuration.Model}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OpenAIClient: 加载配置失败: {ex.Message}");
        }
        finally
        {
            _initialized = true;
        }
    }

    /// <summary>
    /// 确保配置已加载。
    /// </summary>
    private async Task EnsureInitializedAsync()
    {
        if (!_initialized)
        {
            await InitializeAsync();
        }
    }

    public OpenAIConfiguration Configuration => _configuration;

    public bool IsConfigured => _configuration.IsValid;

    public async Task<bool> EnsureConfiguredAsync()
    {
        await EnsureInitializedAsync();
        return _configuration.IsValid;
    }

    public void UpdateConfiguration(OpenAIConfiguration configuration)
    {
        _configuration = configuration;
        _initialized = true;
    }

    public void Configure(OpenAIConfiguration configuration) => UpdateConfiguration(configuration);

    public async Task<string> SendChatCompletionAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        
        var response = await SendChatCompletionRequestAsync(new ChatCompletionRequest
        {
            Model = _configuration.Model,
            Messages = messages,
            MaxTokens = _configuration.MaxTokens,
            Temperature = _configuration.Temperature
        }, cancellationToken);

        return response.FirstContent ?? string.Empty;
    }

    public async Task<ChatCompletionResponse> SendChatCompletionRequestAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        EnsureConfigured();

        var baseUrl = NormalizeBaseUrl(_configuration.BaseUrl);
        var url = $"{baseUrl}/chat/completions";
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
        AddAuthHeaders(httpRequest, _configuration);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_configuration.TimeoutSeconds));

        var response = await _httpClient.SendAsync(httpRequest, cts.Token);
        var responseContent = await response.Content.ReadAsStringAsync(cts.Token);

        if (!response.IsSuccessStatusCode)
        {
            var error = TryParseError(responseContent);
            throw new HttpRequestException(
                $"OpenAI API 请求失败: {response.StatusCode} - {error?.Error?.Message ?? responseContent}");
        }

        var result = JsonSerializer.Deserialize<ChatCompletionResponse>(responseContent, _jsonOptions);
        return result ?? throw new InvalidOperationException("无法解析 API 响应");
    }

    public async IAsyncEnumerable<string> StreamChatCompletionAsync(
        IReadOnlyList<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        EnsureConfigured();

        var request = new ChatCompletionRequest
        {
            Model = _configuration.Model,
            Messages = messages,
            MaxTokens = _configuration.MaxTokens,
            Temperature = _configuration.Temperature,
            Stream = true
        };

        var baseUrl = NormalizeBaseUrl(_configuration.BaseUrl);
        var url = $"{baseUrl}/chat/completions";
        var json = JsonSerializer.Serialize(request, _jsonOptions);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
        AddAuthHeaders(httpRequest, _configuration);

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken);
            
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data: "))
                continue;

            var data = line[6..];
            if (data == "[DONE]")
                break;

            var chunk = JsonSerializer.Deserialize<StreamChunk>(data, _jsonOptions);
            var delta = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
            if (!string.IsNullOrEmpty(delta))
            {
                yield return delta;
            }
        }
    }

    public async Task<APITestResult> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        return await TestConnectionAsync(_configuration, cancellationToken);
    }

    public async Task<APITestResult> TestConnectionAsync(OpenAIConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            if (!configuration.IsValid)
            {
                return new APITestResult
                {
                    Success = false,
                    Message = "API 未配置",
                    ErrorDetails = "请先配置 API Key 和 Base URL"
                };
            }

            // 直接使用提供的配置发送请求，不修改实例配置
            var baseUrl = NormalizeBaseUrl(configuration.BaseUrl);
            var url = $"{baseUrl}/chat/completions";
            var request = new ChatCompletionRequest
            {
                Model = configuration.Model,
                Messages = new[] { ChatMessage.User("Hello") },
                MaxTokens = 10
            };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
            AddAuthHeaders(httpRequest, configuration);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(configuration.TimeoutSeconds));

            var response = await _httpClient.SendAsync(httpRequest, cts.Token);
            var responseContent = await response.Content.ReadAsStringAsync(cts.Token);

            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var error = TryParseError(responseContent);
                return new APITestResult
                {
                    Success = false,
                    Message = "连接失败",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    ErrorDetails = error?.Error?.Message ?? responseContent
                };
            }

            var result = JsonSerializer.Deserialize<ChatCompletionResponse>(responseContent, _jsonOptions);

            return new APITestResult
            {
                Success = true,
                Message = "连接成功",
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                Model = result?.Model
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new APITestResult
            {
                Success = false,
                Message = "连接失败",
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                ErrorDetails = ex.Message
            };
        }
    }

    public async Task<IReadOnlyList<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        return await GetAvailableModelsAsync(_configuration, cancellationToken);
    }

    public async Task<IReadOnlyList<ModelInfo>> GetAvailableModelsAsync(OpenAIConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (!configuration.IsValid)
        {
            return Array.Empty<ModelInfo>();
        }

        try
        {
            // 规范化 BaseUrl，确保有 /v1 后缀
            var baseUrl = NormalizeBaseUrl(configuration.BaseUrl);
            var url = $"{baseUrl}/models";
            
            Debug.WriteLine($"获取模型列表: {url}");
            
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
            AddAuthHeaders(httpRequest, configuration);
            
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30));
            
            var response = await _httpClient.SendAsync(httpRequest, cts.Token);
            var content = await response.Content.ReadAsStringAsync(cts.Token);
            
            Debug.WriteLine($"响应状态: {response.StatusCode}");
            
            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"获取模型列表失败: {response.StatusCode} - {content}");
                return Array.Empty<ModelInfo>();
            }

            // 解析模型列表（兼容多种 API 格式）
            var models = ParseModelsResponse(content, baseUrl);
            
            if (models.Count == 0)
            {
                Debug.WriteLine($"模型列表为空或解析失败，响应内容前500字符: {content.Substring(0, Math.Min(500, content.Length))}");
                return Array.Empty<ModelInfo>();
            }

            Debug.WriteLine($"获取到 {models.Count} 个聊天模型");
            return models;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"获取模型列表异常: {ex.Message}");
            return Array.Empty<ModelInfo>();
        }
    }

    /// <summary>
    /// 规范化 BaseUrl，确保格式正确。
    /// </summary>
    private static string NormalizeBaseUrl(string baseUrl)
    {
        var url = baseUrl.TrimEnd('/');
        
        // 如果 URL 不以 /v1 或 /v2 等版本号结尾，自动添加 /v1
        if (!url.EndsWith("/v1") && !url.EndsWith("/v2") && !url.EndsWith("/v3"))
        {
            // 检查是否是已知的需要 /v1 的 API
            var knownApis = new[]
            {
                "api.openai.com", "api.siliconflow.cn", "api.siliconflow.com",
                "api.deepseek.com", "api.groq.com", "api.together.xyz",
                "api.moonshot.cn", "open.bigmodel.cn", "dashscope.aliyuncs.com",
                "openrouter.ai"
            };
            
            if (knownApis.Any(api => url.Contains(api)))
            {
                url += "/v1";
            }
        }
        
        return url;
    }

    /// <summary>
    /// 解析模型列表响应（兼容 OpenAI、SiliconFlow、DeepSeek、OpenRouter、Groq 等格式）。
    /// </summary>
    private List<ModelInfo> ParseModelsResponse(string content, string baseUrl)
    {
        var models = new List<ModelInfo>();
        
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            
            // 标准 OpenAI 格式: { "object": "list", "data": [...] }
            if (root.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in dataArray.EnumerateArray())
                {
                    var modelId = GetModelId(item);
                    var displayName = GetModelDisplayName(item, modelId);
                    
                    if (!string.IsNullOrEmpty(modelId) && IsChatModel(modelId, baseUrl))
                    {
                        models.Add(new ModelInfo(modelId, displayName));
                    }
                }
            }
            // 某些 API 直接返回数组
            else if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                {
                    var modelId = GetModelId(item);
                    var displayName = GetModelDisplayName(item, modelId);
                    
                    if (!string.IsNullOrEmpty(modelId) && IsChatModel(modelId, baseUrl))
                    {
                        models.Add(new ModelInfo(modelId, displayName));
                    }
                }
            }
            // 某些 API 使用 "models" 字段
            else if (root.TryGetProperty("models", out var modelsArray) && modelsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in modelsArray.EnumerateArray())
                {
                    var modelId = GetModelId(item);
                    var displayName = GetModelDisplayName(item, modelId);
                    
                    if (!string.IsNullOrEmpty(modelId) && IsChatModel(modelId, baseUrl))
                    {
                        models.Add(new ModelInfo(modelId, displayName));
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"解析模型列表 JSON 失败: {ex.Message}");
        }
        
        return models.OrderBy(m => m.Id).ToList();
    }

    /// <summary>
    /// 从 JSON 元素获取模型 ID。
    /// </summary>
    private static string GetModelId(JsonElement item)
    {
        // 尝试多种字段名
        if (item.TryGetProperty("id", out var idProp))
            return idProp.GetString() ?? string.Empty;
        if (item.TryGetProperty("model_id", out var modelIdProp))
            return modelIdProp.GetString() ?? string.Empty;
        if (item.TryGetProperty("name", out var nameProp))
            return nameProp.GetString() ?? string.Empty;
        if (item.TryGetProperty("model", out var modelProp))
            return modelProp.GetString() ?? string.Empty;
        if (item.ValueKind == JsonValueKind.String)
            return item.GetString() ?? string.Empty;
        
        return string.Empty;
    }

    /// <summary>
    /// 从 JSON 元素获取模型显示名称。
    /// </summary>
    private static string GetModelDisplayName(JsonElement item, string fallbackId)
    {
        // OpenRouter 格式有 name 字段
        if (item.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String)
        {
            var name = nameProp.GetString();
            if (!string.IsNullOrEmpty(name) && name != fallbackId)
                return name;
        }
        
        // 某些 API 有 display_name 字段
        if (item.TryGetProperty("display_name", out var displayNameProp))
        {
            var displayName = displayNameProp.GetString();
            if (!string.IsNullOrEmpty(displayName))
                return displayName;
        }
        
        return fallbackId;
    }

    /// <summary>
    /// 判断是否为聊天模型（过滤掉图像、音频、嵌入等模型）。
    /// </summary>
    private static bool IsChatModel(string modelId, string baseUrl)
    {
        var id = modelId.ToLowerInvariant();
        var url = baseUrl.ToLowerInvariant();
        
        // 排除非聊天模型（嵌入、图像、音频、重排序等）
        var excludePatterns = new[]
        {
            "embedding", "embed", "whisper", "tts", "dall-e", "stable-diffusion",
            "sdxl", "flux", "image", "audio", "speech", "vision-encoder",
            "rerank", "bge-", "e5-", "gte-", "text-embedding", "ada-002",
            "moderation", "davinci-002", "babbage-002", "curie", "text-davinci",
            "code-davinci", "code-cushman", "text-curie", "text-babbage", "text-ada",
            "fishaudio", "cosyvoice", "funaudiollm", "f5-tts", "maskgct",
            "stabilityai/", "black-forest-labs/", "recraft", "ideogram",
            "playground-v", "sana", "hunyuan-video", "cogvideox", "ltx-video",
            "omnigen", "photomaker", "instantid", "pulid", "kolors"
        };
        
        if (excludePatterns.Any(p => id.Contains(p)))
        {
            return false;
        }

        // 聊天/语言模型关键词
        var includePatterns = new[]
        {
            // OpenAI
            "gpt-", "gpt4", "o1-", "o3-", "chatgpt",
            // Meta
            "llama", "llama-", "llama2", "llama3",
            // Alibaba
            "qwen", "qwen2", "qwen-",
            // DeepSeek
            "deepseek", "deepseek-",
            // Anthropic
            "claude", "claude-",
            // Google
            "gemini", "gemma", "palm",
            // Mistral
            "mistral", "mixtral", "codestral", "pixtral",
            // 01.AI
            "yi-", "yi/",
            // Zhipu
            "glm", "glm-", "chatglm",
            // Baichuan
            "baichuan",
            // InternLM
            "internlm",
            // Microsoft
            "phi-", "phi3", "phi4",
            // Cohere
            "command", "command-",
            // Others
            "solar", "vicuna", "openchat", "nous", "hermes", "wizard",
            "zephyr", "starling", "neural", "dolphin", "orca", "falcon",
            "mpt-", "redpajama", "pythia", "opt-", "bloom", "cerebras",
            "granite", "dbrx", "jamba", "arctic", "snowflake",
            // Code models (chat capable)
            "codellama", "codeqwen", "starcoder", "magicoder", "wavecoder",
            "deepseek-coder", "codestral", "qwen2.5-coder",
            // Reasoning models
            "r1", "reasoner", "think"
        };

        // 如果包含聊天模型关键词，返回 true
        if (includePatterns.Any(p => id.Contains(p)))
        {
            return true;
        }

        // 对于 OpenRouter 等聚合平台，模型 ID 通常包含 "/"
        // 如 "openai/gpt-4", "anthropic/claude-3"
        if (id.Contains("/"))
        {
            var parts = id.Split('/');
            if (parts.Length >= 2)
            {
                var modelName = parts[^1]; // 最后一部分
                return includePatterns.Any(p => modelName.Contains(p));
            }
        }

        // 对于 SiliconFlow 等平台，如果 URL 包含特定域名，放宽过滤
        if (url.Contains("siliconflow") || url.Contains("openrouter") || 
            url.Contains("together") || url.Contains("groq") ||
            url.Contains("deepseek") || url.Contains("moonshot") ||
            url.Contains("zhipu") || url.Contains("dashscope"))
        {
            // 这些平台的模型列表通常已经过滤，直接返回 true
            // 除非明确是非聊天模型
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    private static void AddAuthHeaders(HttpRequestMessage request, OpenAIConfiguration config)
    {
        if (!string.IsNullOrEmpty(config.ApiKey))
        {
            request.Headers.Add("Authorization", $"Bearer {config.ApiKey}");
        }
        if (!string.IsNullOrEmpty(config.OrganizationId))
        {
            request.Headers.Add("OpenAI-Organization", config.OrganizationId);
        }
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("OpenAI API 未配置。请先设置 API Key。");
        }
    }

    private APIErrorResponse? TryParseError(string content)
    {
        try
        {
            return JsonSerializer.Deserialize<APIErrorResponse>(content, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    // 流式响应的内部类
    private record StreamChunk
    {
        public IReadOnlyList<StreamChoice>? Choices { get; init; }
    }

    private record StreamChoice
    {
        public StreamDelta? Delta { get; init; }
    }

    private record StreamDelta
    {
        public string? Content { get; init; }
    }
}
