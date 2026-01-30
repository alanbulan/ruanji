using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Infrastructure.Services;

/// <summary>
/// Implementation of ISoftwareClassifier for classifying software.
/// Uses multi-level classification: name patterns → vendor → executable path → install path.
/// </summary>
public class SoftwareClassifier : ISoftwareClassifier
{
    private readonly IConfigurationService _configurationService;

    /// <summary>
    /// Classification rules based on software name patterns.
    /// Patterns are checked with word boundary awareness for better accuracy.
    /// Order matters - Game patterns are checked first to avoid misclassification.
    /// </summary>
    private static readonly Dictionary<SoftwareCategory, string[]> CategoryPatterns = new()
    {
        // 游戏 - Game platforms and popular games (MUST BE FIRST to avoid Tencent vendor match)
        [SoftwareCategory.Game] = new[]
        {
            // Game platforms
            "Steam", "Epic Games", "Origin", "Ubisoft", "Battle.net", "GOG Galaxy",
            "Xbox", "EA App", "Riot", "WeGame", "Garena", "Rockstar Games Launcher",
            "Bethesda.net", "Arc", "Wargaming", "Nexon", "Gameforge",
            
            // Popular games - International
            "League of Legends", "Valorant", "Minecraft", "Roblox", "Genshin Impact",
            "PUBG", "Fortnite", "Dota", "CSGO", "Counter-Strike", "Overwatch",
            "World of Warcraft", "Diablo", "Hearthstone", "StarCraft", "Call of Duty",
            "FIFA", "NBA 2K", "GTA", "Grand Theft Auto", "Cyberpunk", "Elden Ring",
            "Apex Legends", "Destiny", "Warframe", "Path of Exile", "Lost Ark",
            "Final Fantasy", "Monster Hunter", "Resident Evil", "Assassin's Creed",
            "Far Cry", "Watch Dogs", "Rainbow Six", "The Sims", "Battlefield",
            "Need for Speed", "Madden", "NHL", "PES", "eFootball", "Rocket League",
            "Fall Guys", "Among Us", "Terraria", "Stardew Valley", "Hollow Knight",
            "Hades", "Celeste", "Cuphead", "Dead Cells", "Slay the Spire",
            
            // Popular games - Chinese
            "英雄联盟", "原神", "王者荣耀", "和平精英", "穿越火线", "地下城与勇士",
            "绝地求生", "堡垒之夜", "守望先锋", "魔兽世界", "暗黑破坏神", "炉石传说",
            "星际争霸", "使命召唤", "赛博朋克", "艾尔登法环", "崩坏", "明日方舟",
            "阴阳师", "梦幻西游", "大话西游", "剑网", "天涯明月刀", "逆水寒",
            "永劫无间", "三国杀", "斗地主", "欢乐麻将", "QQ游戏", "游戏",
            "DNF", "CF", "LOL", "APEX", "CSGO", "CS2", "Genshin"
        },
        
        // 社交通讯 - Social and communication apps
        [SoftwareCategory.Social] = new[]
        {
            "微信", "WeChat", "QQ", "钉钉", "DingTalk", "飞书", "Feishu", "Lark",
            "企业微信", "WeCom", "Slack", "Teams", "Zoom", "腾讯会议", "Skype"
        },
        
        // 即时通讯 - Instant messaging
        [SoftwareCategory.Messaging] = new[]
        {
            "Telegram", "Discord", "WhatsApp", "Signal", "Line", "Viber",
            "ICQ", "Messenger", "TIM"
        },
        
        // 办公软件 - Office and productivity
        [SoftwareCategory.Office] = new[]
        {
            "Microsoft Office", "Word", "Excel", "PowerPoint", "Outlook",
            "LibreOffice", "OpenOffice", "WPS", "金山", "Notion", "Obsidian",
            "OneNote", "Evernote", "Typora", "印象笔记", "有道云笔记"
        },
        
        // 浏览器 - Web browsers
        [SoftwareCategory.Browser] = new[]
        {
            "Chrome", "Firefox", "Edge", "Opera", "Brave", "Vivaldi",
            "Safari", "Internet Explorer", "Chromium", "360浏览器", "QQ浏览器",
            "搜狗浏览器", "UC浏览器", "猎豹浏览器", "2345浏览器"
        },
        
        // 音乐播放 - Music players
        [SoftwareCategory.Music] = new[]
        {
            "Spotify", "网易云音乐", "QQ音乐", "酷狗音乐", "酷我音乐",
            "Apple Music", "iTunes", "foobar2000", "AIMP", "Winamp",
            "千千音乐", "虾米音乐", "咪咕音乐"
        },
        
        // 视频播放 - Video players
        [SoftwareCategory.Video] = new[]
        {
            "VLC", "PotPlayer", "KMPlayer", "MPC-HC", "Media Player",
            "爱奇艺", "优酷", "腾讯视频", "芒果TV", "哔哩哔哩", "bilibili",
            "迅雷影音", "暴风影音", "射手播放器"
        },
        
        // 影音娱乐（综合）- Media production
        [SoftwareCategory.Media] = new[]
        {
            "Audacity", "OBS", "Streamlabs", "XSplit"
        },
        
        // 图形设计 - Graphics design
        [SoftwareCategory.Graphics] = new[]
        {
            "Adobe Photoshop", "Adobe Illustrator", "Adobe InDesign",
            "CorelDRAW", "Sketch", "Figma", "Canva", "GIMP", "Inkscape",
            "Affinity Designer", "Affinity Photo"
        },
        
        // 图片处理 - Photo editing
        [SoftwareCategory.Photo] = new[]
        {
            "Lightroom", "ACDSee", "FastStone", "IrfanView", "XnView",
            "Paint.NET", "Krita", "美图秀秀", "光影魔术手"
        },
        
        // 3D建模 - 3D modeling
        [SoftwareCategory.Modeling3D] = new[]
        {
            "Blender", "3ds Max", "Maya", "Cinema 4D", "ZBrush",
            "SketchUp", "Rhino", "SolidWorks", "AutoCAD", "Fusion 360"
        },
        
        // 系统工具
        [SoftwareCategory.System] = new[]
        {
            "CCleaner", "Advanced SystemCare", "Wise Care", "Glary Utilities",
            "Revo Uninstaller", "Geek Uninstaller", "IObit Uninstaller",
            "Disk Cleanup", "磁盘清理", "系统优化", "鲁大师", "驱动精灵"
        },
        
        // 安全软件
        [SoftwareCategory.Security] = new[]
        {
            "Windows Defender", "火绒", "360安全卫士", "腾讯电脑管家",
            "金山毒霸", "瑞星", "卡巴斯基", "Kaspersky", "Norton", "McAfee",
            "Avast", "AVG", "Bitdefender", "ESET", "Malwarebytes"
        },
        
        // 杀毒软件
        [SoftwareCategory.Antivirus] = new[]
        {
            "Antivirus", "杀毒", "Anti-Malware"
        },
        
        // 下载工具
        [SoftwareCategory.Download] = new[]
        {
            "迅雷", "Thunder", "IDM", "Internet Download Manager", "Free Download Manager",
            "qBittorrent", "uTorrent", "BitTorrent", "Aria2", "Motrix", "Neat Download Manager"
        },
        
        // 网络工具
        [SoftwareCategory.Network] = new[]
        {
            "Wireshark", "Fiddler", "Charles", "Postman", "Insomnia",
            "NetLimiter", "GlassWire", "网络监控"
        },
        
        // VPN工具
        [SoftwareCategory.VPN] = new[]
        {
            "VPN", "Clash", "V2Ray", "Shadowsocks", "WireGuard", "OpenVPN",
            "NordVPN", "ExpressVPN", "Surfshark"
        },
        
        // 教育学习
        [SoftwareCategory.Education] = new[]
        {
            "学习", "教育", "课堂", "作业", "考试", "Anki", "Quizlet",
            "Duolingo", "有道词典", "金山词霸", "欧路词典", "百词斩"
        },
        
        // 驱动程序
        [SoftwareCategory.Driver] = new[]
        {
            "Driver", "驱动", "NVIDIA", "AMD Radeon", "Intel", "Realtek",
            "GeForce", "显卡驱动", "声卡驱动", "网卡驱动"
        },
        
        // 运行库
        [SoftwareCategory.Runtime] = new[]
        {
            "Runtime", "JRE", "Java Runtime", ".NET Runtime", ".NET Framework",
            "Visual C++", "Redistributable", "DirectX", "OpenAL"
        },
        
        // 集成开发环境
        [SoftwareCategory.IDE] = new[]
        {
            "Visual Studio", "IntelliJ IDEA", "IDEA", "PyCharm", "WebStorm",
            "PhpStorm", "Rider", "CLion", "GoLand", "DataGrip", "Eclipse",
            "NetBeans", "Android Studio", "Xcode", "RubyMine", "AppCode"
        },
        
        // 代码编辑器
        [SoftwareCategory.CodeEditor] = new[]
        {
            "VS Code", "VSCode", "Visual Studio Code", "Sublime Text",
            "Notepad++", "Atom", "Vim", "Neovim", "Emacs", "HBuilder"
        },
        
        // 软件开发工具包
        [SoftwareCategory.SDK] = new[]
        {
            "SDK", "JDK", "Java Development Kit", "Android SDK", "Windows SDK",
            ".NET SDK", "Flutter SDK", "Dart SDK"
        },
        
        // 开发辅助工具
        [SoftwareCategory.DevTool] = new[]
        {
            "Postman", "Insomnia", "Fiddler", "CMake", "Gradle", "Maven",
            "npm", "yarn", "pnpm", "Composer", "pip", "NuGet",
            "WinMerge", "Beyond Compare", "Terminal", "PowerShell", "WSL"
        },
        
        // 版本控制
        [SoftwareCategory.VersionControl] = new[]
        {
            "Git", "GitHub Desktop", "GitLab", "Sourcetree", "TortoiseGit",
            "TortoiseSVN", "Fork", "GitKraken", "SmartGit"
        },
        
        // 数据库
        [SoftwareCategory.Database] = new[]
        {
            "SQL Server", "MySQL", "PostgreSQL", "MongoDB", "Redis", "SQLite",
            "Oracle", "MariaDB", "DBeaver", "HeidiSQL", "pgAdmin", "Navicat",
            "Azure Data Studio", "DataGrip"
        },
        
        // 虚拟化
        [SoftwareCategory.Virtualization] = new[]
        {
            "VMware", "VirtualBox", "Hyper-V", "Docker", "Parallels",
            "QEMU", "Vagrant", "Podman", "WSL"
        },
        
        // 实用工具
        [SoftwareCategory.Utility] = new[]
        {
            "Everything", "Listary", "Wox", "PowerToys", "AutoHotkey",
            "Snipaste", "Ditto", "f.lux", "Flux"
        },
        
        // 压缩解压
        [SoftwareCategory.Compression] = new[]
        {
            "7-Zip", "WinRAR", "WinZip", "Bandizip", "PeaZip",
            "好压", "360压缩", "2345压缩"
        },
        
        // 文件管理
        [SoftwareCategory.FileManager] = new[]
        {
            "Total Commander", "Directory Opus", "XYplorer", "Q-Dir",
            "FreeCommander", "Multi Commander", "Files"
        },
        
        // 备份恢复
        [SoftwareCategory.Backup] = new[]
        {
            "Backup", "备份", "Acronis", "EaseUS", "Macrium Reflect",
            "AOMEI Backupper", "Veeam", "Carbonite"
        },
        
        // 远程控制
        [SoftwareCategory.RemoteDesktop] = new[]
        {
            "TeamViewer", "AnyDesk", "Remote Desktop", "向日葵", "ToDesk",
            "RustDesk", "Parsec", "Chrome Remote Desktop", "RDP"
        },
        
        // 截图录屏
        [SoftwareCategory.Screenshot] = new[]
        {
            "Snipaste", "ShareX", "Greenshot", "Lightshot", "PicPick",
            "Snagit", "FastStone Capture", "截图", "录屏", "Bandicam",
            "Camtasia", "ScreenToGif", "OBS Studio"
        },
        
        // 笔记软件
        [SoftwareCategory.Notes] = new[]
        {
            "Notion", "Obsidian", "OneNote", "Evernote", "印象笔记",
            "有道云笔记", "为知笔记", "Joplin", "Logseq", "Roam Research"
        },
        
        // 阅读器
        [SoftwareCategory.Reader] = new[]
        {
            "Adobe Reader", "Acrobat", "Foxit Reader", "福昕阅读器",
            "SumatraPDF", "PDF-XChange", "Calibre"
        },
        
        // 电子书
        [SoftwareCategory.Ebook] = new[]
        {
            "Kindle", "微信读书", "多看阅读", "掌阅", "Kobo", "Calibre"
        },
        
        // 翻译工具
        [SoftwareCategory.Translation] = new[]
        {
            "翻译", "Translate", "DeepL", "有道翻译", "谷歌翻译",
            "百度翻译", "腾讯翻译", "彩云小译"
        },
        
        // 输入法
        [SoftwareCategory.InputMethod] = new[]
        {
            "输入法", "Input Method", "搜狗输入法", "百度输入法", "QQ输入法",
            "微软拼音", "谷歌拼音", "手心输入法", "讯飞输入法"
        },
        
        // 云存储
        [SoftwareCategory.CloudStorage] = new[]
        {
            "OneDrive", "Google Drive", "Dropbox", "iCloud", "百度网盘",
            "阿里云盘", "坚果云", "腾讯微云", "天翼云盘", "115网盘"
        },
        
        // 邮件客户端
        [SoftwareCategory.Email] = new[]
        {
            "Outlook", "Thunderbird", "Foxmail", "Mailbird", "eM Client",
            "网易邮箱大师", "QQ邮箱"
        },
        
        // 财务软件
        [SoftwareCategory.Finance] = new[]
        {
            "财务", "Finance", "记账", "用友", "金蝶", "QuickBooks",
            "支付宝", "微信支付", "银行"
        },
        
        // 健康健身
        [SoftwareCategory.Health] = new[]
        {
            "健康", "Health", "健身", "Fitness", "Keep", "运动"
        },
        
        // 天气
        [SoftwareCategory.Weather] = new[]
        {
            "天气", "Weather", "墨迹天气", "彩云天气"
        },
        
        // 地图导航
        [SoftwareCategory.Maps] = new[]
        {
            "地图", "Maps", "导航", "高德地图", "百度地图", "腾讯地图",
            "Google Maps", "Google Earth"
        },
        
        // 购物
        [SoftwareCategory.Shopping] = new[]
        {
            "购物", "Shopping", "淘宝", "京东", "拼多多", "天猫", "Amazon"
        },
        
        // 新闻资讯
        [SoftwareCategory.News] = new[]
        {
            "新闻", "News", "今日头条", "腾讯新闻", "网易新闻", "澎湃新闻"
        },
        
        // 直播平台
        [SoftwareCategory.Streaming] = new[]
        {
            "直播", "Streaming", "斗鱼", "虎牙", "Twitch", "抖音", "快手",
            "B站直播", "YY直播"
        },
        
        // AI工具
        [SoftwareCategory.AI] = new[]
        {
            "ChatGPT", "Claude", "Copilot", "AI", "人工智能", "GPT",
            "Midjourney", "Stable Diffusion", "DALL-E", "文心一言", "通义千问"
        }
    };

    /// <summary>
    /// Known vendor to category mappings (partial match supported).
    /// Note: Vendor matching is done AFTER name and path matching to avoid false positives.
    /// For example, Tencent makes both games and social apps, so we check game patterns first.
    /// </summary>
    private static readonly (string Pattern, SoftwareCategory Category, bool ExactMatch)[] VendorPatterns = new[]
    {
        // IDE vendors - high confidence
        ("JetBrains", SoftwareCategory.IDE, false),
        
        // Graphics vendors - high confidence
        ("Adobe", SoftwareCategory.Graphics, false),
        ("Corel", SoftwareCategory.Graphics, false),
        
        // Game vendors - high confidence (these are game-only companies)
        ("Valve", SoftwareCategory.Game, false),
        ("Epic Games", SoftwareCategory.Game, false),
        ("Blizzard", SoftwareCategory.Game, false),
        ("Electronic Arts", SoftwareCategory.Game, false),
        ("Ubisoft", SoftwareCategory.Game, false),
        ("Riot Games", SoftwareCategory.Game, false),
        ("miHoYo", SoftwareCategory.Game, false),
        ("米哈游", SoftwareCategory.Game, false),
        ("CD PROJEKT", SoftwareCategory.Game, false),
        ("Rockstar Games", SoftwareCategory.Game, false),
        ("Bethesda", SoftwareCategory.Game, false),
        ("BANDAI NAMCO", SoftwareCategory.Game, false),
        ("CAPCOM", SoftwareCategory.Game, false),
        ("SEGA", SoftwareCategory.Game, false),
        ("Square Enix", SoftwareCategory.Game, false),
        ("2K Games", SoftwareCategory.Game, false),
        ("Take-Two", SoftwareCategory.Game, false),
        ("Paradox", SoftwareCategory.Game, false),
        
        // Browser vendors - high confidence
        ("Mozilla", SoftwareCategory.Browser, false),
        ("Opera Software", SoftwareCategory.Browser, false),
        ("Brave Software", SoftwareCategory.Browser, false),
        
        // Note: Tencent and ByteDance are NOT included here because they make both games and social apps
        // Their products should be classified by name patterns instead
        
        // Communication vendors - specific products only
        ("Zoom Video", SoftwareCategory.Social, false),
        ("Slack Technologies", SoftwareCategory.Social, false),
        
        // Music vendors - high confidence
        ("Spotify", SoftwareCategory.Music, false),
        
        // Virtualization vendors - high confidence
        ("Docker", SoftwareCategory.Virtualization, false),
        ("VMware", SoftwareCategory.Virtualization, false),
        
        // Version control vendors - high confidence
        ("GitHub", SoftwareCategory.VersionControl, false),
        ("GitLab", SoftwareCategory.VersionControl, false),
        ("Atlassian", SoftwareCategory.VersionControl, false),
        
        // Security vendors - high confidence
        ("Kaspersky", SoftwareCategory.Security, false),
        ("Norton", SoftwareCategory.Security, false),
        ("McAfee", SoftwareCategory.Security, false),
        ("Avast", SoftwareCategory.Security, false),
        ("AVG", SoftwareCategory.Security, false),
        ("Bitdefender", SoftwareCategory.Security, false),
        ("ESET", SoftwareCategory.Security, false),
        ("火绒", SoftwareCategory.Security, false),
        ("Malwarebytes", SoftwareCategory.Security, false),
        ("Trend Micro", SoftwareCategory.Security, false),
        
        // Compression vendors - high confidence
        ("RARLAB", SoftwareCategory.Compression, false),
        ("7-Zip", SoftwareCategory.Compression, false),
        
        // Cloud storage vendors - high confidence
        ("Dropbox", SoftwareCategory.CloudStorage, false),
        
        // Remote desktop vendors - high confidence
        ("TeamViewer", SoftwareCategory.RemoteDesktop, false),
        ("AnyDesk", SoftwareCategory.RemoteDesktop, false),
    };

    /// <summary>
    /// Executable name patterns for classification.
    /// These are checked when the software name doesn't match any pattern.
    /// </summary>
    private static readonly (string Pattern, SoftwareCategory Category)[] ExecutablePatterns = new[]
    {
        // IDE executables
        ("idea64", SoftwareCategory.IDE),
        ("idea", SoftwareCategory.IDE),
        ("pycharm64", SoftwareCategory.IDE),
        ("pycharm", SoftwareCategory.IDE),
        ("webstorm64", SoftwareCategory.IDE),
        ("webstorm", SoftwareCategory.IDE),
        ("phpstorm64", SoftwareCategory.IDE),
        ("phpstorm", SoftwareCategory.IDE),
        ("rider64", SoftwareCategory.IDE),
        ("rider", SoftwareCategory.IDE),
        ("clion64", SoftwareCategory.IDE),
        ("clion", SoftwareCategory.IDE),
        ("goland64", SoftwareCategory.IDE),
        ("goland", SoftwareCategory.IDE),
        ("datagrip64", SoftwareCategory.IDE),
        ("datagrip", SoftwareCategory.IDE),
        ("rubymine64", SoftwareCategory.IDE),
        ("rubymine", SoftwareCategory.IDE),
        ("devenv", SoftwareCategory.IDE), // Visual Studio
        ("eclipse", SoftwareCategory.IDE),
        
        // Code editors
        ("code", SoftwareCategory.CodeEditor), // VS Code
        ("sublime_text", SoftwareCategory.CodeEditor),
        ("notepad++", SoftwareCategory.CodeEditor),
        
        // Browsers
        ("chrome", SoftwareCategory.Browser),
        ("firefox", SoftwareCategory.Browser),
        ("msedge", SoftwareCategory.Browser),
        ("opera", SoftwareCategory.Browser),
        ("brave", SoftwareCategory.Browser),
        
        // Games
        ("steam", SoftwareCategory.Game),
        ("epicgameslauncher", SoftwareCategory.Game),
        ("origin", SoftwareCategory.Game),
        ("upc", SoftwareCategory.Game), // Ubisoft Connect
        
        // Social
        ("wechat", SoftwareCategory.Social),
        ("weixin", SoftwareCategory.Social),
        ("qq", SoftwareCategory.Social),
        ("dingtalk", SoftwareCategory.Social),
        ("slack", SoftwareCategory.Social),
        ("teams", SoftwareCategory.Social),
        ("zoom", SoftwareCategory.Social),
        
        // Messaging
        ("telegram", SoftwareCategory.Messaging),
        ("discord", SoftwareCategory.Messaging),
        
        // Media
        ("vlc", SoftwareCategory.Video),
        ("potplayer", SoftwareCategory.Video),
        ("spotify", SoftwareCategory.Music),
        
        // Virtualization
        ("docker", SoftwareCategory.Virtualization),
        ("vmware", SoftwareCategory.Virtualization),
        ("virtualbox", SoftwareCategory.Virtualization),
        
        // Version control
        ("git", SoftwareCategory.VersionControl),
        ("sourcetree", SoftwareCategory.VersionControl),
        ("gitkraken", SoftwareCategory.VersionControl),
        
        // Database
        ("dbeaver", SoftwareCategory.Database),
        ("navicat", SoftwareCategory.Database),
        ("heidisql", SoftwareCategory.Database),
        
        // Remote desktop
        ("teamviewer", SoftwareCategory.RemoteDesktop),
        ("anydesk", SoftwareCategory.RemoteDesktop),
        ("todesk", SoftwareCategory.RemoteDesktop),
        
        // Screenshot
        ("snipaste", SoftwareCategory.Screenshot),
        ("sharex", SoftwareCategory.Screenshot),
        ("obs64", SoftwareCategory.Screenshot),
        ("obs32", SoftwareCategory.Screenshot),
    };

    public SoftwareClassifier(IConfigurationService configurationService)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
    }

    /// <inheritdoc />
    public SoftwareCategory Classify(SoftwareEntry entry)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        // Priority 1: Check name patterns (most reliable)
        var nameCategory = ClassifyByName(entry.Name);
        if (nameCategory != SoftwareCategory.Other)
        {
            return nameCategory;
        }

        // Priority 2: Check install path for hints (before vendor to catch games correctly)
        var pathCategory = ClassifyByPath(entry.InstallPath);
        if (pathCategory != SoftwareCategory.Other)
        {
            return pathCategory;
        }

        // Priority 3: Check executable name patterns
        if (!string.IsNullOrWhiteSpace(entry.ExecutablePath))
        {
            var exeName = Path.GetFileNameWithoutExtension(entry.ExecutablePath).ToLowerInvariant();
            
            // Check specific executable patterns first
            foreach (var (pattern, category) in ExecutablePatterns)
            {
                if (exeName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    return category;
                }
            }
            
            // Then try general name classification on exe name
            var exeCategory = ClassifyByName(exeName);
            if (exeCategory != SoftwareCategory.Other)
            {
                return exeCategory;
            }
        }

        // Priority 4: Check vendor patterns (last because it can cause false positives like Tencent games)
        if (!string.IsNullOrWhiteSpace(entry.Vendor))
        {
            var vendorCategory = ClassifyByVendor(entry.Vendor);
            if (vendorCategory != SoftwareCategory.Other)
            {
                return vendorCategory;
            }
        }

        return SoftwareCategory.Other;
    }

    /// <summary>
    /// Classifies software by vendor name using pattern matching.
    /// </summary>
    private static SoftwareCategory ClassifyByVendor(string vendor)
    {
        if (string.IsNullOrWhiteSpace(vendor))
            return SoftwareCategory.Other;

        foreach (var (pattern, category, exactMatch) in VendorPatterns)
        {
            if (exactMatch)
            {
                if (vendor.Equals(pattern, StringComparison.OrdinalIgnoreCase))
                    return category;
            }
            else
            {
                if (vendor.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    return category;
            }
        }

        return SoftwareCategory.Other;
    }

    /// <summary>
    /// Classifies software by name using pattern matching.
    /// </summary>
    private static SoftwareCategory ClassifyByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return SoftwareCategory.Other;

        foreach (var (category, patterns) in CategoryPatterns)
        {
            foreach (var pattern in patterns)
            {
                // Use word boundary check for short patterns to avoid false positives
                if (pattern.Length <= 3)
                {
                    // For short patterns, require word boundary
                    if (IsWordMatch(name, pattern))
                        return category;
                }
                else
                {
                    // For longer patterns, simple contains is fine
                    if (name.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                        return category;
                }
            }
        }

        return SoftwareCategory.Other;
    }

    /// <summary>
    /// Checks if a pattern matches as a whole word in the text.
    /// </summary>
    private static bool IsWordMatch(string text, string pattern)
    {
        var textLower = text.ToLowerInvariant();
        var patternLower = pattern.ToLowerInvariant();
        
        int index = 0;
        while ((index = textLower.IndexOf(patternLower, index, StringComparison.Ordinal)) >= 0)
        {
            bool startOk = index == 0 || !char.IsLetterOrDigit(textLower[index - 1]);
            bool endOk = index + patternLower.Length >= textLower.Length || 
                         !char.IsLetterOrDigit(textLower[index + patternLower.Length]);
            
            if (startOk && endOk)
                return true;
            
            index++;
        }
        
        return false;
    }

    private static SoftwareCategory ClassifyByPath(string? installPath)
    {
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return SoftwareCategory.Other;
        }

        var pathLower = installPath.ToLowerInvariant();

        // Game paths - CHECK FIRST to avoid vendor misclassification
        if (pathLower.Contains("\\steam\\") || pathLower.Contains("\\steamapps\\"))
            return SoftwareCategory.Game;

        if (pathLower.Contains("\\games\\") || pathLower.Contains("\\game\\") || 
            pathLower.Contains("\\epic games\\") || pathLower.Contains("\\wegame"))
            return SoftwareCategory.Game;

        if (pathLower.Contains("\\origin\\") || pathLower.Contains("\\ubisoft\\") ||
            pathLower.Contains("\\riot games\\") || pathLower.Contains("\\blizzard\\"))
            return SoftwareCategory.Game;

        // WeGameApps is a game directory
        if (pathLower.Contains("\\wegameapps\\"))
            return SoftwareCategory.Game;

        // SDK paths
        if (pathLower.Contains("\\sdk\\") || pathLower.Contains("\\sdks\\"))
            return SoftwareCategory.SDK;

        // Runtime/Language paths
        if (pathLower.Contains("\\jdk") || pathLower.Contains("\\java\\") || pathLower.Contains("\\jre"))
            return SoftwareCategory.Runtime;

        if (pathLower.Contains("\\nodejs") || pathLower.Contains("\\node\\") || pathLower.Contains("\\npm"))
            return SoftwareCategory.Runtime;

        if (pathLower.Contains("\\python") || pathLower.Contains("\\anaconda") || pathLower.Contains("\\miniconda"))
            return SoftwareCategory.Runtime;

        if (pathLower.Contains("\\.net") || pathLower.Contains("\\dotnet"))
            return SoftwareCategory.Runtime;

        // IDE paths
        if (pathLower.Contains("\\jetbrains\\") || pathLower.Contains("\\intellij") || 
            pathLower.Contains("\\pycharm") || pathLower.Contains("\\webstorm") ||
            pathLower.Contains("\\rider") || pathLower.Contains("\\clion") ||
            pathLower.Contains("\\goland") || pathLower.Contains("\\datagrip") ||
            pathLower.Contains("\\phpstorm") || pathLower.Contains("\\rubymine"))
            return SoftwareCategory.IDE;

        // Development tools
        if (pathLower.Contains("\\git\\") || pathLower.Contains("\\github\\"))
            return SoftwareCategory.VersionControl;

        if (pathLower.Contains("\\docker\\"))
            return SoftwareCategory.Virtualization;

        if (pathLower.Contains("\\vmware\\") || pathLower.Contains("\\virtualbox\\"))
            return SoftwareCategory.Virtualization;

        // Adobe products
        if (pathLower.Contains("\\adobe\\"))
            return SoftwareCategory.Graphics;

        return SoftwareCategory.Other;
    }

    /// <inheritdoc />
    public IReadOnlyList<RelatedDirectory> FindRelatedDirectories(SoftwareEntry entry)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        var directories = new List<RelatedDirectory>();

        if (Directory.Exists(entry.InstallPath))
        {
            directories.Add(new RelatedDirectory
            {
                Path = entry.InstallPath,
                Type = DirectoryType.Install,
                SizeBytes = CalculateDirectorySize(entry.InstallPath)
            });
        }

        var appDataLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appDataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        var searchNames = GetSearchNames(entry);

        foreach (var searchName in searchNames)
        {
            FindAndAddDirectory(directories, appDataLocal, searchName, DirectoryType.Cache);
            FindAndAddDirectory(directories, appDataRoaming, searchName, DirectoryType.Config);
            FindAndAddDirectory(directories, programData, searchName, DirectoryType.Data);
        }

        FindCacheDirectories(directories, entry);

        return directories.AsReadOnly();
    }

    private static List<string> GetSearchNames(SoftwareEntry entry)
    {
        var names = new List<string> { entry.Name };

        if (!string.IsNullOrWhiteSpace(entry.Vendor))
        {
            names.Add(entry.Vendor);
            names.Add(Path.Combine(entry.Vendor, entry.Name));
        }

        var simpleName = entry.Name.Replace(" ", "");
        if (simpleName != entry.Name)
        {
            names.Add(simpleName);
        }

        return names;
    }

    private static void FindAndAddDirectory(
        List<RelatedDirectory> directories,
        string basePath,
        string searchName,
        DirectoryType type)
    {
        try
        {
            var fullPath = Path.Combine(basePath, searchName);
            if (Directory.Exists(fullPath) && !directories.Any(d => d.Path.Equals(fullPath, StringComparison.OrdinalIgnoreCase)))
            {
                directories.Add(new RelatedDirectory
                {
                    Path = fullPath,
                    Type = type,
                    SizeBytes = CalculateDirectorySize(fullPath)
                });
            }
        }
        catch { }
    }

    private static void FindCacheDirectories(List<RelatedDirectory> directories, SoftwareEntry entry)
    {
        if (!Directory.Exists(entry.InstallPath))
            return;

        var cachePatterns = new[] { "cache", "Cache", "temp", "Temp", "tmp", "logs", "Logs", "log" };

        try
        {
            foreach (var subDir in Directory.GetDirectories(entry.InstallPath))
            {
                var dirName = Path.GetFileName(subDir);
                if (cachePatterns.Any(p => dirName.Equals(p, StringComparison.OrdinalIgnoreCase)))
                {
                    var type = dirName.ToLowerInvariant() switch
                    {
                        "cache" => DirectoryType.Cache,
                        "temp" or "tmp" => DirectoryType.Temp,
                        "logs" or "log" => DirectoryType.Log,
                        _ => DirectoryType.Cache
                    };

                    if (!directories.Any(d => d.Path.Equals(subDir, StringComparison.OrdinalIgnoreCase)))
                    {
                        directories.Add(new RelatedDirectory
                        {
                            Path = subDir,
                            Type = type,
                            SizeBytes = CalculateDirectorySize(subDir)
                        });
                    }
                }
            }
        }
        catch { }
    }

    private static long CalculateDirectorySize(string path)
    {
        long size = 0;

        try
        {
            foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                try
                {
                    size += new FileInfo(file).Length;
                }
                catch { }
            }
        }
        catch { }

        return size;
    }

    /// <inheritdoc />
    public async Task SaveUserClassificationAsync(string softwareId, SoftwareCategory category)
    {
        if (string.IsNullOrWhiteSpace(softwareId))
        {
            throw new ArgumentException("软件ID不能为空", nameof(softwareId));
        }

        var config = await _configurationService.GetConfigurationAsync();
        var newClassifications = new Dictionary<string, SoftwareCategory>(config.UserClassifications)
        {
            [softwareId] = category
        };
        var newConfig = config with { UserClassifications = newClassifications };
        await _configurationService.SaveConfigurationAsync(newConfig);
    }

    /// <inheritdoc />
    public async Task<SoftwareCategory?> GetUserClassificationAsync(string softwareId)
    {
        if (string.IsNullOrWhiteSpace(softwareId))
        {
            return null;
        }

        var config = await _configurationService.GetConfigurationAsync();
        return config.UserClassifications.TryGetValue(softwareId, out var category)
            ? category
            : null;
    }
}
