# LOG_BITMASK Windows 只读代表行

在 Windows 上用 PowerShell 运行 `.\VehicleLogMaskRows.exe`。需要 .NET Framework 4.7.2 或更高的 4.x 版本。预期退出码为 `0`，末行格式为 `PASS 36 checks | LOG_BITMASK | Copter 4.5.7 / Plane 4.6.3 / Sub 4.7.1 | product <提交>`。发生错误时，探针输出首个失败条件并返回 `1`。运行时记录完整末行和 `$LASTEXITCODE`。本包已在 Linux 交叉编译，两个嵌入的 XML 已与产品提交逐字节核对。Windows 实测待记录。

探针针对[产品提交 `2bc76b14e6e30b73a5d4734ec868604cf12b45fc`](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/commit/2bc76b14e6e30b73a5d4734ec868604cf12b45fc) 的三个 `LOG_BITMASK` 代表用例。局部宿主创建真正的 WinForms 只读参数表格，产品的 `ReadParameterDescription` 和 `ApplyParameterDescriptionToRow` 使用嵌入的官方元数据及增强简中目录填格。每行保持整数 `1024`（位 10），检查原键、完整英文、已审中文、空单位与范围，以及完整选项格的 SHA‑256。选项格按 UTF‑8 原样计算，行以 LF 分隔，末行无 LF。[分版来源与选项摘要](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/blob/2bc76b14e6e30b73a5d4734ec868604cf12b45fc/docs/parameter-vehicle-log-bitmask.zh-CN.md)

| 代表固件 | 选项行数 | 位 10 原标签 | 额外检查 |
| --- | ---: | --- | --- |
| Copter 4.5.7 | 20 | `RC output` | 旧式文本位掩码 |
| Plane 4.6.3 | 18 | `Compass` | 旧记录指纹回退 |
| Sub 4.7.1 | 19 | `RCOUT` | 位 14 原标签 `INAV` |

对照只在内存操作。将 Plane 4.6.3 会话中的原节点换为 Plane 4.5.7 官方节点，向同一行再填说明。旧式记录只有文本位掩码，英文和 18 行选项完全相同。预期显示完整英文与“待复核”，撤下旧中文及其检索命中，保留原键、整数、空单位和全部位标签。普通简中 `zh-Hans` 和英文 `en-US` 只检查已填充的增强缓存及其搜索门控。

宿主使用内存样例和嵌入资源，无需飞控、串口、网络、日志文件或用户配置。覆盖范围是上述三行及 Plane 的指纹回退，完整参数页、真实设备、DPI、其余四个固定版本和 4.7.2 回退本次均未运行。

## 编译

压缩包内的 `src` 已包含项目与所需产品源码。安装 .NET SDK 和 .NET Framework 4.7.2 目标包后，在解压目录的 `src` 下执行

```powershell
dotnet build .\MissionPlannerTests\ParameterDescriptions\VehicleLogMaskRows\VehicleLogMaskRows.csproj -c Release
```

探针源码也可放在树外，构建时用 `-p:ProductRoot=<精确产品树绝对路径>` 指定产品树。
