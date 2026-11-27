# OSD 显示参数的分版说明与覆盖

本批在七份固定 ArduPilot 参数定义中补充 27 个原键的中文 `Description`。本批选取全局 `OSD` 组的 15 个键，以及 `OSD1_` 至 `OSD4_` 四个分屏组的 12 个风速与风向显示键。每键每版均唯一，合计新增 189 个参数与版本组合。显示说明由原英文和整条官方记录的指纹匹配。参数键、数值、枚举、位号、范围、单位及版本回退沿用产品原路径。

最初盘点基线有 696 条已审说明。#305 随后合入，为每版补一条 `LOG_BITMASK` 说明。合入 #305 后为 703 条。最终生成基线随后合入 #309 的寻机提示文案，说明总数及 OSD 分组计数维持同一口径。 [最初盘点基线 `8cc13236db9c3c482b4ca32d6b6e82419ca7fc4c`](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/commit/8cc13236db9c3c482b4ca32d6b6e82419ca7fc4c) · [#305 合入时的开发线 `23319b3b`](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/commit/23319b3b8709a34eea513e7f7b6dcd65e7e4e815) · [最终生成基线 `425aa2e3182e5af3c7409846a9c993c7d73db2d1`](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/commit/425aa2e3182e5af3c7409846a9c993c7d73db2d1) · [#305](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/pull/305)

## 固定 PDEF 与覆盖

分母按每份完整 PDEF 中不同的原键计算，同名重复定义只计一个键。全文件和官方组各自统计，覆盖表示具有已审 `Description` 的参数与版本组合。Plane 列同时对应 QuadPlane。Rover 的条件是共享源码中的原文语义，Rover 和 PX4 都未纳入这七份固定文件的分母。 [完整 PDEF SHA‑256、标签及源码提交登记](../localization/parameters/sources.json)

| 固件 | 官方固定 PDEF | ArduPilot 源码提交 | 全文件已译变化 / 分母 | 全文件余项 | 全局 OSD 已译变化 / 分母 | 四屏合计已译变化 / 分母 |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Copter 4.5.7 | [固定 PDEF](https://autotest.ardupilot.org/Parameters/versioned/Copter/stable-4.5.7/apm.pdef.xml) | [`2a3dc4b7`](https://github.com/ArduPilot/ardupilot/commit/2a3dc4b7bf2507120f7378a7b2fde73185e0c325) | 97 → 124 / 4,426 | 4,302 | 4 → 19 / 24 | 4 → 16 / 804 |
| Copter 4.6.3 | [固定 PDEF](https://autotest.ardupilot.org/Parameters/versioned/Copter/stable-4.6.3/apm.pdef.xml) | [`92b0cd78`](https://github.com/ArduPilot/ardupilot/commit/92b0cd788ec29406f26c6f9c31d5ceedbd1cc538) | 99 → 126 / 4,776 | 4,650 | 6 → 21 / 26 | 4 → 16 / 804 |
| Copter 4.7.1 | [固定 PDEF](https://autotest.ardupilot.org/Parameters/versioned/Copter/stable-4.7.1/apm.pdef.xml) | [`dbe79216`](https://github.com/ArduPilot/ardupilot/commit/dbe792162d06cab66c3475fd5556bf7a120f119e) | 105 → 132 / 5,688 | 5,556 | 6 → 21 / 26 | 4 → 16 / 804 |
| Plane / QuadPlane 4.5.7 | [固定 PDEF](https://autotest.ardupilot.org/Parameters/versioned/Plane/stable-4.5.7/apm.pdef.xml) | [`0358a9c2`](https://github.com/ArduPilot/ardupilot/commit/0358a9c210bc6c965006f5d6029239b7033616df) | 108 → 135 / 4,485 | 4,350 | 4 → 19 / 24 | 4 → 16 / 804 |
| Plane / QuadPlane 4.6.3 | [固定 PDEF](https://autotest.ardupilot.org/Parameters/versioned/Plane/stable-4.6.3/apm.pdef.xml) | [`3fc7011a`](https://github.com/ArduPilot/ardupilot/commit/3fc7011a7d3dc047cbb17d8bd98ee94577d144c6) | 110 → 137 / 4,851 | 4,714 | 6 → 21 / 26 | 4 → 16 / 804 |
| Plane / QuadPlane 4.7.1 | [固定 PDEF](https://autotest.ardupilot.org/Parameters/versioned/Plane/stable-4.7.1/apm.pdef.xml) | [`dbe79216`](https://github.com/ArduPilot/ardupilot/commit/dbe792162d06cab66c3475fd5556bf7a120f119e) | 118 → 145 / 5,722 | 5,577 | 6 → 21 / 26 | 4 → 16 / 804 |
| Sub 4.7.1 | [固定 PDEF](https://autotest.ardupilot.org/Parameters/versioned/Sub/stable-4.7.1/apm.pdef.xml) | [`dbe79216`](https://github.com/ArduPilot/ardupilot/commit/dbe792162d06cab66c3475fd5556bf7a120f119e) | 66 → 93 / 5,369 | 5,276 | 6 → 21 / 26 | 4 → 16 / 804 |
| **合计** | **七版** | | **703 → 892 / 35,317** | **34,425** | **38 → 143 / 178** | **28 → 112 / 5,628** |

全局组选集新增 `105/105`，组内仍有 35 个未覆盖组合。风速与风向显示选集新增 `84/84`，四屏组内仍有 5,516 个未覆盖组合。四个屏组每屏每版均为 201 键，其中基线只译了对应的字体键。加入本批后，每屏每版为 4/201，仍有 197 个未覆盖。同一原键按车型和完整记录指纹归并版本，15 个全局键形成 47 条目录记录，12 个风显示键形成 36 条。目录记录数合计 83，展开后对应上述 189 个组合。

## 已审中文与原字段边界

- **显示选项和单位制（2 键）。** 两个 4.5.7 PDEF 的 `OSD_OPTIONS` 只含文本 `Bitmask` 字段，其他五版还含结构化位节点。七版的英文说明和八个位号、标签、顺序相同。`OSD_UNITS` 使用 0–3 的原枚举。两键的结构化单位和范围均为空。 [选项定义](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD.cpp#L85-L90) · [单位制枚举](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD.cpp#L136-L142)
- **闪烁阈值、事件切屏和电芯数（13 键）。** RSSI 的定义同时保留正百分比、负 dBm 两种表示，默认分别为 30% 和 −100 dBm，PDEF 的单位格仍为空。地形阈值保留 `m` 和 `-1` 关闭闪烁。切屏条件包括上锁、解锁，以及遥控或电池失效保护开始，目的画面需要已启用。`OSD_CELL_COUNT` 的 0 自动估算电芯数，接入时电量较低的锂聚合物或锂离子电池可能估算不准。−1 时两项平均单体电压不显示数值。 [全局定义](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD.cpp#L115-L230) · [事件切屏](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD.cpp#L510-L545) · [单体电压显示分支](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L1453-L1508) · [地形闪烁条件](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L2512)
- **四屏风速与风向显示（12 键）。** 各屏 `EN` 的选项沿用 `0 Disabled` 和 `1 Enabled`，横向 `X` 范围 `0 59`，纵向 `Y` 范围 `0 21`。坐标字段没有单位，译文使用水平和垂直位置。开启项保留相对风向，以及 Rover 装有风向标时显示表观风速和风向的原条件。四屏开关在七版均使用同一英文说明。 [四屏注册](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD.cpp#L62-L76) · [风显示定义](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L309-L323) · [风向标条件](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L1938-L1962)

除地形阈值已有的 `m` 字段，其余本批原记录的结构化单位字段为空。译文内的百分比、dBm 或 dB 来自英文语义，数值单位和选项格不因此补写。以下中文均只用于 `Description`。

| 原键 | 中文说明 |
| --- | --- |
| `OSD_OPTIONS` | 按位组合 OSD 显示选项。例如，位 1 用于反转风向箭头的显示方向。 |
| `OSD_UNITS` | 选择 OSD 显示项目所用的单位制。0 为公制，1 为英制，2 为国际单位制（SI），3 为航空单位制。 |
| `OSD_W_RSSI` | 设置 OSD 的 RSSI 项目闪烁阈值。按所用的显示方式填写正百分比或负 dBm 值，默认分别为 30% 和 -100 dBm。 |
| `OSD_W_NSAT` | 当卫星数低于此阈值时，OSD 的 NSAT 项目闪烁。 |
| `OSD_W_BATVOLT` | 当电池电压低于此阈值时，OSD 的 BAT_VOLT 项目闪烁。 |
| `OSD_W_TERR` | 当距地形的高度低于此阈值时，OSD 的 TER_HGT 项目闪烁。阈值单位为米，设为 -1 可关闭此闪烁提示。 |
| `OSD_W_AVGCELLV` | 当单节平均电压低于此阈值时，OSD 的 AVGCELLV 项目闪烁。 |
| `OSD_W_RESTVOLT` | 当估算的电池静置电压低于此阈值时，OSD 的 RESTVOLT 项目闪烁。 |
| `OSD_W_ACRVOLT` | 当估算的单节平均静置电压低于此阈值时，OSD 的 ACRVOLT 项目闪烁。 |
| `OSD_W_LQ` | 当遥控链路质量低于此阈值时，OSD 的 RC_LQ 项目闪烁。阈值以百分比表示。 |
| `OSD_W_SNR` | 当遥控链路信噪比低于此阈值时，OSD 的 RC_SNR 项目闪烁。阈值单位为 dB。 |
| `OSD_ARM_SCR` | 飞控由上锁转为解锁时，切换到所选且已启用的 OSD 画面。设为 0 时，不因解锁事件切换画面。 |
| `OSD_DSARM_SCR` | 飞控由解锁转为上锁时，切换到所选且已启用的 OSD 画面。设为 0 时，不因上锁事件切换画面。 |
| `OSD_FS_SCR` | 遥控或电池失效保护开始时，切换到所选且已启用的 OSD 画面。设为 0 时，不指定进入保护时要切换到的画面。 |
| `OSD_CELL_COUNT` | 仅用于计算 OSD 显示的平均单体电压和平均单体静置电压。设为 -1 时，这两项不显示电压数值。设为 0 时自动估算电芯数，接入时电量较低的锂聚合物或锂离子电池可能估算不准。正整数表示手动指定的电芯数。 |
| `OSD1_WIND_EN` | 控制第 1 个 OSD 画面是否显示风速和相对风向。Rover 若装有风向标，则显示由风向标测得的表观风速和风向。 |
| `OSD1_WIND_X` | 设置第 1 个 OSD 画面中风速和风向显示项的水平位置。 |
| `OSD1_WIND_Y` | 设置第 1 个 OSD 画面中风速和风向显示项的垂直位置。 |
| `OSD2_WIND_EN` | 控制第 2 个 OSD 画面是否显示风速和相对风向。Rover 若装有风向标，则显示由风向标测得的表观风速和风向。 |
| `OSD2_WIND_X` | 设置第 2 个 OSD 画面中风速和风向显示项的水平位置。 |
| `OSD2_WIND_Y` | 设置第 2 个 OSD 画面中风速和风向显示项的垂直位置。 |
| `OSD3_WIND_EN` | 控制第 3 个 OSD 画面是否显示风速和相对风向。Rover 若装有风向标，则显示由风向标测得的表观风速和风向。 |
| `OSD3_WIND_X` | 设置第 3 个 OSD 画面中风速和风向显示项的水平位置。 |
| `OSD3_WIND_Y` | 设置第 3 个 OSD 画面中风速和风向显示项的垂直位置。 |
| `OSD4_WIND_EN` | 控制第 4 个 OSD 画面是否显示风速和相对风向。Rover 若装有风向标，则显示由风向标测得的表观风速和风向。 |
| `OSD4_WIND_X` | 设置第 4 个 OSD 画面中风速和风向显示项的水平位置。 |
| `OSD4_WIND_Y` | 设置第 4 个 OSD 画面中风速和风向显示项的垂直位置。 |

## 基线未译的 20 个全局原键

指纹为整个原记录 SHA‑256 的前 16 位。可按上表链接取得对应的完整 PDEF，按 `name` 定位记录。Copter / Plane 4.5.7 共用一组旧式位掩码指纹，其余五版共用另一组。表中其余 19 个键均为各键七版同指纹。 [完整记录指纹算法](../scripts/localization/build_parameter_catalog.py)

| 原键 | 本批处理 | 完整记录指纹分组 |
| --- | --- | --- |
| `OSD_ARM_SCR` | 入选 | 七版 `dddc2629c21af124` |
| `OSD_BTN_DELAY` | 暂缓，随摇杆参数页导航与按键去抖另审。 | 七版 `3d6ea03369ad26cb` |
| `OSD_CELL_COUNT` | 入选 | 七版 `ac88fcbce0379ccb` |
| `OSD_CHAN` | 暂缓，随遥控切屏通道及其与切屏方法的配合另审。 | 七版 `3e9ce54248f68e3f` |
| `OSD_DSARM_SCR` | 入选 | 七版 `842ff29fabb87a6a` |
| `OSD_FS_SCR` | 入选 | 七版 `a30d5a8eb7fe5a93` |
| `OSD_OPTIONS` | 入选 | Copter / Plane 4.5.7 `5cb8e55ecc7d0714`，其余五版 `48e4dd0416fa33e7` |
| `OSD_SW_METHOD` | 暂缓，随遥控切屏方法和各画面的 PWM 范围另审。 | 七版 `9eaca76edb4f5a76` |
| `OSD_TYPE` | 暂缓，随板型及主显示后端的协议与硬件支持另审。 | 七版 `259e7b27f25c3b8d` |
| `OSD_TYPE2` | 暂缓，随第二显示后端及双后端组合支持另审。 | 七版 `56f85244f1d9297b` |
| `OSD_UNITS` | 入选 | 七版 `998d77d0bc87d541` |
| `OSD_W_ACRVOLT` | 入选 | 七版 `e97655ed6339d4e0` |
| `OSD_W_AVGCELLV` | 入选 | 七版 `b4c475e865e1b016` |
| `OSD_W_BATVOLT` | 入选 | 七版 `8708697a5a5674e1` |
| `OSD_W_LQ` | 入选 | 七版 `df6a481d0aad2922` |
| `OSD_W_NSAT` | 入选 | 七版 `a85b63ecb2a2f730` |
| `OSD_W_RESTVOLT` | 入选 | 七版 `801f169b49f9a10b` |
| `OSD_W_RSSI` | 入选 | 七版 `d15e7a205ca09522` |
| `OSD_W_SNR` | 入选 | 七版 `abcdc5fc6d98ed34` |
| `OSD_W_TERR` | 入选 | 七版 `f769e176e3265393` |

## 四个屏组在基线的余项

下表每行列出同一个键后缀在四屏的完整原键，合计 200 行、800 个不同原键。加粗的 12 键为本批风显示范围，其他 788 键仍待覆盖。每个屏组有 199 个基线未译键各自七版同指纹，只有每屏的 `TXT_RES` 跨版出现两组。两份 4.5.7 原文与枚举包含 `30x16` 和 `50x18`，其余五版原文更新并增加 `2: 60x22`。官方原说明将该项限制于 MSP DisplayPort。 [文字分辨率定义](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L1072-L1080)

| 原键 | Copter / Plane 4.5.7 指纹 | 其余五版指纹 |
| --- | --- | --- |
| `OSD1_TXT_RES` | `2b912e96fe3c9f14` | `ac6c2504a2d3602e` |
| `OSD2_TXT_RES` | `287cee7a91fc6b7f` | `dc5941b397c2c34c` |
| `OSD3_TXT_RES` | `5e81d08651757ff3` | `17de69ec5d52f88f` |
| `OSD4_TXT_RES` | `efdf8af8f55f612e` | `b2bd7da85857da15` |

<details>
<summary>展开 200 行四屏原键</summary>

| OSD1_ | OSD2_ | OSD3_ | OSD4_ |
| --- | --- | --- | --- |
| `OSD1_ACRVOLT_EN` | `OSD2_ACRVOLT_EN` | `OSD3_ACRVOLT_EN` | `OSD4_ACRVOLT_EN` |
| `OSD1_ACRVOLT_X` | `OSD2_ACRVOLT_X` | `OSD3_ACRVOLT_X` | `OSD4_ACRVOLT_X` |
| `OSD1_ACRVOLT_Y` | `OSD2_ACRVOLT_Y` | `OSD3_ACRVOLT_Y` | `OSD4_ACRVOLT_Y` |
| `OSD1_ALTITUDE_EN` | `OSD2_ALTITUDE_EN` | `OSD3_ALTITUDE_EN` | `OSD4_ALTITUDE_EN` |
| `OSD1_ALTITUDE_X` | `OSD2_ALTITUDE_X` | `OSD3_ALTITUDE_X` | `OSD4_ALTITUDE_X` |
| `OSD1_ALTITUDE_Y` | `OSD2_ALTITUDE_Y` | `OSD3_ALTITUDE_Y` | `OSD4_ALTITUDE_Y` |
| `OSD1_ARMING_EN` | `OSD2_ARMING_EN` | `OSD3_ARMING_EN` | `OSD4_ARMING_EN` |
| `OSD1_ARMING_X` | `OSD2_ARMING_X` | `OSD3_ARMING_X` | `OSD4_ARMING_X` |
| `OSD1_ARMING_Y` | `OSD2_ARMING_Y` | `OSD3_ARMING_Y` | `OSD4_ARMING_Y` |
| `OSD1_ASPD1_EN` | `OSD2_ASPD1_EN` | `OSD3_ASPD1_EN` | `OSD4_ASPD1_EN` |
| `OSD1_ASPD1_X` | `OSD2_ASPD1_X` | `OSD3_ASPD1_X` | `OSD4_ASPD1_X` |
| `OSD1_ASPD1_Y` | `OSD2_ASPD1_Y` | `OSD3_ASPD1_Y` | `OSD4_ASPD1_Y` |
| `OSD1_ASPD2_EN` | `OSD2_ASPD2_EN` | `OSD3_ASPD2_EN` | `OSD4_ASPD2_EN` |
| `OSD1_ASPD2_X` | `OSD2_ASPD2_X` | `OSD3_ASPD2_X` | `OSD4_ASPD2_X` |
| `OSD1_ASPD2_Y` | `OSD2_ASPD2_Y` | `OSD3_ASPD2_Y` | `OSD4_ASPD2_Y` |
| `OSD1_ASPEED_EN` | `OSD2_ASPEED_EN` | `OSD3_ASPEED_EN` | `OSD4_ASPEED_EN` |
| `OSD1_ASPEED_X` | `OSD2_ASPEED_X` | `OSD3_ASPEED_X` | `OSD4_ASPEED_X` |
| `OSD1_ASPEED_Y` | `OSD2_ASPEED_Y` | `OSD3_ASPEED_Y` | `OSD4_ASPEED_Y` |
| `OSD1_ATEMP_EN` | `OSD2_ATEMP_EN` | `OSD3_ATEMP_EN` | `OSD4_ATEMP_EN` |
| `OSD1_ATEMP_X` | `OSD2_ATEMP_X` | `OSD3_ATEMP_X` | `OSD4_ATEMP_X` |
| `OSD1_ATEMP_Y` | `OSD2_ATEMP_Y` | `OSD3_ATEMP_Y` | `OSD4_ATEMP_Y` |
| `OSD1_AVGCELLV_EN` | `OSD2_AVGCELLV_EN` | `OSD3_AVGCELLV_EN` | `OSD4_AVGCELLV_EN` |
| `OSD1_AVGCELLV_X` | `OSD2_AVGCELLV_X` | `OSD3_AVGCELLV_X` | `OSD4_AVGCELLV_X` |
| `OSD1_AVGCELLV_Y` | `OSD2_AVGCELLV_Y` | `OSD3_AVGCELLV_Y` | `OSD4_AVGCELLV_Y` |
| `OSD1_BAT2USED_EN` | `OSD2_BAT2USED_EN` | `OSD3_BAT2USED_EN` | `OSD4_BAT2USED_EN` |
| `OSD1_BAT2USED_X` | `OSD2_BAT2USED_X` | `OSD3_BAT2USED_X` | `OSD4_BAT2USED_X` |
| `OSD1_BAT2USED_Y` | `OSD2_BAT2USED_Y` | `OSD3_BAT2USED_Y` | `OSD4_BAT2USED_Y` |
| `OSD1_BAT2_VLT_EN` | `OSD2_BAT2_VLT_EN` | `OSD3_BAT2_VLT_EN` | `OSD4_BAT2_VLT_EN` |
| `OSD1_BAT2_VLT_X` | `OSD2_BAT2_VLT_X` | `OSD3_BAT2_VLT_X` | `OSD4_BAT2_VLT_X` |
| `OSD1_BAT2_VLT_Y` | `OSD2_BAT2_VLT_Y` | `OSD3_BAT2_VLT_Y` | `OSD4_BAT2_VLT_Y` |
| `OSD1_BATTBAR_EN` | `OSD2_BATTBAR_EN` | `OSD3_BATTBAR_EN` | `OSD4_BATTBAR_EN` |
| `OSD1_BATTBAR_X` | `OSD2_BATTBAR_X` | `OSD3_BATTBAR_X` | `OSD4_BATTBAR_X` |
| `OSD1_BATTBAR_Y` | `OSD2_BATTBAR_Y` | `OSD3_BATTBAR_Y` | `OSD4_BATTBAR_Y` |
| `OSD1_BATUSED_EN` | `OSD2_BATUSED_EN` | `OSD3_BATUSED_EN` | `OSD4_BATUSED_EN` |
| `OSD1_BATUSED_X` | `OSD2_BATUSED_X` | `OSD3_BATUSED_X` | `OSD4_BATUSED_X` |
| `OSD1_BATUSED_Y` | `OSD2_BATUSED_Y` | `OSD3_BATUSED_Y` | `OSD4_BATUSED_Y` |
| `OSD1_BAT_VOLT_EN` | `OSD2_BAT_VOLT_EN` | `OSD3_BAT_VOLT_EN` | `OSD4_BAT_VOLT_EN` |
| `OSD1_BAT_VOLT_X` | `OSD2_BAT_VOLT_X` | `OSD3_BAT_VOLT_X` | `OSD4_BAT_VOLT_X` |
| `OSD1_BAT_VOLT_Y` | `OSD2_BAT_VOLT_Y` | `OSD3_BAT_VOLT_Y` | `OSD4_BAT_VOLT_Y` |
| `OSD1_BTEMP_EN` | `OSD2_BTEMP_EN` | `OSD3_BTEMP_EN` | `OSD4_BTEMP_EN` |
| `OSD1_BTEMP_X` | `OSD2_BTEMP_X` | `OSD3_BTEMP_X` | `OSD4_BTEMP_X` |
| `OSD1_BTEMP_Y` | `OSD2_BTEMP_Y` | `OSD3_BTEMP_Y` | `OSD4_BTEMP_Y` |
| `OSD1_CALLSIGN_EN` | `OSD2_CALLSIGN_EN` | `OSD3_CALLSIGN_EN` | `OSD4_CALLSIGN_EN` |
| `OSD1_CALLSIGN_X` | `OSD2_CALLSIGN_X` | `OSD3_CALLSIGN_X` | `OSD4_CALLSIGN_X` |
| `OSD1_CALLSIGN_Y` | `OSD2_CALLSIGN_Y` | `OSD3_CALLSIGN_Y` | `OSD4_CALLSIGN_Y` |
| `OSD1_CELLVOLT_EN` | `OSD2_CELLVOLT_EN` | `OSD3_CELLVOLT_EN` | `OSD4_CELLVOLT_EN` |
| `OSD1_CELLVOLT_X` | `OSD2_CELLVOLT_X` | `OSD3_CELLVOLT_X` | `OSD4_CELLVOLT_X` |
| `OSD1_CELLVOLT_Y` | `OSD2_CELLVOLT_Y` | `OSD3_CELLVOLT_Y` | `OSD4_CELLVOLT_Y` |
| `OSD1_CHAN_MAX` | `OSD2_CHAN_MAX` | `OSD3_CHAN_MAX` | `OSD4_CHAN_MAX` |
| `OSD1_CHAN_MIN` | `OSD2_CHAN_MIN` | `OSD3_CHAN_MIN` | `OSD4_CHAN_MIN` |
| `OSD1_CLIMBEFF_EN` | `OSD2_CLIMBEFF_EN` | `OSD3_CLIMBEFF_EN` | `OSD4_CLIMBEFF_EN` |
| `OSD1_CLIMBEFF_X` | `OSD2_CLIMBEFF_X` | `OSD3_CLIMBEFF_X` | `OSD4_CLIMBEFF_X` |
| `OSD1_CLIMBEFF_Y` | `OSD2_CLIMBEFF_Y` | `OSD3_CLIMBEFF_Y` | `OSD4_CLIMBEFF_Y` |
| `OSD1_CLK_EN` | `OSD2_CLK_EN` | `OSD3_CLK_EN` | `OSD4_CLK_EN` |
| `OSD1_CLK_X` | `OSD2_CLK_X` | `OSD3_CLK_X` | `OSD4_CLK_X` |
| `OSD1_CLK_Y` | `OSD2_CLK_Y` | `OSD3_CLK_Y` | `OSD4_CLK_Y` |
| `OSD1_COMPASS_EN` | `OSD2_COMPASS_EN` | `OSD3_COMPASS_EN` | `OSD4_COMPASS_EN` |
| `OSD1_COMPASS_X` | `OSD2_COMPASS_X` | `OSD3_COMPASS_X` | `OSD4_COMPASS_X` |
| `OSD1_COMPASS_Y` | `OSD2_COMPASS_Y` | `OSD3_COMPASS_Y` | `OSD4_COMPASS_Y` |
| `OSD1_CRSSHAIR_EN` | `OSD2_CRSSHAIR_EN` | `OSD3_CRSSHAIR_EN` | `OSD4_CRSSHAIR_EN` |
| `OSD1_CRSSHAIR_X` | `OSD2_CRSSHAIR_X` | `OSD3_CRSSHAIR_X` | `OSD4_CRSSHAIR_X` |
| `OSD1_CRSSHAIR_Y` | `OSD2_CRSSHAIR_Y` | `OSD3_CRSSHAIR_Y` | `OSD4_CRSSHAIR_Y` |
| `OSD1_CURRENT2_EN` | `OSD2_CURRENT2_EN` | `OSD3_CURRENT2_EN` | `OSD4_CURRENT2_EN` |
| `OSD1_CURRENT2_X` | `OSD2_CURRENT2_X` | `OSD3_CURRENT2_X` | `OSD4_CURRENT2_X` |
| `OSD1_CURRENT2_Y` | `OSD2_CURRENT2_Y` | `OSD3_CURRENT2_Y` | `OSD4_CURRENT2_Y` |
| `OSD1_CURRENT_EN` | `OSD2_CURRENT_EN` | `OSD3_CURRENT_EN` | `OSD4_CURRENT_EN` |
| `OSD1_CURRENT_X` | `OSD2_CURRENT_X` | `OSD3_CURRENT_X` | `OSD4_CURRENT_X` |
| `OSD1_CURRENT_Y` | `OSD2_CURRENT_Y` | `OSD3_CURRENT_Y` | `OSD4_CURRENT_Y` |
| `OSD1_DIST_EN` | `OSD2_DIST_EN` | `OSD3_DIST_EN` | `OSD4_DIST_EN` |
| `OSD1_DIST_X` | `OSD2_DIST_X` | `OSD3_DIST_X` | `OSD4_DIST_X` |
| `OSD1_DIST_Y` | `OSD2_DIST_Y` | `OSD3_DIST_Y` | `OSD4_DIST_Y` |
| `OSD1_EFF_EN` | `OSD2_EFF_EN` | `OSD3_EFF_EN` | `OSD4_EFF_EN` |
| `OSD1_EFF_X` | `OSD2_EFF_X` | `OSD3_EFF_X` | `OSD4_EFF_X` |
| `OSD1_EFF_Y` | `OSD2_EFF_Y` | `OSD3_EFF_Y` | `OSD4_EFF_Y` |
| `OSD1_ENABLE` | `OSD2_ENABLE` | `OSD3_ENABLE` | `OSD4_ENABLE` |
| `OSD1_ESCAMPS_EN` | `OSD2_ESCAMPS_EN` | `OSD3_ESCAMPS_EN` | `OSD4_ESCAMPS_EN` |
| `OSD1_ESCAMPS_X` | `OSD2_ESCAMPS_X` | `OSD3_ESCAMPS_X` | `OSD4_ESCAMPS_X` |
| `OSD1_ESCAMPS_Y` | `OSD2_ESCAMPS_Y` | `OSD3_ESCAMPS_Y` | `OSD4_ESCAMPS_Y` |
| `OSD1_ESCRPM_EN` | `OSD2_ESCRPM_EN` | `OSD3_ESCRPM_EN` | `OSD4_ESCRPM_EN` |
| `OSD1_ESCRPM_X` | `OSD2_ESCRPM_X` | `OSD3_ESCRPM_X` | `OSD4_ESCRPM_X` |
| `OSD1_ESCRPM_Y` | `OSD2_ESCRPM_Y` | `OSD3_ESCRPM_Y` | `OSD4_ESCRPM_Y` |
| `OSD1_ESCTEMP_EN` | `OSD2_ESCTEMP_EN` | `OSD3_ESCTEMP_EN` | `OSD4_ESCTEMP_EN` |
| `OSD1_ESCTEMP_X` | `OSD2_ESCTEMP_X` | `OSD3_ESCTEMP_X` | `OSD4_ESCTEMP_X` |
| `OSD1_ESCTEMP_Y` | `OSD2_ESCTEMP_Y` | `OSD3_ESCTEMP_Y` | `OSD4_ESCTEMP_Y` |
| `OSD1_ESC_IDX` | `OSD2_ESC_IDX` | `OSD3_ESC_IDX` | `OSD4_ESC_IDX` |
| `OSD1_FENCE_EN` | `OSD2_FENCE_EN` | `OSD3_FENCE_EN` | `OSD4_FENCE_EN` |
| `OSD1_FENCE_X` | `OSD2_FENCE_X` | `OSD3_FENCE_X` | `OSD4_FENCE_X` |
| `OSD1_FENCE_Y` | `OSD2_FENCE_Y` | `OSD3_FENCE_Y` | `OSD4_FENCE_Y` |
| `OSD1_FLTIME_EN` | `OSD2_FLTIME_EN` | `OSD3_FLTIME_EN` | `OSD4_FLTIME_EN` |
| `OSD1_FLTIME_X` | `OSD2_FLTIME_X` | `OSD3_FLTIME_X` | `OSD4_FLTIME_X` |
| `OSD1_FLTIME_Y` | `OSD2_FLTIME_Y` | `OSD3_FLTIME_Y` | `OSD4_FLTIME_Y` |
| `OSD1_FLTMODE_EN` | `OSD2_FLTMODE_EN` | `OSD3_FLTMODE_EN` | `OSD4_FLTMODE_EN` |
| `OSD1_FLTMODE_X` | `OSD2_FLTMODE_X` | `OSD3_FLTMODE_X` | `OSD4_FLTMODE_X` |
| `OSD1_FLTMODE_Y` | `OSD2_FLTMODE_Y` | `OSD3_FLTMODE_Y` | `OSD4_FLTMODE_Y` |
| `OSD1_GPSLAT_EN` | `OSD2_GPSLAT_EN` | `OSD3_GPSLAT_EN` | `OSD4_GPSLAT_EN` |
| `OSD1_GPSLAT_X` | `OSD2_GPSLAT_X` | `OSD3_GPSLAT_X` | `OSD4_GPSLAT_X` |
| `OSD1_GPSLAT_Y` | `OSD2_GPSLAT_Y` | `OSD3_GPSLAT_Y` | `OSD4_GPSLAT_Y` |
| `OSD1_GPSLONG_EN` | `OSD2_GPSLONG_EN` | `OSD3_GPSLONG_EN` | `OSD4_GPSLONG_EN` |
| `OSD1_GPSLONG_X` | `OSD2_GPSLONG_X` | `OSD3_GPSLONG_X` | `OSD4_GPSLONG_X` |
| `OSD1_GPSLONG_Y` | `OSD2_GPSLONG_Y` | `OSD3_GPSLONG_Y` | `OSD4_GPSLONG_Y` |
| `OSD1_GSPEED_EN` | `OSD2_GSPEED_EN` | `OSD3_GSPEED_EN` | `OSD4_GSPEED_EN` |
| `OSD1_GSPEED_X` | `OSD2_GSPEED_X` | `OSD3_GSPEED_X` | `OSD4_GSPEED_X` |
| `OSD1_GSPEED_Y` | `OSD2_GSPEED_Y` | `OSD3_GSPEED_Y` | `OSD4_GSPEED_Y` |
| `OSD1_HDOP_EN` | `OSD2_HDOP_EN` | `OSD3_HDOP_EN` | `OSD4_HDOP_EN` |
| `OSD1_HDOP_X` | `OSD2_HDOP_X` | `OSD3_HDOP_X` | `OSD4_HDOP_X` |
| `OSD1_HDOP_Y` | `OSD2_HDOP_Y` | `OSD3_HDOP_Y` | `OSD4_HDOP_Y` |
| `OSD1_HEADING_EN` | `OSD2_HEADING_EN` | `OSD3_HEADING_EN` | `OSD4_HEADING_EN` |
| `OSD1_HEADING_X` | `OSD2_HEADING_X` | `OSD3_HEADING_X` | `OSD4_HEADING_X` |
| `OSD1_HEADING_Y` | `OSD2_HEADING_Y` | `OSD3_HEADING_Y` | `OSD4_HEADING_Y` |
| `OSD1_HOMEDIR_EN` | `OSD2_HOMEDIR_EN` | `OSD3_HOMEDIR_EN` | `OSD4_HOMEDIR_EN` |
| `OSD1_HOMEDIR_X` | `OSD2_HOMEDIR_X` | `OSD3_HOMEDIR_X` | `OSD4_HOMEDIR_X` |
| `OSD1_HOMEDIR_Y` | `OSD2_HOMEDIR_Y` | `OSD3_HOMEDIR_Y` | `OSD4_HOMEDIR_Y` |
| `OSD1_HOMEDIST_EN` | `OSD2_HOMEDIST_EN` | `OSD3_HOMEDIST_EN` | `OSD4_HOMEDIST_EN` |
| `OSD1_HOMEDIST_X` | `OSD2_HOMEDIST_X` | `OSD3_HOMEDIST_X` | `OSD4_HOMEDIST_X` |
| `OSD1_HOMEDIST_Y` | `OSD2_HOMEDIST_Y` | `OSD3_HOMEDIST_Y` | `OSD4_HOMEDIST_Y` |
| `OSD1_HOME_EN` | `OSD2_HOME_EN` | `OSD3_HOME_EN` | `OSD4_HOME_EN` |
| `OSD1_HOME_X` | `OSD2_HOME_X` | `OSD3_HOME_X` | `OSD4_HOME_X` |
| `OSD1_HOME_Y` | `OSD2_HOME_Y` | `OSD3_HOME_Y` | `OSD4_HOME_Y` |
| `OSD1_HORIZON_EN` | `OSD2_HORIZON_EN` | `OSD3_HORIZON_EN` | `OSD4_HORIZON_EN` |
| `OSD1_HORIZON_X` | `OSD2_HORIZON_X` | `OSD3_HORIZON_X` | `OSD4_HORIZON_X` |
| `OSD1_HORIZON_Y` | `OSD2_HORIZON_Y` | `OSD3_HORIZON_Y` | `OSD4_HORIZON_Y` |
| `OSD1_LINK_Q_EN` | `OSD2_LINK_Q_EN` | `OSD3_LINK_Q_EN` | `OSD4_LINK_Q_EN` |
| `OSD1_LINK_Q_X` | `OSD2_LINK_Q_X` | `OSD3_LINK_Q_X` | `OSD4_LINK_Q_X` |
| `OSD1_LINK_Q_Y` | `OSD2_LINK_Q_Y` | `OSD3_LINK_Q_Y` | `OSD4_LINK_Q_Y` |
| `OSD1_MESSAGE_EN` | `OSD2_MESSAGE_EN` | `OSD3_MESSAGE_EN` | `OSD4_MESSAGE_EN` |
| `OSD1_MESSAGE_X` | `OSD2_MESSAGE_X` | `OSD3_MESSAGE_X` | `OSD4_MESSAGE_X` |
| `OSD1_MESSAGE_Y` | `OSD2_MESSAGE_Y` | `OSD3_MESSAGE_Y` | `OSD4_MESSAGE_Y` |
| `OSD1_PITCH_EN` | `OSD2_PITCH_EN` | `OSD3_PITCH_EN` | `OSD4_PITCH_EN` |
| `OSD1_PITCH_X` | `OSD2_PITCH_X` | `OSD3_PITCH_X` | `OSD4_PITCH_X` |
| `OSD1_PITCH_Y` | `OSD2_PITCH_Y` | `OSD3_PITCH_Y` | `OSD4_PITCH_Y` |
| `OSD1_PLUSCODE_EN` | `OSD2_PLUSCODE_EN` | `OSD3_PLUSCODE_EN` | `OSD4_PLUSCODE_EN` |
| `OSD1_PLUSCODE_X` | `OSD2_PLUSCODE_X` | `OSD3_PLUSCODE_X` | `OSD4_PLUSCODE_X` |
| `OSD1_PLUSCODE_Y` | `OSD2_PLUSCODE_Y` | `OSD3_PLUSCODE_Y` | `OSD4_PLUSCODE_Y` |
| `OSD1_POWER_EN` | `OSD2_POWER_EN` | `OSD3_POWER_EN` | `OSD4_POWER_EN` |
| `OSD1_POWER_X` | `OSD2_POWER_X` | `OSD3_POWER_X` | `OSD4_POWER_X` |
| `OSD1_POWER_Y` | `OSD2_POWER_Y` | `OSD3_POWER_Y` | `OSD4_POWER_Y` |
| `OSD1_RC_ANT_EN` | `OSD2_RC_ANT_EN` | `OSD3_RC_ANT_EN` | `OSD4_RC_ANT_EN` |
| `OSD1_RC_ANT_X` | `OSD2_RC_ANT_X` | `OSD3_RC_ANT_X` | `OSD4_RC_ANT_X` |
| `OSD1_RC_ANT_Y` | `OSD2_RC_ANT_Y` | `OSD3_RC_ANT_Y` | `OSD4_RC_ANT_Y` |
| `OSD1_RC_LQ_EN` | `OSD2_RC_LQ_EN` | `OSD3_RC_LQ_EN` | `OSD4_RC_LQ_EN` |
| `OSD1_RC_LQ_X` | `OSD2_RC_LQ_X` | `OSD3_RC_LQ_X` | `OSD4_RC_LQ_X` |
| `OSD1_RC_LQ_Y` | `OSD2_RC_LQ_Y` | `OSD3_RC_LQ_Y` | `OSD4_RC_LQ_Y` |
| `OSD1_RC_PWR_EN` | `OSD2_RC_PWR_EN` | `OSD3_RC_PWR_EN` | `OSD4_RC_PWR_EN` |
| `OSD1_RC_PWR_X` | `OSD2_RC_PWR_X` | `OSD3_RC_PWR_X` | `OSD4_RC_PWR_X` |
| `OSD1_RC_PWR_Y` | `OSD2_RC_PWR_Y` | `OSD3_RC_PWR_Y` | `OSD4_RC_PWR_Y` |
| `OSD1_RC_SNR_EN` | `OSD2_RC_SNR_EN` | `OSD3_RC_SNR_EN` | `OSD4_RC_SNR_EN` |
| `OSD1_RC_SNR_X` | `OSD2_RC_SNR_X` | `OSD3_RC_SNR_X` | `OSD4_RC_SNR_X` |
| `OSD1_RC_SNR_Y` | `OSD2_RC_SNR_Y` | `OSD3_RC_SNR_Y` | `OSD4_RC_SNR_Y` |
| `OSD1_RESTVOLT_EN` | `OSD2_RESTVOLT_EN` | `OSD3_RESTVOLT_EN` | `OSD4_RESTVOLT_EN` |
| `OSD1_RESTVOLT_X` | `OSD2_RESTVOLT_X` | `OSD3_RESTVOLT_X` | `OSD4_RESTVOLT_X` |
| `OSD1_RESTVOLT_Y` | `OSD2_RESTVOLT_Y` | `OSD3_RESTVOLT_Y` | `OSD4_RESTVOLT_Y` |
| `OSD1_RNGF_EN` | `OSD2_RNGF_EN` | `OSD3_RNGF_EN` | `OSD4_RNGF_EN` |
| `OSD1_RNGF_X` | `OSD2_RNGF_X` | `OSD3_RNGF_X` | `OSD4_RNGF_X` |
| `OSD1_RNGF_Y` | `OSD2_RNGF_Y` | `OSD3_RNGF_Y` | `OSD4_RNGF_Y` |
| `OSD1_ROLL_EN` | `OSD2_ROLL_EN` | `OSD3_ROLL_EN` | `OSD4_ROLL_EN` |
| `OSD1_ROLL_X` | `OSD2_ROLL_X` | `OSD3_ROLL_X` | `OSD4_ROLL_X` |
| `OSD1_ROLL_Y` | `OSD2_ROLL_Y` | `OSD3_ROLL_Y` | `OSD4_ROLL_Y` |
| `OSD1_RPM_EN` | `OSD2_RPM_EN` | `OSD3_RPM_EN` | `OSD4_RPM_EN` |
| `OSD1_RPM_X` | `OSD2_RPM_X` | `OSD3_RPM_X` | `OSD4_RPM_X` |
| `OSD1_RPM_Y` | `OSD2_RPM_Y` | `OSD3_RPM_Y` | `OSD4_RPM_Y` |
| `OSD1_RSSIDBM_EN` | `OSD2_RSSIDBM_EN` | `OSD3_RSSIDBM_EN` | `OSD4_RSSIDBM_EN` |
| `OSD1_RSSIDBM_X` | `OSD2_RSSIDBM_X` | `OSD3_RSSIDBM_X` | `OSD4_RSSIDBM_X` |
| `OSD1_RSSIDBM_Y` | `OSD2_RSSIDBM_Y` | `OSD3_RSSIDBM_Y` | `OSD4_RSSIDBM_Y` |
| `OSD1_RSSI_EN` | `OSD2_RSSI_EN` | `OSD3_RSSI_EN` | `OSD4_RSSI_EN` |
| `OSD1_RSSI_X` | `OSD2_RSSI_X` | `OSD3_RSSI_X` | `OSD4_RSSI_X` |
| `OSD1_RSSI_Y` | `OSD2_RSSI_Y` | `OSD3_RSSI_Y` | `OSD4_RSSI_Y` |
| `OSD1_SATS_EN` | `OSD2_SATS_EN` | `OSD3_SATS_EN` | `OSD4_SATS_EN` |
| `OSD1_SATS_X` | `OSD2_SATS_X` | `OSD3_SATS_X` | `OSD4_SATS_X` |
| `OSD1_SATS_Y` | `OSD2_SATS_Y` | `OSD3_SATS_Y` | `OSD4_SATS_Y` |
| `OSD1_SIDEBARS_EN` | `OSD2_SIDEBARS_EN` | `OSD3_SIDEBARS_EN` | `OSD4_SIDEBARS_EN` |
| `OSD1_SIDEBARS_X` | `OSD2_SIDEBARS_X` | `OSD3_SIDEBARS_X` | `OSD4_SIDEBARS_X` |
| `OSD1_SIDEBARS_Y` | `OSD2_SIDEBARS_Y` | `OSD3_SIDEBARS_Y` | `OSD4_SIDEBARS_Y` |
| `OSD1_STATS_EN` | `OSD2_STATS_EN` | `OSD3_STATS_EN` | `OSD4_STATS_EN` |
| `OSD1_STATS_X` | `OSD2_STATS_X` | `OSD3_STATS_X` | `OSD4_STATS_X` |
| `OSD1_STATS_Y` | `OSD2_STATS_Y` | `OSD3_STATS_Y` | `OSD4_STATS_Y` |
| `OSD1_TEMP_EN` | `OSD2_TEMP_EN` | `OSD3_TEMP_EN` | `OSD4_TEMP_EN` |
| `OSD1_TEMP_X` | `OSD2_TEMP_X` | `OSD3_TEMP_X` | `OSD4_TEMP_X` |
| `OSD1_TEMP_Y` | `OSD2_TEMP_Y` | `OSD3_TEMP_Y` | `OSD4_TEMP_Y` |
| `OSD1_TER_HGT_EN` | `OSD2_TER_HGT_EN` | `OSD3_TER_HGT_EN` | `OSD4_TER_HGT_EN` |
| `OSD1_TER_HGT_X` | `OSD2_TER_HGT_X` | `OSD3_TER_HGT_X` | `OSD4_TER_HGT_X` |
| `OSD1_TER_HGT_Y` | `OSD2_TER_HGT_Y` | `OSD3_TER_HGT_Y` | `OSD4_TER_HGT_Y` |
| `OSD1_THROTTLE_EN` | `OSD2_THROTTLE_EN` | `OSD3_THROTTLE_EN` | `OSD4_THROTTLE_EN` |
| `OSD1_THROTTLE_X` | `OSD2_THROTTLE_X` | `OSD3_THROTTLE_X` | `OSD4_THROTTLE_X` |
| `OSD1_THROTTLE_Y` | `OSD2_THROTTLE_Y` | `OSD3_THROTTLE_Y` | `OSD4_THROTTLE_Y` |
| `OSD1_TXT_RES` | `OSD2_TXT_RES` | `OSD3_TXT_RES` | `OSD4_TXT_RES` |
| `OSD1_VSPEED_EN` | `OSD2_VSPEED_EN` | `OSD3_VSPEED_EN` | `OSD4_VSPEED_EN` |
| `OSD1_VSPEED_X` | `OSD2_VSPEED_X` | `OSD3_VSPEED_X` | `OSD4_VSPEED_X` |
| `OSD1_VSPEED_Y` | `OSD2_VSPEED_Y` | `OSD3_VSPEED_Y` | `OSD4_VSPEED_Y` |
| `OSD1_VTX_PWR_EN` | `OSD2_VTX_PWR_EN` | `OSD3_VTX_PWR_EN` | `OSD4_VTX_PWR_EN` |
| `OSD1_VTX_PWR_X` | `OSD2_VTX_PWR_X` | `OSD3_VTX_PWR_X` | `OSD4_VTX_PWR_X` |
| `OSD1_VTX_PWR_Y` | `OSD2_VTX_PWR_Y` | `OSD3_VTX_PWR_Y` | `OSD4_VTX_PWR_Y` |
| `OSD1_WAYPOINT_EN` | `OSD2_WAYPOINT_EN` | `OSD3_WAYPOINT_EN` | `OSD4_WAYPOINT_EN` |
| `OSD1_WAYPOINT_X` | `OSD2_WAYPOINT_X` | `OSD3_WAYPOINT_X` | `OSD4_WAYPOINT_X` |
| `OSD1_WAYPOINT_Y` | `OSD2_WAYPOINT_Y` | `OSD3_WAYPOINT_Y` | `OSD4_WAYPOINT_Y` |
| **`OSD1_WIND_EN`** | **`OSD2_WIND_EN`** | **`OSD3_WIND_EN`** | **`OSD4_WIND_EN`** |
| **`OSD1_WIND_X`** | **`OSD2_WIND_X`** | **`OSD3_WIND_X`** | **`OSD4_WIND_X`** |
| **`OSD1_WIND_Y`** | **`OSD2_WIND_Y`** | **`OSD3_WIND_Y`** | **`OSD4_WIND_Y`** |
| `OSD1_XTRACK_EN` | `OSD2_XTRACK_EN` | `OSD3_XTRACK_EN` | `OSD4_XTRACK_EN` |
| `OSD1_XTRACK_X` | `OSD2_XTRACK_X` | `OSD3_XTRACK_X` | `OSD4_XTRACK_X` |
| `OSD1_XTRACK_Y` | `OSD2_XTRACK_Y` | `OSD3_XTRACK_Y` | `OSD4_XTRACK_Y` |

</details>

## 定向复核

在仓库根目录运行 `--osd-display-format`。它用 Copter 4.6.3 的七条代表记录检查双语显示、中文搜索，以及位掩码、枚举、范围、单位和增量字段。错版用例在内存中将 `OSD_OPTIONS` 换为 Copter 4.5.7 的原节点。预期保留完整英文和八个位标签，显示中文待复核，旧译文不再命中搜索。同一快照的 `OSD_UNITS` 仍匹配。未登记的 4.7.2 沿用原有回退。

```sh
dotnet run --project MissionPlannerTests/ParameterDescriptions/ParameterDescriptions.csproj -c Release -- --osd-display-format
```

将上表七个完整 PDEF 保存到同一目录，分别命名为 `<机型>-<版本>.xml`。以下命令逐版校验已登记的完整文件摘要，并同时输出全文件、全局 `OSD` 和四个屏组的覆盖及余项。

```sh
pdef_dir=/path/to/pinned-pdef
while read -r family version; do
    python scripts/localization/audit_parameter_coverage.py \
        "$family" "$version" "$pdef_dir/$family-$version.xml" \
        --group OSD --group OSD1_ --group OSD2_ --group OSD3_ --group OSD4_ || exit
done <<'PDEF_RELEASES'
Copter 4.5.7
Copter 4.6.3
Copter 4.7.1
Plane 4.5.7
Plane 4.6.3
Plane 4.7.1
Sub 4.7.1
PDEF_RELEASES
```
