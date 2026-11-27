# ArduPilot 日志后端参数说明

这八个参数用于在「全部参数表」辨认日志保存到哪里、何时换文件，以及流式日志消息按什么频率记录。下表保留官方原键和字段，中文只用于 `Description`。

| 原键 | 官方字段 | 中文说明 |
| --- | --- | --- |
| `LOG_BACKEND_TYPE` | 位 0 `File`、位 1 `MAVLink`、位 2 `Block` | 用位掩码选择要启用的日志后端，可同时选择多个。SITL（软件在环仿真）和带有 DataFlash 芯片的飞控板支持块存储日志后端。 |
| `LOG_FILE_DSRMROT` | `0 Disabled`、`1 Enabled` | 启用后，飞行器从解锁转为上锁时会关闭当前日志文件。若已启用 LOG_DISARMED，则另开一份日志。适用于文件和块存储日志后端。 |
| `LOG_MAV_BUFSIZE` | `kB` | MAVLink 日志后端可分配给缓冲区的最大内存。 |
| `LOG_FILE_TIMEOUT` | `s` | 日志文件持续写入失败时允许等待的时间，单位为秒。超过此时间，文件后端会关闭当前文件并停止记录。 |
| `LOG_FILE_RATEMAX` | `Hz`，0 至 1000，步长 0.1 | 流式日志消息写入文件后端的最高频率。设为 0 时，关闭此项限速。 |
| `LOG_MAV_RATEMAX` | `Hz`，0 至 1000，步长 0.1 | 通过 MAVLink 日志后端记录流式日志消息的最高频率。设为 0 时，关闭此项限速。 |
| `LOG_BLK_RATEMAX` | `Hz`，0 至 1000，步长 0.1 | 流式日志消息写入块存储后端的最高频率。设为 0 时，关闭此项限速。 |
| `LOG_DARM_RATEMAX` | `Hz`，0 至 1000，步长 0.1 | 飞行器处于上锁状态时，各日志后端记录流式日志消息的最高频率。设为 0 时，沿用各后端通常的限速设置。 |

## 分版来源与覆盖分母

基线为 [`d07af4b0`](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/commit/d07af4b04948fe73e3f9fa9c5c097e673cde3b2a)。每行按该版完整官方参数文件（PDEF）的不同原键计数。同一键在不同固件分别计数。本批八个键每版均在 `LOG` 参数组中各出现一次，在基线中均无中文说明。

| 固件 | 固定官方 PDEF | 全文件不同键 | 基线已译 | 基线未覆盖 | `LOG` 组基线已译／总数 | 本批已译 |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Copter | [4.5.7](https://autotest.ardupilot.org/Parameters/versioned/Copter/stable-4.5.7/apm.pdef.xml) | 4,426 | 85 | 4,341 | 4/13 | 8/8 |
| Copter | [4.6.3](https://autotest.ardupilot.org/Parameters/versioned/Copter/stable-4.6.3/apm.pdef.xml) | 4,776 | 87 | 4,689 | 4/13 | 8/8 |
| Copter | [4.7.1](https://autotest.ardupilot.org/Parameters/versioned/Copter/stable-4.7.1/apm.pdef.xml) | 5,688 | 93 | 5,595 | 4/13 | 8/8 |
| Plane / QuadPlane | [4.5.7](https://autotest.ardupilot.org/Parameters/versioned/Plane/stable-4.5.7/apm.pdef.xml) | 4,485 | 96 | 4,389 | 4/13 | 8/8 |
| Plane / QuadPlane | [4.6.3](https://autotest.ardupilot.org/Parameters/versioned/Plane/stable-4.6.3/apm.pdef.xml) | 4,851 | 98 | 4,753 | 4/13 | 8/8 |
| Plane / QuadPlane | [4.7.1](https://autotest.ardupilot.org/Parameters/versioned/Plane/stable-4.7.1/apm.pdef.xml) | 5,722 | 106 | 5,616 | 4/13 | 8/8 |
| Sub | [4.7.1](https://autotest.ardupilot.org/Parameters/versioned/Sub/stable-4.7.1/apm.pdef.xml) | 5,369 | 52 | 5,317 | 2/13 | 8/8 |
| 合计 | 七份固定文件 | 35,317 | 617 | 34,700 | 26/91 | 56/56 |

这八个键在 #294 合入时将 `LOG` 组的覆盖提高到 82/91 条。随后补齐七版的 `LOG_FILE_BUFSIZE` 和 Sub 4.7.1 的 `LOG_DISARMED`、`LOG_REPLAY`，组内达到 91/91 条。按 `LOG_` 前缀计为 91/98，各版机型组的 `LOG_BITMASK` 仍待翻译。分母限于表中七份 ArduPilot 文件。[后三个原键的分版记录](parameter-logger-remaining.zh-CN.md) · [固定源码提交、标签和完整 XML 摘要](../localization/parameters/sources.json)

PX4 v1.16.0 的固定参数参考列有 `SDLOG_MODE` 和 `SDLOG_PROFILE`。逐个检索这八个 ArduPilot 原键，命中 0/8 个。PX4 各构建的完整参数分母尚未核实。[PX4 v1.16.0 参数参考](https://github.com/PX4/PX4-Autopilot/blob/6ea3539157ca358c70a515878b77077af7d4611d/docs/en/advanced_config/parameter_reference.md)

## 原始定义与复核

`LOG_BACKEND_TYPE` 的位号在七版相同。Copter 和 Plane 4.5.7 的 PDEF 只有文本 `Bitmask` 字段，记录指纹为 `ef94558023286f9ac6eb483a2afdd4e406afc98bd66adc85c271f2e134c512db`。其余五版还提供结构化位节点，记录指纹为 `5c7b458c3f172dfdf78ecc1e0613001c1c7d62bc5fb96a58b5487a111bf20e11`。其余七个键各自的英文与整条记录指纹均跨七版相同。[4.5.7 定义](https://github.com/ArduPilot/ardupilot/blob/2a3dc4b7bf2507120f7378a7b2fde73185e0c325/libraries/AP_Logger/AP_Logger.cpp#L84-L181) · [4.6.3 定义](https://github.com/ArduPilot/ardupilot/blob/92b0cd788ec29406f26c6f9c31d5ceedbd1cc538/libraries/AP_Logger/AP_Logger.cpp#L84-L183) · [4.7.1 定义](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_Logger/AP_Logger.cpp#L97-L196)

在仓库根目录运行以下命令可复核 Copter 4.6.3 的文件摘要、版本标记、全文件分母、`LOG` 组原键及现有说明。其余六版用上表对应的机型、版本和文件复核。

```sh
python3 scripts/localization/audit_parameter_coverage.py Copter 4.6.3 Copter-4.6.3.xml --group LOG
```
