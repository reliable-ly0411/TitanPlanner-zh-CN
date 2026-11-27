# OSD 四屏信息显示的分版来源

本批补充四个原始 OSD 屏组的飞行模式、文字提示和时钟说明。每个显示项在每屏各有开关和两个位置字段，共 36 个原键。七版每个键在对应的 `libraries/parameters` 官方组内都恰有一条记录，选集覆盖从 0/252 提高到 252/252。只登记中文 `Description`，显示项的开关和位置不参与飞行、告警、遥测或时钟设置。首轮来源对照采用 [#310 初始提交 `634b74118335e542338624ef24402f4d8312aa10`](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/commit/634b74118335e542338624ef24402f4d8312aa10)。目录生成采用已接入 #311 的 [#310 恢复点 `971b081d`](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/commit/971b081d65f6ce4a40440b29892d5698f5470402)。

## 固定文件与四屏覆盖

| 固件 | 固定官方 PDEF | 完整文件 SHA‑256 | 固定屏组源码 | 四屏已译变化 / 分母 | 四屏余项 |
| --- | --- | --- | --- | ---: | ---: |
| Copter 4.5.7 | [PDEF](https://autotest.ardupilot.org/Parameters/versioned/Copter/stable-4.5.7/apm.pdef.xml) | `61599e773eadeba781d69c124aff3acfbeea884b2a159d9ca6f7891f379513c9` | [2a3dc4b7](https://github.com/ArduPilot/ardupilot/blob/2a3dc4b7bf2507120f7378a7b2fde73185e0c325/libraries/AP_OSD/AP_OSD_Screen.cpp#L181) | 16 → 52 / 804 | 752 |
| Copter 4.6.3 | [PDEF](https://autotest.ardupilot.org/Parameters/versioned/Copter/stable-4.6.3/apm.pdef.xml) | `e6375727b7a05a0c820b7b5c3e28c755d52c1e6da6b4506682d1dd3b19552190` | [92b0cd78](https://github.com/ArduPilot/ardupilot/blob/92b0cd788ec29406f26c6f9c31d5ceedbd1cc538/libraries/AP_OSD/AP_OSD_Screen.cpp#L181) | 16 → 52 / 804 | 752 |
| Copter 4.7.1 | [PDEF](https://autotest.ardupilot.org/Parameters/versioned/Copter/stable-4.7.1/apm.pdef.xml) | `9e5e674c6029bd9f680540c62119086c26991d3ba586c585f4e98484036c0518` | [dbe79216](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L181) | 16 → 52 / 804 | 752 |
| Plane / QuadPlane 4.5.7 | [PDEF](https://autotest.ardupilot.org/Parameters/versioned/Plane/stable-4.5.7/apm.pdef.xml) | `4abf0156f5c68d61fe0efef5f24daefc31754ee1ddf6cd8e70b01aff300bfa83` | [0358a9c2](https://github.com/ArduPilot/ardupilot/blob/0358a9c210bc6c965006f5d6029239b7033616df/libraries/AP_OSD/AP_OSD_Screen.cpp#L181) | 16 → 52 / 804 | 752 |
| Plane / QuadPlane 4.6.3 | [PDEF](https://autotest.ardupilot.org/Parameters/versioned/Plane/stable-4.6.3/apm.pdef.xml) | `154e7bd4c38a75cc09e7109a4b7cfaa7f7a6fbf03125cfe6a849032f3b8389aa` | [3fc7011a](https://github.com/ArduPilot/ardupilot/blob/3fc7011a7d3dc047cbb17d8bd98ee94577d144c6/libraries/AP_OSD/AP_OSD_Screen.cpp#L181) | 16 → 52 / 804 | 752 |
| Plane / QuadPlane 4.7.1 | [PDEF](https://autotest.ardupilot.org/Parameters/versioned/Plane/stable-4.7.1/apm.pdef.xml) | `924fdf56b972719bde381082adb29fd415731aa8cd899ec111eb4a5eda5192d6` | [dbe79216](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L181) | 16 → 52 / 804 | 752 |
| Sub 4.7.1 | [PDEF](https://autotest.ardupilot.org/Parameters/versioned/Sub/stable-4.7.1/apm.pdef.xml) | `c543ae155e6c48c98ca6f5fc803c7ce9b6161780fc79aa9e35e8c808891bb5ba` | [dbe79216](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L181) | 16 → 52 / 804 | 752 |
| **七版合计** | | | | **112 → 364 / 5,628** | **5,264** |

每屏每版分母为 201 个不同原键。前件覆盖每屏 4 个，本批每屏新增 9 个，变为每屏每版 13/201。单个屏组跨七版为 28 → 91 / 1,407，余 1,316 个版本组合。同一个屏组在七版剩余原键集合一致，每屏仍有 188 个不同原键。下面的审计命令可按原组输出完整余项。 [固定来源登记](../localization/parameters/sources.json)

## 原键与指纹

表中每个原键后为完整记录 SHA‑256 的前 12 位，可在固定 PDEF 内按原键定位并复算。每键在七版共用一组英文和完整记录指纹，36 个原键共有 36 个不同的完整指纹。开关 `EN` 保留原枚举 `0 Disabled`、`1 Enabled`。`X` 保留横向范围 `0 59`，`Y` 保留纵向范围 `0 21`。这三类字段的单位格均为空。 [指纹算法](../scripts/localization/build_parameter_catalog.py)

| 官方屏组 | 显示项 | 开关原键 | 水平位置原键 | 垂直位置原键 |
| --- | --- | --- | --- | --- |
| `OSD1_` | 飞行模式 | `OSD1_FLTMODE_EN` · `b43bde366335` | `OSD1_FLTMODE_X` · `615d49e8650e` | `OSD1_FLTMODE_Y` · `f94c94f6ce51` |
| `OSD1_` | 文字提示 | `OSD1_MESSAGE_EN` · `e8e71de1966b` | `OSD1_MESSAGE_X` · `69da8c768e7d` | `OSD1_MESSAGE_Y` · `ef14841497c5` |
| `OSD1_` | 时钟 | `OSD1_CLK_EN` · `a42345a72b3c` | `OSD1_CLK_X` · `6d2905fe1939` | `OSD1_CLK_Y` · `f246068ff695` |
| `OSD2_` | 飞行模式 | `OSD2_FLTMODE_EN` · `b47b15b7f197` | `OSD2_FLTMODE_X` · `47e83c277ead` | `OSD2_FLTMODE_Y` · `456418c88643` |
| `OSD2_` | 文字提示 | `OSD2_MESSAGE_EN` · `db629d8e79e3` | `OSD2_MESSAGE_X` · `e7d42ea37236` | `OSD2_MESSAGE_Y` · `2433e0ebb108` |
| `OSD2_` | 时钟 | `OSD2_CLK_EN` · `39c8896feaea` | `OSD2_CLK_X` · `aa772f8f1283` | `OSD2_CLK_Y` · `1ef3f8c81a9f` |
| `OSD3_` | 飞行模式 | `OSD3_FLTMODE_EN` · `88387a64e7fb` | `OSD3_FLTMODE_X` · `e4ab037106bc` | `OSD3_FLTMODE_Y` · `affe61999aa1` |
| `OSD3_` | 文字提示 | `OSD3_MESSAGE_EN` · `29721441b67e` | `OSD3_MESSAGE_X` · `77c72e083e38` | `OSD3_MESSAGE_Y` · `b0ab4d41813f` |
| `OSD3_` | 时钟 | `OSD3_CLK_EN` · `0d6a4fc786ed` | `OSD3_CLK_X` · `9e9d316245fc` | `OSD3_CLK_Y` · `a634608ed2da` |
| `OSD4_` | 飞行模式 | `OSD4_FLTMODE_EN` · `dd9ad82c93ca` | `OSD4_FLTMODE_X` · `202369c04466` | `OSD4_FLTMODE_Y` · `47ecd3a967c0` |
| `OSD4_` | 文字提示 | `OSD4_MESSAGE_EN` · `432b12c2b708` | `OSD4_MESSAGE_X` · `6b48defc702a` | `OSD4_MESSAGE_Y` · `4d2cc033a2ca` |
| `OSD4_` | 时钟 | `OSD4_CLK_EN` · `44d1a77e8931` | `OSD4_CLK_X` · `06d030b1dc8a` | `OSD4_CLK_Y` · `c54c250baa4e` |

## 源码边界

四屏在 `AP_OSD.cpp` 第 62–76 行分别注册。上表五个固定提交的 `AP_OSD_Screen.cpp` 定义行一致。绘制入口的起始行按飞行模式、文字提示、时钟顺序列出。 [四屏注册](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD.cpp#L62-L76)

| 固件版本 | 三个绘制入口行号 |
| --- | --- |
| 4.5.7 | 1573 / 1616 / 2419 |
| 4.6.3 | 1577 / 1622 / 2433 |
| 4.7.1 | 1577 / 1622 / 2431 |

- 飞行模式绘制读取飞控报告的当前模式文字。中文说明只描述这项文字的显示与位置。定义第 181–195 行。 [参数定义](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L181-L195) · [模式绘制](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L1577)
- 文字提示绘制读取 `AP_Notify` 当前缓冲中的一条文字，仍受原有显示时限约束。GCS 状态文本（MAVLink `STATUSTEXT`）的首段会送入该缓冲，后续写入会覆盖。定义第 197–211 行。 [参数定义](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L197-L211) · [文字绘制](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L1622) · [状态文本交接](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/GCS_MAVLink/GCS_Common.cpp#L2620) · [缓冲写入](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_Notify/AP_Notify.cpp#L478-L483)
- 时钟在固件具备 `AP_RTC` 且能读取本地时间时，以 24 小时制绘出小时与分钟。定义第 714–728 行。 [参数定义](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L714-L728) · [时钟绘制](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L2431) · [本地时间来源](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_RTC/AP_RTC.cpp#L216)

## 定向读表用例

`--osd-screen-info` 使用 Copter 4.6.3 的三条代表记录，检查原英文、中文显示和检索、原范围与数值标签以及空单位。三个代表行分别覆盖枚举、水平范围和垂直范围。

| 原键 | 原字段 |
| --- | --- |
| `OSD1_MESSAGE_EN` | `0 Disabled`、`1 Enabled` |
| `OSD4_FLTMODE_X` | `0 59` |
| `OSD2_CLK_Y` | `0 21` |

错指纹对照在内存中把 `OSD4_MESSAGE_X` 的 `humanName` 复制到 `OSD4_FLTMODE_X`，保留后者的原键、英文与范围，生成仅供测试的合成节点。预期飞行模式行保留英文和 `0 59`，中文转为“待复核”，旧中文退出检索。原飞行模式及对照中的时钟仍命中。未登记的 Copter 4.7.2 继续沿用版本回退。

```sh
dotnet run --project MissionPlannerTests/ParameterDescriptions/ParameterDescriptions.csproj -c Release -- --osd-screen-info
```

将上表七份完整 PDEF 保存为 `<机型>-<版本>.xml`，从仓库根目录逐一运行原审计。JSON 中每个屏组的 `untranslated` 给出全部剩余原键。

```sh
pdef_dir=/path/to/pinned-pdef
while read -r family version; do
    python scripts/localization/audit_parameter_coverage.py \
        "$family" "$version" "$pdef_dir/$family-$version.xml" \
        --group OSD1_ --group OSD2_ --group OSD3_ --group OSD4_ || exit
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
