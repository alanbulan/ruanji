using System.Text.Json.Serialization;

namespace WindowsSoftwareOrganizer.Core.Models;

/// <summary>
/// AI Agent 工具定义。
/// </summary>
public record AgentTool
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "function";

    [JsonPropertyName("function")]
    public required AgentFunction Function { get; init; }
}

/// <summary>
/// Agent 函数定义。
/// </summary>
public record AgentFunction
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("parameters")]
    public required AgentFunctionParameters Parameters { get; init; }
}

/// <summary>
/// 函数参数定义。
/// </summary>
public record AgentFunctionParameters
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "object";

    [JsonPropertyName("properties")]
    public required Dictionary<string, AgentParameterProperty> Properties { get; init; }

    [JsonPropertyName("required")]
    public IReadOnlyList<string>? Required { get; init; }
}

/// <summary>
/// 参数属性定义。
/// </summary>
public record AgentParameterProperty
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("enum")]
    public IReadOnlyList<string>? Enum { get; init; }
}

/// <summary>
/// 工具调用请求。
/// </summary>
public record ToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = "function";

    [JsonPropertyName("function")]
    public ToolCallFunction? Function { get; init; }
}

/// <summary>
/// 工具调用函数。
/// </summary>
public record ToolCallFunction
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("arguments")]
    public string Arguments { get; init; } = "{}";
}

/// <summary>
/// 工具执行结果。
/// </summary>
public record ToolResult
{
    public required string ToolCallId { get; init; }
    public required string Name { get; init; }
    public required bool Success { get; init; }
    public required string Result { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Agent 执行上下文。
/// </summary>
public class AgentContext
{
    public string CurrentPath { get; set; } = string.Empty;
    public List<string> ExecutedActions { get; } = new();
    public List<ToolResult> ToolResults { get; } = new();
    public int MaxIterations { get; set; } = 10;
    public int CurrentIteration { get; set; } = 0;
}
