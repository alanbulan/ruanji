using Microsoft.UI.Xaml.Data;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Converters;

/// <summary>
/// 将文件类型分类转换为图标字符。
/// </summary>
public class FileTypeCategoryToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is FileTypeCategory category)
        {
            return GetIconForCategory(category);
        }
        return "\uE8A5"; // 默认文件图标
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

    private static string GetIconForCategory(FileTypeCategory category)
    {
        return category switch
        {
            // 文档类
            FileTypeCategory.WordDocument or
            FileTypeCategory.Spreadsheet or
            FileTypeCategory.Presentation or
            FileTypeCategory.PDF or
            FileTypeCategory.PlainText or
            FileTypeCategory.RichText or
            FileTypeCategory.Markup or
            FileTypeCategory.LaTeX or
            FileTypeCategory.Note => "\uE8A5", // Document
            
            FileTypeCategory.Ebook => "\uE736", // Library
            
            // 图像类
            FileTypeCategory.RasterImage or
            FileTypeCategory.VectorImage or
            FileTypeCategory.RawImage or
            FileTypeCategory.ImageProject or
            FileTypeCategory.HDRImage => "\uEB9F", // Photo
            
            FileTypeCategory.Icon => "\uE91B", // Emoji
            
            // 音频类
            FileTypeCategory.LossyAudio or
            FileTypeCategory.LosslessAudio or
            FileTypeCategory.MIDI or
            FileTypeCategory.AudioProject or
            FileTypeCategory.AudioSample or
            FileTypeCategory.Audiobook or
            FileTypeCategory.Playlist => "\uE8D6", // Audio
            
            // 视频类
            FileTypeCategory.Video or
            FileTypeCategory.ProVideo or
            FileTypeCategory.VideoProject or
            FileTypeCategory.DiscVideo or
            FileTypeCategory.Animation => "\uE714", // Video
            
            FileTypeCategory.Subtitle => "\uE8C1", // Caption
            
            // 压缩类
            FileTypeCategory.Archive or
            FileTypeCategory.PackageArchive or
            FileTypeCategory.SplitArchive => "\uF012", // ZipFolder
            
            FileTypeCategory.DiskImage => "\uEDA2", // HardDrive
            
            // 代码类
            FileTypeCategory.CppSource or
            FileTypeCategory.CSharpSource or
            FileTypeCategory.JavaSource or
            FileTypeCategory.JavaScriptSource or
            FileTypeCategory.PythonSource or
            FileTypeCategory.WebSource or
            FileTypeCategory.PHPSource or
            FileTypeCategory.RubySource or
            FileTypeCategory.GoSource or
            FileTypeCategory.RustSource or
            FileTypeCategory.SwiftSource or
            FileTypeCategory.ShellScript or
            FileTypeCategory.SQLSource or
            FileTypeCategory.AssemblySource or
            FileTypeCategory.LuaSource or
            FileTypeCategory.PerlSource or
            FileTypeCategory.RSource or
            FileTypeCategory.ScalaSource or
            FileTypeCategory.HaskellSource or
            FileTypeCategory.ErlangSource or
            FileTypeCategory.DartSource or
            FileTypeCategory.KotlinSource or
            FileTypeCategory.OtherSource => "\uE943", // Code
            
            // 可执行类
            FileTypeCategory.WindowsExecutable or
            FileTypeCategory.WindowsInstaller or
            FileTypeCategory.MacApplication or
            FileTypeCategory.LinuxExecutable or
            FileTypeCategory.LinuxPackage or
            FileTypeCategory.AndroidPackage or
            FileTypeCategory.iOSPackage => "\uE756", // Setting
            
            FileTypeCategory.DynamicLibrary or
            FileTypeCategory.StaticLibrary or
            FileTypeCategory.Driver => "\uE74C", // Library
            
            // 数据类
            FileTypeCategory.JSON or
            FileTypeCategory.XML or
            FileTypeCategory.YAML or
            FileTypeCategory.TOML or
            FileTypeCategory.ConfigFile or
            FileTypeCategory.RegistryFile => "\uE90F", // Settings
            
            FileTypeCategory.Certificate => "\uE8D7", // Permissions
            
            // 数据库类
            FileTypeCategory.SQLiteDatabase or
            FileTypeCategory.AccessDatabase or
            FileTypeCategory.DatabaseBackup or
            FileTypeCategory.DatabaseData => "\uE8F1", // Database
            
            // 3D/CAD 类
            FileTypeCategory.Model3D or
            FileTypeCategory.CADFile or
            FileTypeCategory.Project3D or
            FileTypeCategory.Material3D or
            FileTypeCategory.PointCloud => "\uF158", // View3D
            
            // 字体类
            FileTypeCategory.Font or
            FileTypeCategory.WebFont or
            FileTypeCategory.BitmapFont => "\uE8D2", // Font
            
            // 游戏类
            FileTypeCategory.GameSave or
            FileTypeCategory.GameROM or
            FileTypeCategory.GameAsset or
            FileTypeCategory.GameMod => "\uE7FC", // Game
            
            // 系统类
            FileTypeCategory.SystemFile or
            FileTypeCategory.Driver => "\uE770", // System
            
            FileTypeCategory.LogFile => "\uE9D9", // History
            
            FileTypeCategory.TempFile or
            FileTypeCategory.CacheFile => "\uE74D", // Delete
            
            FileTypeCategory.Shortcut => "\uE71B", // Link
            
            FileTypeCategory.RecycleBin => "\uE74D", // Delete
            
            // 科学类
            FileTypeCategory.MATLABFile or
            FileTypeCategory.MathematicaFile or
            FileTypeCategory.ScientificData or
            FileTypeCategory.GISData => "\uE9D9", // Calculator
            
            // 虚拟化类
            FileTypeCategory.VirtualDisk or
            FileTypeCategory.VirtualMachineConfig or
            FileTypeCategory.ContainerFile => "\uE7F4", // PC
            
            // 邮件类
            FileTypeCategory.Email => "\uE715", // Mail
            FileTypeCategory.Contact => "\uE77B", // Contact
            FileTypeCategory.Calendar => "\uE787", // Calendar
            
            // 项目类
            FileTypeCategory.VisualStudioProject or
            FileTypeCategory.JetBrainsProject or
            FileTypeCategory.XcodeProject or
            FileTypeCategory.BuildFile => "\uE8B8", // Project
            
            // 版本控制类
            FileTypeCategory.GitFile or
            FileTypeCategory.PatchFile => "\uE8F1", // Branch
            
            // 其他
            FileTypeCategory.Torrent => "\uE896", // Download
            
            _ => "\uE8A5" // 默认文件图标
        };
    }
}
