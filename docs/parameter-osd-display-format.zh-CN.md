# OSD 显示选项与单位制

在「中文(简体)2」的全部参数表中，以下中文说明接在固件原英文后面。两键都属于官方全局 `OSD` 参数组。

| 参数键 | 已审中文说明 |
| --- | --- |
| `OSD_OPTIONS` | 按位组合 OSD 显示选项。例如，位 1 用于反转风向箭头的显示方向。 |
| `OSD_UNITS` | 选择 OSD 显示项目所用的单位制。0 为公制，1 为英制，2 为国际单位制（SI），3 为航空单位制。 |

`OSD_OPTIONS` 的位号依次为 0 至 7。位 1 的原标签是 `InvertedWindArrow`，源码把该选项定义为 `1U<<1`，绘制风向箭头时将其显示角度转过半圈。`OSD_UNITS` 的原选项依次为 `0: Metric`、`1: Imperial`、`2: SI`、`3: Aviation`。两个参数的官方范围和单位字段均为空。[参数登记](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD.cpp#L85-L141) · [位号定义](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD.h#L604-L609) · [4.5.7 的风向箭头](https://github.com/ArduPilot/ardupilot/blob/2a3dc4b7bf2507120f7378a7b2fde73185e0c325/libraries/AP_OSD/AP_OSD_Screen.cpp#L1928-L1948) · [4.7.1 的风向箭头](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L1938-L1958)

## 版本、来源和覆盖

七份完整官方参数文件（PDEF）各包含这两个键的唯一原记录。本批增加 14/14 条分版说明。表中的分母按机型和版本分别计数。全局 `OSD` 组共有 26 个跨版不同键，各版合计 178 条，已译由 38 增至 52。完整样本共 35,317 条，已译由 696 增至 710，余 34,607 条。[开发线基线](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/commit/8cc13236db9c3c482b4ca32d6b6e82419ca7fc4c) · [下载 SHA-256、发布标签与源码提交](../localization/parameters/sources.json)

| 机型 | 官方 PDEF | OPTIONS 行 | UNITS 行 | 全局 OSD 已译变化 / 分母 | 全文件已译变化 / 分母 |
| --- | --- | ---: | ---: | ---: | ---: |
| Copter | [4.5.7](https://autotest.ardupilot.org/Parameters/versioned/Copter/stable-4.5.7/apm.pdef.xml) | 14062–14064 | 14085–14092 | 4 → 6 / 24 | 96 → 98 / 4,426 |
| Copter | [4.6.3](https://autotest.ardupilot.org/Parameters/versioned/Copter/stable-4.6.3/apm.pdef.xml) | 17200–17212 | 17233–17240 | 6 → 8 / 26 | 98 → 100 / 4,776 |
| Copter | [4.7.1](https://autotest.ardupilot.org/Parameters/versioned/Copter/stable-4.7.1/apm.pdef.xml) | 22041–22053 | 22074–22081 | 6 → 8 / 26 | 104 → 106 / 5,688 |
| Plane / QuadPlane | [4.5.7](https://autotest.ardupilot.org/Parameters/versioned/Plane/stable-4.5.7/apm.pdef.xml) | 12906–12908 | 12929–12936 | 4 → 6 / 24 | 107 → 109 / 4,485 |
| Plane / QuadPlane | [4.6.3](https://autotest.ardupilot.org/Parameters/versioned/Plane/stable-4.6.3/apm.pdef.xml) | 16013–16025 | 16046–16053 | 6 → 8 / 26 | 109 → 111 / 4,851 |
| Plane / QuadPlane | [4.7.1](https://autotest.ardupilot.org/Parameters/versioned/Plane/stable-4.7.1/apm.pdef.xml) | 20712–20724 | 20745–20752 | 6 → 8 / 26 | 117 → 119 / 5,722 |
| Sub | [4.7.1](https://autotest.ardupilot.org/Parameters/versioned/Sub/stable-4.7.1/apm.pdef.xml) | 26872–26884 | 26905–26912 | 6 → 8 / 26 | 65 → 67 / 5,369 |

每版全局 `OSD` 组还余下相同的 18 个原键，共 126 条分版记录。

`OSD_ARM_SCR`、`OSD_BTN_DELAY`、`OSD_CELL_COUNT`、`OSD_CHAN`、`OSD_DSARM_SCR`、`OSD_FS_SCR`、`OSD_SW_METHOD`、`OSD_TYPE`、`OSD_TYPE2`、`OSD_W_ACRVOLT`、`OSD_W_AVGCELLV`、`OSD_W_BATVOLT`、`OSD_W_LQ`、`OSD_W_NSAT`、`OSD_W_RESTVOLT`、`OSD_W_RSSI`、`OSD_W_SNR`、`OSD_W_TERR`。

PX4 v1.14、v1.15、v1.16 的官方参数参考均未列出这两个原键。上表分母只含七份 ArduPilot PDEF。[PX4 v1.14](https://docs.px4.io/v1.14/en/advanced_config/parameter_reference) · [PX4 v1.15](https://docs.px4.io/v1.15/en/advanced_config/parameter_reference) · [PX4 v1.16](https://docs.px4.io/v1.16/en/advanced_config/parameter_reference)

## 指纹与独立读取用例

| 原键和适用记录 | 完整记录 SHA-256 | 参数表选项格 SHA-256 |
| --- | --- | --- |
| `OSD_OPTIONS`，Copter / Plane 4.5.7，仅有 `Bitmask` 文本 | `5cb8e55ecc7d0714db266538cc01b3e6531862a6a516d7c7e4e87b513ee38849` | `d73a5fe3c2355b1e27e420ceb5eb6873ff838e23ddf167b88accea1907660b5e` |
| `OSD_OPTIONS`，其余五版，同时有 `Bitmask` 文本和八个位节点 | `48e4dd0416fa33e71c155cfa3f2ecef918586795fe3d20b12071258e5a8b0b84` | `d73a5fe3c2355b1e27e420ceb5eb6873ff838e23ddf167b88accea1907660b5e` |
| `OSD_UNITS`，七版，四个枚举节点 | `998d77d0bc87d54119261aee4d58b8177ba7c5d506d5aa414dff01e8198e3708` | `c045f7167a2b13910e005d34fb5d8a3bc844fd9a5368a2ad0d2aa28aad7e1cc3` |

选项格摘要按 UTF-8、LF 分行、末行无换行计算。错版用例在 Copter 4.6.3 的内存快照中换入 4.5.7 的 `OSD_OPTIONS` 原节点。两版的英文及八行选项逐字相同，生产显示入口应保留这些内容、提示中文待复核，并撤下旧中文搜索命中。同一快照中的 `OSD_UNITS` 仍应显示已审中文。未登记的 4.7.2 沿用原有回退。

在仓库根目录运行以下定向读取检查。第二条命令接受来源表中同版本的完整官方 XML。

```sh
dotnet run --project MissionPlannerTests/ParameterDescriptions/ParameterDescriptions.csproj -c Release -- --osd-display-format
python scripts/localization/audit_parameter_coverage.py Copter 4.6.3 Copter-4.6.3.xml --group OSD
```
