using Microsoft.UI.Xaml.Controls;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Views.Dialogs;

/// <summary>
/// 文件类型统计对话框。
/// </summary>
public sealed partial class TypeStatisticsDialog : ContentDialog
{
    public TypeStatisticsResult Result { get; }

    public TypeStatisticsDialog(TypeStatisticsResult result)
    {
        Result = result;
        this.InitializeComponent();
    }

    /// <summary>
    /// 获取类别显示名称。
    /// </summary>
    public static string GetCategoryDisplayName(FileTypeCategory category)
    {
        return category switch
        {
            // 文档类
            FileTypeCategory.WordDocument => "文字文档",
            FileTypeCategory.Spreadsheet => "电子表格",
            FileTypeCategory.Presentation => "演示文稿",
            FileTypeCategory.PDF => "PDF 文档",
            FileTypeCategory.PlainText => "纯文本",
            FileTypeCategory.RichText => "富文本",
            FileTypeCategory.Ebook => "电子书",
            FileTypeCategory.Markup => "标记语言",
            FileTypeCategory.LaTeX => "LaTeX",
            FileTypeCategory.Note => "笔记",
            
            // 图像类
            FileTypeCategory.RasterImage => "光栅图像",
            FileTypeCategory.VectorImage => "矢量图像",
            FileTypeCategory.RawImage => "RAW 图像",
            FileTypeCategory.Icon => "图标",
            FileTypeCategory.ImageProject => "图像项目",
            FileTypeCategory.HDRImage => "HDR 图像",
            
            // 音频类
            FileTypeCategory.LossyAudio => "有损音频",
            FileTypeCategory.LosslessAudio => "无损音频",
            FileTypeCategory.MIDI => "MIDI",
            FileTypeCategory.AudioProject => "音频项目",
            FileTypeCategory.AudioSample => "音效采样",
            FileTypeCategory.Audiobook => "有声书",
            FileTypeCategory.Playlist => "播放列表",
            
            // 视频类
            FileTypeCategory.Video => "视频",
            FileTypeCategory.ProVideo => "专业视频",
            FileTypeCategory.VideoProject => "视频项目",
            FileTypeCategory.Subtitle => "字幕",
            FileTypeCategory.DiscVideo => "光盘视频",
            FileTypeCategory.Animation => "动画",
            
            // 压缩类
            FileTypeCategory.Archive => "压缩包",
            FileTypeCategory.PackageArchive => "安装包",
            FileTypeCategory.SplitArchive => "分卷压缩",
            FileTypeCategory.DiskImage => "磁盘镜像",
            
            // 代码类
            FileTypeCategory.CppSource => "C/C++",
            FileTypeCategory.CSharpSource => "C#",
            FileTypeCategory.JavaSource => "Java",
            FileTypeCategory.JavaScriptSource => "JavaScript",
            FileTypeCategory.PythonSource => "Python",
            FileTypeCategory.WebSource => "Web 前端",
            FileTypeCategory.PHPSource => "PHP",
            FileTypeCategory.RubySource => "Ruby",
            FileTypeCategory.GoSource => "Go",
            FileTypeCategory.RustSource => "Rust",
            FileTypeCategory.SwiftSource => "Swift",
            FileTypeCategory.ShellScript => "Shell 脚本",
            FileTypeCategory.SQLSource => "SQL",
            FileTypeCategory.AssemblySource => "汇编",
            FileTypeCategory.LuaSource => "Lua",
            FileTypeCategory.PerlSource => "Perl",
            FileTypeCategory.RSource => "R",
            FileTypeCategory.ScalaSource => "Scala",
            FileTypeCategory.HaskellSource => "Haskell",
            FileTypeCategory.ErlangSource => "Erlang",
            FileTypeCategory.DartSource => "Dart",
            FileTypeCategory.KotlinSource => "Kotlin",
            FileTypeCategory.OtherSource => "其他源码",
            
            // 可执行类
            FileTypeCategory.WindowsExecutable => "Windows 程序",
            FileTypeCategory.WindowsInstaller => "Windows 安装包",
            FileTypeCategory.MacApplication => "macOS 应用",
            FileTypeCategory.LinuxExecutable => "Linux 程序",
            FileTypeCategory.LinuxPackage => "Linux 包",
            FileTypeCategory.AndroidPackage => "Android 应用",
            FileTypeCategory.iOSPackage => "iOS 应用",
            FileTypeCategory.DynamicLibrary => "动态库",
            FileTypeCategory.StaticLibrary => "静态库",
            FileTypeCategory.Driver => "驱动程序",
            
            // 数据类
            FileTypeCategory.JSON => "JSON",
            FileTypeCategory.XML => "XML",
            FileTypeCategory.YAML => "YAML",
            FileTypeCategory.TOML => "TOML",
            FileTypeCategory.ConfigFile => "配置文件",
            FileTypeCategory.RegistryFile => "注册表",
            FileTypeCategory.Certificate => "证书",
            
            // 数据库类
            FileTypeCategory.SQLiteDatabase => "SQLite",
            FileTypeCategory.AccessDatabase => "Access",
            FileTypeCategory.DatabaseBackup => "数据库备份",
            FileTypeCategory.DatabaseData => "数据库数据",
            
            // 3D/CAD 类
            FileTypeCategory.Model3D => "3D 模型",
            FileTypeCategory.CADFile => "CAD 文件",
            FileTypeCategory.Project3D => "3D 项目",
            FileTypeCategory.Material3D => "材质",
            FileTypeCategory.PointCloud => "点云",
            
            // 字体类
            FileTypeCategory.Font => "字体",
            FileTypeCategory.WebFont => "Web 字体",
            FileTypeCategory.BitmapFont => "位图字体",
            
            // 游戏类
            FileTypeCategory.GameSave => "游戏存档",
            FileTypeCategory.GameROM => "游戏 ROM",
            FileTypeCategory.GameAsset => "游戏资源",
            FileTypeCategory.GameMod => "游戏模组",
            
            // 系统类
            FileTypeCategory.SystemFile => "系统文件",
            FileTypeCategory.LogFile => "日志文件",
            FileTypeCategory.TempFile => "临时文件",
            FileTypeCategory.CacheFile => "缓存文件",
            FileTypeCategory.Shortcut => "快捷方式",
            FileTypeCategory.RecycleBin => "回收站",
            
            // 科学类
            FileTypeCategory.MATLABFile => "MATLAB",
            FileTypeCategory.MathematicaFile => "Mathematica",
            FileTypeCategory.ScientificData => "科学数据",
            FileTypeCategory.GISData => "GIS 数据",
            
            // 虚拟化类
            FileTypeCategory.VirtualDisk => "虚拟磁盘",
            FileTypeCategory.VirtualMachineConfig => "虚拟机配置",
            FileTypeCategory.ContainerFile => "容器文件",
            
            // 邮件类
            FileTypeCategory.Email => "邮件",
            FileTypeCategory.Contact => "通讯录",
            FileTypeCategory.Calendar => "日历",
            
            // 项目类
            FileTypeCategory.VisualStudioProject => "Visual Studio",
            FileTypeCategory.JetBrainsProject => "JetBrains",
            FileTypeCategory.XcodeProject => "Xcode",
            FileTypeCategory.BuildFile => "构建文件",
            
            // 版本控制类
            FileTypeCategory.GitFile => "Git 文件",
            FileTypeCategory.PatchFile => "补丁文件",
            
            // 其他
            FileTypeCategory.Torrent => "种子文件",
            FileTypeCategory.Unknown => "未知类型",
            
            _ => category.ToString()
        };
    }
}
