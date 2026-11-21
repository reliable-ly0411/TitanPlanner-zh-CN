# 航点命令汉化与定向验证

增强 `zh-CN` 在 MAVLink 命令原名后显示中文说明，并翻译随命令变化的参数表头和选项描述。原版 `zh-Hans`、英文和未收录的自定义名称保持原文。译文使用 `Mission command: `、`Mission parameter: ` 前缀限定上下文，维护于 `localization/zh-Hans/code-translations.json`，通过既有生成脚本更新卫星资源。

航点表的显示成员与原始命令键分别绑定；命令选择逻辑读取 `SelectedValue`。原始参数标签保留在列的 `Tag`，地图编辑据此识别纬度、经度、高度和延时。显示单位的后缀保持原文，单位乘数仍由原有元数据读取和任务转换代码处理。

以下检查均不构建主程序：

```shell
python3 scripts/localization/verify_zh_hans.py
python3 -m unittest discover -s scripts/ci/tests -p test_mission_command_translations.py -v
dotnet run --project MissionPlannerTests/Localization/Localization.csproj -c Release
dotnet run --project MissionPlannerTests/Localization/MissionCommandProtocol/MissionCommandProtocol.csproj -c Release
```

第一个独立 .NET 项目验证实际编译的显示辅助代码与翻译资源。第二个项目直接链接生产 MAVLink、Locationwp 和 MissionFile 源码，核对三种高度框架、原始/增强中文及英文界面和不同数字文化下的任务 JSON、两种 MAVLink 航点载荷与 WPL 解析值。自定义和未知命令使用合成 ID，不连接飞控。其异常消息框替身只用于让非法 WPL 测试输入立即失败。

Windows 控件定向回归位于 `MissionPlannerTests/GCSViews/FlightPlannerCommandTranslationTests.cs`，需要包含修复的主程序及对应测试程序集：

- `RealConstructorAndRepeatedCommandRefreshPreserveBindingMembers`：三语言真实构造函数，构造后挂入 Form，缓存 Items 后多次刷新命令列；不预设 BindingContext。
- `TranslatedCellsPreserveMissionIdsParametersFramesJsonAndMavlinkPayloads`
- `ChineseHeadersPreserveMapCoordinatesAltitudeDelayAndNonCoordinateParameters`
- `ChineseSelectionRetainsTakeoffLandAndCameraDefaultsAndIgnoresFrameLabels`

控件测试使用隔离的航点表和真实处理方法，避免初始化地图或连接设备。界面验收仍应检查下拉框、长表头提示、地图点击/拖动、高度单位、命令默认值、自定义/未知命令及任务保存/打开。SITL 上传读回与实际执行另行验收；独立协议检查不能代替这些操作。现有表头裁切问题另由 #18 跟踪。

命令列的 DisplayMember/ValueMember 必须在构造函数的首次数据绑定之前配置一次。updateCMDParams 只更新数据源；避免读取 Items 后再次设置成员导致列模板绑定缓存的空引用。真实构造回归与此前的隔离处理方法测试分别记录；旧候选的启动失败在新候选本机复测前保持失败。

## 命令下拉尺寸回归

桌面命令编辑器在 EditingControlShowing 和实际 DropDown 时都按当前字体测量完整列表，同时设置单元格及编辑控件的 DropDownWidth。宽度受当前屏幕工作区限制；超过可用宽度的标签按文字元素换行，完整保留协议名称及中文。绘制只影响显示，不改绑定的 Key。原生弹出窗口出现后将边界限制在当前工作区；切换到高度框架列时移除命令专用绘制。

独立 Windows 原生尺寸回归只编译相关显示源码，不构建主程序：

```shell
dotnet build MissionPlannerTests/Localization/MissionCommandDropDown/MissionCommandDropDown.csproj -c Release
```

在 Windows 执行 `MissionPlannerTests/Localization/MissionCommandDropDown/bin/Release/net472/MissionCommandDropDown.exe`。程序使用与主程序相同的 DPI manifest，实际展开 DataGridView 的原生下拉窗口，通过 GetComboBoxInfo/GetWindowRect/GetClientRect/CB_GETITEMHEIGHT 核对弹出边界、文本宽度和行高，并验证重复展开、原始命令键提交及高度框架编辑器恢复。覆盖真实命令词典、简中/英文、左右屏幕边缘及超宽标签，共 24 个尺寸场景。

100%/125%/150%/200% 的字体大小组合用于布局压力测试，不能等同于改变 Windows DPI。还需在实际 Windows 100%/125%/150% 缩放设置下分别复跑，并在完整规划页复验长命令。Linux 只编译该程序，不执行或报告 Windows 尺寸通过。SITL 上传读回不属于本尺寸测试。
