# SITL 环境与只读统计参数说明

范围是七份已固定的 ArduPilot PDEF，详见 [版本和文件 SHA-256](../localization/parameters/sources.json)。只增加参数说明的展示文字，不增删选项、不写参数、不改模拟或实飞行为。

- **仅 SITL 的环境项（参数可编辑）**：16 个独立原键、92 条分版记录。风：SIM_WIND_SPD、SIM_WIND_DIR、SIM_WIND_TURB、SIM_WIND_TC、SIM_WIND_DIR_Z、SIM_WIND_T、SIM_WIND_T_ALT、SIM_WIND_T_COEF。波浪：SIM_WAVE_ENABLE、SIM_WAVE_LENGTH、SIM_WAVE_AMP、SIM_WAVE_DIR、SIM_WAVE_SPEED。潮汐：SIM_TIDE_DIR、SIM_TIDE_SPEED。热气流：SIM_THML_SCENARI。不包含故障注入、执行机构或控制器设置。
- **纯只读统计**：Sub 4.7.1 的 STAT_DISTFLWN 增加一条分版说明。原标记 ReadOnly=True、单位 m；整条原记录指纹与已审 Copter/Plane 完全一致，复用原中文。没有翻译可用于清零的 STAT_RESET，也不改其他既有 STAT 条目。
- 合计净增加 93 条分版说明（53 个按机型及完整记录指纹归组的目录项），不是 93 个不同的原键。

| 固件 | SITL 环境新说明 | 只读统计新说明 |
| --- | ---: | ---: |
| Copter 4.5.7 / 4.6.3 / 4.7.1 | 6 / 16 / 16 | 0 |
| Plane 4.5.7 / 4.6.3 / 4.7.1 | 6 / 16 / 16 | 0 |
| Sub 4.7.1 | 16 | 1 |

原英文、单位、范围、选项及记录属性保留。译文只在版本和完整记录指纹匹配时显示，未登记的版本或记录不沿用中文。既有 OSD、LOG 和其他开发批次不纳入。
