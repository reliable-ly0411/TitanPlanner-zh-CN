# 四屏飞行时间与气压计温度说明

本批覆盖四个屏组中的飞行时间、主气压计温度和第二个气压计温度。每个项目在每屏各有显示开关、水平位置和垂直位置，共 36 个原键。七份正式版官方参数文件（PDEF）中每个原键各有一条唯一定义，共 252 条分版说明。起始目录为 [#314 的提交](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/commit/9c2c13ca0b97164a3cbcc7e0fdfeacebee019aef)。

## 七份官方来源

| 机型 | 版本 | 官方完整文件 | 文件 SHA-256 | 绘制源码〔飞行时间／主温度／第二温度〕 | 本批已译／全部 |
| --- | --- | --- | --- | --- | ---: |
| Copter | 4.5.7 | [固定 PDEF](https://autotest.ardupilot.org/Parameters/versioned/Copter/stable-4.5.7/apm.pdef.xml) | `61599e773eadeba781d69c124aff3acfbeea884b2a159d9ca6f7891f379513c9` | [2293](https://github.com/ArduPilot/ardupilot/blob/2a3dc4b7bf2507120f7378a7b2fde73185e0c325/libraries/AP_OSD/AP_OSD_Screen.cpp#L2293-L2300) / [2240](https://github.com/ArduPilot/ardupilot/blob/2a3dc4b7bf2507120f7378a7b2fde73185e0c325/libraries/AP_OSD/AP_OSD_Screen.cpp#L2240-L2245) / [2351](https://github.com/ArduPilot/ardupilot/blob/2a3dc4b7bf2507120f7378a7b2fde73185e0c325/libraries/AP_OSD/AP_OSD_Screen.cpp#L2351-L2356) | 36 / 36 |
| Copter | 4.6.3 | [固定 PDEF](https://autotest.ardupilot.org/Parameters/versioned/Copter/stable-4.6.3/apm.pdef.xml) | `e6375727b7a05a0c820b7b5c3e28c755d52c1e6da6b4506682d1dd3b19552190` | [2303](https://github.com/ArduPilot/ardupilot/blob/92b0cd788ec29406f26c6f9c31d5ceedbd1cc538/libraries/AP_OSD/AP_OSD_Screen.cpp#L2303-L2310) / [2250](https://github.com/ArduPilot/ardupilot/blob/92b0cd788ec29406f26c6f9c31d5ceedbd1cc538/libraries/AP_OSD/AP_OSD_Screen.cpp#L2250-L2255) / [2365](https://github.com/ArduPilot/ardupilot/blob/92b0cd788ec29406f26c6f9c31d5ceedbd1cc538/libraries/AP_OSD/AP_OSD_Screen.cpp#L2365-L2370) | 36 / 36 |
| Copter | 4.7.1 | [固定 PDEF](https://autotest.ardupilot.org/Parameters/versioned/Copter/stable-4.7.1/apm.pdef.xml) | `9e5e674c6029bd9f680540c62119086c26991d3ba586c585f4e98484036c0518` | [2301](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L2301-L2308) / [2248](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L2248-L2253) / [2363](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L2363-L2368) | 36 / 36 |
| Plane / QuadPlane | 4.5.7 | [固定 PDEF](https://autotest.ardupilot.org/Parameters/versioned/Plane/stable-4.5.7/apm.pdef.xml) | `4abf0156f5c68d61fe0efef5f24daefc31754ee1ddf6cd8e70b01aff300bfa83` | [2293](https://github.com/ArduPilot/ardupilot/blob/0358a9c210bc6c965006f5d6029239b7033616df/libraries/AP_OSD/AP_OSD_Screen.cpp#L2293-L2300) / [2240](https://github.com/ArduPilot/ardupilot/blob/0358a9c210bc6c965006f5d6029239b7033616df/libraries/AP_OSD/AP_OSD_Screen.cpp#L2240-L2245) / [2351](https://github.com/ArduPilot/ardupilot/blob/0358a9c210bc6c965006f5d6029239b7033616df/libraries/AP_OSD/AP_OSD_Screen.cpp#L2351-L2356) | 36 / 36 |
| Plane / QuadPlane | 4.6.3 | [固定 PDEF](https://autotest.ardupilot.org/Parameters/versioned/Plane/stable-4.6.3/apm.pdef.xml) | `154e7bd4c38a75cc09e7109a4b7cfaa7f7a6fbf03125cfe6a849032f3b8389aa` | [2303](https://github.com/ArduPilot/ardupilot/blob/3fc7011a7d3dc047cbb17d8bd98ee94577d144c6/libraries/AP_OSD/AP_OSD_Screen.cpp#L2303-L2310) / [2250](https://github.com/ArduPilot/ardupilot/blob/3fc7011a7d3dc047cbb17d8bd98ee94577d144c6/libraries/AP_OSD/AP_OSD_Screen.cpp#L2250-L2255) / [2365](https://github.com/ArduPilot/ardupilot/blob/3fc7011a7d3dc047cbb17d8bd98ee94577d144c6/libraries/AP_OSD/AP_OSD_Screen.cpp#L2365-L2370) | 36 / 36 |
| Plane / QuadPlane | 4.7.1 | [固定 PDEF](https://autotest.ardupilot.org/Parameters/versioned/Plane/stable-4.7.1/apm.pdef.xml) | `924fdf56b972719bde381082adb29fd415731aa8cd899ec111eb4a5eda5192d6` | [2301](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L2301-L2308) / [2248](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L2248-L2253) / [2363](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L2363-L2368) | 36 / 36 |
| Sub | 4.7.1 | [固定 PDEF](https://autotest.ardupilot.org/Parameters/versioned/Sub/stable-4.7.1/apm.pdef.xml) | `c543ae155e6c48c98ca6f5fc803c7ce9b6161780fc79aa9e35e8c808891bb5ba` | [2301](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L2301-L2308) / [2248](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L2248-L2253) / [2363](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L2363-L2368) | 36 / 36 |

每份文件的四个分屏组共 804 个原键，每屏各 201 个。在 #314 的目录上，每版四屏由 52/804 增至 88/804。七版合计由 364/5,628 增至 616/5,628，余 5,012 条，每屏每版仍有 179 个原键待译。[完整参数分母与各版余项](parameter-descriptions.zh-CN.md#当前范围)

## 原键与字段

同一原键的英文、原字段和完整记录 SHA-256 在七版一致。36 个原键共有 36 个完整指纹。下表列出每个原键和完整指纹的前 12 位。

| 屏 | 显示项 | 显示开关 | 水平位置 | 垂直位置 |
| ---: | --- | --- | --- | --- |
| 1 | 累计飞行时间 | `OSD1_FLTIME_EN` · `7f5452eea280` | `OSD1_FLTIME_X` · `2b805e46a7e4` | `OSD1_FLTIME_Y` · `8dec180486bd` |
| 1 | 主气压计温度 | `OSD1_TEMP_EN` · `51fe5f66bc27` | `OSD1_TEMP_X` · `3ce4a0472eb2` | `OSD1_TEMP_Y` · `529692148310` |
| 1 | 第二个气压计温度 | `OSD1_BTEMP_EN` · `622554c476a1` | `OSD1_BTEMP_X` · `c562dd86371d` | `OSD1_BTEMP_Y` · `09f492c2756b` |
| 2 | 累计飞行时间 | `OSD2_FLTIME_EN` · `68b6c8d09fb1` | `OSD2_FLTIME_X` · `90509a05d56b` | `OSD2_FLTIME_Y` · `2d0e17df90d9` |
| 2 | 主气压计温度 | `OSD2_TEMP_EN` · `a4ca6d82b76b` | `OSD2_TEMP_X` · `824df85d4902` | `OSD2_TEMP_Y` · `c3da4635edab` |
| 2 | 第二个气压计温度 | `OSD2_BTEMP_EN` · `9ea6a9c46531` | `OSD2_BTEMP_X` · `f9506d15ee57` | `OSD2_BTEMP_Y` · `b1bc059dec80` |
| 3 | 累计飞行时间 | `OSD3_FLTIME_EN` · `8b88d7a24a94` | `OSD3_FLTIME_X` · `f602c6f7463e` | `OSD3_FLTIME_Y` · `1a8f1d29edbf` |
| 3 | 主气压计温度 | `OSD3_TEMP_EN` · `cdbb5ad119ed` | `OSD3_TEMP_X` · `bbb216c5625b` | `OSD3_TEMP_Y` · `360f329eed99` |
| 3 | 第二个气压计温度 | `OSD3_BTEMP_EN` · `5d69bf8779a1` | `OSD3_BTEMP_X` · `ba7c2cf04d09` | `OSD3_BTEMP_Y` · `6b3137e4ef08` |
| 4 | 累计飞行时间 | `OSD4_FLTIME_EN` · `ffd35b5f292c` | `OSD4_FLTIME_X` · `f15a8034b3c6` | `OSD4_FLTIME_Y` · `2a424d0892a6` |
| 4 | 主气压计温度 | `OSD4_TEMP_EN` · `a0f1dea9f40c` | `OSD4_TEMP_X` · `a337e49c48f1` | `OSD4_TEMP_Y` · `bd591ce11e65` |
| 4 | 第二个气压计温度 | `OSD4_BTEMP_EN` · `cb157db9643e` | `OSD4_BTEMP_X` · `f72665adcafe` | `OSD4_BTEMP_Y` · `39ca0d19036c` |

显示开关保留原选项 `0 Disabled` 和 `1 Enabled`。水平坐标范围为 `0 59`，垂直坐标为 `0 21`。三种字段的单位格均为空。译文中的温度和时间单位描述屏显内容。

## 文案依据

飞行时间项读取飞控统计模块在本次开机期间累计的飞行秒数，按分钟与秒绘制。同次开机的多个飞行区间可以相加。统计模块重置时可重建基线。Sub 的固件以电机解锁状态累计，沿用官方的“飞行时间”措辞。[原键定义](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L567-L581) · [统计基线](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_Stats/AP_Stats.cpp#L70-L106) · [读取累计时间](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_Stats/AP_Stats.cpp#L222-L229) · [Sub 计时来源](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/ArduSub/Sub.cpp#L412-L419)

主温度项读取当前主气压计报告的温度。第二温度项读取索引 1，绘制代码要求固件支持多个气压计实例。屏幕按所选 OSD 单位制绘制温度数值和符号。[主温度定义](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L471-L485) · [第二温度定义](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L615-L631) · [主实例与指定索引](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_Baro/AP_Baro.h#L82-L83) · [屏显温度单位](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/libraries/AP_OSD/AP_OSD_Screen.cpp#L1327-L1429)

其中，第 1 屏的飞行时间开关说明为“选择是否在 OSD 第 1 屏以分钟和秒显示飞控在本次开机期间累计的飞行时间。”

第 4 屏第二个气压计的水平位置说明为“设置 OSD 第 4 屏上第二个气压计温度的水平位置。”

## 离线定向复核

启动已有参数说明测试项目，选择本批入口。它读取 Copter 4.6.3 的 `OSD1_FLTIME_EN` 和 `OSD4_BTEMP_X`。错指纹用例只在内存中把 `OSD4_TEMP_X` 的 `humanName` 复制给 `OSD4_BTEMP_X`，保留后者的原键、英文和范围。预期中文转为待复核，旧译退出检索。未登记的 4.7.2 沿用原有回退。

```sh
dotnet run --project MissionPlannerTests/ParameterDescriptions/ParameterDescriptions.csproj -c Release -- --osd-time-temperature
```

用上表相应的正式版本号及 PDEF 调用原审计入口，即可按屏组输出 `translated` 和 `untranslated` 全部原键。

```sh
python scripts/localization/audit_parameter_coverage.py Copter 4.6.3 /path/to/Copter-4.6.3.xml --group OSD1_ --group OSD2_ --group OSD3_ --group OSD4_
```
