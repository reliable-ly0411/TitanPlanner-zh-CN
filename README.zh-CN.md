# TitanPlanner-zh-CN 中文双版本

基于 Titan-Dynamics/TitanPlanner，在保留原项目许可和署名的基础上维护简体中文体验、Windows 构建及自动上游同步。见 [COPYING.txt](COPYING.txt)、[上游说明](README.md)。

## 双上游差异数字与比较

以下数字直接读取 GitHub compare API：`ahead` 表示该汉化线领先镜像的提交数，`behind` 表示落后镜像的提交数。每列镜像为 base，每行汉化线为 head；不是未翻译条数。

| 汉化线 | 相对 TitanPlanner 镜像 `Titanmain` | 相对 MissionPlanner 镜像 `missionmain` |
| --- | --- | --- |
| `zh-CN` | [![zh-CN 相对 Titanmain ahead](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.github.com%2Frepos%2Freliable-ly0411%2FTitanPlanner-zh-CN%2Fcompare%2FTitanmain...zh-CN&query=%24.ahead_by&label=ahead&cacheSeconds=300&color=blue)](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/Titanmain...zh-CN) [![zh-CN 相对 Titanmain behind](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.github.com%2Frepos%2Freliable-ly0411%2FTitanPlanner-zh-CN%2Fcompare%2FTitanmain...zh-CN&query=%24.behind_by&label=behind&cacheSeconds=300&color=orange)](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/Titanmain...zh-CN) | [![zh-CN 相对 missionmain ahead](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.github.com%2Frepos%2Freliable-ly0411%2FTitanPlanner-zh-CN%2Fcompare%2Fmissionmain...zh-CN&query=%24.ahead_by&label=ahead&cacheSeconds=300&color=blue)](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/missionmain...zh-CN) [![zh-CN 相对 missionmain behind](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.github.com%2Frepos%2Freliable-ly0411%2FTitanPlanner-zh-CN%2Fcompare%2Fmissionmain...zh-CN&query=%24.behind_by&label=behind&cacheSeconds=300&color=orange)](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/missionmain...zh-CN) |
| `zh-CN-dev` | [![zh-CN-dev 相对 Titanmain ahead](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.github.com%2Frepos%2Freliable-ly0411%2FTitanPlanner-zh-CN%2Fcompare%2FTitanmain...zh-CN-dev&query=%24.ahead_by&label=ahead&cacheSeconds=300&color=blue)](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/Titanmain...zh-CN-dev) [![zh-CN-dev 相对 Titanmain behind](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.github.com%2Frepos%2Freliable-ly0411%2FTitanPlanner-zh-CN%2Fcompare%2FTitanmain...zh-CN-dev&query=%24.behind_by&label=behind&cacheSeconds=300&color=orange)](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/Titanmain...zh-CN-dev) | [![zh-CN-dev 相对 missionmain ahead](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.github.com%2Frepos%2Freliable-ly0411%2FTitanPlanner-zh-CN%2Fcompare%2Fmissionmain...zh-CN-dev&query=%24.ahead_by&label=ahead&cacheSeconds=300&color=blue)](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/missionmain...zh-CN-dev) [![zh-CN-dev 相对 missionmain behind](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.github.com%2Frepos%2Freliable-ly0411%2FTitanPlanner-zh-CN%2Fcompare%2Fmissionmain...zh-CN-dev&query=%24.behind_by&label=behind&cacheSeconds=300&color=orange)](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/missionmain...zh-CN-dev) |

数字使用 Shields 动态徽章，请求缓存 300 秒，GitHub 图片代理也可能缓存，因此不是瞬时实时数据；服务不提供可在徽章中可靠显示的取数时间。限流或服务异常时可能显示错误或旧缓存，不能把它当作零。点击数字或下列比较链接可核对 GitHub 当前结果。镜像本身按同步流程刷新，不保证始终等于上游此刻的最新提交。

以下链接在打开或刷新时按 GitHub 当前分支计算，不是写死的领先/落后数字。

| 汉化线 | 相对 TitanPlanner 镜像 | 相对 MissionPlanner 镜像 |
| --- | --- | --- |
| 稳定 `zh-CN` | [本线新增](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/Titanmain...zh-CN) · [镜像侧新增](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/zh-CN...Titanmain) | [本线新增](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/missionmain...zh-CN) · [镜像侧新增](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/zh-CN...missionmain) |
| 开发 `zh-CN-dev` | [本线新增](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/Titanmain...zh-CN-dev) · [镜像侧新增](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/zh-CN-dev...Titanmain) | [本线新增](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/missionmain...zh-CN-dev) · [镜像侧新增](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/zh-CN-dev...missionmain) |

- “本线新增”以镜像为 base、汉化线为 compare；“镜像侧新增”反向比较，查看尚未进入该汉化线历史的提交。
- 三点比较的文件差异从共同祖先算到 compare 分支，不是两个最新文件树的逐项差异。提交数包含合并、流程与文档，不能当作未翻译条目或缺陷数量。
- `Titanmain` 是直接上游 TitanPlanner 的纯镜像；`missionmain` 是 MissionPlanner 的纯镜像。相对 MissionPlanner 的差异还包含 TitanPlanner 自身功能，不能全算作汉化改动，也不表示应直接合并 MissionPlanner。
- 比较针对本仓镜像的当前状态；镜像通过定时或手动同步刷新，并非上游一提交就自动实时同步。同步情况见 [Actions](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/actions) 和 [同步流程](AUTOMATION.zh-CN.md)。两个镜像不加入本仓 README 改动。

## 选择界面语言

- **中文(简体)**：原版 `zh-Hans`，69 个原版资源文件按基线哈希保留。
- **中文(简体)2**：增强 `zh-CN`，补齐主界面、规划、设置、日志等资源和动态文本。

首次启动且没有语言偏好时默认使用增强版；已有偏好保留。改变语言后保存，下次启动生效，不关闭当前会话；保存失败恢复原选择。无效语言配置会安全回退为增强简体中文。

新版修正“姿态”“无遥控接收机”等语境译法，设置页按可用宽度分为一至三列，文字控件按内容调整大小。MAVLink 参数、枚举、协议字段、数值格式保留原值。

## 获取和更新

本仓库 [Releases](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/releases) 提供 Windows 安装版和便携版。优先核对具体发布的源码提交、附件与已知限制；历史发布可能同时存在正式/预发布标记，不能仅凭 Latest 标记认定已完成实机验收。

- **MSI 安装版**：适用于 64 位 Windows，安装到当前用户的 `%LOCALAPPDATA%\Programs\TitanPlanner-zh-CN`，并创建开始菜单入口；可通过系统“应用和功能”卸载，保留个人配置和日志。安装界面为简体中文，需要 .NET Framework 4.7.2 或更新版本。
- **ZIP 便携版**：解压完整 ZIP 到新目录，运行 `MissionPlanner.exe`，保留全部中文卫星程序集及原生库。

两种格式均包含原版中文和“中文(简体)2”。当前打包约定为固定附件名 `TitanPlanner-zh-CN.msi` 和 `TitanPlanner-zh-CN.zip`，发布标题、不可变标签及包内构建信息用于区分版本与源码提交。历史附件曾使用带版本和提交的名称；校验时使用该次发布实际列出的文件名及 SHA-256，不能将不同发布的清单混用。

应用内更新入口也指向本仓库，不再用上游二进制覆盖中文版本。自动发布仍需真实飞控、飞行、高 DPI 与完整功能验收；不能把通过单元测试等同于飞行验证。

## 中国区 30 米离线高程

独立数据包、覆盖范围、校验及安装说明见 [中国区 30 米高程包](data/terrain/README.zh-CN.md)。地形数据通过独立 Release 发布，不包含在程序便携包中。

## 用 VS Code 构建

需要 Git、独立 .NET 8 SDK；Python 静态校验需要 Python 3.12。无需 Visual Studio IDE。

```powershell
git clone https://github.com/reliable-ly0411/TitanPlanner-zh-CN.git
cd TitanPlanner-zh-CN
git submodule update --init --recursive
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/localization/build_windows.ps1
```

便携 SDK 可使用 `-DotNet D:\Programs\dotnet\dotnet.exe`。`-OutputDirectory D:\TitanPlanner-test` 把程序放入独立目录，`-Configuration Debug` 构建调试版本。默认输出 `bin/Release/net461/` 是上游保留的目录名，实际主程序目标仍是 .NET Framework 4.7.2。

VS Code 的简体中文包为 `MS-CEINTL.vscode-language-pack-zh-hans`，C# 扩展为 `ms-dotnettools.csharp`；VS Code 不提供 Visual Studio 的 WinForms 可视化设计器。

## 上游同步记录

2026-11-03 同步官方 `Titan-Dynamics/TitanPlanner` 的 `master` 至 `0b3fa903e793`，保留完整历史、中文双版本、SSH.NET 2026.0.0 及独立 Windows 构建和发布检查。桌面应用与 Linux 原生绘图库随上游升级至 SkiaSharp 3.119.4；共享兼容组件保留本仓 2.88.9 修补版本。此轮包含规划页地形剖面、视频面板、3D 地图、固件选择、飞行模式即时写入、POI 读写保护与参数安全提示，并补齐新增提示的增强中文。上游已用新地形剖面替代旧高程窗口，并移除 Altitude Angel 集成。源码同步不代表已发布新安装包；发布仍须通过对应验证。

## 测试与自动化

详见 [AUTOMATION.zh-CN.md](AUTOMATION.zh-CN.md)。每日 03:17 UTC 同步上游源码并运行轻量检查；普通 push/PR 不触发完整编译或发布。只有新识别的上游正式 Release 才触发自动候选构建，手动原生构建与已验工件提升是独立入口。冲突或策略变化会停止同步，不应把同步失败直接称为 Windows 构建失败。

原生构建包括 Release/Debug、中文静态及运行测试、真实 Windows 控件、离线回归测试、依赖漏洞审计和 ZIP/MSI 校验。自动发布仍受不可变源码锚点与版本/说明映射门禁限制；通过轻量检查或构建均不等于已经发布。

```powershell
python scripts/localization/verify_zh_hans.py
python -m unittest discover -s scripts/ci/tests -v
dotnet run --project MissionPlannerTests/Localization/Localization.csproj
```

## 维护翻译

在 `localization/zh-Hans/ui-translations.json` 和 `code-translations.json` 维护增强译文，再执行：

```powershell
python scripts/localization/update_zh_hans.py
python scripts/localization/build_ui_catalog.py
python scripts/localization/verify_zh_hans.py
```

历史词典目录名继续沿用 `zh-Hans`，生成目标是增强 `zh-CN`。不要改动原版 `zh-Hans` 资源；`original-resources.json` 校验原版基线。空的 `UiText.zh-Hans` 兼容资源防止增量构建残留旧增强译文。

动态文本只在显示位置调用 `UiText.Translate` / `UiText.Format`；保留格式占位符、链接和协议标识。原版与其他语言不会串用增强版动态译文。

原版资源基线：`26d0836e3854c2166dc0cbcacbbbb9813b890b2d`。本轮不改飞控控制算法。

## 维护状态与反馈

实际维护入口为稳定线 `zh-CN` 和开发线 `zh-CN-dev`，`master` 仅保留迁移兼容历史；`release-policy-state` 是发布识别台账，不是程序版本分支。历史 Release/tag、Actions 运行和构建工件各有不同用途，不以数量或相同程序版本号判断内容重复。

- [PR #8：上游同步冲突修复](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/pull/8) 跟踪同步候选及验证。以 PR 和对应精确提交的 Actions 结果为准；进入开发线、通过测试、推进稳定线与发布安装包是不同阶段。
- 最新发布的源码不一定等于当前汉化分支。下载时以该次 Release 的构建提交为准，不把尚未发布的同步改进计入旧安装包。
- 本仓库目前未开放 Issues。已有同步提案和验证进度通过上述 PR 跟踪；不要把部分验证或文档整理视为问题已关闭。


## 分支导航 / Branches

- `Titanmain`：TitanPlanner 上游 `master` 精确镜像。
- `missionmain`：MissionPlanner 上游 `master` 精确镜像。
- `zh-CN`：汉化稳定维护线；分支存在不代表其中每个提交均已发包。
- `zh-CN-dev`：汉化开发与上游合并候选；验收后推进稳定线。
- `master`：迁移兼容历史；`release-policy-state`：发布状态记录，非产品分支。

镜像不含本仓改动。定时同步先刷新镜像，再检查开发线；不自动将开发候选升级为稳定版。详见 [分支和发布规范](RELEASING.repository.zh-CN.md)。
