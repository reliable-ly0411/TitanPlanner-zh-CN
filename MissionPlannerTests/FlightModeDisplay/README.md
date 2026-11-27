# 当前飞行模式显示定向检查

显示层使用 `FlightModeDisplay`，原始 `CurrentState.mode`、模式列表、协议枚举和命令参数保持原值。增强 `zh-CN` 显示中文主行与较小英文行；`en-US`、原版 `zh-Hans` 和未翻译的未知模式使用单行回退。顶部对象为客户区 `ModeDropdownButton`，不是 Windows 系统标题栏。

ArduPlane `Autoland`（26）显示为“自动着陆”，指[官方 AUTOLAND 文档](https://ardupilot.org/plane/docs/mode_autoland.html)中的固定翼自动着陆模式，区别于 QLAND。`Autoland`、`AUTOLAND`、`AutoLand` 三种显示源文均覆盖。Rover `Dock`（8）显示为“自动停靠”，对应[官方 DOCK 文档](https://ardupilot.org/rover/docs/dock-mode.html)的自动接近固定对接目标；也覆盖 `DOCK` 源文。协议枚举未改。

顶栏下拉的 AntennaTracker `Auto` 行在增强简体显示 `Auto · 自动跟踪`，对应[官方跟踪器模式文档](https://docs.ardupilot.org/antennatracker/docs/how-to-operate.html)。现有 `Common.getModesList(ArduTracker)` 中仍为原英语 `Auto` 和编号 10。只改顶栏行标题；其他机型、当前模式徽标、未知模式和原版语言保持原状。

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

HUD 截帧先等待 40 ms，越过生产控件的 30 ms 绘制节流，再验证绘制时间戳推进、画面非空且缓存的模式与目标相符。最多尝试四次；被丢弃的空/陈旧帧会记录原因并保存 `rejected-capture-*.png`，无法取得有效帧则测试失败。有效 before/after 帧也保存，原来的航点/高度/告警区域逐像素相等断言不变，不遮罩、不增加像素容差，也不修改生产 HUD 节流。

可另外运行 MSTest 的 `FlightModeTranslationTests`：真实固件模式列表测试汇总全部漏译并要求零遗漏；另有 Turtle/Flip 值与显示断言，以及从 `Common.getModesList` 按值读取 ArduPlane Autoland（26）和 Rover Dock（8）、验证三语言显示和原始值不变的两组数据用例。静态检查还覆盖 `scripts/ci/tests/fixtures/flight_mode_metadata.json` 中固定的 2026-11-17 官方元数据 64 个模式名称/值，记录来源和 XML 摘要，补充较旧备份的缺口。未删除模式或放宽翻译要求。完整应用、默认 OpenGL HUD、实际 DPI 切换、SITL 状态切换和告警仍需独立 GUI 验收；本探针成功不代替这些项目。

## 当前验证范围

Linux：加入 Autoland / Dock 后显示/合成几何 8,316 条断言通过；显示翻译静态测试 6 项通过；资源校验通过。Windows 探针已用 .NET Framework 4.7.2 引用程序集编译，未在此环境执行 Windows GUI。

独立 Windows 复验报告：`178463cf` Release 编译成功；原有 3 项真实模式测试中 2 通过、1 在 ArduPlane Autoland 漏译处失败，本次补译待同环境复验。该版本的 HUD 像素比较偶现失败已捕获到整张黑色 before 帧、正常 after 帧，与 30 ms 节流导致紧邻 `DrawToBitmap` 未绘制的路径吻合。本次修复探针取帧并保留遮挡断言，仍须在 Windows 分语言反复执行；空帧原因已找到不代表其他遮挡已排除。完整应用、OpenGL、真实 DPI、SITL 不能由这些检查代替。

2026-11-27：AntennaTracker 顶栏显示层定向测试通过 8,430 项断言。Mono/Xvfb 使用实际模式列表分支、上下文选择、弹窗代码和三套资源，3 种语言均验证六条原始模式的数量和键值未变，点击 `Auto` 行的事件值仍为原文，未挂接指令发送。未运行完整构建、Windows 原生 DPI 矩阵、实机或真实模式切换。
