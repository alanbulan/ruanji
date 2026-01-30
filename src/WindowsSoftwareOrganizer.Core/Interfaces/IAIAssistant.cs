using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Core.Interfaces;

/// <summary>
/// AI 助手接口 - 统一的 AI 功能入口。
/// </summary>
public interface IAIAssistant
{
    /// <summary>
    /// 检查 AI 是否已配置（同步，可能返回 false 如果配置尚未加载）。
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// 确保 AI 配置已加载并检查是否有效。
    /// </summary>
    /// <returns>配置是否有效</returns>
    Task<bool> EnsureConfiguredAsync();

    /// <summary>
    /// 运行 AI 助手。
    /// </summary>
    /// <param name="context">助手上下文</param>
    /// <param name="userRequest">用户请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>AI 事件流</returns>
    IAsyncEnumerable<AIAssistantEvent> RunAsync(
        AIAssistantContext context,
        string userRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定模块的快捷操作。
    /// </summary>
    /// <param name="module">模块类型</param>
    /// <returns>快捷操作列表</returns>
    IReadOnlyList<QuickAction> GetQuickActions(AIModule module);
}

/// <summary>
/// AI 模块类型。
/// </summary>
public enum AIModule
{
    /// <summary>软件列表</summary>
    SoftwareList,
    /// <summary>文件管理</summary>
    FileManager,
    /// <summary>清理</summary>
    Cleanup,
    /// <summary>迁移</summary>
    Migration
}

/// <summary>
/// AI 助手上下文。
/// </summary>
public class AIAssistantContext
{
    /// <summary>当前模块</summary>
    public AIModule Module { get; set; }
    
    /// <summary>当前路径（文件管理器用）</summary>
    public string? CurrentPath { get; set; }
    
    /// <summary>选中的软件列表</summary>
    public IReadOnlyList<SoftwareEntry>? SelectedSoftware { get; set; }
    
    /// <summary>清理项列表</summary>
    public IReadOnlyList<CleanupItem>? CleanupItems { get; set; }
    
    /// <summary>迁移目标路径</summary>
    public string? MigrationTargetPath { get; set; }
    
    /// <summary>额外数据</summary>
    public Dictionary<string, object> ExtraData { get; } = new();
    
    /// <summary>最大迭代次数</summary>
    public int MaxIterations { get; set; } = 15;
    
    /// <summary>当前迭代</summary>
    public int CurrentIteration { get; set; } = 0;
    
    /// <summary>已执行的操作</summary>
    public List<string> ExecutedActions { get; } = new();
}

/// <summary>
/// AI 助手事件类型。
/// </summary>
public enum AIAssistantEventType
{
    /// <summary>正在思考</summary>
    Thinking,
    /// <summary>消息</summary>
    Message,
    /// <summary>工具调用</summary>
    ToolCall,
    /// <summary>工具结果</summary>
    ToolResult,
    /// <summary>完成</summary>
    Complete,
    /// <summary>错误</summary>
    Error,
    /// <summary>需要确认</summary>
    NeedConfirmation
}

/// <summary>
/// AI 助手事件。
/// </summary>
public record AIAssistantEvent
{
    public required AIAssistantEventType Type { get; init; }
    public string? Message { get; init; }
    public string? ToolName { get; init; }
    public string? ToolResult { get; init; }
    public bool? ToolSuccess { get; init; }
    public Exception? Error { get; init; }
    public ConfirmationRequest? Confirmation { get; init; }
}

/// <summary>
/// 确认请求。
/// </summary>
public record ConfirmationRequest
{
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required string ConfirmText { get; init; }
    public required string CancelText { get; init; }
    public required Action OnConfirm { get; init; }
    public required Action OnCancel { get; init; }
}

/// <summary>
/// 快捷操作。
/// </summary>
public record QuickAction
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Icon { get; init; }
    public required string Prompt { get; init; }
}
