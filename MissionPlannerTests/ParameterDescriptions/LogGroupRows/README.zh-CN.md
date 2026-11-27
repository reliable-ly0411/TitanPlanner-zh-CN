# 日志参数两行 Windows 只读探针

这个无参程序用于在 Windows 上核对 #300 的两个代表行。解压 ZIP，把 PowerShell 切到解压目录，运行下面两行。运行环境为 .NET Framework 4.7.2 或更高的 4.x 版本。[产品提交 `d82da902`](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/commit/d82da902b031b05e3f241449b15fa5373eed92f9)

```powershell
.\LogGroupRows.exe
$LASTEXITCODE
```

正常完成时，控制台依次输出七条 `ROW` 记录和下面的末行，退出码为 0。每条记录依次输出语言、原键、原值、单位、范围或选项、说明。格内换行在输出中用 `\n` 表示。失败时输出 `FAIL` 和首个失败条件，退出码为 1。验收时请记录实际末行及退出码。

```text
PASS 2 rows x 3 languages + 1 unit-fingerprint fallback | product d82da902b031b05e3f241449b15fa5373eed92f9
```

| 固件与原键 | 内存样例 | 需保留的原字段 |
| --- | ---: | --- |
| Copter 4.6.3 · `LOG_FILE_BUFSIZE` | 16 | 单位 `kB`，范围 `4 200` |
| Sub 4.7.1 · `LOG_DISARMED` | 3 | 单位空白。原选项编号与标签为 `0 Disabled`、`1 Enabled`、`2 Disabled on USB connection`、`3 Discard log on reboot if never armed` |

每个机型各用一个只有一行的只读 DataGridView。语言切换时先清空旧格，经生产填行方法派送到 WinForms 界面线程，再读取单元格的 `FormattedValue`。两行的原整数及其类型也在每次回填后核对。[固定参数来源与审定译文](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/blob/d82da902b031b05e3f241449b15fa5373eed92f9/docs/parameter-logger-remaining.zh-CN.md)

| 界面语言 | 比对的两行实表文字 |
| --- | --- |
| 中文(简体)2 · `zh-CN` | 官方英文全文及该版完整中文。Copter 保留 `kB`，Sub 的中文保留 USB 例外和下次启动丢弃日志的条件。来源提示及中文检索须命中。 |
| 普通简中 · `zh-Hans` | 旧语言填行方法写入固定英文全文、单位和原选项，撤下增强中文及其检索。 |
| 英文 · `en-US` | 清空后再次经旧语言填行方法写入，两行均须得到同一组原英文字段。 |

唯一的回退案例复用 Copter 那一行。保留 4.6.3 会话，在内存中换入 4.7.1 的真实缓冲区记录。两版说明英文相同，单位由 `kB` 变为 `KiB`。该行应保留英文、数值 16 和范围 `4 200`，单位格显示 `KiB`，说明出现“中文说明待复核”，旧中文检索退出。

## 工件范围

沿用已验 LoggerRows 的 .NET Framework 4.7.2 项目和 WinForms 宿主。增强与旧语言的两个填行方法、旧式提示换行方法和对应常量均逐段取自 #300 的 `ConfigRawParams.cs`。控制台文本来自真正的 WinForms 单元格。普通简中和英文的旧元数据查询由测试宿主从固定 PDEF 快照供数，实际安装环境的旧元数据库、整页切换和 DPI 布局另验。全程使用内存样例，不需要连接、设备、日志或用户配置。[LoggerRows 骨架](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/tree/0b274ebf701a27400e684a4fc73dd945239d09eb/MissionPlannerTests/ParameterDescriptions/LoggerRows) · [生产填行方法](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/blob/d82da902b031b05e3f241449b15fa5373eed92f9/GCSViews/ConfigurationView/ConfigRawParams.cs#L756-L866)

ZIP 附有完整编译输入、输入摘要 `inputs.json`、包内文件校验表 `SHA256SUMS.txt` 和许可证。云端完成交叉编译及 EXE 入口、嵌入资源的静态核对。上面的 `PASS` 是运行预期，Windows 实际结果由运行者记录。

具备 .NET SDK 和 .NET Framework 4.7.2 目标包时，可从解压目录复编。

```powershell
dotnet build .\src\MissionPlannerTests\ParameterDescriptions\LogGroupRows\LogGroupRows.csproj -c Release
```
