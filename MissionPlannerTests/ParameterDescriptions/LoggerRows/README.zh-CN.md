# 日志后端三行 Windows 只读探针

在 Windows 上解压 ZIP，在解压目录打开 PowerShell，运行 `.\LoggerRows.exe`。运行需要 .NET Framework 4.7.2 或更高的 4.x 版本。最后一行预期以 `PASS 50 checks | Copter 4.6.3 | 3 logger rows` 开头，退出码为 0。遇错会输出 `FAIL` 和首个失败条件，退出码为 1。实际验收时记录完整末行和 `$LASTEXITCODE`。

探针选用 [#294 产品提交 `594333121fee8a5f624f945035eaa3cefc6d05b4`](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/commit/594333121fee8a5f624f945035eaa3cefc6d05b4)，沿用 [#277 的 WinForms 项目与局部宿主](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/tree/25eb2da0425e721278bb6720cd75961606ccb87e/MissionPlannerTests/ParameterDescriptions/OsdFontRows)。只读表格中的 5、0、1 都是内存样例。无需飞控、串口、网络、日志文件或用户配置。探针不写参数。

| Copter 4.6.3 原键 | 内存样例 | 需要保留的行内容 |
| --- | ---: | --- |
| `LOG_BACKEND_TYPE` | 5 | 单位为空，位号 0 File、1 MAVLink、2 Block，允许组合 |
| `LOG_DARM_RATEMAX` | 0 | `Hz`、范围 `0 1000`，0 沿用各后端通常的限速设置 |
| `LOG_FILE_DSRMROT` | 1 | 单位为空，`0: Disabled`、`1: Enabled`，另开日志的条件含 `LOG_DISARMED` |

正常分支使用 Copter 4.6.3 构建 `92b0cd78` 的登记记录。三行分别检查中文检索、完整双语说明、原键与数值类型、范围单位及选项、来源提示和只读展开窗。暖缓存切换普通简中和英文时也会检查增强中文检索是否退出。

- **错误位掩码指纹**。仅在内存将 4.6.3 的 `LOG_BACKEND_TYPE` 原节点换为 4.5.7 的官方原节点。英文和三个位号相同，4.5.7 只有文本位掩码字段。新会话向同一行重新填入说明，应保留英文、数字 5 和三个位号，显示“待复核”，撤下旧中文及其检索词。另两行仍应命中。
- **未登记版本**。切换到 4.7.2 上下文后，生产重置逻辑清空三行。测试宿主为旧元数据接口提供相同的 4.6.3 英文、范围、单位和枚举，用于检验英文回退。旧接口不提供位掩码，故此对照的位掩码选项格为空。三行的旧中文和中文检索均应退出。这组输入不代表真实 4.7.2 文件。

工件直接编译产品的参数读取类和 `ConfigRawParams` 行显示代码。连接对象、后台调度和旧元数据库由局部宿主替代。云端交叉编译和静态核对嵌入资源已完成。Windows 原生运行、完整参数页筛选、布局与实际 DPI 尚待验收。

## 核对与复编

ZIP 中的 `inputs.json` 列出产品提交、测试提交和构建输入摘要。`SHA256SUMS.txt` 可核对 EXE、配置和随附源码。工件根目录的 `remaining-log-sources.zh-CN.md` 是剩余三键的分版清单。[测试分支上的原清单](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/blob/538137ba4afd4d89bc607b66babca4309477df9a/docs/parameter-logger-remaining.zh-CN.md)

```powershell
Get-FileHash .\LoggerRows.exe -Algorithm SHA256
.\LoggerRows.exe
$LASTEXITCODE
```

编译随包源码时，在已安装 .NET SDK 和 .NET Framework 4.7.2 目标包的环境运行以下命令。项目也可在精确测试分支中直接编译。

```powershell
dotnet build .\src\MissionPlannerTests\ParameterDescriptions\LoggerRows\LoggerRows.csproj -c Release
```
