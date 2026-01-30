namespace WindowsSoftwareOrganizer.Core.Models;

/// <summary>
/// Represents the application configuration.
/// </summary>
public record AppConfiguration
{
    /// <summary>
    /// Gets the default target path for migrations.
    /// </summary>
    public string DefaultTargetPath { get; init; } = @"D:\Software";

    /// <summary>
    /// Gets the default naming template ID.
    /// </summary>
    public string DefaultNamingTemplateId { get; init; } = "detailed";

    /// <summary>
    /// Gets the preferred link type for migrations.
    /// </summary>
    public LinkType PreferredLinkType { get; init; } = LinkType.Junction;

    /// <summary>
    /// Gets whether to automatically update registry references.
    /// </summary>
    public bool AutoUpdateRegistry { get; init; } = true;

    /// <summary>
    /// Gets whether to move deleted items to recycle bin.
    /// </summary>
    public bool MoveToRecycleBin { get; init; } = true;

    /// <summary>
    /// Gets the number of days to keep operation history.
    /// </summary>
    public int OperationHistoryDays { get; init; } = 30;

    /// <summary>
    /// Gets the theme mode.
    /// </summary>
    public ThemeMode Theme { get; init; } = ThemeMode.System;

    /// <summary>
    /// Gets the user-defined software classifications.
    /// </summary>
    public Dictionary<string, SoftwareCategory> UserClassifications { get; init; } = new();

    /// <summary>
    /// Gets the user-defined custom naming templates.
    /// </summary>
    public List<NamingTemplate> CustomTemplates { get; init; } = new();

    /// <summary>
    /// Gets the OpenAI API configuration.
    /// </summary>
    public OpenAIConfiguration OpenAIConfiguration { get; init; } = new();

    /// <summary>
    /// Gets the file manager settings.
    /// </summary>
    public FileManagerSettings FileManagerSettings { get; init; } = new();
}

/// <summary>
/// Theme modes for the application.
/// </summary>
public enum ThemeMode
{
    /// <summary>Light theme</summary>
    Light,
    /// <summary>Dark theme</summary>
    Dark,
    /// <summary>Follow system theme</summary>
    System
}

/// <summary>
/// 文件管理器设置。
/// </summary>
public record FileManagerSettings
{
    /// <summary>
    /// 是否显示隐藏文件。
    /// </summary>
    public bool ShowHiddenFiles { get; init; } = false;

    /// <summary>
    /// 是否显示系统文件。
    /// </summary>
    public bool ShowSystemFiles { get; init; } = false;

    /// <summary>
    /// 默认视图模式。
    /// </summary>
    public FileViewMode DefaultViewMode { get; init; } = FileViewMode.Details;

    /// <summary>
    /// 默认排序方式。
    /// </summary>
    public FileSortBy DefaultSortBy { get; init; } = FileSortBy.Name;

    /// <summary>
    /// 默认排序方向。
    /// </summary>
    public SortDirection DefaultSortDirection { get; init; } = SortDirection.Ascending;

    /// <summary>
    /// 启动时的默认路径。
    /// </summary>
    public string? DefaultStartPath { get; init; }

    /// <summary>
    /// 是否记住上次浏览位置。
    /// </summary>
    public bool RememberLastPath { get; init; } = true;

    /// <summary>
    /// 上次浏览的路径。
    /// </summary>
    public string? LastBrowsedPath { get; init; }

    /// <summary>
    /// 收藏的路径列表。
    /// </summary>
    public List<string> FavoritePaths { get; init; } = new();

    /// <summary>
    /// 是否启用预览面板。
    /// </summary>
    public bool EnablePreviewPane { get; init; } = false;

    /// <summary>
    /// 是否启用缩略图。
    /// </summary>
    public bool EnableThumbnails { get; init; } = true;
}
