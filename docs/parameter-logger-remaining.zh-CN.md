# LOG 参数组剩余三键的固定版本来源

在 [#294 产品提交](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/commit/594333121fee8a5f624f945035eaa3cefc6d05b4) 中，七份 ArduPilot PDEF 的 `LOG` 组共有 91 条分版记录，已译 82 条。余下 9 条涉及以下三个原键。每个键在每版均有一条唯一的官方记录。表中行号对应原始 PDEF，依次为 `LOG_FILE_BUFSIZE`、`LOG_DISARMED`、`LOG_REPLAY`。完整文件摘要、标签和源码提交可与[来源登记](../localization/parameters/sources.json)核对。

| 固件 | 固定 PDEF | 缓冲区大小 | 未解锁日志 | EKF 重放日志 | 三个原始行号 |
| --- | --- | --- | --- | --- | --- |
| Copter | [4.5.7](https://autotest.ardupilot.org/Parameters/versioned/Copter/stable-4.5.7/apm.pdef.xml) | 未译 | 已译 | 已译 | 12869 / 12871 / 12879 |
| Copter | [4.6.3](https://autotest.ardupilot.org/Parameters/versioned/Copter/stable-4.6.3/apm.pdef.xml) | 未译 | 已译 | 已译 | 15922 / 15927 / 15935 |
| Copter | [4.7.1](https://autotest.ardupilot.org/Parameters/versioned/Copter/stable-4.7.1/apm.pdef.xml) | 未译 | 已译 | 已译 | 18140 / 18145 / 18153 |
| Plane / QuadPlane | [4.5.7](https://autotest.ardupilot.org/Parameters/versioned/Plane/stable-4.5.7/apm.pdef.xml) | 未译 | 已译 | 已译 | 11956 / 11958 / 11966 |
| Plane / QuadPlane | [4.6.3](https://autotest.ardupilot.org/Parameters/versioned/Plane/stable-4.6.3/apm.pdef.xml) | 未译 | 已译 | 已译 | 14985 / 14990 / 14998 |
| Plane / QuadPlane | [4.7.1](https://autotest.ardupilot.org/Parameters/versioned/Plane/stable-4.7.1/apm.pdef.xml) | 未译 | 已译 | 已译 | 17086 / 17091 / 17099 |
| Sub | [4.7.1](https://autotest.ardupilot.org/Parameters/versioned/Sub/stable-4.7.1/apm.pdef.xml) | 未译 | 未译 | 未译 | 22994 / 22999 / 23007 |

三个键合计 21 条，其中已有 `Description` 中文 12 条。七份 PDEF 均未为这三个键标记 `ReadOnly`，也没有结构化默认值。按 `LOG_` 前缀统计时，每版还会计入机型组的 `LOG_BITMASK`；该口径为 82/98。

## LOG_FILE_BUFSIZE

4.5.7 的单位和范围只出现在英文说明里。原文写 PixHawk 至少需要 4 kilobytes，并提到最高 64 kilobytes。4.6.3 的结构化字段为 `kB`、`kilobytes`、`4 200`。4.7.1 为 `KiB`、`kibibytes`、`4 200`。后两版的英文说明相同，整条记录的指纹不同。文件和块存储后端均按可用内存分配缓冲区，块存储代码另有 64 的上限。具体默认值与板级构建有关。Copter 现有待审断言仍将此键列为未译项。[4.5.7 参数定义](https://github.com/ArduPilot/ardupilot/blob/2a3dc4b7bf2507120f7378a7b2fde73185e0c325/libraries/AP_Logger/AP_Logger.cpp#L91-L95) · [4.6.3 参数定义](https://github.com/ArduPilot/ardupilot/blob/92b0cd788ec29406f26c6f9c31d5ceedbd1cc538/libraries/AP_Logger/AP_Logger.cpp#L91-L97) · [4.7.1 文件后端](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_Logger/AP_Logger_File.cpp#L77-L92) · [4.7.1 块存储后端](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_Logger/AP_Logger_Block.cpp#L46-L60)

## LOG_DISARMED 与 LOG_REPLAY

`LOG_DISARMED` 的原选项 0、1、2、3 在七版相同。0 关闭此项额外的未解锁日志记录，1 包括未解锁阶段，2 在该阶段检测到 USB 连接时例外，3 仅在飞行器一直未解锁时于下次启动丢弃文件系统后端的该次日志。原文所说的丢弃有明确的后端和时机条件。[4.6.3 参数定义](https://github.com/ArduPilot/ardupilot/blob/92b0cd788ec29406f26c6f9c31d5ceedbd1cc538/libraries/AP_Logger/AP_Logger.cpp#L99-L104) · [4.7.1 未解锁阶段的记录判定](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_Logger/AP_Logger.cpp#L1568-L1580)

`LOG_REPLAY` 的原选项始终为 `0 Disabled`、`1 Enabled`，控制记录 EKF2／EKF3 重放诊断所需的详细数据。英文说明将飞行前重放数据与 `LOG_DISARMED` 的 1 或 2 联系起来。两键在 Sub 4.7.1 各有独立的来源记录，现有中文仅登记在 Copter 和 Plane 的六个版本，Sub 两条仍待分机型审阅。[Sub 4.7.1 参数定义](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_Logger/AP_Logger.cpp#L112-L124)

| 原键与版本 | 仓库算法计算的完整记录 SHA-256 |
| --- | --- |
| `LOG_FILE_BUFSIZE` 4.5.7 | `677a255dc0e718a8b1b02567e0a2184f804bddbe06ec81cc993970935689fbac` |
| `LOG_FILE_BUFSIZE` 4.6.3 | `572221df9e127cd721859a66b3bdb294fa366a0165bac7ce3b403d11a457e856` |
| `LOG_FILE_BUFSIZE` 4.7.1 | `74ba32412e8eb981db82fbb43f8c79c97026b425311df7c1c2d16e49db56cb28` |
| `LOG_DISARMED` 七版 | `16802fe8a387398fb7730f1c4fb9ce0a1ddd6ab7ef811658b93b95417d86b1b4` |
| `LOG_REPLAY` 七版 | `ca015b469df9d92021e1f6233ce045f771cdea5be6411c555e89c4ef6d81d426` |

## 九条分版中文说明

下表按产品原键列出五种措辞，对应九条“机型 × 版本 × 原键”记录。三个版本的 `LOG_FILE_BUFSIZE` 分别绑定各自的完整字段指纹。Sub 的两条说明单独绑定 Sub 4.7.1 的官方记录。选项编号、范围和单位取自每条记录的原字段。[原始缺口盘点](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/blob/538137ba4afd4d89bc607b66babca4309477df9a/docs/parameter-logger-remaining.zh-CN.md)

| 原键 | 固件范围 | 中文说明 |
| --- | --- | --- |
| `LOG_FILE_BUFSIZE` | Copter、Plane/QuadPlane 4.5.7 | 文件和块存储日志后端在写入块设备前先暂存数据。较大的缓冲区可能减少 SD 卡日志中的记录缺口，实际缓冲区可能因可用内存不足而缩小。PixHawk 至少需要 4 千字节，本版说明列出的上限为 64 千字节。 |
| `LOG_FILE_BUFSIZE` | Copter、Plane/QuadPlane 4.6.3 | 文件和块存储日志后端在写入块设备前先暂存数据，缓冲区上限以 kB 计。较大的缓冲区可能减少 SD 卡日志中的记录缺口，但会占用更多内存。实际缓冲区可能缩小，以腾出内存。 |
| `LOG_FILE_BUFSIZE` | Copter、Plane/QuadPlane、Sub 4.7.1 | 文件和块存储日志后端在写入块设备前先暂存数据，缓冲区上限以 KiB 计。较大的缓冲区可能减少 SD 卡日志中的记录缺口，但会占用更多内存。实际缓冲区可能缩小，以腾出内存。 |
| `LOG_DISARMED` | Sub 4.7.1 | 控制未解锁时的日志记录。设为 1 时始终记录，包括未解锁阶段。解锁前的记录有助于排查启动问题，也是 LOG_REPLAY 进行 EKF 重放所需的数据，但日志文件可能很大。设为 2 时也记录未解锁阶段，检测到 USB 连接时除外，以免下载日志或修改参数时生成多余日志。设为 3 时记录未解锁阶段。若本次运行始终未解锁，文件系统后端会在下次启动时丢弃本次日志。 |
| `LOG_REPLAY` | Sub 4.7.1 | 设为 1 时，EKF2 和 EKF3 状态估计器会记录诊断卡尔曼滤波问题所需的详细数据。要在日志中保留重放测试所需的飞行前数据，LOG_DISARMED 必须设为 1 或 2。ArduPilot 原说明建议增大 LOG_FILE_BUFSIZE 以增加日志缓冲空间，并使用优质 microSD 卡，以免丢失传感器数据。 |

本批涉及的九条均为 `Description`。Sub 的 `LOG_DISARMED` 四个原始选项为 `0 Disabled`、`1 Enabled`、`2 Disabled on USB connection`、`3 Discard log on reboot if never armed`。`LOG_REPLAY` 的两个选项为 `0 Disabled`、`1 Enabled`。4.5.7 缓冲区说明中的 4 和 64 只出现在正文。4.6.3 和 4.7.1 的原始范围都是 `4 200`，单位分别为 `kB` 和 `KiB`。
