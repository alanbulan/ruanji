using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Core.Interfaces;

/// <summary>
/// 文件操作 Agent 接口 - 支持 AI 自动调用文件操作。
/// </summary>
public interface IFileAgent
{
    /// <summary>
    /// 获取可用的工具列表。
    /// </summary>
    IReadOnlyList<AgentTool> GetAvailableTools();

    /// <summary>
    /// 执行工具调用。
    /// </summary>
    /// <param name="toolCall">工具调用请求</param>
    /// <param name="context">执行上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行结果</returns>
    Task<ToolResult> ExecuteToolAsync(ToolCall toolCall, AgentContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// 运行 Agent 循环，自动执行 AI 建议的操作。
    /// </summary>
    /// <param name="userRequest">用户请求</param>
    /// <param name="context">执行上下文</param>
    /// <param name="onMessage">消息回调</param>
    /// <param name="onToolExecution">工具执行回调</param>
    /// <param name="cancellationToken">取消令牌</param>
    IAsyncEnumerable<AgentEvent> RunAsync(
        string userRequest,
        AgentContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Agent 事件类型。
/// </summary>
public enum AgentEventType
{
    Thinking,       // AI 正在思考
    Message,        // AI 消息
    ToolCall,       // 准备调用工具
    ToolResult,     // 工具执行结果
    Complete,       // 完成
    Error           // 错误
}

/// <summary>
/// Agent 事件。
/// </summary>
public record AgentEvent
{
    public required AgentEventType Type { get; init; }
    public string? Message { get; init; }
    public ToolCall? ToolCall { get; init; }
    public ToolResult? ToolResult { get; init; }
    public Exception? Error { get; init; }
}
