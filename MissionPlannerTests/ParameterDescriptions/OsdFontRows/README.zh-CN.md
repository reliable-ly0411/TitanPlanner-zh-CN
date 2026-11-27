# OSD 字体参数表只读探针

在 Windows 上解压定向工件，打开 PowerShell，运行 `.\OsdFontRows.exe`。运行只需 .NET Framework 4.7.2 或更高的 4.x 版。探针在内存中建立一个 WinForms 表格和只读详情窗口，使用合成整数，不打开串口或网络连接。成功时最后一行以 `PASS 53 checks` 开头，退出码为 0。失败时会打印首个失败的键或条件，退出码为 1。

## 检查范围

输入取自 [#273 的精确产品提交](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/commit/caf9bf6ec2b4b5d72d60b54afdd2fc09c484e22f)，选用 Copter 4.6.3，固件构建 `92b0cd78`。测试沿用仓库的局部宿主，直接编译生产代码 `ParameterDescriptions.cs`、`ConfigRawParams.ParameterDescriptions.cs` 及两份嵌入 XML。

- [ ] `OSD_FONT` 与 `OSD1_FONT` 至 `OSD4_FONT` 均可在说明格尚空时按中文找到。填行后核对完整英文、本屏中文、原键、原始整数、单位、范围和选项。
- [ ] 通用字体的范围栏为空。四个分屏的范围为 `0 21`，各自的说明包含对应屏号及 MSP DisplayPort 限制。参数名、数值和说明格的提示均含来源摘要，展开窗口可读到完整双语内容。
- [ ] 同一个已缓存会话切到普通简中或英文时，五个键均退出增强说明和中文检索。
- [ ] 仅在内存把 `OSD1_FONT` 的范围改为 `0 20`。重新填行后保留英文、新范围和原始值，显示待复核并撤下旧中文。切到本次目录未收录的 4.7.2 时，五个键都不继承译文。

局部宿主提供表格列，替代连接对象、旧元数据仓和后台调度。云端编译与资源校验可以独立完成。Windows 实际运行需另记退出码及最后一行输出。普通简中和英文的完整行加载、真实窗口 DPI、主题、排版及滚动须在完整参数页一起验收。

## 从源码复跑

工件的 `src` 目录内已放入上述精确输入和测试项目，可直接用 .NET 8 SDK 编译。构建使用 .NET Framework 4.7.2 目标程序集，缺少本地开发包时由 SDK 还原引用包。

```powershell
dotnet build .\src\MissionPlannerTests\ParameterDescriptions\OsdFontRows\OsdFontRows.csproj -c Release
```

从仓库单独复跑时，在探针源码所在的仓库根目录执行以下命令。已有对应产品检出时可直接将 `$inputRoot` 指向它。

```powershell
git fetch origin feat/21-osd-font-descriptions
git worktree add --detach ..\TitanPlanner-273-input caf9bf6ec2b4b5d72d60b54afdd2fc09c484e22f
$inputRoot = (Resolve-Path ..\TitanPlanner-273-input).Path
$project = '.\MissionPlannerTests\ParameterDescriptions\OsdFontRows'
dotnet build "$project\OsdFontRows.csproj" -c Release "-p:ProductRoot=$inputRoot"
if ($LASTEXITCODE) { throw '探针编译失败' }
& "$project\bin\Release\net472\OsdFontRows.exe"
```
