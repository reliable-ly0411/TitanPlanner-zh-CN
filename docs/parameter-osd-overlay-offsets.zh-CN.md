# OSD 叠加位置的分版说明

`OSD_H_OFFSET` 和 `OSD_V_OFFSET` 控制整层 OSD 内容在视频画面中的位置。两键属于固定 PDEF 的 `libraries/parameters[@name='OSD']` 组。七版原记录均包含 `Range` 和 `RebootRequired=True` 两个字段，单位、枚举和位选项均为空。中文只登记在对应机型和版本的 `Description` 字段中。

| 原键 | 官方英文 | 中文说明 | 原范围 |
| --- | --- | --- | --- |
| `OSD_H_OFFSET` | Sets horizontal offset of the osd inside image | 设置 OSD 叠加内容在视频画面中的水平偏移。 | `0 63` |
| `OSD_V_OFFSET` | Sets vertical offset of the osd inside image | 设置 OSD 叠加内容在视频画面中的垂直偏移。 | `0 31` |

源文件将两个参数标为需要重启。MAX7456 后端使用水平和垂直偏移寄存器，刷新时也会检查偏移是否变化。参数表按原记录保留重启标记。译文只描述偏移的轴向，硬件坐标单位和正方向在固定 PDEF 中均未注明。[4.5.7 定义](https://github.com/ArduPilot/ardupilot/blob/2a3dc4b7bf2507120f7378a7b2fde73185e0c325/libraries/AP_OSD/AP_OSD.cpp#L99-L113) · [4.6.3 定义](https://github.com/ArduPilot/ardupilot/blob/92b0cd788ec29406f26c6f9c31d5ceedbd1cc538/libraries/AP_OSD/AP_OSD.cpp#L99-L113) · [4.7.1 定义](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD.cpp#L99-L113) · [MAX7456 偏移寄存器及刷新](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_MAX7456.cpp#L345-L377)

## 分版覆盖

每行按一份完整官方 PDEF 的不同原键计数。两个选定原键在每份文件中各出现一次，本批新增 14/14 条分版说明。相同机型的相同原文和完整字段指纹共用一条目录记录，共六条。下表列出新增前后的全局 `OSD` 组覆盖，以及新增后全文件的覆盖。[原开发线基线](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/commit/b7205463f2ea61fa531282418dd7e9fcd75439e4) · [文件 SHA-256、固件标签与源码提交](../localization/parameters/sources.json)

| 固件 | 固定 PDEF | 原始行号（水平 / 垂直） | 全局 OSD 组已译变化 / 总数 | 全文件已译 / 不同键 | 全文件未译 |
| --- | --- | ---: | ---: | ---: | ---: |
| Copter | [4.5.7](https://autotest.ardupilot.org/Parameters/versioned/Copter/stable-4.5.7/apm.pdef.xml) | 14072 / 14068 | 2 → 4 / 24 | 96 / 4,426 | 4,330 |
| Copter | [4.6.3](https://autotest.ardupilot.org/Parameters/versioned/Copter/stable-4.6.3/apm.pdef.xml) | 17220 / 17216 | 4 → 6 / 26 | 98 / 4,776 | 4,678 |
| Copter | [4.7.1](https://autotest.ardupilot.org/Parameters/versioned/Copter/stable-4.7.1/apm.pdef.xml) | 22061 / 22057 | 4 → 6 / 26 | 104 / 5,688 | 5,584 |
| Plane / QuadPlane | [4.5.7](https://autotest.ardupilot.org/Parameters/versioned/Plane/stable-4.5.7/apm.pdef.xml) | 12916 / 12912 | 2 → 4 / 24 | 107 / 4,485 | 4,378 |
| Plane / QuadPlane | [4.6.3](https://autotest.ardupilot.org/Parameters/versioned/Plane/stable-4.6.3/apm.pdef.xml) | 16033 / 16029 | 4 → 6 / 26 | 109 / 4,851 | 4,742 |
| Plane / QuadPlane | [4.7.1](https://autotest.ardupilot.org/Parameters/versioned/Plane/stable-4.7.1/apm.pdef.xml) | 20732 / 20728 | 4 → 6 / 26 | 117 / 5,722 | 5,605 |
| Sub | [4.7.1](https://autotest.ardupilot.org/Parameters/versioned/Sub/stable-4.7.1/apm.pdef.xml) | 26892 / 26888 | 4 → 6 / 26 | 65 / 5,369 | 5,304 |
| 合计 | 七份完整文件 | | 24 → 38 / 178 | 696 / 35,317 | 34,621 |

七份文件中，全局 `OSD` 组的原键集合与 `OSD_` 前缀的原键集合相同。每版仍有以下 20 个键待译，共 140 条。分屏组 `OSD1_` 至 `OSD4_` 单独计数。

- `OSD_ARM_SCR`、`OSD_BTN_DELAY`、`OSD_CELL_COUNT`、`OSD_CHAN`、`OSD_DSARM_SCR`、`OSD_FS_SCR`
- `OSD_OPTIONS`、`OSD_SW_METHOD`、`OSD_TYPE`、`OSD_TYPE2`、`OSD_UNITS`
- `OSD_W_ACRVOLT`、`OSD_W_AVGCELLV`、`OSD_W_BATVOLT`、`OSD_W_LQ`、`OSD_W_NSAT`、`OSD_W_RESTVOLT`、`OSD_W_RSSI`、`OSD_W_SNR`、`OSD_W_TERR`

PX4 v1.16.0 固定参数参考中，这两个原键均为零命中。表中分母仅覆盖上述七份 ArduPilot 文件。PX4 各构建的实际参数总数未核实。[PX4 v1.16.0 参数参考](https://github.com/PX4/PX4-Autopilot/blob/6ea3539157ca358c70a515878b77077af7d4611d/docs/en/advanced_config/parameter_reference.md)

## 代表行与复核入口

Copter 4.6.3 可使用内存样例 `OSD_H_OFFSET=12`、`OSD_V_OFFSET=7`。在增强简中，说明格预期为上表的完整英文和对应中文，选项格分别显示 `0 63`、`0 31`，单位格留空。表中数字仅用于只读检查。

| 原键 | 英文 SHA-256 | 整条官方记录 SHA-256 |
| --- | --- | --- |
| `OSD_H_OFFSET` | `81e8b12ac6f4e13ea24bc5f39fbb8587cd43330dbda68aeab3afe286b5d92b03` | `1f151c3eee9c597747ef39f85c31bb183f009df0f6a0a91bc15057390e0b07e1` |
| `OSD_V_OFFSET` | `3febbbd768fc03db24d0e40b919d4db67bed8a43e0f9b9e4d598fc80522d6b3d` | `5406cfb2fd53cabeea901c0b56dce20314d98da92b66923aa1c9caae91b292c4` |

生产读取类的定向检查覆盖十四条分版记录。回退对照保持 Copter 4.6.3 水平偏移的英文，在内存中把范围改成合成值 `0 62`，预期显示待复核、撤下旧中文，同一快照中的垂直偏移仍匹配。未登记的 4.7.2 上下文也不能继承这些译文。Windows 真实表格的三语言显示、布局和 DPI 由后续代表件验收。

```sh
dotnet run --project MissionPlannerTests/ParameterDescriptions/ParameterDescriptions.csproj -c Release -- --osd-overlay-offsets
python scripts/localization/audit_parameter_coverage.py Copter 4.6.3 Copter-4.6.3.xml --group OSD
```
