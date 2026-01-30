namespace WindowsSoftwareOrganizer.Core.Models;

/// <summary>
/// AI 分析选项。
/// </summary>
public record AIAnalysisOptions
{
    public int MaxDepth { get; init; } = 3;
    public int MaxFilesPerDirectory { get; init; } = 100;
    public bool IncludeHiddenFiles { get; init; } = false;
    public bool AnonymizeFilenames { get; init; } = true;
    public string? CustomPrompt { get; init; }
}

/// <summary>
/// AI 分析结果。
/// </summary>
public record AIAnalysisResult
{
    public string RootPath { get; init; } = string.Empty;
    public IReadOnlyList<OrganizationSuggestion> Suggestions { get; init; } = Array.Empty<OrganizationSuggestion>();
    public string? Summary { get; init; }
    public int TokensUsed { get; init; }
    public TimeSpan AnalysisDuration { get; init; }
}

/// <summary>
/// 整理建议。
/// </summary>
public class OrganizationSuggestion
{
    public string Id { get; set; } = string.Empty;
    public SuggestionType Type { get; set; }
    public string SourcePath { get; set; } = string.Empty;
    public string? DestinationPath { get; set; }
    public string? NewName { get; set; }
    public string Reason { get; set; } = string.Empty;
    public SuggestionPriority Priority { get; set; }
    public bool IsSelected { get; set; } = true;
}

/// <summary>
/// 建议类型。
/// </summary>
public enum SuggestionType
{
    Move,           // 移动到其他位置
    Rename,         // 重命名
    Delete,         // 删除（冗余/临时文件）
    CreateFolder,   // 创建新文件夹
    Merge,          // 合并相似文件夹
    Archive         // 归档旧文件
}

/// <summary>
/// 建议优先级。
/// </summary>
public enum SuggestionPriority
{
    Low,
    Medium,
    High
}

/// <summary>
/// AI 分析进度。
/// </summary>
public record AIAnalysisProgress
{
    public AIAnalysisPhase Phase { get; init; }
    public int ProgressPercentage { get; init; }
    public string? StatusMessage { get; init; }
}

/// <summary>
/// AI 分析阶段。
/// </summary>
public enum AIAnalysisPhase
{
    CollectingData,     // 收集文件信息
    PreparingRequest,   // 准备 API 请求
    WaitingForResponse, // 等待 AI 响应
    ParsingResponse,    // 解析响应
    Complete            // 完成
}
