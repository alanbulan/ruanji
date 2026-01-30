using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Core.Interfaces;

/// <summary>
/// 批量文件操作接口 - 执行批量文件操作。
/// </summary>
public interface IBatchFileOperator
{
    /// <summary>
    /// 批量移动文件。
    /// </summary>
    /// <param name="operations">移动操作列表（源路径 -> 目标路径）</param>
    /// <param name="overwrite">是否覆盖已存在的文件</param>
    /// <param name="progress">进度报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<BatchOperationResult> MoveAsync(
        IReadOnlyList<(string Source, string Destination)> operations,
        bool overwrite = false,
        IProgress<BatchOperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量复制文件。
    /// </summary>
    /// <param name="operations">复制操作列表（源路径 -> 目标路径）</param>
    /// <param name="overwrite">是否覆盖已存在的文件</param>
    /// <param name="progress">进度报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<BatchOperationResult> CopyAsync(
        IReadOnlyList<(string Source, string Destination)> operations,
        bool overwrite = false,
        IProgress<BatchOperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量删除文件。
    /// </summary>
    /// <param name="paths">要删除的路径列表</param>
    /// <param name="useRecycleBin">是否移动到回收站</param>
    /// <param name="progress">进度报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<BatchOperationResult> DeleteAsync(
        IReadOnlyList<string> paths,
        bool useRecycleBin = true,
        IProgress<BatchOperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量重命名文件。
    /// </summary>
    /// <param name="operations">重命名操作列表</param>
    /// <param name="progress">进度报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<BatchOperationResult> RenameAsync(
        IReadOnlyList<RenameOperation> operations,
        IProgress<BatchOperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 按规则批量重命名文件。
    /// </summary>
    /// <param name="files">文件列表</param>
    /// <param name="rule">重命名规则</param>
    /// <param name="preview">是否仅预览（不实际执行）</param>
    /// <param name="progress">进度报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果（预览模式下返回预期结果）</returns>
    Task<BatchOperationResult> RenameByRuleAsync(
        IReadOnlyList<FileEntry> files,
        RenameRule rule,
        bool preview = false,
        IProgress<BatchOperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 按类型整理文件到子目录。
    /// </summary>
    /// <param name="sourcePath">源目录</param>
    /// <param name="organizationRules">整理规则（类型 -> 目标子目录名）</param>
    /// <param name="preview">是否仅预览</param>
    /// <param name="progress">进度报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<BatchOperationResult> OrganizeByTypeAsync(
        string sourcePath,
        IReadOnlyDictionary<FileTypeCategory, string>? organizationRules = null,
        bool preview = false,
        IProgress<BatchOperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 应用 AI 建议的整理操作。
    /// </summary>
    /// <param name="suggestions">AI 建议列表</param>
    /// <param name="preview">是否仅预览</param>
    /// <param name="progress">进度报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<BatchOperationResult> ApplySuggestionsAsync(
        IReadOnlyList<OrganizationSuggestion> suggestions,
        bool preview = false,
        IProgress<BatchOperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销上一次批量操作。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功撤销</returns>
    Task<bool> UndoLastOperationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查是否可以撤销。
    /// </summary>
    bool CanUndo { get; }

    /// <summary>
    /// 获取上一次操作的描述。
    /// </summary>
    string? LastOperationDescription { get; }
}

/// <summary>
/// 重命名规则。
/// </summary>
public record RenameRule
{
    /// <summary>
    /// 规则类型。
    /// </summary>
    public required RenameRuleType Type { get; init; }

    /// <summary>
    /// 查找文本（用于替换规则）。
    /// </summary>
    public string? FindText { get; init; }

    /// <summary>
    /// 替换文本。
    /// </summary>
    public string? ReplaceText { get; init; }

    /// <summary>
    /// 前缀。
    /// </summary>
    public string? Prefix { get; init; }

    /// <summary>
    /// 后缀。
    /// </summary>
    public string? Suffix { get; init; }

    /// <summary>
    /// 序号起始值。
    /// </summary>
    public int SequenceStart { get; init; } = 1;

    /// <summary>
    /// 序号步长。
    /// </summary>
    public int SequenceStep { get; init; } = 1;

    /// <summary>
    /// 序号位数。
    /// </summary>
    public int SequenceDigits { get; init; } = 3;

    /// <summary>
    /// 日期格式。
    /// </summary>
    public string? DateFormat { get; init; }

    /// <summary>
    /// 是否使用正则表达式。
    /// </summary>
    public bool UseRegex { get; init; }

    /// <summary>
    /// 是否区分大小写。
    /// </summary>
    public bool CaseSensitive { get; init; }

    /// <summary>
    /// 大小写转换类型。
    /// </summary>
    public CaseConversion? CaseConversion { get; init; }

    /// <summary>
    /// 是否包含扩展名。
    /// </summary>
    public bool IncludeExtension { get; init; }
}

/// <summary>
/// 重命名规则类型。
/// </summary>
public enum RenameRuleType
{
    /// <summary>查找替换</summary>
    FindReplace,
    /// <summary>添加前缀</summary>
    AddPrefix,
    /// <summary>添加后缀</summary>
    AddSuffix,
    /// <summary>添加序号</summary>
    AddSequence,
    /// <summary>添加日期</summary>
    AddDate,
    /// <summary>大小写转换</summary>
    ChangeCase,
    /// <summary>移除字符</summary>
    RemoveCharacters,
    /// <summary>正则替换</summary>
    RegexReplace,
    /// <summary>自定义模板</summary>
    CustomTemplate
}

/// <summary>
/// 大小写转换类型。
/// </summary>
public enum CaseConversion
{
    /// <summary>全部大写</summary>
    UpperCase,
    /// <summary>全部小写</summary>
    LowerCase,
    /// <summary>首字母大写</summary>
    TitleCase,
    /// <summary>句首大写</summary>
    SentenceCase,
    /// <summary>反转大小写</summary>
    ToggleCase
}
