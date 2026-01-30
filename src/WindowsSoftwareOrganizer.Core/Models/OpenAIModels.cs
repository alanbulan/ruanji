using System.Text.Json.Serialization;

namespace WindowsSoftwareOrganizer.Core.Models;

/// <summary>
/// OpenAI API 配置。
/// </summary>
public record OpenAIConfiguration
{
    /// <summary>
    /// API 密钥。
    /// </summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>
    /// API 基础 URL（支持自定义端点）。
    /// </summary>
    public string BaseUrl { get; init; } = "https://api.openai.com/v1";

    /// <summary>
    /// 使用的模型名称。
    /// </summary>
    public string Model { get; init; } = "gpt-4o-mini";

    /// <summary>
    /// 最大 Token 数。
    /// </summary>
    public int MaxTokens { get; init; } = 4096;

    /// <summary>
    /// 温度参数（0-2）。
    /// </summary>
    public double Temperature { get; init; } = 0.7;

    /// <summary>
    /// 请求超时时间（秒）。
    /// </summary>
    public int TimeoutSeconds { get; init; } = 60;

    /// <summary>
    /// 是否启用 AI 功能。
    /// </summary>
    public bool IsEnabled { get; init; } = false;

    /// <summary>
    /// 组织 ID（可选）。
    /// </summary>
    public string? OrganizationId { get; init; }

    /// <summary>
    /// 检查配置是否有效。
    /// </summary>
    public bool IsValid => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(BaseUrl);
}

/// <summary>
/// Chat Completion 请求。
/// </summary>
public record ChatCompletionRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("messages")]
    public required IReadOnlyList<ChatMessage> Messages { get; init; }

    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; init; }

    [JsonPropertyName("temperature")]
    public double? Temperature { get; init; }

    [JsonPropertyName("top_p")]
    public double? TopP { get; init; }

    [JsonPropertyName("n")]
    public int? N { get; init; }

    [JsonPropertyName("stream")]
    public bool? Stream { get; init; }

    [JsonPropertyName("stop")]
    public IReadOnlyList<string>? Stop { get; init; }

    [JsonPropertyName("presence_penalty")]
    public double? PresencePenalty { get; init; }

    [JsonPropertyName("frequency_penalty")]
    public double? FrequencyPenalty { get; init; }

    [JsonPropertyName("user")]
    public string? User { get; init; }

    [JsonPropertyName("response_format")]
    public ResponseFormat? ResponseFormat { get; init; }
}

/// <summary>
/// 响应格式。
/// </summary>
public record ResponseFormat
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "text";
}

/// <summary>
/// 聊天消息。
/// </summary>
public record ChatMessage
{
    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("content")]
    public required string Content { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// 创建系统消息。
    /// </summary>
    public static ChatMessage System(string content) => new() { Role = "system", Content = content };

    /// <summary>
    /// 创建用户消息。
    /// </summary>
    public static ChatMessage User(string content) => new() { Role = "user", Content = content };

    /// <summary>
    /// 创建助手消息。
    /// </summary>
    public static ChatMessage Assistant(string content) => new() { Role = "assistant", Content = content };
}

/// <summary>
/// Chat Completion 响应。
/// </summary>
public record ChatCompletionResponse
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("object")]
    public string Object { get; init; } = string.Empty;

    [JsonPropertyName("created")]
    public long Created { get; init; }

    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;

    [JsonPropertyName("choices")]
    public IReadOnlyList<ChatChoice> Choices { get; init; } = Array.Empty<ChatChoice>();

    [JsonPropertyName("usage")]
    public ChatUsage? Usage { get; init; }

    [JsonPropertyName("system_fingerprint")]
    public string? SystemFingerprint { get; init; }

    /// <summary>
    /// 获取第一个选择的内容。
    /// </summary>
    public string? FirstContent => Choices.FirstOrDefault()?.Message?.Content;
}

/// <summary>
/// 聊天选择。
/// </summary>
public record ChatChoice
{
    [JsonPropertyName("index")]
    public int Index { get; init; }

    [JsonPropertyName("message")]
    public ChatMessage? Message { get; init; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; init; }

    [JsonPropertyName("logprobs")]
    public object? Logprobs { get; init; }
}

/// <summary>
/// Token 使用情况。
/// </summary>
public record ChatUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; init; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; init; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; init; }
}

/// <summary>
/// API 错误响应。
/// </summary>
public record APIErrorResponse
{
    [JsonPropertyName("error")]
    public APIError? Error { get; init; }
}

/// <summary>
/// API 错误详情。
/// </summary>
public record APIError
{
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("param")]
    public string? Param { get; init; }

    [JsonPropertyName("code")]
    public string? Code { get; init; }
}

/// <summary>
/// API 连接测试结果。
/// </summary>
public record APITestResult
{
    /// <summary>
    /// 测试是否成功。
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// 响应消息。
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 响应时间（毫秒）。
    /// </summary>
    public long ResponseTimeMs { get; init; }

    /// <summary>
    /// 使用的模型。
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// 错误详情（如果失败）。
    /// </summary>
    public string? ErrorDetails { get; init; }

    /// <summary>
    /// 测试时间。
    /// </summary>
    public DateTime TestedAt { get; init; } = DateTime.Now;
}

/// <summary>
/// 模型列表响应。
/// </summary>
public record ModelsListResponse
{
    [JsonPropertyName("object")]
    public string Object { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public IReadOnlyList<ModelData> Data { get; init; } = Array.Empty<ModelData>();
}

/// <summary>
/// 模型数据。
/// </summary>
public record ModelData
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("object")]
    public string Object { get; init; } = string.Empty;

    [JsonPropertyName("created")]
    public long Created { get; init; }

    [JsonPropertyName("owned_by")]
    public string OwnedBy { get; init; } = string.Empty;
}

/// <summary>
/// 模型信息（用于 UI 显示）。
/// </summary>
public record ModelInfo(
    string Id,
    string DisplayName
)
{
    /// <summary>
    /// 从模型 ID 创建 ModelInfo。
    /// </summary>
    public static ModelInfo FromId(string id) => new(id, id);
}
