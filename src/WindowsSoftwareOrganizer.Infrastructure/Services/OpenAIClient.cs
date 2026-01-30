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
    private OpenAIConfiguration _configuration;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    public OpenAIClient(OpenAIConfiguration? configuration = null)
    {
        _configuration = configuration ?? new OpenAIConfiguration();
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(_configuration.TimeoutSeconds)
        };
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        UpdateHttpClientHeaders();
    }

    public OpenAIConfiguration Configuration => _configuration;

    public bool IsConfigured => _configuration.IsValid;

    public void UpdateConfiguration(OpenAIConfiguration configuration)
    {
        _configuration = configuration;
        _httpClient.Timeout = TimeSpan.FromSeconds(configuration.TimeoutSeconds);
        UpdateHttpClientHeaders();
    }

    public void Configure(OpenAIConfiguration configuration) => UpdateConfiguration(configuration);

    public async Task<string> SendChatCompletionAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
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
        EnsureConfigured();

        var url = $"{_configuration.BaseUrl.TrimEnd('/')}/chat/completions";
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

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
        EnsureConfigured();

        var request = new ChatCompletionRequest
        {
            Model = _configuration.Model,
            Messages = messages,
            MaxTokens = _configuration.MaxTokens,
            Temperature = _configuration.Temperature,
            Stream = true
        };

        var url = $"{_configuration.BaseUrl.TrimEnd('/')}/chat/completions";
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
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

            // 临时使用提供的配置
            var originalConfig = _configuration;
            try
            {
                UpdateConfiguration(configuration);
                
                var messages = new[] { ChatMessage.User("Hello") };
                var response = await SendChatCompletionRequestAsync(new ChatCompletionRequest
                {
                    Model = configuration.Model,
                    Messages = messages,
                    MaxTokens = 10
                }, cancellationToken);

                stopwatch.Stop();

                return new APITestResult
                {
                    Success = true,
                    Message = "连接成功",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Model = response.Model
                };
            }
            finally
            {
                // 恢复原始配置
                UpdateConfiguration(originalConfig);
            }
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
            // 临时使用提供的配置
            var originalConfig = _configuration;
            try
            {
                UpdateConfiguration(configuration);
                
                var url = $"{configuration.BaseUrl.TrimEnd('/')}/models";
                var response = await _httpClient.GetAsync(url, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    // 如果获取模型列表失败，返回空列表（用户可以手动输入）
                    return Array.Empty<ModelInfo>();
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var modelsResponse = JsonSerializer.Deserialize<ModelsListResponse>(content, _jsonOptions);
                
                if (modelsResponse?.Data == null || modelsResponse.Data.Count == 0)
                {
                    return Array.Empty<ModelInfo>();
                }

                // 过滤出聊天模型（通常包含 chat、gpt、llama、qwen、deepseek 等关键词）
                var chatModels = modelsResponse.Data
                    .Where(m => IsChatModel(m.Id))
                    .Select(m => ModelInfo.FromId(m.Id))
                    .OrderBy(m => m.Id)
                    .ToList();

                return chatModels;
            }
            finally
            {
                // 恢复原始配置
                UpdateConfiguration(originalConfig);
            }
        }
        catch
        {
            // 获取失败时返回空列表
            return Array.Empty<ModelInfo>();
        }
    }

    /// <summary>
    /// 判断是否为聊天模型（过滤掉图像、音频等模型）。
    /// </summary>
    private static bool IsChatModel(string modelId)
    {
        var id = modelId.ToLowerInvariant();
        
        // 排除非聊天模型
        var excludePatterns = new[]
        {
            "embedding", "embed", "whisper", "tts", "dall-e", "stable-diffusion",
            "sdxl", "flux", "image", "audio", "speech", "vision-encoder",
            "rerank", "bge-", "e5-", "gte-"
        };
        
        if (excludePatterns.Any(p => id.Contains(p)))
        {
            return false;
        }

        // 包含聊天模型关键词
        var includePatterns = new[]
        {
            "gpt", "chat", "llama", "qwen", "deepseek", "claude", "gemini",
            "mistral", "yi-", "glm", "baichuan", "internlm", "mixtral",
            "phi-", "command", "solar", "vicuna", "openchat", "nous",
            "codellama", "codeqwen", "starcoder", "codestral"
        };

        return includePatterns.Any(p => id.Contains(p)) || !id.Contains("/");
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

    private void UpdateHttpClientHeaders()
    {
        _httpClient.DefaultRequestHeaders.Clear();
        if (!string.IsNullOrEmpty(_configuration.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_configuration.ApiKey}");
        }
        if (!string.IsNullOrEmpty(_configuration.OrganizationId))
        {
            _httpClient.DefaultRequestHeaders.Add("OpenAI-Organization", _configuration.OrganizationId);
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
