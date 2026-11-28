# 参数表双语说明与验证边界

本功能保留参数键、官方英文及原始单位、范围、枚举编号，在“中文(简体)2”参数表增加独立中文说明与中文搜索。显示时完整英文在前，中文解释在后；缺译、待复核和冲突提示也放在原文之后。中文可重新组织解释，但只对已登记的机型、正式版本及原文/字段指纹生效。缺译保留英文；原文或关联字段变化显示“待复核”，不复用旧中文。首批只改变显示、搜索与阅读布局，参数读取、写入、范围校验、默认值及位掩码编辑算法沿用原链路。

## 当前范围

当前目录覆盖 9,574 个不同的机型参数键，按固件版本展开为 36,004 条中文说明。末个正式版已登记 54 个，另保留 7 个已登记的历史摘录。目标范围内已译 31,423 条，仍有 87,808 个键保留英文。Copter 4.7.2 未登记译文。

| 固件 | 正式版本 | 官方不同参数键 | 已有中文说明 | 未覆盖 | OSD 字体已译／全部 |
| --- | --- | ---: | ---: | ---: | ---: |
| Copter | 4.1.6 | 2,839 | 681 | 2,158 | 1 / 1 |
| Copter | 4.2.4 | 2,974 | 722 | 2,252 | 1 / 1 |
| Copter | 4.3.8 | 3,166 | 814 | 2,352 | 1 / 5 |
| Copter | 4.4.4 | 3,485 | 871 | 2,614 | 1 / 5 |
| Copter | 4.5.7 | 4,426 | 1,498 | 2,928 | 5 / 5 |
| Copter | 4.6.3 | 4,776 | 1,577 | 3,199 | 5 / 5 |
| Copter | 4.7.1 | 5,688 | 781 | 4,907 | 5 / 5 |
| Copter | 4.7.3 | 5,692 | 2,123 | 3,569 | 5 / 5 |
| Plane／QuadPlane | 4.1.7 | 2,933 | 674 | 2,259 | 1 / 1 |
| Plane／QuadPlane | 4.3.8 | 3,207 | 804 | 2,403 | 1 / 5 |
| Plane／QuadPlane | 4.4.4 | 3,513 | 862 | 2,651 | 1 / 5 |
| Plane／QuadPlane | 4.5.7 | 4,485 | 1,502 | 2,983 | 5 / 5 |
| Plane／QuadPlane | 4.6.3 | 4,851 | 1,579 | 3,272 | 5 / 5 |
| Plane／QuadPlane | 4.7.1 | 5,722 | 793 | 4,929 | 5 / 5 |
| Plane／QuadPlane | 4.7.3 | 5,726 | 2,142 | 3,584 | 5 / 5 |
| Rover | 4.1.5 | 2,650 | 610 | 2,040 | 1 / 1 |
| Rover | 4.2.3 | 2,749 | 642 | 2,107 | 1 / 1 |
| Rover | 4.4.0 | 3,275 | 784 | 2,491 | 1 / 5 |
| Rover | 4.5.7 | 4,209 | 1,411 | 2,798 | 5 / 5 |
| Rover | 4.6.3 | 4,559 | 1,488 | 3,071 | 5 / 5 |
| Rover | 4.7.2 | 5,442 | 636 | 4,806 | 0 / 5 |
| Rover | 4.7.3 | 5,446 | 1,988 | 3,458 | 5 / 5 |
| Sub | 4.0.3 | 1,355 | 270 | 1,085 | 0 / 0 |
| Sub | 4.1.2 | 1,686 | 361 | 1,325 | 0 / 0 |
| Sub | 4.7.1 | 5,369 | 743 | 4,626 | 5 / 5 |
| Sub | 4.7.2 | 5,369 | 1,271 | 4,098 | 5 / 5 |
| Sub | 4.7.3 | 5,373 | 2,002 | 3,371 | 5 / 5 |
| Tracker | 4.5.7 | 2,432 | 400 | 2,032 | 0 / 0 |
| Tracker | 4.6.3 | 2,728 | 473 | 2,255 | 0 / 0 |
| Tracker | 4.7.1 | 3,582 | 2 | 3,580 | 0 / 0 |
| Tracker | 4.7.2 | 3,582 | 355 | 3,227 | 0 / 0 |
| Tracker | 4.7.3 | 3,582 | 907 | 2,675 | 0 / 0 |

每行的分母是该版完整官方参数文件（PDEF）中不同的参数键数，同一键在不同版本分别计数。重名或字段冲突的键计入分母，只有与唯一原记录完全匹配的中文说明才计入已译。实际机体的参数数量取决于固件构建和硬件配置。文件地址、发布标签、源码提交和完整 SHA-256 见[来源登记](../localization/parameters/sources.json)。


Rover、Sub 和 Tracker 4.7.2 共复用 2,262 条说明。来源文件内的标签和完整提交与上游标签、各自的官方 SITL 构建记录一致。只复用同机型中原键、英文和完整记录指纹一致的已有译文。同名多定义的键和其余未审阅的键保留英文。

### 复用已审阅说明

同一参数组、参数键及完整记录指纹一致时，目录中的复用条目引用现有译文。机型间复用仅适用于两边的同名库参数组。生成器用各版本的固定原始 PDEF 核对每条来源和目标，排除重名定义、空说明和多个不同译文。生成结果按机型、正式版本与原键单独登记，完整记录变化时回到已有英文显示规则。

本批把 9,085 个分版参数说明登记为 2,918 条复用索引。Plane 4.7.3 新增 53 个，Copter 4.7.3 新增 161 个。可对本地核验过的完整 PDEF 重跑提案生成器，输入按 Family-version.xml 命名。

    python scripts/localization/reuse_parameter_descriptions.py /path/to/pinned-pdef /path/to/proposal

### 历史固件重建资料

固定官方 PDEF 缺失时，从已核对的上游正式标签或官方板卡的发布记录取得源码提交，用对应提交内的参数生成器重建完整英文元数据。登记分别保存源码归档摘要、PDEF 摘要、生成器与上游凭据。地面站以机型和完整正式版本号选择重建资料。启动横幅与 MAVLink 正式版本同时可用时，版本号必须一致。构建哈希用于追溯重建源码。同版本定制构建可能存在参数定义差异。

| 固件 | 正式版本 | 重建不同参数键 | 已有中文说明 | 未覆盖 |
| --- | --- | ---: | ---: | ---: |
| Copter | 2.7.3 | 117 | 19 | 98 |
| Copter | 2.8.1 | 124 | 21 | 103 |
| Copter | 2.9.1 | 173 | 22 | 151 |
| Copter | 3.1.5 | 412 | 30 | 382 |
| Copter | 3.2.1 | 511 | 30 | 481 |
| Copter | 3.4.6 | 749 | 44 | 705 |
| Copter | 3.5.7 | 999 | 85 | 914 |
| Copter | 3.6.12 | 1,457 | 248 | 1,209 |
| Copter | 4.0.8 | 2,163 | 503 | 1,660 |
| Plane | 3.0.3 | 410 | 29 | 381 |
| Plane | 3.1.0 | 460 | 29 | 431 |
| Plane | 3.2.2 | 528 | 29 | 499 |
| Plane | 3.3.0 | 565 | 38 | 527 |
| Plane | 3.5.3 | 750 | 38 | 712 |
| Plane | 3.6.0 | 799 | 38 | 761 |
| Plane | 3.7.1 | 848 | 42 | 806 |
| Plane | 3.8.5 | 1,137 | 209 | 928 |
| Plane | 3.9.11 | 1,600 | 246 | 1,354 |
| Plane | 4.0.9 | 2,201 | 505 | 1,696 |
| Plane | 4.2.3 | 3,055 | 713 | 2,342 |
| Rover | 3.2.3 | 811 | 155 | 656 |
| Rover | 3.3.0 | 900 | 145 | 755 |
| Rover | 3.4.2 | 937 | 167 | 770 |
| Rover | 3.5.2 | 1,631 | 212 | 1,419 |
| Rover | 4.0.0 | 1,955 | 430 | 1,525 |
| Sub | 3.5.4 | 863 | 64 | 799 |
| Tracker | 0.7.2 | 178 | 38 | 140 |
| Tracker | 1.0.0 | 230 | 38 | 192 |
| Tracker | 1.1.0 | 551 | 71 | 480 |

Copter 2.7.3、2.8.1 和 2.9.1 的历史生成器只输出 Wiki。重建读取该生成器产生的 ArduCopter 参数和引用库对象，保留原键、说明和字段。表中分母是这些提交中生成器可识别的已注释键数。

Copter 2.4.1 与 2.5.5 的固定源码未附生成器，3.3.2 暂时只能恢复部分记录。Plane 3.4.0 的官方板卡记录与数字标签指向不同提交。Rover 2.4.2 和 2.5.1 目前只核实到 Beta 构建。Sub 4.5.7 的官方板卡源码提交暂未恢复。这七个候选仍沿用英文。

七份固定文件另收录 16 个 SITL 环境键和 Sub 4.7.1 的一条只读里程说明，共增加 93 条分版说明。[范围和来源](parameter-sitl-environment.zh-CN.md)。

调度器的 `SCHED_DEBUG` 只增加调试消息说明，区分 CPU 负载造成任务延迟和任务自身耗时超限。原选项 0、2、3 保持不变；不包含调度频率或其他调度选项。

OSD 消息仅增加显示时长说明。原范围 1 至 20 不变，单位秒来自官方说明；不更改计时、消息内容、告警规则或默认值。

本地日志文件计数与预留空间只增加说明，保留各版本原范围、单位和重启条件，不改变存储或日志轮换行为。

八个日志后端参数在上述七版各有一条中文说明，涵盖后端选择、上锁换文件、缓冲区、写入超时和流式日志限速。七版分别补齐了文件和块存储缓冲区说明，Sub 4.7.1 还补齐了未解锁记录与 EKF 重放日志说明。官方 `LOG` 参数组现已译 91/91 条。机型组的 `LOG_BITMASK` 在七版也已登记，按 `LOG_` 前缀计为 98/98。[八个后端参数](parameter-logger-backends.zh-CN.md) · [后三个原键的分版来源、单位与选项](parameter-logger-remaining.zh-CN.md) · [机型日志位掩码与只读代表行](parameter-vehicle-log-bitmask.zh-CN.md)

板载 I²C 显示屏类型只增加帮助，不更改默认禁用的原文条件、选项编号或其他通知功能。

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

### OSD 画面偏移

七份固定 PDEF 的水平、垂直画面偏移说明均已登记，共 14/14 条。原范围分别为 `0 63`、`0 31`，原字段均保留重启标记，单位格为空。[画面偏移的分版来源与只读代表行](parameter-osd-overlay-offsets.zh-CN.md)

### OSD 显示格式、闪烁与分屏风向

27 个原键在七版共增加 189 条说明，涵盖显示选项、单位制、项目闪烁、事件选屏、平均单体电压显示和四个画面的风速风向位置。该批完成时，全局 `OSD` 组已译 143/178 条，四个分屏组合计已译 112/5,628 条。[原键清单、分版指纹和只读代表用例](parameter-osd-display-batch.zh-CN.md)

### OSD 分屏信息显示

四个分屏各增加飞行模式文字、飞控当前文字提示和本地时钟的显示开关与位置说明。36 个原键在七份官方文件中各有唯一定义，共增加 252 条分版说明。该批完成时，四个分屏组合计已译 364/5,628 条，余 5,264 条。[完整原键、逐版来源与定向读表用例](parameter-osd-screen-info.zh-CN.md)

### OSD 分屏飞行时间与气压计温度

四屏各增加累计飞行时间、主气压计温度和第二个气压计温度的显示开关与位置说明。每份官方文件新增 36 条，七版共 252 条。飞行时间取本次开机的累计值，主温度取当前主气压计，第二温度取索引 1。四个分屏组合计已译 616/5,628 条，余 5,012 条。[36 个原键、逐版来源和回退用例](parameter-osd-time-temperature.zh-CN.md)

### 按稳定版本登记的 MAVLink 遥测说明

按机型选择每个主、次版本系列的末个正式版。官方固定 PDEF 可与上游标签提交核对时，登记对应的完整文件摘要。同一机型、参数键、英文和完整字段指纹一致的记录可复用已审阅译文。MAVLink 消息组按各版原文登记，保留消息名、可用条件、值和位编号。Copter 和 Plane 4.7.3 各覆盖 32 个通道的 352 条遥测说明，历史版本以各自实际提供的通道和参数为准。

官方固定元数据缺失、构建无法核对或同名定义有歧义时沿用英文。按 [ArduPilot 固件发布目录](https://firmware.ardupilot.org/) 与 [ArduPilot 源码标签](https://github.com/ArduPilot/ardupilot/tags) 核对各版。

### Copter 与 Plane 4.7.3 四屏说明

正式版在官方固件服务器的分版目录登记，完整 SHA-256、源码标签和构建提交见来源登记。与 4.7.1 的英文和完整记录指纹均一致时，逐条登记到 4.7.3。Copter 与 Plane／QuadPlane 各有四个完整屏组，每屏 201 个键均已覆盖，共 1,608 条分版说明。

四屏补齐空速传感器温度和两路电池累计耗电量的显示与位置。此前已登记的八个含这些原键的固件版本也增加了对应说明。Copter 与 Plane 4.7.3 的 `ALTITUDE` 屏显按源码读取相对于 Home 点的高度，英文元数据保留 AGL 写法。[官方绘制源码](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_OSD/AP_OSD_Screen.cpp#L1442-L1450)

### OSD 侧栏间距与长度

只给横向间距、竖向长度两个键增加说明，保留「向右按列扩展」「向上下按行等量延伸」的区别和原范围。当前目录已经登记的 Copter 和 Plane 4.6.3／4.7.1、Sub 4.7.1 共 5 版新增十条。4.5.7 无对应键。未引入新固件来源、修改默认值或写入；其他布局和告警不在此范围内。

### OSD 字体

七份完整 PDEF 中的 OSD 字体键均为 `OSD_FONT`、`OSD1_FONT`、`OSD2_FONT`、`OSD3_FONT` 和 `OSD4_FONT`，各版从 0/5 增至 5/5。通用字体编号从 0 开始，上限取决于可用字体，元数据标注需重启。四个分屏键分别选择第 1 至第 4 屏的字体，仅适用于 MSP DisplayPort，原范围均为 0 至 21。五个键均无单位或枚举选项。[通用字体定义](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD.cpp#L92-L97) · [分屏字体定义](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L1071-L1085)

PX4 v1.16.0 的参数参考、MSP 和 ATXXXX 两个 OSD 驱动均未列出这五个键。上表只统计已登记的 ArduPilot 固件，PX4 各构建的完整参数分母尚未核实。[PX4 固定版本参数参考](https://github.com/PX4/PX4-Autopilot/blob/6ea3539157ca358c70a515878b77077af7d4611d/docs/en/advanced_config/parameter_reference.md) · [MSP 驱动参数](https://github.com/PX4/PX4-Autopilot/blob/6ea3539157ca358c70a515878b77077af7d4611d/src/drivers/osd/msp_osd/module.yaml) · [ATXXXX 驱动参数](https://github.com/PX4/PX4-Autopilot/blob/6ea3539157ca358c70a515878b77077af7d4611d/src/drivers/osd/atxxxx/params.c)

### IMU 诊断日志帮助

只增加已登记的 7 版中样本总数、写入日志间隔和每间隔写入样本数的说明，共 21 条。保留官方英文、向下取为 32 倍数、毫秒单位和原重启条件，不是修改飞控的 IMU 采样率。没有加入位掩码选项、远程日志、推荐值或写入操作；不带其他未登记版本。三条帮助的作用范围仅为本地诊断日志。

### Rover 4.6.3

本批登记 `CRUISE_SPEED`、`TURN_RADIUS`、`WP_SPEED`、`WP_RADIUS` 的中文说明。原键、官方英文、单位和范围按对应正式版本的固定文件校验。

### Tracker 4.7.1

本批登记 `STAT_BOOTCNT`、`STAT_RUNTIME` 的中文说明。原键、官方英文、单位和范围按对应正式版本的固定文件校验。

### 四屏电池用量条（仅 MSP OSD）

恢复此前审过的 8 个位置原键在七版的 56 条显示说明。原横向范围为 0 至 59，纵向范围为 0 至 21。只显示帮助文字，不改变位置或电池测量、告警计算。新增 4 个分屏显示开关原键跨七版的 28 条说明，保留原选项 0 / 1，仅解释是否显示电池用量条。

### 四屏平均单体电压显示（仅 MSP OSD）

七份固定 PDEF 的 12 个原键新增 84 条显示说明，保留原显示选项 0 / 1 和横向 0 至 59、纵向 0 至 21 的范围。仅解释四屏项目显示和位置，不改变测量、估算、告警或遥测。此批连同电池用量条完成时，四分屏组共覆盖 784 / 5,628 条。

其余预备批次的来源和范围见 [osd-basic-speed](parameter-osd-basic-speed.zh-CN.md) · [raw-sensor-rate](parameter-raw-sensor-rate.zh-CN.md) · [vfr-hud-rate](parameter-vfr-hud-rate.zh-CN.md)。


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

Copter 和 Plane 的 4.7.3 正式版已登记固定来源。报告 4.7.3 时仍拒绝 4.7.2 响应，报告 4.7.2 时也不会套用 4.7.3 说明。未登记版本沿用英文。

每次显示绑定机体对象、版本和语言；新来源、切换机体或语言会换用新会话，旧异步结果不能回填。搜索查当前快照中的说明与枚举，包含从未滚动到的行，支持一个汉字。长行按可见内容计算高度；说明/选项支持双击打开只读滚动窗口，超出屏幕的英文仍可完整阅读。

## 已知盲点与后续阶段

- 官方最新文档来自开发源码，版本目录有空档；正式发布服务器的 `__METADATA__` 也是另一来源。本轮已加入按实际版本固定并经过文档版本/构建验证的备用来源，仍不接受移动 stable 别名。
- 官方 XML 本身可包含同名不同后端/机架定义。实测最新 Copter 的 `ATC_RAT_RLL_P` 有不同范围，部分 `BATT2_*` 有不同解释；原索引取第一条。新显示层保留冲突文本/范围并标示冲突，暂停这类中文，不据此改写原编辑器的限制。
- 同名枚举跨固件含义不同，例如 `FS_GCS_ENABLE=1` 在 Copter、Rover、Sub 并不等价。Rover 4.6.3 提供导航、四屏 OSD 和遥测说明。未覆盖的参数沿用英文。Sub 的中文说明覆盖上表所列的 4.7.1。Blimp、AP_Periph、旧 Heli、开发/定制固件、Lua 动态参数、外围组件与编译裁剪均未承诺完整覆盖。
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
