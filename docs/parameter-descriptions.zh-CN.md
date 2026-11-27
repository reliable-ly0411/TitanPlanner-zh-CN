# 参数表双语说明与验证边界

本功能保留参数键、官方英文及原始单位、范围、枚举编号，在“中文(简体)2”参数表增加独立中文说明与中文搜索。显示时完整英文在前，中文解释在后；缺译、待复核和冲突提示也放在原文之后。中文可重新组织解释，但只对已登记的机型、正式版本及原文/字段指纹生效。缺译保留英文；原文或关联字段变化显示“待复核”，不复用旧中文。首批只改变显示、搜索与阅读布局，参数读取、写入、范围校验、默认值及位掩码编辑算法沿用原链路。

## 当前范围

当前包含 242 个不同的“机型 + 参数键”、523 条精确版本说明记录。Copter 4.5.7／4.6.3／4.7.1 分别为 73／73／79 条，Plane 4.5.7／4.6.3／4.7.1 为 84／84／92 条，Sub 4.7.1 为 38 条。独立中文目录共 381 条按版本和指纹划分的说明与枚举译文。4.7.2 尚未纳入，不套用其他版本。

下表保留首批示例；新增说明涵盖姿态角控制、Loiter、爬升下降、航点、固定翼空速和 TECS 等，完整精确范围以目录资源为准。

| 固件 | 正式版本 | 首批示例参数键 |
| --- | --- | --- |
| Copter | 4.5.7、4.6.3 | `RTL_ALT`、`RTL_LOIT_TIME`、`RTL_ALT_FINAL`、`WPNAV_SPEED`、`PILOT_SPEED_UP`、`WP_YAW_BEHAVIOR` |
| Copter | 4.7.1 | `RTL_ALT_M`、`RTL_LOIT_TIME`、`RTL_ALT_FINAL_M`、`WP_SPD`、`PILOT_SPD_UP`、`WP_YAW_BEHAVIOR` |
| Plane / QuadPlane | 4.5.7、4.6.3 | `RTL_ALTITUDE`、`WP_RADIUS`、`AIRSPEED_CRUISE`、`Q_RTL_MODE`、`Q_RTL_ALT`、`Q_WP_SPEED` |
| Plane / QuadPlane | 4.7.1 | `RTL_ALTITUDE`、`WP_RADIUS`、`AIRSPEED_CRUISE`、`Q_RTL_MODE`、`Q_RTL_ALT`、`Q_WP_SPD` |
| Sub | 4.7.1 | `STAT_BOOTCNT`、`STAT_FLTTIME`、`STAT_RUNTIME`、`STAT_FLTCNT` |

Sub 小批次仅翻译官方标为只读的统计说明。飞行时间/次数按固件记录的统计名称翻译，不推断潜水时间、驾驶条件或实际计数行为；不收录 `STAT_RESET`。来源为[版本固定的官方元数据](https://autotest.ardupilot.org/Parameters/versioned/Sub/stable-4.7.1/apm.pdef.xml)，文件内标记的 `Sub-4.7.1` SHA 与官方 Git 标签一致；完整源文件 SHA-256 见 `sources.json`。

`WP_YAW_BEHAVIOR` 和 `Q_RTL_MODE` 的枚举显示原英文及中文，原编号不变。4.7 的改名与单位变化分别保存官方原记录，不做参数别名替换或数值换算。解锁检查、失效保护、电机/舵机方向等高风险参数不在首批中文范围内。

Plane 4.6.3 的 `BARO1_DEVID` 依据同版本[官方参数资料](https://autotest.ardupilot.org/Parameters/versioned/Plane/stable-4.6.3/apm.pdef.xml)中的只读设备编号说明；不引申成气压计设定项。

### Copter 只读说明扩充

在原三版各 37 条基础上，新增 114 条精确分版说明。范围为传感器设备编号、校准后的地面气压及温度学习结果、固件统计、任务项计数、图传频率显示和 GPS 节点编号状态。逐版核对固定原始 PDEF 的英文、全部字段指纹及只读标记；保留单位、范围、增量和易变标记，不加入参数设置、重置或状态告警。旧版缺失的扩展编号及统计不套译；GPS 名称按所属版本独立登记，不使用别名。

| 正式版 | 精确中文／官方不同键（原来各 37） | 未覆盖 | 重名键／指纹冲突 |
| --- | ---: | ---: | ---: |
| 4.5.7 | 73 / 4,426 | 4,353 | 77 / 44 |
| 4.6.3 | 73 / 4,776 | 4,703 | 77 / 44 |
| 4.7.1 | 79 / 5,688 | 5,609 | 235 / 156 |

分母不是任意设备的实际参数数。固定原文件 SHA 和版本标记见 sources.json；4.5.7 文件内为标签与生成时间，构建提交另由官方标签核验。离线覆盖可用以下命令按版本复跑；重复或不同指纹的官方定义不得算入有效覆盖。

    python scripts/localization/audit_parameter_coverage.py Copter 4.7.1 /path/to/apm.pdef.xml

### Plane／QuadPlane 只读说明扩充

在原 51／52／53 条基础上，只增加 104 条 PDEF 英文、只读标记及完整指纹相符的分版说明。覆盖设备编号、校准后气压、统计、任务项计数、图传频率显示与 GPS 节点状态。不加入 TCAL 或重置，不翻译旧版缺失的扩展空速编号，也不将改名后的 GPS 字段当同义别名。原有 4.6.3 BARO1_DEVID 译文和签名不变。

| 正式版 | 精确中文／官方不同键 | 未覆盖 | 重名键／指纹冲突 |
| --- | ---: | ---: | ---: |
| 4.5.7 | 84 / 4,485 | 4,401 | 32 / 16 |
| 4.6.3 | 84 / 4,851 | 4,767 | 32 / 16 |
| 4.7.1 | 92 / 5,722 | 5,630 | 190 / 128 |

分母是固定官方文件的唯一键，不代表任意设备具备全部参数。可用离线审计脚本按版核对完整 PDEF，重复键保持排除：

    python scripts/localization/audit_parameter_coverage.py Plane 4.7.1 /path/to/apm.pdef.xml

### Sub 4.7.1 的新增只读说明

原四条统计保留。另加 30 项设备编号与校准后地面气压、两项 GPS 节点状态、任务项计数和图传频率显示，共 34 条。均与固定 4.7.1 PDEF 的原文和全部字段指纹一致；不改任何设置、范围、单位或读写。不把元数据列出的设备当成每台潜航器的实际配置。5,369 个官方不同键中精确说明由 4 增至 38，仍缺 5,331；190 个重名含 128 个字段指纹冲突，均不计覆盖。4.6.3 官方首选、备用固定文件未取得，不借用 4.7.1 的说明。

## 普通英文说明从哪里来

ArduPilot 源码 `Parameters.cpp` 和各库中的 `@DisplayName`、`@Description`、`@Values`、`@Bitmask` 等注释，经官方 `Tools/autotest/param_metadata/param_parse.py` / `xmlemit.py` 生成 `apm.pdef.xml`。MAVLink 普通参数协议本身传参数名、值和类型，不随每个值传送完整英文或中文说明。

现有普通参数表实际走 `ConfigRawParams.LoadMetadataAndUpdateCells` → `ParameterMetaDataRepository.GetParameterMetaData`。原链路按每个字段查询：当前机型 PDEF → SITL PDEF → AP_Periph PDEF → 旧格式 APM XML；成功结果还有 5 分钟滑动内存缓存。旧格式先尝试用户目录的 `ParameterMetaData.xml`，加载失败才使用运行目录 `ParameterMetaDataBackup.xml`。这是代码规定的顺序，不能从截图或某个文件存在就认定当前进程实际采用了哪一份。

原 PDEF 下载地址是 `https://autotest.ardupilot.org/Parameters/{vehicle}/apm.pdef.xml.gz`，机型名为 `ArduCopter`、`ArduPlane`、`Rover`、`ArduSub`、`AntennaTracker`，另有 AP_Periph、Blimp、Heli、SITL。原代码也尝试 `Parameters/versioned/{Copter|Plane|Rover|Sub|Tracker}/stable-{版本}/apm.pdef.xml`。 gzip 下载先解压为机型 `.xml`，`Reload` 使用 `XDocument.Load` 读取 XML；例如共享数据目录下的 `ArduCopter.apm.pdef.xml`，版本文件则是 `Copter4.6.3.apm.pdef.xml`。版本加载会把内存中的机型索引指向版本文件；因此磁盘上通用文件存在，也不足以证明当前查询采用它。`app.config` 中的 `raw.oborne.me` C++ 地址用于旧解析器/调试生成路径，不能称为普通参数表每次查询时的唯一来源。

| 文件种类 | 当前源码的解析位置 | Windows 目录规则，非 GPD 实测路径 |
| --- | --- | --- |
| 下载的 `*.apm.pdef.xml(.gz)` 与版本文件 | `ParameterMetaDataRepositoryAPMpdef` | `Settings.GetDataDirectory()`，非 Mono 下为系统 `CommonApplicationData` 特殊目录加 `TitanPlanner` |
| 用户旧格式 `ParameterMetaData.xml` | `ParameterMetaDataRepositoryAPM` | `Settings.GetUserDataDirectory()`，Windows 默认是系统 `MyDocuments` 特殊目录加 `TitanPlanner`，也可受 `CustomUserDataDirectory` 影响 |
| `ParameterMetaDataBackup.xml` | 同上，旧格式后备 | `Settings.GetRunningDirectory()`，通常为入口程序集所在目录；桌面 csproj 将它作为 Content 复制到输出，非桌面程序集中的内嵌英文说明 |
| `ParameterFactMetaData.xml` | PX4 分支解析器 | 不属于 ArduPilot 的普通说明查询分支 |
| 首批官方子集与中文目录 | 新显示层 | 嵌入 `MissionPlanner.Utilities.dll` 的两个独立 XML 资源；原版 `zh-Hans` 资源字节保持不变 |
| 新显示层下载缓存 | `ParameterDescriptionStore` | 用户目录下 `ParameterDescriptions/<机型>/<实际版本>/<源 SHA256>.apm.pdef.xml`，`current.txt` 同时核对源 URL 与 SHA256 |

系统特殊目录可能被迁移或重定向，安装版也可能来自另一源码提交。这里没有读取 GPD 进程、安装文件或用户缓存，不能把上述规则写成某个真实 `C:\...` 路径，也不能断言 GPD 当前加载的文件。`Program.cs` 启动输出含 Data Dir、Running Dir、User Data Dir；原下载器日志会记录 URL。GPD 验收需另保存入口 EXE 路径/版本/源码 SHA、这些目录的实际值、相关 XML 的 SHA256，以及待核参数的实际源节点。仅查看安装目录中有后备文件仍不能证明它被采用。

## 新显示层的匹配、更新与离线行为

显示层独立解析正式固件的完整版本字符串，与已经收到的 `AUTOPILOT_VERSION` 数字版本及类型交叉核对。机型必须一致；beta/rc/dev/自定义后缀、未知固件或两个版本信号冲突时不应用中文。报告的 Git SHA 若无法与元数据出处对应，也不应用中文；没有报告 SHA 时只表示按正式版本查官方参考，不能证明任意同版本定制构建都相同。

下载先请求带实际版本号的官方 `Parameters/versioned/.../stable-x.y.z/apm.pdef.xml`，有空档时再试 `https://firmware.ardupilot.org/<目录>/stable-x.y.z/__METADATA__/apm.pdef.xml`（Tracker 目录为 `AntennaTracker`）。备用文件必须在文档内明确标识同版本和构建，不能只信 URL；缓存和提示记录实际采用的来源。不把移动的 `stable` 或最新开发目录冒充当前固件。下载后检查 XML 结构、机型、可用的版本出处及构建标识，原始元数据文件按源 SHA256 分开缓存；最后才原子更新指针。校验失败、超时、无写权限或断网保留旧的有效同版本数据；冷启动离线只提供上述小子集。缓存成功检查间隔为 7 天，网络不阻塞 UI；未知版本没有凭空生成的离线记录。中文只依赖登记的原文 SHA256 和整条字段指纹，不调用在线翻译服务。

2026-11-16 核实过 Copter `stable/__METADATA__/release-tag.txt` 为 `Copter-4.7.2`，而之前 GPD SITL 记录为 4.7.3；这两个身份不能直接视为相同。本次对两类 `stable-4.7.3` 固件元数据地址的读取返回 404。首批测试特意验证：报告 4.7.3 时即使响应里是 4.7.2，也拒绝当作匹配数据；无精确来源则显示已有英文并在提示中标明未核实匹配。此项是新显示层的定向验证，不是对 GPD 安装版下载器当前实际行为的实机判定。

每次显示绑定机体对象、版本和语言；新来源、切换机体或语言会换用新会话，旧异步结果不能回填。搜索查当前快照中的说明与枚举，包含从未滚动到的行，支持一个汉字。长行按可见内容计算高度；说明/选项支持双击打开只读滚动窗口，超出屏幕的英文仍可完整阅读。

## 已知盲点与后续阶段

- 官方最新文档来自开发源码，版本目录有空档；正式发布服务器的 `__METADATA__` 也是另一来源。本轮已加入按实际版本固定并经过文档版本/构建验证的备用来源，仍不接受移动 stable 别名。
- 官方 XML 本身可包含同名不同后端/机架定义。实测最新 Copter 的 `ATC_RAT_RLL_P` 有不同范围，部分 `BATT2_*` 有不同解释；原索引取第一条。新显示层保留冲突文本/范围并标示冲突，暂停这类中文，不据此改写原编辑器的限制。
- 同名枚举跨固件含义不同，例如 `FS_GCS_ENABLE=1` 在 Copter、Rover、Sub 并不等价。Rover、Tracker 目前可按正式版本读取英文；Sub 仅提供上述 4 项 4.7.1 统计中文说明。Blimp、AP_Periph、旧 Heli、开发/定制固件、Lua 动态参数、外围组件与编译裁剪均未承诺完整覆盖。
- 实际参数列表仍以飞控收到的数据为准，不把元数据全集作为当前硬件的可写参数列表。板型引脚、可选驱动/后端、启用后才出现的参数，需要实际构建及设备上下文。
- 中文说明层与原控件/数值校验层分开。本批不统一基本/扩展调参页或位掩码编辑窗的翻译，也不修正原共享元数据缓存与约束匹配。普通编辑器的整数枚举 API 还会忽略小数枚举；新说明显示保留原小数字符串，不改变原编辑行为。
- 原文也可能不充分或含错，不能仅凭机器翻译生成缺失条件。高风险说明应逐项核对对应源码与官方文档，审阅解锁/上锁、失效保护优先级、条件、否定、特殊值和单位，完成对应 SITL/实机只读验证后再扩大覆盖。
- 将来可探测 MAVLink Component Metadata 的 URI/CRC/翻译能力，但不能假定所有 ArduPilot 构建都支持；厂商/脚本参数需可追溯的扩展包和独立许可。

## 维护和复跑

中文来源为 `localization/zh-Hans/parameter-translations.json`，经 `scripts/localization/build_parameter_catalog.py` 生成独立资源。生成器会拒绝原文/字段指纹变化和重复范围，不自动给新英文批准旧中文。官方摘录和完整源文件摘要见 `localization/parameters/official-first-batch.xml`、`sources.json`、`NOTICE.md`；不修改冻结的原版中文资源，不从第三方中文包批量搬运未知许可内容。

```powershell
python scripts/localization/build_parameter_catalog.py --check
python -m unittest discover -s scripts/ci/tests -p test_parameter_descriptions.py -v
dotnet run --project MissionPlannerTests/ParameterDescriptions/ParameterDescriptions.csproj -c Release
```

核心测试覆盖版本/构建冲突、4.7.3 与移动 stable=4.7.2 的区别、同版本不同源摘要、英文/范围/单位/枚举不变、待复核、重复定义、未知固件、缓存新鲜/过期/损坏/断网/写失败、XML DTD 拒绝及中文搜索数据。可选 `--source-directory` 对下载原文件与首批摘录逐记录比对；默认测试完全离线。

在独立 Windows 测试会话编译本分支，再运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/localization/test_parameter_descriptions.ps1 -BuildDirectory <已编译目录> -ResultsDirectory <独立结果目录>
```

GUI 探针加载实际程序和 Utilities DLL，只填合成参数，不激活连接、不发送参数/解锁命令；检查未呈现行的中文/英文搜索、双语枚举编号、640/1024/1600 窗宽、100%/125%/150%/200% 字号下行高和只读展开，并输出截图。字号缩放不等于系统 DPI，Windows 实际 DPI、深浅主题、真实连接与切换仍须 GPD 验收。

本轮云端执行了 .NET 8 核心断言、源文件比对、Python 目录校验及定向 .NET Framework 4.7.2 编译（新 UI 部分使用明确的宿主替身）。GUI 探针完成编译，未在云端执行 Windows 原生 GUI，也未执行完整应用构建或飞行验证。原程序已存在的显示、读写和元数据限制不因这些测试通过而视为全部解决。
