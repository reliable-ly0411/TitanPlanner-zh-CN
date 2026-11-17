# 当前飞行模式显示定向检查

显示层使用 `FlightModeDisplay`，原始 `CurrentState.mode`、模式列表、协议枚举和命令参数保持原值。增强 `zh-CN` 显示中文主行与较小英文行；`en-US`、原版 `zh-Hans` 和未翻译的未知模式使用单行回退。顶部对象为客户区 `ModeDropdownButton`，不是 Windows 系统标题栏。

HUD 的徽标限制在底部中央与实际电池、GPS、EKF、振动指示之间，位于航点/高度带及预解锁提示下方。长文本省略；预留区域被遥测占满时不绘制徽标。顶部提示和辅助功能名称保留完整文字。第三方 HUD 插件自行绘制的内容不在这些边界的测量范围内。

## Linux / 跨平台逻辑检查

需要 .NET 8 SDK 与 Python，无需 GUI 或 NuGet。脚本编译生产显示/布局源码和实际三套资源，只运行显示及合成几何断言，不构建产品。

```sh
python scripts/ci/test_bilingual_mode_logic.py --dotnet /path/to/dotnet
python scripts/localization/verify_zh_hans.py
python -m unittest scripts.ci.tests.test_action_display_translation -v
```

正常 SDK 环境也可使用项目入口：

```sh
dotnet run --project MissionPlannerTests/FlightModeDisplay/FlightModeDisplay.csproj -c Release
```

覆盖三语言、实际格式模式名称、Turtle/Flip 区分、长名称、未知/空模式、窄区域、100%/150%/200% 合成字体压力、航点/高度和预解锁区域边界及反复模式/语言更新。合成度量不能证明原生字体或实际 DPI 表现。

## Windows 定向原生检查

先从已核实的精确源码提交构建独立输出目录，再运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/localization/build_windows.ps1 -DotNet $dotnet -OutputDirectory $build
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/ci/test_bilingual_mode.ps1 -BuildDirectory $build -ResultsDirectory $results
```

`$dotnet`、`$build`、`$results` 由测试环境设置。探针默认用于隔离 Windows 会话；明确允许在当前桌面显示独立测试窗体时加 `-AllowInteractive`。不连接飞控、不设置模式、不发送命令。测试使用隔离用户数据目录，并输出三语言日志、`toolbar.png` 和 `hud-gdi.png`。

检查真实固件模式列表及原始数值（Turtle=28、Flip=14）、原生顶部绘制、80/140/280 px 按钮、100%/150%/200% 字体压力、520/800/1420 px 菜单宿主及相邻控件边界、模式反复更新，以及 320/640/960 px 的 GDI HUD、双电池和单/双 GPS。HUD 模式更新前后的像素比较要求航点、高度和告警区域保持一致。字体压力不是多 DPI 实测。

可另外运行现有 MSTest 的 `FlightModeTranslationTests`，其中增加了 Turtle 和 Flip 的值与显示断言。完整应用、默认 OpenGL HUD、实际 DPI 切换、SITL 状态切换和告警仍需独立 GUI 验收；本探针成功不代替这些项目。

## 当前验证范围

Linux：显示/合成几何 6,466 条断言通过；显示翻译静态测试 5 项通过；资源校验通过。Windows 探针已用 .NET Framework 4.7.2 引用程序集编译，未执行 Windows GUI。完整产品、OpenGL、真实 DPI、SITL 均未在此环境验证。
