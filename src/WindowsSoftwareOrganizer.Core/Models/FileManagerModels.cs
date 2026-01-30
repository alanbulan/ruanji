namespace WindowsSoftwareOrganizer.Core.Models;

/// <summary>
/// 文件类型分类 - 涵盖所有主要文件类型。
/// </summary>
public enum FileTypeCategory
{
    // ===== 文档类 =====
    /// <summary>文字处理文档：doc, docx, odt, rtf, wps, wpd, pages</summary>
    WordDocument,
    /// <summary>电子表格：xls, xlsx, ods, csv, numbers, wk1, wks</summary>
    Spreadsheet,
    /// <summary>演示文稿：ppt, pptx, odp, key, pps, ppsx</summary>
    Presentation,
    /// <summary>PDF 文档：pdf</summary>
    PDF,
    /// <summary>纯文本：txt, text, log, nfo, diz</summary>
    PlainText,
    /// <summary>富文本：rtf, rtfd</summary>
    RichText,
    /// <summary>电子书：epub, mobi, azw, azw3, fb2, lit, pdb, prc, cbr, cbz</summary>
    Ebook,
    /// <summary>Markdown/标记语言：md, markdown, rst, asciidoc, adoc, textile, wiki</summary>
    Markup,
    /// <summary>LaTeX/学术文档：tex, latex, bib, cls, sty</summary>
    LaTeX,
    /// <summary>笔记/大纲：one, onenote, enex, opml</summary>
    Note,

    // ===== 图像类 =====
    /// <summary>光栅图像：jpg, jpeg, png, gif, bmp, tiff, tif, webp, heic, heif, avif</summary>
    RasterImage,
    /// <summary>矢量图像：svg, svgz, eps, ai, cdr, wmf, emf</summary>
    VectorImage,
    /// <summary>RAW 相机格式：raw, cr2, cr3, nef, arw, orf, rw2, dng, raf, pef, srw, x3f</summary>
    RawImage,
    /// <summary>图标：ico, icns, cur, ani</summary>
    Icon,
    /// <summary>Photoshop/图像编辑：psd, psb, xcf, kra, clip, sai, procreate</summary>
    ImageProject,
    /// <summary>HDR 图像：hdr, exr, pfm, rgbe</summary>
    HDRImage,

    // ===== 音频类 =====
    /// <summary>有损音频：mp3, aac, m4a, wma, ogg, opus, mpc, ra, rm</summary>
    LossyAudio,
    /// <summary>无损音频：flac, alac, ape, wav, aiff, aif, wv, tta, tak</summary>
    LosslessAudio,
    /// <summary>MIDI/音序：mid, midi, kar, rmi</summary>
    MIDI,
    /// <summary>音频项目：als, flp, logic, ptx, cpr, rpp, aup, aup3, sesx</summary>
    AudioProject,
    /// <summary>音效/采样：sf2, sfz, nki, exs, gig, akp</summary>
    AudioSample,
    /// <summary>播客/有声书：m4b, aa, aax</summary>
    Audiobook,
    /// <summary>播放列表：m3u, m3u8, pls, wpl, xspf, asx</summary>
    Playlist,

    // ===== 视频类 =====
    /// <summary>通用视频：mp4, m4v, mkv, avi, mov, wmv, flv, webm, ogv, 3gp, 3g2</summary>
    Video,
    /// <summary>高清/专业视频：mxf, r3d, braw, prores, dnxhd</summary>
    ProVideo,
    /// <summary>视频项目：prproj, aep, veg, kdenlive, fcpx, imovieproj, drp</summary>
    VideoProject,
    /// <summary>字幕：srt, sub, ass, ssa, vtt, idx, sup, smi</summary>
    Subtitle,
    /// <summary>DVD/蓝光：vob, ifo, bup, m2ts, mts, bdmv</summary>
    DiscVideo,
    /// <summary>动画/GIF：gif, apng, webp (animated), lottie, json (lottie)</summary>
    Animation,

    // ===== 压缩/归档类 =====
    /// <summary>通用压缩：zip, rar, 7z, tar, gz, bz2, xz, lz, lzma, lz4, zst</summary>
    Archive,
    /// <summary>安装包压缩：cab, msi, deb, rpm, pkg, dmg, appx, msix</summary>
    PackageArchive,
    /// <summary>分卷压缩：001, r00, z01, part1</summary>
    SplitArchive,
    /// <summary>磁盘镜像：iso, img, bin, cue, nrg, mdf, mds, ccd, vhd, vhdx, vmdk, qcow2</summary>
    DiskImage,

    // ===== 代码/开发类 =====
    /// <summary>C/C++：c, cpp, cc, cxx, h, hpp, hxx, hh</summary>
    CppSource,
    /// <summary>C#：cs, csx</summary>
    CSharpSource,
    /// <summary>Java：java, jar, class, kt, kts, groovy, gradle</summary>
    JavaSource,
    /// <summary>JavaScript/TypeScript：js, jsx, ts, tsx, mjs, cjs</summary>
    JavaScriptSource,
    /// <summary>Python：py, pyw, pyx, pxd, pyi, pyc, pyo</summary>
    PythonSource,
    /// <summary>Web 前端：html, htm, xhtml, css, scss, sass, less, vue, svelte</summary>
    WebSource,
    /// <summary>PHP：php, phtml, php3, php4, php5, phps</summary>
    PHPSource,
    /// <summary>Ruby：rb, rbw, rake, gemspec, erb</summary>
    RubySource,
    /// <summary>Go：go, mod, sum</summary>
    GoSource,
    /// <summary>Rust：rs, rlib</summary>
    RustSource,
    /// <summary>Swift/Objective-C：swift, m, mm</summary>
    SwiftSource,
    /// <summary>Shell 脚本：sh, bash, zsh, fish, csh, ksh, ps1, psm1, psd1, bat, cmd</summary>
    ShellScript,
    /// <summary>SQL：sql, ddl, dml, pgsql, plsql, tsql</summary>
    SQLSource,
    /// <summary>汇编：asm, s, S, inc</summary>
    AssemblySource,
    /// <summary>Lua：lua, luac</summary>
    LuaSource,
    /// <summary>Perl：pl, pm, pod, t</summary>
    PerlSource,
    /// <summary>R：r, R, rmd, Rmd</summary>
    RSource,
    /// <summary>Scala：scala, sc, sbt</summary>
    ScalaSource,
    /// <summary>Haskell：hs, lhs, cabal</summary>
    HaskellSource,
    /// <summary>Erlang/Elixir：erl, hrl, ex, exs</summary>
    ErlangSource,
    /// <summary>Dart/Flutter：dart</summary>
    DartSource,
    /// <summary>Kotlin：kt, kts</summary>
    KotlinSource,
    /// <summary>其他源代码：f, f90, f95, for, cob, cbl, pas, pp, d, nim, zig, v, vhdl, vhd</summary>
    OtherSource,

    // ===== 可执行/二进制类 =====
    /// <summary>Windows 可执行：exe, com, scr</summary>
    WindowsExecutable,
    /// <summary>Windows 安装包：msi, msix, appx, appxbundle</summary>
    WindowsInstaller,
    /// <summary>macOS 应用：app, dmg, pkg</summary>
    MacApplication,
    /// <summary>Linux 可执行：AppImage, run, bin, elf</summary>
    LinuxExecutable,
    /// <summary>Linux 包：deb, rpm, snap, flatpak</summary>
    LinuxPackage,
    /// <summary>Android：apk, aab, xapk</summary>
    AndroidPackage,
    /// <summary>iOS：ipa</summary>
    iOSPackage,
    /// <summary>动态库：dll, so, dylib</summary>
    DynamicLibrary,
    /// <summary>静态库：lib, a</summary>
    StaticLibrary,
    /// <summary>驱动程序：sys, ko, kext</summary>
    Driver,

    // ===== 数据/配置类 =====
    /// <summary>JSON：json, jsonc, json5, geojson</summary>
    JSON,
    /// <summary>XML：xml, xsl, xslt, xsd, dtd, rss, atom, svg, xaml, xib, storyboard</summary>
    XML,
    /// <summary>YAML：yaml, yml</summary>
    YAML,
    /// <summary>TOML：toml</summary>
    TOML,
    /// <summary>INI/配置：ini, cfg, conf, config, properties, env</summary>
    ConfigFile,
    /// <summary>注册表：reg</summary>
    RegistryFile,
    /// <summary>证书/密钥：pem, crt, cer, der, p12, pfx, key, pub, ppk</summary>
    Certificate,

    // ===== 数据库类 =====
    /// <summary>SQLite：db, sqlite, sqlite3, db3</summary>
    SQLiteDatabase,
    /// <summary>Access：mdb, accdb</summary>
    AccessDatabase,
    /// <summary>数据库备份：bak, sql, dump</summary>
    DatabaseBackup,
    /// <summary>数据库数据：mdf, ldf, ndf, frm, ibd, myd, myi</summary>
    DatabaseData,

    // ===== 3D/CAD 类 =====
    /// <summary>3D 模型：obj, fbx, gltf, glb, dae, 3ds, stl, ply, blend</summary>
    Model3D,
    /// <summary>CAD 文件：dwg, dxf, dgn, step, stp, iges, igs, sat, sldprt, sldasm</summary>
    CADFile,
    /// <summary>3D 项目：blend, max, ma, mb, c4d, skp, hip, hiplc</summary>
    Project3D,
    /// <summary>纹理/材质：mtl, mat, sbsar</summary>
    Material3D,
    /// <summary>点云：las, laz, xyz, pts, e57</summary>
    PointCloud,

    // ===== 字体类 =====
    /// <summary>TrueType/OpenType：ttf, otf, ttc, otc</summary>
    Font,
    /// <summary>Web 字体：woff, woff2, eot</summary>
    WebFont,
    /// <summary>位图字体：fon, fnt, bdf, pcf</summary>
    BitmapFont,

    // ===== 游戏类 =====
    /// <summary>游戏存档：sav, save, gamesave</summary>
    GameSave,
    /// <summary>游戏 ROM：nes, snes, sfc, gba, gbc, gb, nds, 3ds, n64, z64, iso, cso, pbp</summary>
    GameROM,
    /// <summary>游戏资源：pak, vpk, wad, bsp, pk3, pk4, gcf, vpk</summary>
    GameAsset,
    /// <summary>游戏模组：esp, esm, esl, ba2, bsa</summary>
    GameMod,

    // ===== 系统类 =====
    /// <summary>系统文件：sys, drv, cpl, ocx</summary>
    SystemFile,
    /// <summary>日志文件：log, logs, trace, etl</summary>
    LogFile,
    /// <summary>临时文件：tmp, temp, swp, swo, bak, old, orig</summary>
    TempFile,
    /// <summary>缓存文件：cache</summary>
    CacheFile,
    /// <summary>快捷方式：lnk, url, webloc, desktop</summary>
    Shortcut,
    /// <summary>回收站文件</summary>
    RecycleBin,

    // ===== 科学/工程类 =====
    /// <summary>MATLAB：m, mat, fig, mlx, mlapp</summary>
    MATLABFile,
    /// <summary>Mathematica：nb, cdf, m</summary>
    MathematicaFile,
    /// <summary>科学数据：hdf, hdf5, h5, nc, netcdf, fits, fit</summary>
    ScientificData,
    /// <summary>GIS 数据：shp, shx, dbf, prj, kml, kmz, gpx, osm</summary>
    GISData,

    // ===== 虚拟化类 =====
    /// <summary>虚拟机磁盘：vmdk, vdi, vhd, vhdx, qcow, qcow2</summary>
    VirtualDisk,
    /// <summary>虚拟机配置：vmx, vbox, ovf, ova</summary>
    VirtualMachineConfig,
    /// <summary>容器：dockerfile, docker-compose.yml</summary>
    ContainerFile,

    // ===== 邮件/通讯类 =====
    /// <summary>邮件：eml, msg, emlx, mbox, pst, ost</summary>
    Email,
    /// <summary>通讯录：vcf, vcard, ldif</summary>
    Contact,
    /// <summary>日历：ics, ical, vcs</summary>
    Calendar,

    // ===== 项目/工程文件类 =====
    /// <summary>Visual Studio：sln, csproj, vbproj, fsproj, vcxproj, props, targets</summary>
    VisualStudioProject,
    /// <summary>JetBrains IDE：iml, ipr, iws</summary>
    JetBrainsProject,
    /// <summary>Xcode：xcodeproj, xcworkspace, pbxproj</summary>
    XcodeProject,
    /// <summary>构建文件：makefile, cmake, meson.build, build.gradle, pom.xml, package.json</summary>
    BuildFile,

    // ===== 版本控制类 =====
    /// <summary>Git：gitignore, gitattributes, gitmodules</summary>
    GitFile,
    /// <summary>补丁/差异：patch, diff</summary>
    PatchFile,

    // ===== 其他 =====
    /// <summary>种子文件：torrent</summary>
    Torrent,
    /// <summary>未知/其他类型</summary>
    Unknown
}


/// <summary>
/// 文件类型分类辅助类 - 提供扩展名到分类的映射。
/// </summary>
public static class FileTypeCategoryHelper
{
    private static readonly Dictionary<string, FileTypeCategory> ExtensionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // ===== 文字处理文档 =====
        { ".doc", FileTypeCategory.WordDocument },
        { ".docx", FileTypeCategory.WordDocument },
        { ".docm", FileTypeCategory.WordDocument },
        { ".dot", FileTypeCategory.WordDocument },
        { ".dotx", FileTypeCategory.WordDocument },
        { ".dotm", FileTypeCategory.WordDocument },
        { ".odt", FileTypeCategory.WordDocument },
        { ".ott", FileTypeCategory.WordDocument },
        { ".rtf", FileTypeCategory.RichText },
        { ".rtfd", FileTypeCategory.RichText },
        { ".wps", FileTypeCategory.WordDocument },
        { ".wpd", FileTypeCategory.WordDocument },
        { ".pages", FileTypeCategory.WordDocument },
        { ".hwp", FileTypeCategory.WordDocument },
        { ".hwpx", FileTypeCategory.WordDocument },

        // ===== 电子表格 =====
        { ".xls", FileTypeCategory.Spreadsheet },
        { ".xlsx", FileTypeCategory.Spreadsheet },
        { ".xlsm", FileTypeCategory.Spreadsheet },
        { ".xlsb", FileTypeCategory.Spreadsheet },
        { ".xlt", FileTypeCategory.Spreadsheet },
        { ".xltx", FileTypeCategory.Spreadsheet },
        { ".xltm", FileTypeCategory.Spreadsheet },
        { ".ods", FileTypeCategory.Spreadsheet },
        { ".ots", FileTypeCategory.Spreadsheet },
        { ".csv", FileTypeCategory.Spreadsheet },
        { ".tsv", FileTypeCategory.Spreadsheet },
        { ".numbers", FileTypeCategory.Spreadsheet },
        { ".wk1", FileTypeCategory.Spreadsheet },
        { ".wks", FileTypeCategory.Spreadsheet },
        { ".et", FileTypeCategory.Spreadsheet },

        // ===== 演示文稿 =====
        { ".ppt", FileTypeCategory.Presentation },
        { ".pptx", FileTypeCategory.Presentation },
        { ".pptm", FileTypeCategory.Presentation },
        { ".pot", FileTypeCategory.Presentation },
        { ".potx", FileTypeCategory.Presentation },
        { ".potm", FileTypeCategory.Presentation },
        { ".pps", FileTypeCategory.Presentation },
        { ".ppsx", FileTypeCategory.Presentation },
        { ".ppsm", FileTypeCategory.Presentation },
        { ".odp", FileTypeCategory.Presentation },
        { ".otp", FileTypeCategory.Presentation },
        { ".key", FileTypeCategory.Presentation },
        { ".dps", FileTypeCategory.Presentation },

        // ===== PDF =====
        { ".pdf", FileTypeCategory.PDF },
        { ".fdf", FileTypeCategory.PDF },
        { ".xfdf", FileTypeCategory.PDF },

        // ===== 纯文本 =====
        { ".txt", FileTypeCategory.PlainText },
        { ".text", FileTypeCategory.PlainText },
        { ".log", FileTypeCategory.LogFile },
        { ".logs", FileTypeCategory.LogFile },
        { ".nfo", FileTypeCategory.PlainText },
        { ".diz", FileTypeCategory.PlainText },
        { ".ans", FileTypeCategory.PlainText },

        // ===== 电子书 =====
        { ".epub", FileTypeCategory.Ebook },
        { ".mobi", FileTypeCategory.Ebook },
        { ".azw", FileTypeCategory.Ebook },
        { ".azw3", FileTypeCategory.Ebook },
        { ".azw4", FileTypeCategory.Ebook },
        { ".kfx", FileTypeCategory.Ebook },
        { ".fb2", FileTypeCategory.Ebook },
        { ".lit", FileTypeCategory.Ebook },
        { ".pdb", FileTypeCategory.Ebook },
        { ".prc", FileTypeCategory.Ebook },
        { ".cbr", FileTypeCategory.Ebook },
        { ".cbz", FileTypeCategory.Ebook },
        { ".cb7", FileTypeCategory.Ebook },
        { ".cbt", FileTypeCategory.Ebook },
        { ".djvu", FileTypeCategory.Ebook },
        { ".djv", FileTypeCategory.Ebook },
        { ".chm", FileTypeCategory.Ebook },

        // ===== Markdown/标记语言 =====
        { ".md", FileTypeCategory.Markup },
        { ".markdown", FileTypeCategory.Markup },
        { ".mdown", FileTypeCategory.Markup },
        { ".mkd", FileTypeCategory.Markup },
        { ".mkdn", FileTypeCategory.Markup },
        { ".rst", FileTypeCategory.Markup },
        { ".asciidoc", FileTypeCategory.Markup },
        { ".adoc", FileTypeCategory.Markup },
        { ".textile", FileTypeCategory.Markup },
        { ".wiki", FileTypeCategory.Markup },
        { ".mediawiki", FileTypeCategory.Markup },
        { ".creole", FileTypeCategory.Markup },
        { ".org", FileTypeCategory.Markup },

        // ===== LaTeX =====
        { ".tex", FileTypeCategory.LaTeX },
        { ".latex", FileTypeCategory.LaTeX },
        { ".ltx", FileTypeCategory.LaTeX },
        { ".bib", FileTypeCategory.LaTeX },
        { ".bst", FileTypeCategory.LaTeX },
        { ".cls", FileTypeCategory.LaTeX },
        { ".sty", FileTypeCategory.LaTeX },
        { ".dtx", FileTypeCategory.LaTeX },
        { ".ins", FileTypeCategory.LaTeX },

        // ===== 笔记 =====
        { ".one", FileTypeCategory.Note },
        { ".onetoc2", FileTypeCategory.Note },
        { ".enex", FileTypeCategory.Note },
        { ".opml", FileTypeCategory.Note },

        // ===== 光栅图像 =====
        { ".jpg", FileTypeCategory.RasterImage },
        { ".jpeg", FileTypeCategory.RasterImage },
        { ".jpe", FileTypeCategory.RasterImage },
        { ".jfif", FileTypeCategory.RasterImage },
        { ".png", FileTypeCategory.RasterImage },
        { ".gif", FileTypeCategory.RasterImage },
        { ".bmp", FileTypeCategory.RasterImage },
        { ".dib", FileTypeCategory.RasterImage },
        { ".tiff", FileTypeCategory.RasterImage },
        { ".tif", FileTypeCategory.RasterImage },
        { ".webp", FileTypeCategory.RasterImage },
        { ".heic", FileTypeCategory.RasterImage },
        { ".heif", FileTypeCategory.RasterImage },
        { ".avif", FileTypeCategory.RasterImage },
        { ".jxl", FileTypeCategory.RasterImage },
        { ".jp2", FileTypeCategory.RasterImage },
        { ".j2k", FileTypeCategory.RasterImage },
        { ".jpf", FileTypeCategory.RasterImage },
        { ".jpx", FileTypeCategory.RasterImage },
        { ".jpm", FileTypeCategory.RasterImage },
        { ".pcx", FileTypeCategory.RasterImage },
        { ".tga", FileTypeCategory.RasterImage },
        { ".ppm", FileTypeCategory.RasterImage },
        { ".pgm", FileTypeCategory.RasterImage },
        { ".pbm", FileTypeCategory.RasterImage },
        { ".pnm", FileTypeCategory.RasterImage },
        { ".wbmp", FileTypeCategory.RasterImage },

        // ===== 矢量图像 =====
        { ".svg", FileTypeCategory.VectorImage },
        { ".svgz", FileTypeCategory.VectorImage },
        { ".eps", FileTypeCategory.VectorImage },
        { ".ai", FileTypeCategory.VectorImage },
        { ".cdr", FileTypeCategory.VectorImage },
        { ".cmx", FileTypeCategory.VectorImage },
        { ".wmf", FileTypeCategory.VectorImage },
        { ".emf", FileTypeCategory.VectorImage },
        { ".emz", FileTypeCategory.VectorImage },
        { ".wmz", FileTypeCategory.VectorImage },
        { ".cgm", FileTypeCategory.VectorImage },
        { ".sk", FileTypeCategory.VectorImage },
        { ".sk1", FileTypeCategory.VectorImage },
        { ".odg", FileTypeCategory.VectorImage },
        { ".vsd", FileTypeCategory.VectorImage },
        { ".vsdx", FileTypeCategory.VectorImage },
        { ".vdx", FileTypeCategory.VectorImage },

        // ===== RAW 相机格式 =====
        { ".raw", FileTypeCategory.RawImage },
        { ".cr2", FileTypeCategory.RawImage },
        { ".cr3", FileTypeCategory.RawImage },
        { ".crw", FileTypeCategory.RawImage },
        { ".nef", FileTypeCategory.RawImage },
        { ".nrw", FileTypeCategory.RawImage },
        { ".arw", FileTypeCategory.RawImage },
        { ".srf", FileTypeCategory.RawImage },
        { ".sr2", FileTypeCategory.RawImage },
        { ".orf", FileTypeCategory.RawImage },
        { ".rw2", FileTypeCategory.RawImage },
        { ".dng", FileTypeCategory.RawImage },
        { ".raf", FileTypeCategory.RawImage },
        { ".pef", FileTypeCategory.RawImage },
        { ".ptx", FileTypeCategory.RawImage },
        { ".srw", FileTypeCategory.RawImage },
        { ".x3f", FileTypeCategory.RawImage },
        { ".dcr", FileTypeCategory.RawImage },
        { ".kdc", FileTypeCategory.RawImage },
        { ".mrw", FileTypeCategory.RawImage },
        { ".erf", FileTypeCategory.RawImage },
        { ".mef", FileTypeCategory.RawImage },
        { ".mos", FileTypeCategory.RawImage },
        { ".3fr", FileTypeCategory.RawImage },
        { ".fff", FileTypeCategory.RawImage },
        { ".rwl", FileTypeCategory.RawImage },
        { ".iiq", FileTypeCategory.RawImage },
        { ".gpr", FileTypeCategory.RawImage },

        // ===== 图标 =====
        { ".ico", FileTypeCategory.Icon },
        { ".icns", FileTypeCategory.Icon },
        { ".cur", FileTypeCategory.Icon },
        { ".ani", FileTypeCategory.Icon },

        // ===== 图像项目 =====
        { ".psd", FileTypeCategory.ImageProject },
        { ".psb", FileTypeCategory.ImageProject },
        { ".pdd", FileTypeCategory.ImageProject },
        { ".xcf", FileTypeCategory.ImageProject },
        { ".kra", FileTypeCategory.ImageProject },
        { ".clip", FileTypeCategory.ImageProject },
        { ".sai", FileTypeCategory.ImageProject },
        { ".sai2", FileTypeCategory.ImageProject },
        { ".procreate", FileTypeCategory.ImageProject },
        { ".afphoto", FileTypeCategory.ImageProject },
        { ".afdesign", FileTypeCategory.ImageProject },
        { ".sketch", FileTypeCategory.ImageProject },
        { ".fig", FileTypeCategory.ImageProject },
        { ".xd", FileTypeCategory.ImageProject },

        // ===== HDR 图像 =====
        { ".hdr", FileTypeCategory.HDRImage },
        { ".exr", FileTypeCategory.HDRImage },
        { ".pfm", FileTypeCategory.HDRImage },
        { ".rgbe", FileTypeCategory.HDRImage },

        // ===== 有损音频 =====
        { ".mp3", FileTypeCategory.LossyAudio },
        { ".aac", FileTypeCategory.LossyAudio },
        { ".m4a", FileTypeCategory.LossyAudio },
        { ".m4p", FileTypeCategory.LossyAudio },
        { ".wma", FileTypeCategory.LossyAudio },
        { ".ogg", FileTypeCategory.LossyAudio },
        { ".oga", FileTypeCategory.LossyAudio },
        { ".opus", FileTypeCategory.LossyAudio },
        { ".mpc", FileTypeCategory.LossyAudio },
        { ".mp+", FileTypeCategory.LossyAudio },
        { ".mpp", FileTypeCategory.LossyAudio },
        { ".ra", FileTypeCategory.LossyAudio },
        { ".rm", FileTypeCategory.LossyAudio },
        { ".ram", FileTypeCategory.LossyAudio },
        { ".ac3", FileTypeCategory.LossyAudio },
        { ".dts", FileTypeCategory.LossyAudio },
        { ".amr", FileTypeCategory.LossyAudio },
        { ".awb", FileTypeCategory.LossyAudio },
        { ".spx", FileTypeCategory.LossyAudio },
        { ".gsm", FileTypeCategory.LossyAudio },

        // ===== 无损音频 =====
        { ".flac", FileTypeCategory.LosslessAudio },
        { ".alac", FileTypeCategory.LosslessAudio },
        { ".ape", FileTypeCategory.LosslessAudio },
        { ".wav", FileTypeCategory.LosslessAudio },
        { ".wave", FileTypeCategory.LosslessAudio },
        { ".aiff", FileTypeCategory.LosslessAudio },
        { ".aif", FileTypeCategory.LosslessAudio },
        { ".aifc", FileTypeCategory.LosslessAudio },
        { ".wv", FileTypeCategory.LosslessAudio },
        { ".tta", FileTypeCategory.LosslessAudio },
        { ".tak", FileTypeCategory.LosslessAudio },
        { ".shn", FileTypeCategory.LosslessAudio },
        { ".ofr", FileTypeCategory.LosslessAudio },
        { ".ofs", FileTypeCategory.LosslessAudio },
        { ".la", FileTypeCategory.LosslessAudio },
        { ".pac", FileTypeCategory.LosslessAudio },
        { ".au", FileTypeCategory.LosslessAudio },
        { ".snd", FileTypeCategory.LosslessAudio },
        { ".caf", FileTypeCategory.LosslessAudio },
        { ".w64", FileTypeCategory.LosslessAudio },
        { ".rf64", FileTypeCategory.LosslessAudio },

        // ===== MIDI =====
        { ".mid", FileTypeCategory.MIDI },
        { ".midi", FileTypeCategory.MIDI },
        { ".kar", FileTypeCategory.MIDI },
        { ".rmi", FileTypeCategory.MIDI },

        // ===== 音频项目 =====
        { ".als", FileTypeCategory.AudioProject },
        { ".alp", FileTypeCategory.AudioProject },
        { ".flp", FileTypeCategory.AudioProject },
        { ".logic", FileTypeCategory.AudioProject },
        { ".logicx", FileTypeCategory.AudioProject },
        { ".ptf", FileTypeCategory.AudioProject },
        { ".cpr", FileTypeCategory.AudioProject },
        { ".npr", FileTypeCategory.AudioProject },
        { ".rpp", FileTypeCategory.AudioProject },
        { ".rpp-bak", FileTypeCategory.AudioProject },
        { ".aup", FileTypeCategory.AudioProject },
        { ".aup3", FileTypeCategory.AudioProject },
        { ".sesx", FileTypeCategory.AudioProject },
        { ".band", FileTypeCategory.AudioProject },
        { ".reason", FileTypeCategory.AudioProject },
        { ".rns", FileTypeCategory.AudioProject },

        // ===== 音效/采样 =====
        { ".sf2", FileTypeCategory.AudioSample },
        { ".sf3", FileTypeCategory.AudioSample },
        { ".sfz", FileTypeCategory.AudioSample },
        { ".nki", FileTypeCategory.AudioSample },
        { ".nkm", FileTypeCategory.AudioSample },
        { ".nkc", FileTypeCategory.AudioSample },
        { ".exs", FileTypeCategory.AudioSample },
        { ".gig", FileTypeCategory.AudioSample },
        { ".akp", FileTypeCategory.AudioSample },
        { ".rex", FileTypeCategory.AudioSample },
        { ".rx2", FileTypeCategory.AudioSample },

        // ===== 有声书 =====
        { ".m4b", FileTypeCategory.Audiobook },
        { ".aa", FileTypeCategory.Audiobook },
        { ".aax", FileTypeCategory.Audiobook },

        // ===== 播放列表 =====
        { ".m3u", FileTypeCategory.Playlist },
        { ".m3u8", FileTypeCategory.Playlist },
        { ".pls", FileTypeCategory.Playlist },
        { ".wpl", FileTypeCategory.Playlist },
        { ".xspf", FileTypeCategory.Playlist },
        { ".asx", FileTypeCategory.Playlist },
        { ".cue", FileTypeCategory.Playlist },
        { ".vlc", FileTypeCategory.Playlist },

        // ===== 通用视频 =====
        { ".mp4", FileTypeCategory.Video },
        { ".m4v", FileTypeCategory.Video },
        { ".mkv", FileTypeCategory.Video },
        { ".avi", FileTypeCategory.Video },
        { ".mov", FileTypeCategory.Video },
        { ".qt", FileTypeCategory.Video },
        { ".wmv", FileTypeCategory.Video },
        { ".asf", FileTypeCategory.Video },
        { ".flv", FileTypeCategory.Video },
        { ".f4v", FileTypeCategory.Video },
        { ".webm", FileTypeCategory.Video },
        { ".ogv", FileTypeCategory.Video },
        { ".3gp", FileTypeCategory.Video },
        { ".3g2", FileTypeCategory.Video },
        { ".3gpp", FileTypeCategory.Video },
        { ".3gpp2", FileTypeCategory.Video },
        { ".mpg", FileTypeCategory.Video },
        { ".mpeg", FileTypeCategory.Video },
        { ".mpe", FileTypeCategory.Video },
        { ".mpv", FileTypeCategory.Video },
        { ".m2v", FileTypeCategory.Video },
        { ".m1v", FileTypeCategory.Video },
        { ".m2p", FileTypeCategory.Video },
        { ".m2ts", FileTypeCategory.Video },
        { ".mts", FileTypeCategory.Video },
        { ".divx", FileTypeCategory.Video },
        { ".xvid", FileTypeCategory.Video },
        { ".rmvb", FileTypeCategory.Video },
        { ".vob", FileTypeCategory.DiscVideo },
        { ".ifo", FileTypeCategory.DiscVideo },
        { ".bup", FileTypeCategory.DiscVideo },
        { ".bdmv", FileTypeCategory.DiscVideo },

        // ===== 专业视频 =====
        { ".mxf", FileTypeCategory.ProVideo },
        { ".r3d", FileTypeCategory.ProVideo },
        { ".braw", FileTypeCategory.ProVideo },
        { ".prores", FileTypeCategory.ProVideo },
        { ".dnxhd", FileTypeCategory.ProVideo },
        { ".dnxhr", FileTypeCategory.ProVideo },
        { ".dcp", FileTypeCategory.ProVideo },

        // ===== 视频项目 =====
        { ".prproj", FileTypeCategory.VideoProject },
        { ".prel", FileTypeCategory.VideoProject },
        { ".aep", FileTypeCategory.VideoProject },
        { ".aepx", FileTypeCategory.VideoProject },
        { ".aet", FileTypeCategory.VideoProject },
        { ".veg", FileTypeCategory.VideoProject },
        { ".vf", FileTypeCategory.VideoProject },
        { ".kdenlive", FileTypeCategory.VideoProject },
        { ".fcpx", FileTypeCategory.VideoProject },
        { ".fcpxml", FileTypeCategory.VideoProject },
        { ".imovieproj", FileTypeCategory.VideoProject },
        { ".drp", FileTypeCategory.VideoProject },
        { ".wlmp", FileTypeCategory.VideoProject },
        { ".mswmm", FileTypeCategory.VideoProject },
        { ".camproj", FileTypeCategory.VideoProject },
        { ".vpj", FileTypeCategory.VideoProject },
        { ".edl", FileTypeCategory.VideoProject },

        // ===== 字幕 =====
        { ".srt", FileTypeCategory.Subtitle },
        { ".sub", FileTypeCategory.Subtitle },
        { ".ass", FileTypeCategory.Subtitle },
        { ".ssa", FileTypeCategory.Subtitle },
        { ".vtt", FileTypeCategory.Subtitle },
        { ".idx", FileTypeCategory.Subtitle },
        { ".sup", FileTypeCategory.Subtitle },
        { ".smi", FileTypeCategory.Subtitle },
        { ".sami", FileTypeCategory.Subtitle },
        { ".sbv", FileTypeCategory.Subtitle },
        { ".lrc", FileTypeCategory.Subtitle },
        { ".usf", FileTypeCategory.Subtitle },
        { ".ttml", FileTypeCategory.Subtitle },
        { ".dfxp", FileTypeCategory.Subtitle },

        // ===== 动画 =====
        { ".apng", FileTypeCategory.Animation },
        { ".lottie", FileTypeCategory.Animation },
        { ".bodymovin", FileTypeCategory.Animation },
        { ".rive", FileTypeCategory.Animation },

        // ===== 通用压缩 =====
        { ".zip", FileTypeCategory.Archive },
        { ".rar", FileTypeCategory.Archive },
        { ".7z", FileTypeCategory.Archive },
        { ".tar", FileTypeCategory.Archive },
        { ".gz", FileTypeCategory.Archive },
        { ".gzip", FileTypeCategory.Archive },
        { ".bz2", FileTypeCategory.Archive },
        { ".bzip2", FileTypeCategory.Archive },
        { ".xz", FileTypeCategory.Archive },
        { ".lz", FileTypeCategory.Archive },
        { ".lzma", FileTypeCategory.Archive },
        { ".lz4", FileTypeCategory.Archive },
        { ".zst", FileTypeCategory.Archive },
        { ".zstd", FileTypeCategory.Archive },
        { ".tgz", FileTypeCategory.Archive },
        { ".tbz2", FileTypeCategory.Archive },
        { ".txz", FileTypeCategory.Archive },
        { ".tlz", FileTypeCategory.Archive },
        { ".tar.gz", FileTypeCategory.Archive },
        { ".tar.bz2", FileTypeCategory.Archive },
        { ".tar.xz", FileTypeCategory.Archive },
        { ".tar.lz", FileTypeCategory.Archive },
        { ".tar.zst", FileTypeCategory.Archive },
        { ".ace", FileTypeCategory.Archive },
        { ".arj", FileTypeCategory.Archive },
        { ".lzh", FileTypeCategory.Archive },
        { ".lha", FileTypeCategory.Archive },
        { ".zoo", FileTypeCategory.Archive },
        { ".sit", FileTypeCategory.Archive },
        { ".sitx", FileTypeCategory.Archive },
        { ".sea", FileTypeCategory.Archive },
        { ".sqx", FileTypeCategory.Archive },
        { ".arc", FileTypeCategory.Archive },
        { ".pak", FileTypeCategory.Archive },
        { ".cpio", FileTypeCategory.Archive },
        { ".shar", FileTypeCategory.Archive },
        { ".war", FileTypeCategory.Archive },
        { ".ear", FileTypeCategory.Archive },

        // ===== 安装包压缩 =====
        { ".cab", FileTypeCategory.PackageArchive },
        { ".msi", FileTypeCategory.WindowsInstaller },
        { ".msix", FileTypeCategory.WindowsInstaller },
        { ".appx", FileTypeCategory.WindowsInstaller },
        { ".appxbundle", FileTypeCategory.WindowsInstaller },
        { ".msixbundle", FileTypeCategory.WindowsInstaller },
        { ".deb", FileTypeCategory.LinuxPackage },
        { ".rpm", FileTypeCategory.LinuxPackage },
        { ".pkg", FileTypeCategory.MacApplication },
        { ".mpkg", FileTypeCategory.MacApplication },
        { ".dmg", FileTypeCategory.MacApplication },

        // ===== 分卷压缩 =====
        { ".001", FileTypeCategory.SplitArchive },
        { ".r00", FileTypeCategory.SplitArchive },
        { ".z01", FileTypeCategory.SplitArchive },
        { ".part1", FileTypeCategory.SplitArchive },
        { ".part01", FileTypeCategory.SplitArchive },

        // ===== 磁盘镜像 =====
        { ".iso", FileTypeCategory.DiskImage },
        { ".img", FileTypeCategory.DiskImage },
        { ".bin", FileTypeCategory.DiskImage },
        { ".nrg", FileTypeCategory.DiskImage },
        { ".mdf", FileTypeCategory.DiskImage },
        { ".mds", FileTypeCategory.DiskImage },
        { ".ccd", FileTypeCategory.DiskImage },
        { ".cif", FileTypeCategory.DiskImage },
        { ".c2d", FileTypeCategory.DiskImage },
        { ".daa", FileTypeCategory.DiskImage },
        { ".b5i", FileTypeCategory.DiskImage },
        { ".b6i", FileTypeCategory.DiskImage },
        { ".bwi", FileTypeCategory.DiskImage },
        { ".cdi", FileTypeCategory.DiskImage },
        { ".pdi", FileTypeCategory.DiskImage },
        { ".gi", FileTypeCategory.DiskImage },
        { ".uif", FileTypeCategory.DiskImage },
        { ".vcd", FileTypeCategory.DiskImage },
        { ".vhd", FileTypeCategory.VirtualDisk },
        { ".vhdx", FileTypeCategory.VirtualDisk },
        { ".vmdk", FileTypeCategory.VirtualDisk },
        { ".vdi", FileTypeCategory.VirtualDisk },
        { ".qcow", FileTypeCategory.VirtualDisk },
        { ".qcow2", FileTypeCategory.VirtualDisk },
        { ".qed", FileTypeCategory.VirtualDisk },
        { ".hdd", FileTypeCategory.VirtualDisk },
        { ".parallels", FileTypeCategory.VirtualDisk },
        { ".fdd", FileTypeCategory.DiskImage },
        { ".ima", FileTypeCategory.DiskImage },
        { ".imz", FileTypeCategory.DiskImage },
        { ".wim", FileTypeCategory.DiskImage },
        { ".esd", FileTypeCategory.DiskImage },
        { ".swm", FileTypeCategory.DiskImage },

        // ===== C/C++ =====
        { ".c", FileTypeCategory.CppSource },
        { ".cpp", FileTypeCategory.CppSource },
        { ".cc", FileTypeCategory.CppSource },
        { ".cxx", FileTypeCategory.CppSource },
        { ".c++", FileTypeCategory.CppSource },
        { ".h", FileTypeCategory.CppSource },
        { ".hpp", FileTypeCategory.CppSource },
        { ".hh", FileTypeCategory.CppSource },
        { ".hxx", FileTypeCategory.CppSource },
        { ".h++", FileTypeCategory.CppSource },
        { ".inl", FileTypeCategory.CppSource },
        { ".ipp", FileTypeCategory.CppSource },
        { ".tpp", FileTypeCategory.CppSource },
        { ".tcc", FileTypeCategory.CppSource },

        // ===== C# =====
        { ".cs", FileTypeCategory.CSharpSource },
        { ".csx", FileTypeCategory.CSharpSource },
        { ".cake", FileTypeCategory.CSharpSource },

        // ===== Java =====
        { ".java", FileTypeCategory.JavaSource },
        { ".jar", FileTypeCategory.JavaSource },
        { ".class", FileTypeCategory.JavaSource },
        { ".jad", FileTypeCategory.JavaSource },
        { ".jsp", FileTypeCategory.JavaSource },
        { ".jspx", FileTypeCategory.JavaSource },

        // ===== Kotlin =====
        { ".kt", FileTypeCategory.KotlinSource },
        { ".kts", FileTypeCategory.KotlinSource },
        { ".ktm", FileTypeCategory.KotlinSource },

        // ===== Groovy =====
        { ".groovy", FileTypeCategory.JavaSource },
        { ".gvy", FileTypeCategory.JavaSource },
        { ".gy", FileTypeCategory.JavaSource },
        { ".gsh", FileTypeCategory.JavaSource },

        // ===== JavaScript/TypeScript =====
        { ".js", FileTypeCategory.JavaScriptSource },
        { ".jsx", FileTypeCategory.JavaScriptSource },
        { ".ts", FileTypeCategory.JavaScriptSource },
        { ".tsx", FileTypeCategory.JavaScriptSource },
        { ".mjs", FileTypeCategory.JavaScriptSource },
        { ".cjs", FileTypeCategory.JavaScriptSource },
        { ".es", FileTypeCategory.JavaScriptSource },
        { ".es6", FileTypeCategory.JavaScriptSource },

        // ===== Python =====
        { ".py", FileTypeCategory.PythonSource },
        { ".pyw", FileTypeCategory.PythonSource },
        { ".pyx", FileTypeCategory.PythonSource },
        { ".pxd", FileTypeCategory.PythonSource },
        { ".pxi", FileTypeCategory.PythonSource },
        { ".pyi", FileTypeCategory.PythonSource },
        { ".pyc", FileTypeCategory.PythonSource },
        { ".pyo", FileTypeCategory.PythonSource },
        { ".pyd", FileTypeCategory.PythonSource },
        { ".ipynb", FileTypeCategory.PythonSource },
        { ".rpy", FileTypeCategory.PythonSource },

        // ===== Web 前端 =====
        { ".html", FileTypeCategory.WebSource },
        { ".htm", FileTypeCategory.WebSource },
        { ".xhtml", FileTypeCategory.WebSource },
        { ".shtml", FileTypeCategory.WebSource },
        { ".mhtml", FileTypeCategory.WebSource },
        { ".mht", FileTypeCategory.WebSource },
        { ".css", FileTypeCategory.WebSource },
        { ".scss", FileTypeCategory.WebSource },
        { ".sass", FileTypeCategory.WebSource },
        { ".less", FileTypeCategory.WebSource },
        { ".styl", FileTypeCategory.WebSource },
        { ".stylus", FileTypeCategory.WebSource },
        { ".vue", FileTypeCategory.WebSource },
        { ".svelte", FileTypeCategory.WebSource },
        { ".astro", FileTypeCategory.WebSource },
        { ".mdx", FileTypeCategory.WebSource },

        // ===== PHP =====
        { ".php", FileTypeCategory.PHPSource },
        { ".phtml", FileTypeCategory.PHPSource },
        { ".php3", FileTypeCategory.PHPSource },
        { ".php4", FileTypeCategory.PHPSource },
        { ".php5", FileTypeCategory.PHPSource },
        { ".php7", FileTypeCategory.PHPSource },
        { ".phps", FileTypeCategory.PHPSource },
        { ".phar", FileTypeCategory.PHPSource },

        // ===== Ruby =====
        { ".rb", FileTypeCategory.RubySource },
        { ".rbw", FileTypeCategory.RubySource },
        { ".rake", FileTypeCategory.RubySource },
        { ".gemspec", FileTypeCategory.RubySource },
        { ".erb", FileTypeCategory.RubySource },
        { ".rhtml", FileTypeCategory.RubySource },
        { ".rjs", FileTypeCategory.RubySource },
        { ".rxml", FileTypeCategory.RubySource },
        { ".builder", FileTypeCategory.RubySource },
        { ".ru", FileTypeCategory.RubySource },
        { ".podspec", FileTypeCategory.RubySource },

        // ===== Go =====
        { ".go", FileTypeCategory.GoSource },
        { ".mod", FileTypeCategory.GoSource },
        { ".sum", FileTypeCategory.GoSource },

        // ===== Rust =====
        { ".rs", FileTypeCategory.RustSource },
        { ".rlib", FileTypeCategory.RustSource },

        // ===== Swift/Objective-C =====
        { ".swift", FileTypeCategory.SwiftSource },
        { ".m", FileTypeCategory.SwiftSource },
        { ".mm", FileTypeCategory.SwiftSource },

        // ===== Shell 脚本 =====
        { ".sh", FileTypeCategory.ShellScript },
        { ".bash", FileTypeCategory.ShellScript },
        { ".zsh", FileTypeCategory.ShellScript },
        { ".fish", FileTypeCategory.ShellScript },
        { ".csh", FileTypeCategory.ShellScript },
        { ".tcsh", FileTypeCategory.ShellScript },
        { ".ksh", FileTypeCategory.ShellScript },
        { ".ps1", FileTypeCategory.ShellScript },
        { ".psm1", FileTypeCategory.ShellScript },
        { ".psd1", FileTypeCategory.ShellScript },
        { ".bat", FileTypeCategory.ShellScript },
        { ".cmd", FileTypeCategory.ShellScript },
        { ".btm", FileTypeCategory.ShellScript },
        { ".command", FileTypeCategory.ShellScript },

        // ===== SQL =====
        { ".sql", FileTypeCategory.SQLSource },
        { ".ddl", FileTypeCategory.SQLSource },
        { ".dml", FileTypeCategory.SQLSource },
        { ".pgsql", FileTypeCategory.SQLSource },
        { ".plsql", FileTypeCategory.SQLSource },
        { ".tsql", FileTypeCategory.SQLSource },
        { ".mysql", FileTypeCategory.SQLSource },
        { ".hql", FileTypeCategory.SQLSource },
        { ".cql", FileTypeCategory.SQLSource },

        // ===== 汇编 =====
        { ".asm", FileTypeCategory.AssemblySource },
        { ".s", FileTypeCategory.AssemblySource },
        { ".inc", FileTypeCategory.AssemblySource },
        { ".nasm", FileTypeCategory.AssemblySource },
        { ".masm", FileTypeCategory.AssemblySource },

        // ===== Lua =====
        { ".lua", FileTypeCategory.LuaSource },
        { ".luac", FileTypeCategory.LuaSource },
        { ".luau", FileTypeCategory.LuaSource },

        // ===== Perl =====
        { ".pl", FileTypeCategory.PerlSource },
        { ".pm", FileTypeCategory.PerlSource },
        { ".pod", FileTypeCategory.PerlSource },
        { ".t", FileTypeCategory.PerlSource },
        { ".psgi", FileTypeCategory.PerlSource },

        // ===== R =====
        { ".r", FileTypeCategory.RSource },
        { ".rmd", FileTypeCategory.RSource },
        { ".rnw", FileTypeCategory.RSource },
        { ".rdata", FileTypeCategory.RSource },
        { ".rds", FileTypeCategory.RSource },

        // ===== Scala =====
        { ".scala", FileTypeCategory.ScalaSource },
        { ".sc", FileTypeCategory.ScalaSource },
        { ".sbt", FileTypeCategory.ScalaSource },

        // ===== Haskell =====
        { ".hs", FileTypeCategory.HaskellSource },
        { ".lhs", FileTypeCategory.HaskellSource },
        { ".cabal", FileTypeCategory.HaskellSource },
        { ".hsc", FileTypeCategory.HaskellSource },

        // ===== Erlang/Elixir =====
        { ".erl", FileTypeCategory.ErlangSource },
        { ".hrl", FileTypeCategory.ErlangSource },
        { ".ex", FileTypeCategory.ErlangSource },
        { ".eex", FileTypeCategory.ErlangSource },
        { ".leex", FileTypeCategory.ErlangSource },
        { ".heex", FileTypeCategory.ErlangSource },

        // ===== Dart =====
        { ".dart", FileTypeCategory.DartSource },

        // ===== 其他源代码 =====
        { ".f", FileTypeCategory.OtherSource },
        { ".for", FileTypeCategory.OtherSource },
        { ".f77", FileTypeCategory.OtherSource },
        { ".f90", FileTypeCategory.OtherSource },
        { ".f95", FileTypeCategory.OtherSource },
        { ".f03", FileTypeCategory.OtherSource },
        { ".f08", FileTypeCategory.OtherSource },
        { ".cob", FileTypeCategory.OtherSource },
        { ".cbl", FileTypeCategory.OtherSource },
        { ".cpy", FileTypeCategory.OtherSource },
        { ".pas", FileTypeCategory.OtherSource },
        { ".pp", FileTypeCategory.OtherSource },
        { ".dpr", FileTypeCategory.OtherSource },
        { ".dpk", FileTypeCategory.OtherSource },
        { ".d", FileTypeCategory.OtherSource },
        { ".di", FileTypeCategory.OtherSource },
        { ".nim", FileTypeCategory.OtherSource },
        { ".nims", FileTypeCategory.OtherSource },
        { ".nimble", FileTypeCategory.OtherSource },
        { ".zig", FileTypeCategory.OtherSource },
        { ".v", FileTypeCategory.OtherSource },
        { ".vhdl", FileTypeCategory.OtherSource },
        { ".sv", FileTypeCategory.OtherSource },
        { ".svh", FileTypeCategory.OtherSource },
        { ".verilog", FileTypeCategory.OtherSource },
        { ".tcl", FileTypeCategory.OtherSource },
        { ".tk", FileTypeCategory.OtherSource },
        { ".awk", FileTypeCategory.OtherSource },
        { ".sed", FileTypeCategory.OtherSource },
        { ".clj", FileTypeCategory.OtherSource },
        { ".cljs", FileTypeCategory.OtherSource },
        { ".cljc", FileTypeCategory.OtherSource },
        { ".edn", FileTypeCategory.OtherSource },
        { ".lisp", FileTypeCategory.OtherSource },
        { ".lsp", FileTypeCategory.OtherSource },
        { ".cl", FileTypeCategory.OtherSource },
        { ".el", FileTypeCategory.OtherSource },
        { ".scm", FileTypeCategory.OtherSource },
        { ".ss", FileTypeCategory.OtherSource },
        { ".rkt", FileTypeCategory.OtherSource },
        { ".ml", FileTypeCategory.OtherSource },
        { ".mli", FileTypeCategory.OtherSource },
        { ".fs", FileTypeCategory.OtherSource },
        { ".fsi", FileTypeCategory.OtherSource },
        { ".fsx", FileTypeCategory.OtherSource },
        { ".fsscript", FileTypeCategory.OtherSource },
        { ".vb", FileTypeCategory.OtherSource },
        { ".vbs", FileTypeCategory.OtherSource },
        { ".vba", FileTypeCategory.OtherSource },
        { ".bas", FileTypeCategory.OtherSource },
        { ".frm", FileTypeCategory.OtherSource },
        { ".ctl", FileTypeCategory.OtherSource },
        { ".pli", FileTypeCategory.OtherSource },
        { ".pl1", FileTypeCategory.OtherSource },
        { ".ada", FileTypeCategory.OtherSource },
        { ".adb", FileTypeCategory.OtherSource },
        { ".ads", FileTypeCategory.OtherSource },
        { ".prolog", FileTypeCategory.OtherSource },
        { ".pro", FileTypeCategory.OtherSource },
        { ".P", FileTypeCategory.OtherSource },
        { ".cr", FileTypeCategory.OtherSource },
        { ".jl", FileTypeCategory.OtherSource },
        { ".coffee", FileTypeCategory.OtherSource },
        { ".litcoffee", FileTypeCategory.OtherSource },
        { ".elm", FileTypeCategory.OtherSource },
        { ".purs", FileTypeCategory.OtherSource },
        { ".re", FileTypeCategory.OtherSource },
        { ".rei", FileTypeCategory.OtherSource },
        { ".res", FileTypeCategory.OtherSource },
        { ".resi", FileTypeCategory.OtherSource },
        { ".odin", FileTypeCategory.OtherSource },
        { ".jai", FileTypeCategory.OtherSource },
        { ".wren", FileTypeCategory.OtherSource },
        { ".hx", FileTypeCategory.OtherSource },
        { ".hxml", FileTypeCategory.OtherSource },
        { ".moon", FileTypeCategory.OtherSource },
        { ".squirrel", FileTypeCategory.OtherSource },
        { ".nut", FileTypeCategory.OtherSource },
        { ".gd", FileTypeCategory.OtherSource },
        { ".gdscript", FileTypeCategory.OtherSource },

        // ===== Windows 可执行 =====
        { ".exe", FileTypeCategory.WindowsExecutable },
        { ".com", FileTypeCategory.WindowsExecutable },
        { ".scr", FileTypeCategory.WindowsExecutable },
        { ".pif", FileTypeCategory.WindowsExecutable },

        // ===== macOS 应用 =====
        { ".app", FileTypeCategory.MacApplication },

        // ===== Linux 可执行 =====
        { ".AppImage", FileTypeCategory.LinuxExecutable },
        { ".run", FileTypeCategory.LinuxExecutable },
        { ".elf", FileTypeCategory.LinuxExecutable },

        // ===== Linux 包 =====
        { ".snap", FileTypeCategory.LinuxPackage },
        { ".flatpak", FileTypeCategory.LinuxPackage },
        { ".flatpakref", FileTypeCategory.LinuxPackage },

        // ===== Android =====
        { ".apk", FileTypeCategory.AndroidPackage },
        { ".aab", FileTypeCategory.AndroidPackage },
        { ".xapk", FileTypeCategory.AndroidPackage },
        { ".apks", FileTypeCategory.AndroidPackage },

        // ===== iOS =====
        { ".ipa", FileTypeCategory.iOSPackage },

        // ===== 动态库 =====
        { ".dll", FileTypeCategory.DynamicLibrary },
        { ".so", FileTypeCategory.DynamicLibrary },
        { ".dylib", FileTypeCategory.DynamicLibrary },
        { ".bundle", FileTypeCategory.DynamicLibrary },
        { ".framework", FileTypeCategory.DynamicLibrary },

        // ===== 静态库 =====
        { ".lib", FileTypeCategory.StaticLibrary },
        { ".a", FileTypeCategory.StaticLibrary },

        // ===== 驱动程序 =====
        { ".sys", FileTypeCategory.Driver },
        { ".ko", FileTypeCategory.Driver },
        { ".kext", FileTypeCategory.Driver },
        { ".drv", FileTypeCategory.Driver },

        // ===== JSON =====
        { ".json", FileTypeCategory.JSON },
        { ".jsonc", FileTypeCategory.JSON },
        { ".json5", FileTypeCategory.JSON },
        { ".geojson", FileTypeCategory.JSON },
        { ".topojson", FileTypeCategory.JSON },
        { ".jsonl", FileTypeCategory.JSON },
        { ".ndjson", FileTypeCategory.JSON },
        { ".har", FileTypeCategory.JSON },

        // ===== XML =====
        { ".xml", FileTypeCategory.XML },
        { ".xsl", FileTypeCategory.XML },
        { ".xslt", FileTypeCategory.XML },
        { ".xsd", FileTypeCategory.XML },
        { ".dtd", FileTypeCategory.XML },
        { ".rss", FileTypeCategory.XML },
        { ".atom", FileTypeCategory.XML },
        { ".rdf", FileTypeCategory.XML },
        { ".owl", FileTypeCategory.XML },
        { ".wsdl", FileTypeCategory.XML },
        { ".wadl", FileTypeCategory.XML },
        { ".soap", FileTypeCategory.XML },
        { ".plist", FileTypeCategory.XML },
        { ".resx", FileTypeCategory.XML },
        { ".csproj", FileTypeCategory.VisualStudioProject },
        { ".vbproj", FileTypeCategory.VisualStudioProject },
        { ".fsproj", FileTypeCategory.VisualStudioProject },
        { ".vcxproj", FileTypeCategory.VisualStudioProject },
        { ".vcproj", FileTypeCategory.VisualStudioProject },
        { ".props", FileTypeCategory.VisualStudioProject },
        { ".targets", FileTypeCategory.VisualStudioProject },
        { ".nuspec", FileTypeCategory.XML },
        { ".xaml", FileTypeCategory.XML },
        { ".axaml", FileTypeCategory.XML },
        { ".xib", FileTypeCategory.XML },
        { ".storyboard", FileTypeCategory.XML },
        { ".nib", FileTypeCategory.XML },
        { ".glade", FileTypeCategory.XML },
        { ".ui", FileTypeCategory.XML },
        { ".qrc", FileTypeCategory.XML },
        { ".gpx", FileTypeCategory.GISData },
        { ".kml", FileTypeCategory.GISData },
        { ".kmz", FileTypeCategory.GISData },
        { ".osm", FileTypeCategory.GISData },

        // ===== YAML =====
        { ".yaml", FileTypeCategory.YAML },
        { ".yml", FileTypeCategory.YAML },

        // ===== TOML =====
        { ".toml", FileTypeCategory.TOML },

        // ===== INI/配置 =====
        { ".ini", FileTypeCategory.ConfigFile },
        { ".cfg", FileTypeCategory.ConfigFile },
        { ".conf", FileTypeCategory.ConfigFile },
        { ".config", FileTypeCategory.ConfigFile },
        { ".properties", FileTypeCategory.ConfigFile },
        { ".env", FileTypeCategory.ConfigFile },
        { ".envrc", FileTypeCategory.ConfigFile },
        { ".editorconfig", FileTypeCategory.ConfigFile },
        { ".htaccess", FileTypeCategory.ConfigFile },
        { ".htpasswd", FileTypeCategory.ConfigFile },
        { ".npmrc", FileTypeCategory.ConfigFile },
        { ".yarnrc", FileTypeCategory.ConfigFile },
        { ".babelrc", FileTypeCategory.ConfigFile },
        { ".eslintrc", FileTypeCategory.ConfigFile },
        { ".prettierrc", FileTypeCategory.ConfigFile },
        { ".stylelintrc", FileTypeCategory.ConfigFile },
        { ".browserslistrc", FileTypeCategory.ConfigFile },

        // ===== 注册表 =====
        { ".reg", FileTypeCategory.RegistryFile },

        // ===== 证书/密钥 =====
        { ".pem", FileTypeCategory.Certificate },
        { ".crt", FileTypeCategory.Certificate },
        { ".cer", FileTypeCategory.Certificate },
        { ".der", FileTypeCategory.Certificate },
        { ".p12", FileTypeCategory.Certificate },
        { ".pfx", FileTypeCategory.Certificate },
        { ".p7b", FileTypeCategory.Certificate },
        { ".p7c", FileTypeCategory.Certificate },
        { ".pub", FileTypeCategory.Certificate },
        { ".ppk", FileTypeCategory.Certificate },
        { ".asc", FileTypeCategory.Certificate },
        { ".gpg", FileTypeCategory.Certificate },
        { ".pgp", FileTypeCategory.Certificate },
        { ".sig", FileTypeCategory.Certificate },
        { ".csr", FileTypeCategory.Certificate },
        { ".crl", FileTypeCategory.Certificate },
        { ".jks", FileTypeCategory.Certificate },
        { ".keystore", FileTypeCategory.Certificate },
        { ".truststore", FileTypeCategory.Certificate },

        // ===== SQLite =====
        { ".db", FileTypeCategory.SQLiteDatabase },
        { ".sqlite", FileTypeCategory.SQLiteDatabase },
        { ".sqlite3", FileTypeCategory.SQLiteDatabase },
        { ".db3", FileTypeCategory.SQLiteDatabase },
        { ".s3db", FileTypeCategory.SQLiteDatabase },
        { ".sl3", FileTypeCategory.SQLiteDatabase },

        // ===== Access =====
        { ".mdb", FileTypeCategory.AccessDatabase },
        { ".accdb", FileTypeCategory.AccessDatabase },
        { ".accde", FileTypeCategory.AccessDatabase },
        { ".accdt", FileTypeCategory.AccessDatabase },
        { ".accdr", FileTypeCategory.AccessDatabase },

        // ===== 数据库备份 =====
        { ".bak", FileTypeCategory.DatabaseBackup },
        { ".dump", FileTypeCategory.DatabaseBackup },

        // ===== 数据库数据 =====
        { ".ldf", FileTypeCategory.DatabaseData },
        { ".ndf", FileTypeCategory.DatabaseData },
        { ".ibd", FileTypeCategory.DatabaseData },
        { ".myd", FileTypeCategory.DatabaseData },
        { ".myi", FileTypeCategory.DatabaseData },
        { ".dbf", FileTypeCategory.DatabaseData },
        { ".fpt", FileTypeCategory.DatabaseData },
        { ".cdx", FileTypeCategory.DatabaseData },

        // ===== 3D 模型 =====
        { ".obj", FileTypeCategory.Model3D },
        { ".fbx", FileTypeCategory.Model3D },
        { ".gltf", FileTypeCategory.Model3D },
        { ".glb", FileTypeCategory.Model3D },
        { ".dae", FileTypeCategory.Model3D },
        { ".3ds", FileTypeCategory.Model3D },
        { ".stl", FileTypeCategory.Model3D },
        { ".ply", FileTypeCategory.Model3D },
        { ".x3d", FileTypeCategory.Model3D },
        { ".x3dv", FileTypeCategory.Model3D },
        { ".x3db", FileTypeCategory.Model3D },
        { ".wrl", FileTypeCategory.Model3D },
        { ".vrml", FileTypeCategory.Model3D },
        { ".abc", FileTypeCategory.Model3D },
        { ".usd", FileTypeCategory.Model3D },
        { ".usda", FileTypeCategory.Model3D },
        { ".usdc", FileTypeCategory.Model3D },
        { ".usdz", FileTypeCategory.Model3D },
        { ".lwo", FileTypeCategory.Model3D },
        { ".lws", FileTypeCategory.Model3D },
        { ".lxo", FileTypeCategory.Model3D },
        { ".md2", FileTypeCategory.Model3D },
        { ".md3", FileTypeCategory.Model3D },
        { ".md5mesh", FileTypeCategory.Model3D },
        { ".md5anim", FileTypeCategory.Model3D },
        { ".mdl", FileTypeCategory.Model3D },
        { ".smd", FileTypeCategory.Model3D },
        { ".vta", FileTypeCategory.Model3D },
        { ".dmx", FileTypeCategory.Model3D },

        // ===== CAD 文件 =====
        { ".dwg", FileTypeCategory.CADFile },
        { ".dxf", FileTypeCategory.CADFile },
        { ".dgn", FileTypeCategory.CADFile },
        { ".step", FileTypeCategory.CADFile },
        { ".stp", FileTypeCategory.CADFile },
        { ".iges", FileTypeCategory.CADFile },
        { ".igs", FileTypeCategory.CADFile },
        { ".sat", FileTypeCategory.CADFile },
        { ".sab", FileTypeCategory.CADFile },
        { ".sldprt", FileTypeCategory.CADFile },
        { ".sldasm", FileTypeCategory.CADFile },
        { ".slddrw", FileTypeCategory.CADFile },
        { ".prt", FileTypeCategory.CADFile },
        { ".drw", FileTypeCategory.CADFile },
        { ".catpart", FileTypeCategory.CADFile },
        { ".catproduct", FileTypeCategory.CADFile },
        { ".catdrawing", FileTypeCategory.CADFile },
        { ".ipt", FileTypeCategory.CADFile },
        { ".iam", FileTypeCategory.CADFile },
        { ".idw", FileTypeCategory.CADFile },
        { ".dwf", FileTypeCategory.CADFile },
        { ".dwfx", FileTypeCategory.CADFile },
        { ".rvt", FileTypeCategory.CADFile },
        { ".rfa", FileTypeCategory.CADFile },
        { ".rte", FileTypeCategory.CADFile },
        { ".rft", FileTypeCategory.CADFile },
        { ".nwd", FileTypeCategory.CADFile },
        { ".nwc", FileTypeCategory.CADFile },
        { ".nwf", FileTypeCategory.CADFile },
        { ".3dm", FileTypeCategory.CADFile },
        { ".skp", FileTypeCategory.Project3D },
        { ".skb", FileTypeCategory.Project3D },

        // ===== 3D 项目 =====
        { ".blend", FileTypeCategory.Project3D },
        { ".blend1", FileTypeCategory.Project3D },
        { ".max", FileTypeCategory.Project3D },
        { ".ma", FileTypeCategory.Project3D },
        { ".mb", FileTypeCategory.Project3D },
        { ".c4d", FileTypeCategory.Project3D },
        { ".hip", FileTypeCategory.Project3D },
        { ".hiplc", FileTypeCategory.Project3D },
        { ".hipnc", FileTypeCategory.Project3D },
        { ".ztl", FileTypeCategory.Project3D },
        { ".zpr", FileTypeCategory.Project3D },
        { ".mud", FileTypeCategory.Project3D },
        { ".spp", FileTypeCategory.Project3D },
        { ".sbs", FileTypeCategory.Project3D },
        { ".sbsar", FileTypeCategory.Material3D },

        // ===== 材质 =====
        { ".mtl", FileTypeCategory.Material3D },
        { ".mat", FileTypeCategory.Material3D },

        // ===== 点云 =====
        { ".las", FileTypeCategory.PointCloud },
        { ".laz", FileTypeCategory.PointCloud },
        { ".xyz", FileTypeCategory.PointCloud },
        { ".pts", FileTypeCategory.PointCloud },
        { ".e57", FileTypeCategory.PointCloud },
        { ".pcd", FileTypeCategory.PointCloud },

        // ===== 字体 =====
        { ".ttf", FileTypeCategory.Font },
        { ".otf", FileTypeCategory.Font },
        { ".ttc", FileTypeCategory.Font },
        { ".otc", FileTypeCategory.Font },
        { ".dfont", FileTypeCategory.Font },
        { ".pfb", FileTypeCategory.Font },
        { ".afm", FileTypeCategory.Font },

        // ===== Web 字体 =====
        { ".woff", FileTypeCategory.WebFont },
        { ".woff2", FileTypeCategory.WebFont },
        { ".eot", FileTypeCategory.WebFont },

        // ===== 位图字体 =====
        { ".fon", FileTypeCategory.BitmapFont },
        { ".fnt", FileTypeCategory.BitmapFont },
        { ".bdf", FileTypeCategory.BitmapFont },
        { ".pcf", FileTypeCategory.BitmapFont },
        { ".psf", FileTypeCategory.BitmapFont },

        // ===== 游戏存档 =====
        { ".sav", FileTypeCategory.GameSave },
        { ".save", FileTypeCategory.GameSave },
        { ".gamesave", FileTypeCategory.GameSave },
        { ".savegame", FileTypeCategory.GameSave },
        { ".profile", FileTypeCategory.GameSave },

        // ===== 游戏 ROM =====
        { ".nes", FileTypeCategory.GameROM },
        { ".snes", FileTypeCategory.GameROM },
        { ".sfc", FileTypeCategory.GameROM },
        { ".smc", FileTypeCategory.GameROM },
        { ".gba", FileTypeCategory.GameROM },
        { ".gbc", FileTypeCategory.GameROM },
        { ".gb", FileTypeCategory.GameROM },
        { ".nds", FileTypeCategory.GameROM },
        { ".cia", FileTypeCategory.GameROM },
        { ".n64", FileTypeCategory.GameROM },
        { ".z64", FileTypeCategory.GameROM },
        { ".v64", FileTypeCategory.GameROM },
        { ".gcm", FileTypeCategory.GameROM },
        { ".gcz", FileTypeCategory.GameROM },
        { ".wbfs", FileTypeCategory.GameROM },
        { ".wad", FileTypeCategory.GameAsset },
        { ".cso", FileTypeCategory.GameROM },
        { ".pbp", FileTypeCategory.GameROM },
        { ".xci", FileTypeCategory.GameROM },
        { ".nsp", FileTypeCategory.GameROM },
        { ".nsz", FileTypeCategory.GameROM },
        { ".xiso", FileTypeCategory.GameROM },
        { ".god", FileTypeCategory.GameROM },

        // ===== 游戏资源 =====
        { ".vpk", FileTypeCategory.GameAsset },
        { ".bsp", FileTypeCategory.GameAsset },
        { ".pk3", FileTypeCategory.GameAsset },
        { ".pk4", FileTypeCategory.GameAsset },
        { ".gcf", FileTypeCategory.GameAsset },
        { ".ncf", FileTypeCategory.GameAsset },
        { ".vdf", FileTypeCategory.GameAsset },
        { ".acf", FileTypeCategory.GameAsset },
        { ".manifest", FileTypeCategory.GameAsset },
        { ".upk", FileTypeCategory.GameAsset },
        { ".uasset", FileTypeCategory.GameAsset },
        { ".umap", FileTypeCategory.GameAsset },
        { ".u", FileTypeCategory.GameAsset },
        { ".utx", FileTypeCategory.GameAsset },
        { ".umx", FileTypeCategory.GameAsset },
        { ".unr", FileTypeCategory.GameAsset },
        { ".unity3d", FileTypeCategory.GameAsset },
        { ".assets", FileTypeCategory.GameAsset },
        { ".resource", FileTypeCategory.GameAsset },
        { ".resS", FileTypeCategory.GameAsset },

        // ===== 游戏模组 =====
        { ".esp", FileTypeCategory.GameMod },
        { ".esm", FileTypeCategory.GameMod },
        { ".esl", FileTypeCategory.GameMod },
        { ".ba2", FileTypeCategory.GameMod },
        { ".bsa", FileTypeCategory.GameMod },
        { ".fomod", FileTypeCategory.GameMod },
        { ".omod", FileTypeCategory.GameMod },
        { ".mcpack", FileTypeCategory.GameMod },
        { ".mcworld", FileTypeCategory.GameMod },
        { ".mcaddon", FileTypeCategory.GameMod },
        { ".mctemplate", FileTypeCategory.GameMod },

        // ===== 系统文件 =====
        { ".cpl", FileTypeCategory.SystemFile },
        { ".ocx", FileTypeCategory.SystemFile },
        { ".ax", FileTypeCategory.SystemFile },
        { ".tlb", FileTypeCategory.SystemFile },
        { ".olb", FileTypeCategory.SystemFile },
        { ".mui", FileTypeCategory.SystemFile },
        { ".cat", FileTypeCategory.SystemFile },
        { ".inf", FileTypeCategory.SystemFile },

        // ===== 日志文件 =====
        { ".trace", FileTypeCategory.LogFile },
        { ".etl", FileTypeCategory.LogFile },
        { ".evtx", FileTypeCategory.LogFile },
        { ".evt", FileTypeCategory.LogFile },

        // ===== 临时文件 =====
        { ".tmp", FileTypeCategory.TempFile },
        { ".temp", FileTypeCategory.TempFile },
        { ".swp", FileTypeCategory.TempFile },
        { ".swo", FileTypeCategory.TempFile },
        { ".swn", FileTypeCategory.TempFile },
        { ".old", FileTypeCategory.TempFile },
        { ".orig", FileTypeCategory.TempFile },
        { ".~", FileTypeCategory.TempFile },
        { ".part", FileTypeCategory.TempFile },
        { ".partial", FileTypeCategory.TempFile },
        { ".crdownload", FileTypeCategory.TempFile },
        { ".download", FileTypeCategory.TempFile },

        // ===== 缓存文件 =====
        { ".cache", FileTypeCategory.CacheFile },

        // ===== 快捷方式 =====
        { ".lnk", FileTypeCategory.Shortcut },
        { ".url", FileTypeCategory.Shortcut },
        { ".webloc", FileTypeCategory.Shortcut },
        { ".desktop", FileTypeCategory.Shortcut },
        { ".directory", FileTypeCategory.Shortcut },

        // ===== MATLAB =====
        { ".mlx", FileTypeCategory.MATLABFile },
        { ".mlapp", FileTypeCategory.MATLABFile },
        { ".mlappinstall", FileTypeCategory.MATLABFile },
        { ".mltbx", FileTypeCategory.MATLABFile },
        { ".slx", FileTypeCategory.MATLABFile },

        // ===== Mathematica =====
        { ".nb", FileTypeCategory.MathematicaFile },
        { ".cdf", FileTypeCategory.MathematicaFile },
        { ".wl", FileTypeCategory.MathematicaFile },
        { ".wls", FileTypeCategory.MathematicaFile },

        // ===== 科学数据 =====
        { ".hdf", FileTypeCategory.ScientificData },
        { ".hdf4", FileTypeCategory.ScientificData },
        { ".hdf5", FileTypeCategory.ScientificData },
        { ".h5", FileTypeCategory.ScientificData },
        { ".he5", FileTypeCategory.ScientificData },
        { ".nc", FileTypeCategory.ScientificData },
        { ".nc4", FileTypeCategory.ScientificData },
        { ".netcdf", FileTypeCategory.ScientificData },
        { ".fits", FileTypeCategory.ScientificData },
        { ".fit", FileTypeCategory.ScientificData },
        { ".fts", FileTypeCategory.ScientificData },
        { ".grib", FileTypeCategory.ScientificData },
        { ".grib2", FileTypeCategory.ScientificData },
        { ".grb", FileTypeCategory.ScientificData },
        { ".grb2", FileTypeCategory.ScientificData },
        { ".bufr", FileTypeCategory.ScientificData },
        { ".npy", FileTypeCategory.ScientificData },
        { ".npz", FileTypeCategory.ScientificData },
        { ".parquet", FileTypeCategory.ScientificData },
        { ".feather", FileTypeCategory.ScientificData },
        { ".arrow", FileTypeCategory.ScientificData },
        { ".orc", FileTypeCategory.ScientificData },
        { ".avro", FileTypeCategory.ScientificData },

        // ===== GIS 数据 =====
        { ".shp", FileTypeCategory.GISData },
        { ".shx", FileTypeCategory.GISData },
        { ".prj", FileTypeCategory.GISData },
        { ".qpj", FileTypeCategory.GISData },
        { ".cpg", FileTypeCategory.GISData },
        { ".sbn", FileTypeCategory.GISData },
        { ".sbx", FileTypeCategory.GISData },
        { ".gml", FileTypeCategory.GISData },
        { ".mif", FileTypeCategory.GISData },
        { ".tab", FileTypeCategory.GISData },
        { ".gdb", FileTypeCategory.GISData },
        { ".gpkg", FileTypeCategory.GISData },
        { ".mbtiles", FileTypeCategory.GISData },
        { ".pmtiles", FileTypeCategory.GISData },
        { ".tpk", FileTypeCategory.GISData },
        { ".vtpk", FileTypeCategory.GISData },
        { ".slpk", FileTypeCategory.GISData },
        { ".dem", FileTypeCategory.GISData },
        { ".bil", FileTypeCategory.GISData },
        { ".bip", FileTypeCategory.GISData },
        { ".bsq", FileTypeCategory.GISData },
        { ".sid", FileTypeCategory.GISData },
        { ".ecw", FileTypeCategory.GISData },
        { ".ntf", FileTypeCategory.GISData },
        { ".nitf", FileTypeCategory.GISData },
        { ".dt0", FileTypeCategory.GISData },
        { ".dt1", FileTypeCategory.GISData },
        { ".dt2", FileTypeCategory.GISData },

        // ===== 虚拟机配置 =====
        { ".vmx", FileTypeCategory.VirtualMachineConfig },
        { ".vmxf", FileTypeCategory.VirtualMachineConfig },
        { ".vmsd", FileTypeCategory.VirtualMachineConfig },
        { ".vmsn", FileTypeCategory.VirtualMachineConfig },
        { ".vmss", FileTypeCategory.VirtualMachineConfig },
        { ".vmem", FileTypeCategory.VirtualMachineConfig },
        { ".nvram", FileTypeCategory.VirtualMachineConfig },
        { ".vbox", FileTypeCategory.VirtualMachineConfig },
        { ".vbox-prev", FileTypeCategory.VirtualMachineConfig },
        { ".ovf", FileTypeCategory.VirtualMachineConfig },
        { ".ova", FileTypeCategory.VirtualMachineConfig },
        { ".pvm", FileTypeCategory.VirtualMachineConfig },
        { ".pvs", FileTypeCategory.VirtualMachineConfig },
        { ".xva", FileTypeCategory.VirtualMachineConfig },

        // ===== 容器 =====
        { ".dockerfile", FileTypeCategory.ContainerFile },

        // ===== 邮件 =====
        { ".eml", FileTypeCategory.Email },
        { ".emlx", FileTypeCategory.Email },
        { ".msg", FileTypeCategory.Email },
        { ".mbox", FileTypeCategory.Email },
        { ".mbx", FileTypeCategory.Email },
        { ".pst", FileTypeCategory.Email },
        { ".ost", FileTypeCategory.Email },
        { ".dbx", FileTypeCategory.Email },
        { ".nsf", FileTypeCategory.Email },

        // ===== 通讯录 =====
        { ".vcf", FileTypeCategory.Contact },
        { ".vcard", FileTypeCategory.Contact },
        { ".ldif", FileTypeCategory.Contact },

        // ===== 日历 =====
        { ".ics", FileTypeCategory.Calendar },
        { ".ical", FileTypeCategory.Calendar },
        { ".icalendar", FileTypeCategory.Calendar },
        { ".vcs", FileTypeCategory.Calendar },
        { ".ifb", FileTypeCategory.Calendar },

        // ===== Visual Studio 项目 =====
        { ".sln", FileTypeCategory.VisualStudioProject },
        { ".suo", FileTypeCategory.VisualStudioProject },
        { ".user", FileTypeCategory.VisualStudioProject },
        { ".vspscc", FileTypeCategory.VisualStudioProject },
        { ".vssscc", FileTypeCategory.VisualStudioProject },
        { ".vsp", FileTypeCategory.VisualStudioProject },
        { ".vspx", FileTypeCategory.VisualStudioProject },
        { ".dbmdl", FileTypeCategory.VisualStudioProject },
        { ".dbproj", FileTypeCategory.VisualStudioProject },

        // ===== JetBrains 项目 =====
        { ".iml", FileTypeCategory.JetBrainsProject },
        { ".ipr", FileTypeCategory.JetBrainsProject },
        { ".iws", FileTypeCategory.JetBrainsProject },

        // ===== Xcode 项目 =====
        { ".xcodeproj", FileTypeCategory.XcodeProject },
        { ".xcworkspace", FileTypeCategory.XcodeProject },
        { ".pbxproj", FileTypeCategory.XcodeProject },
        { ".xcconfig", FileTypeCategory.XcodeProject },
        { ".entitlements", FileTypeCategory.XcodeProject },
        { ".xcassets", FileTypeCategory.XcodeProject },
        { ".xcstrings", FileTypeCategory.XcodeProject },

        // ===== 构建文件 =====
        { ".makefile", FileTypeCategory.BuildFile },
        { ".mk", FileTypeCategory.BuildFile },
        { ".cmake", FileTypeCategory.BuildFile },
        { ".gradle", FileTypeCategory.BuildFile },
        { ".pom", FileTypeCategory.BuildFile },
        { ".ant", FileTypeCategory.BuildFile },
        { ".gemfile", FileTypeCategory.BuildFile },
        { ".podfile", FileTypeCategory.BuildFile },
        { ".cartfile", FileTypeCategory.BuildFile },
        { ".fastfile", FileTypeCategory.BuildFile },
        { ".appveyor", FileTypeCategory.BuildFile },
        { ".travis", FileTypeCategory.BuildFile },
        { ".jenkinsfile", FileTypeCategory.BuildFile },
        { ".circleci", FileTypeCategory.BuildFile },
        { ".gitlab-ci", FileTypeCategory.BuildFile },
        { ".github", FileTypeCategory.BuildFile },
        { ".bazel", FileTypeCategory.BuildFile },
        { ".bzl", FileTypeCategory.BuildFile },
        { ".buck", FileTypeCategory.BuildFile },
        { ".pants", FileTypeCategory.BuildFile },
        { ".ninja", FileTypeCategory.BuildFile },
        { ".gyp", FileTypeCategory.BuildFile },
        { ".gypi", FileTypeCategory.BuildFile },
        { ".gn", FileTypeCategory.BuildFile },
        { ".gni", FileTypeCategory.BuildFile },

        // ===== Git 文件 =====
        { ".gitignore", FileTypeCategory.GitFile },
        { ".gitattributes", FileTypeCategory.GitFile },
        { ".gitmodules", FileTypeCategory.GitFile },
        { ".gitconfig", FileTypeCategory.GitFile },
        { ".gitkeep", FileTypeCategory.GitFile },

        // ===== 补丁/差异 =====
        { ".patch", FileTypeCategory.PatchFile },
        { ".diff", FileTypeCategory.PatchFile },

        // ===== 种子文件 =====
        { ".torrent", FileTypeCategory.Torrent },
    };

    /// <summary>
    /// 根据文件扩展名获取文件类型分类。
    /// </summary>
    /// <param name="extension">文件扩展名（带或不带点）</param>
    /// <returns>文件类型分类</returns>
    public static FileTypeCategory GetCategory(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return FileTypeCategory.Unknown;

        // 确保扩展名以点开头
        if (!extension.StartsWith('.'))
            extension = "." + extension;

        return ExtensionMap.TryGetValue(extension, out var category) 
            ? category 
            : FileTypeCategory.Unknown;
    }

    /// <summary>
    /// 根据文件路径获取文件类型分类。
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>文件类型分类</returns>
    public static FileTypeCategory GetCategoryFromPath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return FileTypeCategory.Unknown;

        var extension = Path.GetExtension(filePath);
        return GetCategory(extension);
    }

    /// <summary>
    /// 获取指定分类的所有扩展名。
    /// </summary>
    /// <param name="category">文件类型分类</param>
    /// <returns>扩展名列表</returns>
    public static IReadOnlyList<string> GetExtensions(FileTypeCategory category)
    {
        return ExtensionMap
            .Where(kvp => kvp.Value == category)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    /// <summary>
    /// 获取所有已注册的扩展名。
    /// </summary>
    /// <returns>所有扩展名</returns>
    public static IReadOnlyList<string> GetAllExtensions()
    {
        return ExtensionMap.Keys.ToList();
    }

    /// <summary>
    /// 获取分类的显示名称。
    /// </summary>
    /// <param name="category">文件类型分类</param>
    /// <returns>显示名称</returns>
    public static string GetDisplayName(FileTypeCategory category)
    {
        return category switch
        {
            FileTypeCategory.WordDocument => "文字文档",
            FileTypeCategory.Spreadsheet => "电子表格",
            FileTypeCategory.Presentation => "演示文稿",
            FileTypeCategory.PDF => "PDF 文档",
            FileTypeCategory.PlainText => "纯文本",
            FileTypeCategory.RichText => "富文本",
            FileTypeCategory.Ebook => "电子书",
            FileTypeCategory.Markup => "标记语言",
            FileTypeCategory.LaTeX => "LaTeX 文档",
            FileTypeCategory.Note => "笔记",
            FileTypeCategory.RasterImage => "位图图像",
            FileTypeCategory.VectorImage => "矢量图像",
            FileTypeCategory.RawImage => "RAW 图像",
            FileTypeCategory.Icon => "图标",
            FileTypeCategory.ImageProject => "图像项目",
            FileTypeCategory.HDRImage => "HDR 图像",
            FileTypeCategory.LossyAudio => "有损音频",
            FileTypeCategory.LosslessAudio => "无损音频",
            FileTypeCategory.MIDI => "MIDI",
            FileTypeCategory.AudioProject => "音频项目",
            FileTypeCategory.AudioSample => "音频采样",
            FileTypeCategory.Audiobook => "有声书",
            FileTypeCategory.Playlist => "播放列表",
            FileTypeCategory.Video => "视频",
            FileTypeCategory.ProVideo => "专业视频",
            FileTypeCategory.VideoProject => "视频项目",
            FileTypeCategory.Subtitle => "字幕",
            FileTypeCategory.DiscVideo => "光盘视频",
            FileTypeCategory.Animation => "动画",
            FileTypeCategory.Archive => "压缩包",
            FileTypeCategory.PackageArchive => "安装包",
            FileTypeCategory.SplitArchive => "分卷压缩",
            FileTypeCategory.DiskImage => "磁盘镜像",
            FileTypeCategory.CppSource => "C/C++ 源码",
            FileTypeCategory.CSharpSource => "C# 源码",
            FileTypeCategory.JavaSource => "Java 源码",
            FileTypeCategory.JavaScriptSource => "JavaScript/TypeScript",
            FileTypeCategory.PythonSource => "Python 源码",
            FileTypeCategory.WebSource => "Web 前端",
            FileTypeCategory.PHPSource => "PHP 源码",
            FileTypeCategory.RubySource => "Ruby 源码",
            FileTypeCategory.GoSource => "Go 源码",
            FileTypeCategory.RustSource => "Rust 源码",
            FileTypeCategory.SwiftSource => "Swift/Obj-C",
            FileTypeCategory.ShellScript => "Shell 脚本",
            FileTypeCategory.SQLSource => "SQL",
            FileTypeCategory.AssemblySource => "汇编",
            FileTypeCategory.LuaSource => "Lua 源码",
            FileTypeCategory.PerlSource => "Perl 源码",
            FileTypeCategory.RSource => "R 源码",
            FileTypeCategory.ScalaSource => "Scala 源码",
            FileTypeCategory.HaskellSource => "Haskell 源码",
            FileTypeCategory.ErlangSource => "Erlang/Elixir",
            FileTypeCategory.DartSource => "Dart 源码",
            FileTypeCategory.KotlinSource => "Kotlin 源码",
            FileTypeCategory.OtherSource => "其他源码",
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
            FileTypeCategory.JSON => "JSON",
            FileTypeCategory.XML => "XML",
            FileTypeCategory.YAML => "YAML",
            FileTypeCategory.TOML => "TOML",
            FileTypeCategory.ConfigFile => "配置文件",
            FileTypeCategory.RegistryFile => "注册表",
            FileTypeCategory.Certificate => "证书/密钥",
            FileTypeCategory.SQLiteDatabase => "SQLite 数据库",
            FileTypeCategory.AccessDatabase => "Access 数据库",
            FileTypeCategory.DatabaseBackup => "数据库备份",
            FileTypeCategory.DatabaseData => "数据库数据",
            FileTypeCategory.Model3D => "3D 模型",
            FileTypeCategory.CADFile => "CAD 文件",
            FileTypeCategory.Project3D => "3D 项目",
            FileTypeCategory.Material3D => "3D 材质",
            FileTypeCategory.PointCloud => "点云数据",
            FileTypeCategory.Font => "字体",
            FileTypeCategory.WebFont => "Web 字体",
            FileTypeCategory.BitmapFont => "位图字体",
            FileTypeCategory.GameSave => "游戏存档",
            FileTypeCategory.GameROM => "游戏 ROM",
            FileTypeCategory.GameAsset => "游戏资源",
            FileTypeCategory.GameMod => "游戏模组",
            FileTypeCategory.SystemFile => "系统文件",
            FileTypeCategory.LogFile => "日志文件",
            FileTypeCategory.TempFile => "临时文件",
            FileTypeCategory.CacheFile => "缓存文件",
            FileTypeCategory.Shortcut => "快捷方式",
            FileTypeCategory.RecycleBin => "回收站",
            FileTypeCategory.MATLABFile => "MATLAB",
            FileTypeCategory.MathematicaFile => "Mathematica",
            FileTypeCategory.ScientificData => "科学数据",
            FileTypeCategory.GISData => "GIS 数据",
            FileTypeCategory.VirtualDisk => "虚拟磁盘",
            FileTypeCategory.VirtualMachineConfig => "虚拟机配置",
            FileTypeCategory.ContainerFile => "容器文件",
            FileTypeCategory.Email => "邮件",
            FileTypeCategory.Contact => "通讯录",
            FileTypeCategory.Calendar => "日历",
            FileTypeCategory.VisualStudioProject => "Visual Studio",
            FileTypeCategory.JetBrainsProject => "JetBrains IDE",
            FileTypeCategory.XcodeProject => "Xcode",
            FileTypeCategory.BuildFile => "构建文件",
            FileTypeCategory.GitFile => "Git 文件",
            FileTypeCategory.PatchFile => "补丁文件",
            FileTypeCategory.Torrent => "种子文件",
            FileTypeCategory.Unknown => "未知类型",
            _ => "未知类型"
        };
    }

    /// <summary>
    /// 获取分类的图标名称（用于 UI 显示）。
    /// </summary>
    /// <param name="category">文件类型分类</param>
    /// <returns>图标名称</returns>
    public static string GetIconGlyph(FileTypeCategory category)
    {
        return category switch
        {
            // 文档类
            FileTypeCategory.WordDocument or FileTypeCategory.RichText => "\uE8A5",
            FileTypeCategory.Spreadsheet => "\uE80A",
            FileTypeCategory.Presentation => "\uE8A1",
            FileTypeCategory.PDF => "\uEA90",
            FileTypeCategory.PlainText or FileTypeCategory.Markup => "\uE8A4",
            FileTypeCategory.Ebook => "\uE82D",
            FileTypeCategory.LaTeX => "\uE943",
            FileTypeCategory.Note => "\uE70B",

            // 图像类
            FileTypeCategory.RasterImage or FileTypeCategory.VectorImage or 
            FileTypeCategory.RawImage or FileTypeCategory.HDRImage => "\uEB9F",
            FileTypeCategory.Icon => "\uE91B",
            FileTypeCategory.ImageProject => "\uE8B3",

            // 音频类
            FileTypeCategory.LossyAudio or FileTypeCategory.LosslessAudio or
            FileTypeCategory.Audiobook => "\uE8D6",
            FileTypeCategory.MIDI or FileTypeCategory.AudioProject or
            FileTypeCategory.AudioSample => "\uE90B",
            FileTypeCategory.Playlist => "\uE93E",

            // 视频类
            FileTypeCategory.Video or FileTypeCategory.ProVideo or
            FileTypeCategory.DiscVideo or FileTypeCategory.Animation => "\uE8B2",
            FileTypeCategory.VideoProject => "\uE8B1",
            FileTypeCategory.Subtitle => "\uE8C1",

            // 压缩类
            FileTypeCategory.Archive or FileTypeCategory.PackageArchive or
            FileTypeCategory.SplitArchive => "\uF012",
            FileTypeCategory.DiskImage => "\uE958",

            // 代码类
            FileTypeCategory.CppSource or FileTypeCategory.CSharpSource or
            FileTypeCategory.JavaSource or FileTypeCategory.JavaScriptSource or
            FileTypeCategory.PythonSource or FileTypeCategory.WebSource or
            FileTypeCategory.PHPSource or FileTypeCategory.RubySource or
            FileTypeCategory.GoSource or FileTypeCategory.RustSource or
            FileTypeCategory.SwiftSource or FileTypeCategory.ShellScript or
            FileTypeCategory.SQLSource or FileTypeCategory.AssemblySource or
            FileTypeCategory.LuaSource or FileTypeCategory.PerlSource or
            FileTypeCategory.RSource or FileTypeCategory.ScalaSource or
            FileTypeCategory.HaskellSource or FileTypeCategory.ErlangSource or
            FileTypeCategory.DartSource or FileTypeCategory.KotlinSource or
            FileTypeCategory.OtherSource => "\uE943",

            // 可执行类
            FileTypeCategory.WindowsExecutable or FileTypeCategory.WindowsInstaller or
            FileTypeCategory.MacApplication or FileTypeCategory.LinuxExecutable or
            FileTypeCategory.LinuxPackage or FileTypeCategory.AndroidPackage or
            FileTypeCategory.iOSPackage => "\uE756",
            FileTypeCategory.DynamicLibrary or FileTypeCategory.StaticLibrary => "\uE74C",
            FileTypeCategory.Driver => "\uE964",

            // 数据类
            FileTypeCategory.JSON or FileTypeCategory.XML or
            FileTypeCategory.YAML or FileTypeCategory.TOML => "\uE9D5",
            FileTypeCategory.ConfigFile or FileTypeCategory.RegistryFile => "\uE713",
            FileTypeCategory.Certificate => "\uE8D7",

            // 数据库类
            FileTypeCategory.SQLiteDatabase or FileTypeCategory.AccessDatabase or
            FileTypeCategory.DatabaseBackup or FileTypeCategory.DatabaseData => "\uE968",

            // 3D/CAD 类
            FileTypeCategory.Model3D or FileTypeCategory.CADFile or
            FileTypeCategory.Project3D or FileTypeCategory.Material3D or
            FileTypeCategory.PointCloud => "\uF158",

            // 字体类
            FileTypeCategory.Font or FileTypeCategory.WebFont or
            FileTypeCategory.BitmapFont => "\uE8D2",

            // 游戏类
            FileTypeCategory.GameSave or FileTypeCategory.GameROM or
            FileTypeCategory.GameAsset or FileTypeCategory.GameMod => "\uE7FC",

            // 系统类
            FileTypeCategory.SystemFile or FileTypeCategory.Driver => "\uE770",
            FileTypeCategory.LogFile => "\uE9F9",
            FileTypeCategory.TempFile or FileTypeCategory.CacheFile => "\uE74D",
            FileTypeCategory.Shortcut => "\uE71B",
            FileTypeCategory.RecycleBin => "\uE74D",

            // 科学类
            FileTypeCategory.MATLABFile or FileTypeCategory.MathematicaFile or
            FileTypeCategory.ScientificData => "\uE9D9",
            FileTypeCategory.GISData => "\uE909",

            // 虚拟化类
            FileTypeCategory.VirtualDisk or FileTypeCategory.VirtualMachineConfig => "\uE977",
            FileTypeCategory.ContainerFile => "\uE7B8",

            // 通讯类
            FileTypeCategory.Email => "\uE715",
            FileTypeCategory.Contact => "\uE77B",
            FileTypeCategory.Calendar => "\uE787",

            // 项目类
            FileTypeCategory.VisualStudioProject or FileTypeCategory.JetBrainsProject or
            FileTypeCategory.XcodeProject or FileTypeCategory.BuildFile => "\uE8F1",
            FileTypeCategory.GitFile or FileTypeCategory.PatchFile => "\uE8AD",

            // 其他
            FileTypeCategory.Torrent => "\uE896",
            FileTypeCategory.Unknown => "\uE8A5",
            _ => "\uE8A5"
        };
    }
}


/// <summary>
/// 驱动器信息。
/// </summary>
public record DriveEntry
{
    public required string Name { get; init; }
    public required string RootPath { get; init; }
    public required DriveType DriveType { get; init; }
    public required string VolumeLabel { get; init; }
    public long TotalSize { get; init; }
    public long FreeSpace { get; init; }
    public long UsedSpace => TotalSize - FreeSpace;
    public double UsedPercentage => TotalSize > 0 ? (double)UsedSpace / TotalSize * 100 : 0;
    public bool IsReady { get; init; }
    public string FileSystem { get; init; } = string.Empty;
}

/// <summary>
/// 目录条目。
/// </summary>
public record DirectoryEntry
{
    public required string Name { get; init; }
    public required string FullPath { get; init; }
    public DateTime CreatedTime { get; init; }
    public DateTime ModifiedTime { get; init; }
    public DateTime AccessedTime { get; init; }
    public FileAttributes Attributes { get; init; }
    public bool HasSubdirectories { get; init; }
    public bool IsAccessible { get; init; } = true;
    public bool IsHidden => (Attributes & FileAttributes.Hidden) != 0;
    public bool IsSystem => (Attributes & FileAttributes.System) != 0;
    public bool IsReadOnly => (Attributes & FileAttributes.ReadOnly) != 0;
    public string? ParentPath { get; init; }
}

/// <summary>
/// 文件条目。
/// </summary>
public record FileEntry
{
    public required string Name { get; init; }
    public required string FullPath { get; init; }
    public required string Extension { get; init; }
    public long Size { get; init; }
    public DateTime CreatedTime { get; init; }
    public DateTime ModifiedTime { get; init; }
    public DateTime AccessedTime { get; init; }
    public FileAttributes Attributes { get; init; }
    public FileTypeCategory Category { get; init; }
    public bool IsHidden => (Attributes & FileAttributes.Hidden) != 0;
    public bool IsSystem => (Attributes & FileAttributes.System) != 0;
    public bool IsReadOnly => (Attributes & FileAttributes.ReadOnly) != 0;
    public string? ParentPath { get; init; }
    
    /// <summary>
    /// 获取不带扩展名的文件名。
    /// </summary>
    public string NameWithoutExtension => Path.GetFileNameWithoutExtension(Name);
}

/// <summary>
/// 目录内容。
/// </summary>
public record DirectoryContent
{
    public required string Path { get; init; }
    public required IReadOnlyList<DirectoryEntry> Directories { get; init; }
    public required IReadOnlyList<FileEntry> Files { get; init; }
    public int TotalItems => Directories.Count + Files.Count;
    public int DirectoryCount => Directories.Count;
    public int FileCount => Files.Count;
    public long TotalSize => Files.Sum(f => f.Size);
    public bool IsEmpty => TotalItems == 0;
    public string? ParentPath { get; init; }
    public bool HasParent => !string.IsNullOrEmpty(ParentPath);
}

/// <summary>
/// 文件系统项（目录或文件的基类）。
/// </summary>
public abstract record FileSystemItem
{
    public required string Name { get; init; }
    public required string FullPath { get; init; }
    public DateTime CreatedTime { get; init; }
    public DateTime ModifiedTime { get; init; }
    public FileAttributes Attributes { get; init; }
    public abstract bool IsDirectory { get; }
}

/// <summary>
/// 导航历史记录项。
/// </summary>
public record NavigationHistoryItem
{
    public required string Path { get; init; }
    public DateTime VisitedTime { get; init; } = DateTime.Now;
    public string DisplayName => System.IO.Path.GetFileName(Path) ?? Path;
}

/// <summary>
/// 文件排序方式。
/// </summary>
public enum FileSortBy
{
    Name,
    Size,
    Type,
    DateModified,
    DateCreated,
    Extension
}

/// <summary>
/// 排序方向。
/// </summary>
public enum SortDirection
{
    Ascending,
    Descending
}

/// <summary>
/// 文件视图模式。
/// </summary>
public enum FileViewMode
{
    List,
    Details,
    Tiles,
    Icons,
    SmallIcons,
    LargeIcons
}

/// <summary>
/// 文件筛选选项。
/// </summary>
public record FileFilterOptions
{
    public bool ShowHiddenFiles { get; init; } = false;
    public bool ShowSystemFiles { get; init; } = false;
    public IReadOnlyList<FileTypeCategory>? IncludeCategories { get; init; }
    public IReadOnlyList<FileTypeCategory>? ExcludeCategories { get; init; }
    public IReadOnlyList<string>? IncludeExtensions { get; init; }
    public IReadOnlyList<string>? ExcludeExtensions { get; init; }
    public long? MinSize { get; init; }
    public long? MaxSize { get; init; }
    public DateTime? ModifiedAfter { get; init; }
    public DateTime? ModifiedBefore { get; init; }
    public string? NamePattern { get; init; }
}

/// <summary>
/// 文件操作类型。
/// </summary>
public enum FileOperationType
{
    Copy,
    Move,
    Delete,
    Rename,
    CreateDirectory,
    CreateFile
}

/// <summary>
/// 文件操作结果。
/// </summary>
public record FileOperationResult
{
    public bool Success { get; init; }
    public FileOperationType OperationType { get; init; }
    public required string SourcePath { get; init; }
    public string? DestinationPath { get; init; }
    public string? ErrorMessage { get; init; }
    public Exception? Exception { get; init; }
}

/// <summary>
/// 剪贴板操作类型。
/// </summary>
public enum ClipboardOperation
{
    None,
    Copy,
    Cut
}

/// <summary>
/// 文件剪贴板数据。
/// </summary>
public record FileClipboardData
{
    public ClipboardOperation Operation { get; init; }
    public IReadOnlyList<string> Paths { get; init; } = Array.Empty<string>();
    public bool HasData => Paths.Count > 0 && Operation != ClipboardOperation.None;
}

/// <summary>
/// 特殊文件夹类型。
/// </summary>
public enum SpecialFolderType
{
    Desktop,
    Documents,
    Downloads,
    Music,
    Pictures,
    Videos,
    UserProfile,
    ProgramFiles,
    ProgramFilesX86,
    AppData,
    LocalAppData,
    CommonAppData,
    Temp,
    Recent,
    Favorites,
    StartMenu,
    Startup,
    SendTo,
    Fonts,
    CommonDocuments,
    CommonMusic,
    CommonPictures,
    CommonVideos,
    OneDrive,
    NetworkShortcuts,
    PrinterShortcuts
}

/// <summary>
/// 特殊文件夹信息。
/// </summary>
public record SpecialFolderInfo
{
    public required SpecialFolderType Type { get; init; }
    public required string Path { get; init; }
    public required string DisplayName { get; init; }
    public string IconGlyph { get; init; } = "\uE8B7";
    public bool Exists { get; init; }
}
