# Windows 本机语音定向验证

这个 x64 控制台薄件检查 [#272](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/pull/272) 在 Windows 上调用真实 `System.Speech` 的行为。产品基线为 `68c038311829448cfeee5923e0093b01f42e8aaf`。薄件编入该提交的语音、音色、配置读写和文本资源。程序把合成器输出重定向到内存 WAV 后才允许提交提示，普通电脑和 GPD 都无需连接飞控或使用扬声器。

## 运行

在装有 .NET Framework 4.7.2 或更高 4.x 版本的 64 位 Windows 上，解压薄件 ZIP，在解压目录打开 PowerShell。脚本使用本机已经安装的语音。

```powershell
.\run.ps1
$LASTEXITCODE
```

每项输出通过、失败或待验。汇总退出码为 0（全部具备证据）、1（有失败）、2（至少一项缺少证据）。脚本在当前用户的临时目录下新建两套独立的程序目录及配置区，只复制薄件 EXE、运行配置和指定的卫星资源。结束时清除本次临时目录。控制台保留测试结果，WAV 仅存在于内存。

| 项目 | 观察内容 |
| --- | --- |
| `auto` | 通过产品试听入口提交固定句。按同一提示的原生完成事件、实际语音身份及含非静音 PCM 数据的 WAV 记录系统自动选声。 |
| `choose`、`reload` | 动态枚举可用的微软中文音色，优先选与自动模式不同的一种。在新临时目录调用产品 `Settings.Save()`，再启动另一个进程读回。实际 `VoiceChange` 身份须与保存的音色一致。 |
| `missing` | 在内存中设置一个本机不存在的名称，以相同固定句比较实际音色与 `auto` 的结果。 |
| `neutral` | 在独立进程中移除增强简中卫星，保留中性和普通简中资源，验证产品试听回退为原英文并能合成。实际提示正文用 SSML 或原生语音进度核对。 |
| `priority` | 请求试听前后须观察到同一普通播报仍在进行，且试听已被拒绝。在真实试听已开始时，按接收器原有顺序检查 `IsReady`、提交固定告警。核对告警提示的实际正文；只有同一试听提示被取消、随后告警正常完成，才记录为观察到打断。此组的两条提示共用内存 WAV，音频数据仅能归于该组。 |

短句写入内存可能很快。竞态每种最多尝试三次，自然播完只记待验。缺可用中文音色、只有一种音色、没有可辨认的实际语音身份，分别按影响项记录。虚构的旧音色名称只覆盖找不到所选音色的回退；确实没有中文包或实际卸载需要对应的 Windows 环境。

## 从源码只构建薄件

在包含本页的精确测试提交中，用 .NET 8 SDK 和 .NET Framework 4.7.2 引用程序集，针对该项目执行下列命令。此项目没有地面站解决方案或产品工程引用。

```powershell
dotnet build .\MissionPlannerTests\WindowsSpeechProbe\WindowsSpeechProbe.csproj -c Release
.\MissionPlannerTests\WindowsSpeechProbe\run.ps1 -BinaryDirectory .\MissionPlannerTests\WindowsSpeechProbe\bin\Release\net472
```

本件使用 Windows .NET Framework 自带的 `System.Speech`。产品整包随附程序集的兼容性仍待单独验收。薄件只用反射取得产品实例里的合成器以重定向和观察，不设置全局音色，也不替换合成器。地面站总开关在该控制台宿主中设为开，飞控、设置窗体及完整应用均未启动。该验证覆盖临时 XML 的跨进程往返。完整应用关闭落盘、真实设置控件、听感和扬声器输出仍需对应入口验收。

[微软的已安装音色接口](https://learn.microsoft.com/dotnet/api/system.speech.synthesis.speechsynthesizer.getinstalledvoices) · [内存 WAV 输出](https://learn.microsoft.com/dotnet/api/system.speech.synthesis.speechsynthesizer.setoutputtowavestream) · [语音身份事件](https://learn.microsoft.com/dotnet/api/system.speech.synthesis.speechsynthesizer.voicechange)
