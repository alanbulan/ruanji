# 构建目标与操作验证

[项目首页](./README.md) · [主工程](./src/WindowsSoftwareOrganizer/WindowsSoftwareOrganizer.csproj)

## 按项目文件理解环境

当前主工程声明如下，不应把不同概念写成同一个最低系统版本：

| 配置 | 文件中的值 | 含义 |
| --- | --- | --- |
| TargetFramework | `net8.0-windows10.0.19041.0` | .NET 与 Windows API 编译目标 |
| TargetPlatformMinVersion | `10.0.17763.0` | 工程声明的最低平台版本，不等于本轮实机验收结论 |
| Platforms | `x86;x64;ARM64` | 可选构建平台，不能由此断言所有安装资产已经发布 |
| WindowsAppSDKSelfContained | `true` | Windows App SDK 的打包设置，不等于所有 .NET 运行时配置都相同 |
| WindowsPackageType | `None` | 当前主工程使用非 MSIX 包形式 |

首页旧文字“1903 (19041)”混用了版本标签和构建编号。排错时直接核对上述工程值、所选发布配置和实际目标系统，避免靠这句旧描述选择环境。

## 源码构建

使用具备 WinUI、.NET 8 SDK 与对应 Windows SDK 工具的 Windows 开发环境。先显式指定项目和平台：

```powershell
dotnet build .\src\WindowsSoftwareOrganizer\WindowsSoftwareOrganizer.csproj -p:Platform=x64
```

发布应先审阅工程引用的 `win-$(Platform).pubxml` 以及实际输出位置；不要把 `dotnet publish` 的成功理解为系统安装、卸载和升级已验收。当前依赖固定了 Windows App SDK 1.5 与其他历史版本，本次没有升级或重新打包。

## 首次验证使用可丢弃目录

软件迁移、清理、卸载和注册表操作可能改变真实系统。首先建立虚构的文件树与备份，在非系统目录测试：空目录、重名目标、无权限、磁盘不足、取消、失败恢复及重复执行。确认符号链接和目录联接不会导致清理越界。

不要为了让界面有结果而操作 Windows 系统目录、正在运行的软件目录或用户真实文档。AI 分析也可能发送文件信息到配置的服务，应先核对发送范围，测试使用脱敏样例。

| 层次 | 应保留的证据 |
| --- | --- |
| 编译 | 工具版本、平台、完整输出与错误 |
| 单元测试 | 只针对可控文件系统和模拟数据的结果 |
| 桌面交互 | 扫描、取消、错误反馈、恢复和权限提示 |
| 迁移验收 | 操作前后文件清单、校验与可执行的恢复步骤 |

本次读取了 README 和主 csproj，未运行 Windows、调用清理/卸载功能、修改注册表或迁移用户文件。原 README、图标、许可及业务源码均保留。
