# 软迹 (Ruanji)

<p align="center">
  <img src="src/WindowsSoftwareOrganizer/Assets/AppIcon.png" alt="软迹" width="128" height="128">
</p>

<p align="center">
  <strong>Windows 软件管理与迁移工具</strong>
</p>

<p align="center">
  <a href="#功能特性">功能特性</a> •
  <a href="#安装要求">安装要求</a> •
  <a href="#快速开始">快速开始</a> •
  <a href="#使用说明">使用说明</a> •
  <a href="#开发">开发</a>
</p>

---

## 功能特性

### 📋 软件列表
- 自动扫描系统已安装软件
- 智能分类（游戏、办公、开发工具等 50+ 类别）
- 显示软件大小、版本、安装路径
- 支持搜索和筛选
- 软件图标自动提取

### 🚀 软件迁移
- 将软件从 C 盘迁移到其他磁盘
- 自动创建符号链接保持原路径可用
- 支持自定义命名模板
- 可选更新注册表引用
- 文件完整性验证

### 📁 文件管理
- 文件浏览与搜索
- 文件类型统计分析
- 磁盘空间分析
- AI 智能文件分析（需配置 OpenAI API）

### 🧹 系统清理
- 扫描软件残留文件
- 清理临时文件
- 注册表残留检测

### ⚙️ 设置
- OpenAI API 配置（支持多种兼容接口）
- 动态加载可用模型列表
- 界面主题设置

## 安装要求

- Windows 10 版本 1903 (19041) 或更高版本
- Windows 11
- .NET 8.0 Runtime
- Windows App SDK 1.5

## 快速开始

### 从源码构建

```bash
# 克隆仓库
git clone https://github.com/alanbulan/ruanji.git
cd ruanji

# 构建项目
dotnet build -p:Platform=x64

# 运行应用
.\src\WindowsSoftwareOrganizer\bin\x64\Debug\net8.0-windows10.0.19041.0\软迹.exe
```

### 发布版本

```bash
dotnet publish -c Release -p:Platform=x64
```

## 使用说明

### 软件列表
1. 点击「扫描」按钮扫描系统已安装软件
2. 使用搜索框筛选软件
3. 使用类别下拉框按分类筛选
4. 点击软件查看详情
5. 可执行卸载、打开目录、迁移等操作

### 软件迁移
1. 在软件列表中选择要迁移的软件
2. 点击「迁移此软件」
3. 在迁移页面设置目标路径
4. 选择命名模板和链接类型
5. 点击「开始迁移」

### AI 文件分析
1. 进入设置页面配置 OpenAI API
2. 支持 OpenAI、Azure OpenAI、SiliconFlow 等兼容接口
3. 点击「加载模型」获取可用模型列表
4. 在文件管理页面使用 AI 分析功能

## 项目结构

```
ruanji/
├── src/
│   ├── WindowsSoftwareOrganizer/          # WinUI 3 主应用
│   │   ├── Views/                         # 页面视图
│   │   ├── ViewModels/                    # 视图模型
│   │   ├── Converters/                    # 值转换器
│   │   └── Helpers/                       # 辅助类
│   ├── WindowsSoftwareOrganizer.Core/     # 核心业务逻辑
│   │   ├── Interfaces/                    # 接口定义
│   │   └── Models/                        # 数据模型
│   └── WindowsSoftwareOrganizer.Infrastructure/  # 基础设施实现
│       └── Services/                      # 服务实现
└── tests/
    └── WindowsSoftwareOrganizer.Tests/    # 单元测试
```

## 技术栈

- **UI 框架**: WinUI 3 (Windows App SDK 1.5)
- **开发语言**: C# 12 / .NET 8
- **架构模式**: MVVM
- **依赖注入**: Microsoft.Extensions.DependencyInjection
- **MVVM 工具**: CommunityToolkit.Mvvm

## 开发

### 环境要求
- Visual Studio 2022 17.8+ 或 VS Code
- .NET 8.0 SDK
- Windows App SDK 1.5 工作负载

### 构建命令

```bash
# 调试构建
dotnet build -p:Platform=x64

# 发布构建
dotnet publish -c Release -p:Platform=x64

# 运行测试
dotnet test
```

## 许可证

MIT License

## 贡献

欢迎提交 Issue 和 Pull Request！

---

<p align="center">
  Made with ❤️ for Windows users
</p>
