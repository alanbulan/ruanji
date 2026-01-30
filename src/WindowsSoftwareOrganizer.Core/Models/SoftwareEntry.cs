namespace WindowsSoftwareOrganizer.Core.Models;

/// <summary>
/// Represents a software entry discovered on the system.
/// </summary>
public record SoftwareEntry
{
    /// <summary>
    /// Gets the unique identifier for this software entry.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the display name of the software.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the version of the software.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Gets the vendor/publisher of the software.
    /// </summary>
    public string? Vendor { get; init; }

    /// <summary>
    /// Gets the installation path of the software.
    /// </summary>
    public required string InstallPath { get; init; }

    /// <summary>
    /// Gets the installation date of the software.
    /// </summary>
    public DateTime? InstallDate { get; init; }

    /// <summary>
    /// Gets the category of the software.
    /// </summary>
    public SoftwareCategory Category { get; init; } = SoftwareCategory.Other;

    /// <summary>
    /// Gets the related directories for this software.
    /// </summary>
    public IReadOnlyList<RelatedDirectory> RelatedDirectories { get; init; }
        = Array.Empty<RelatedDirectory>();

    /// <summary>
    /// Gets the total size in bytes of all related directories.
    /// </summary>
    public long TotalSizeBytes { get; init; }

    /// <summary>
    /// Gets the path to the software's executable file (for icon extraction).
    /// </summary>
    public string? ExecutablePath { get; init; }

    /// <summary>
    /// Gets the path to the software's icon file (if available).
    /// </summary>
    public string? IconPath { get; init; }
}

/// <summary>
/// Represents the category of software.
/// </summary>
public enum SoftwareCategory
{
    /// <summary>游戏</summary>
    Game,
    /// <summary>社交通讯</summary>
    Social,
    /// <summary>即时通讯</summary>
    Messaging,
    /// <summary>办公软件</summary>
    Office,
    /// <summary>浏览器</summary>
    Browser,
    /// <summary>音乐播放</summary>
    Music,
    /// <summary>视频播放</summary>
    Video,
    /// <summary>影音娱乐</summary>
    Media,
    /// <summary>图形设计</summary>
    Graphics,
    /// <summary>图片处理</summary>
    Photo,
    /// <summary>3D建模</summary>
    Modeling3D,
    /// <summary>系统工具</summary>
    System,
    /// <summary>安全软件</summary>
    Security,
    /// <summary>杀毒软件</summary>
    Antivirus,
    /// <summary>下载工具</summary>
    Download,
    /// <summary>网络工具</summary>
    Network,
    /// <summary>VPN工具</summary>
    VPN,
    /// <summary>教育学习</summary>
    Education,
    /// <summary>驱动程序</summary>
    Driver,
    /// <summary>运行库</summary>
    Runtime,
    /// <summary>集成开发环境</summary>
    IDE,
    /// <summary>代码编辑器</summary>
    CodeEditor,
    /// <summary>软件开发工具包</summary>
    SDK,
    /// <summary>开发辅助工具</summary>
    DevTool,
    /// <summary>版本控制</summary>
    VersionControl,
    /// <summary>数据库</summary>
    Database,
    /// <summary>虚拟化</summary>
    Virtualization,
    /// <summary>实用工具</summary>
    Utility,
    /// <summary>压缩解压</summary>
    Compression,
    /// <summary>文件管理</summary>
    FileManager,
    /// <summary>备份恢复</summary>
    Backup,
    /// <summary>远程控制</summary>
    RemoteDesktop,
    /// <summary>截图录屏</summary>
    Screenshot,
    /// <summary>笔记软件</summary>
    Notes,
    /// <summary>阅读器</summary>
    Reader,
    /// <summary>电子书</summary>
    Ebook,
    /// <summary>翻译工具</summary>
    Translation,
    /// <summary>输入法</summary>
    InputMethod,
    /// <summary>云存储</summary>
    CloudStorage,
    /// <summary>邮件客户端</summary>
    Email,
    /// <summary>财务软件</summary>
    Finance,
    /// <summary>健康健身</summary>
    Health,
    /// <summary>天气</summary>
    Weather,
    /// <summary>地图导航</summary>
    Maps,
    /// <summary>购物</summary>
    Shopping,
    /// <summary>新闻资讯</summary>
    News,
    /// <summary>直播平台</summary>
    Streaming,
    /// <summary>AI工具</summary>
    AI,
    /// <summary>其他</summary>
    Other
}

/// <summary>
/// Represents a directory related to a software installation.
/// </summary>
public record RelatedDirectory
{
    /// <summary>
    /// Gets the path of the directory.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the type of the directory.
    /// </summary>
    public required DirectoryType Type { get; init; }

    /// <summary>
    /// Gets the size of the directory in bytes.
    /// </summary>
    public long SizeBytes { get; init; }
}

/// <summary>
/// Represents the type of a related directory.
/// </summary>
public enum DirectoryType
{
    /// <summary>Main installation directory</summary>
    Install,
    /// <summary>Configuration directory</summary>
    Config,
    /// <summary>Cache directory</summary>
    Cache,
    /// <summary>Log directory</summary>
    Log,
    /// <summary>Data directory</summary>
    Data,
    /// <summary>Temporary files directory</summary>
    Temp
}
