# 增强简体中文语音播报

在 Windows 的「地面站设置 → 语音播报」可选实际后端枚举到的微软中文音色。自动模式优先匹配简体中文语言，缺少可用音色时回退为系统默认。开启语音后可在空闲时试听。正式播报进入语音后端时会取消当前试听。已保存的音色随地面站设置读写。普通简中和英文沿用其原有声音。Linux/Mono 沿用 Festival。

### Windows 微软晓晓离线语音包

选择「微软晓晓（离线语音包）」后，地面站用语音包朗读匹配的试听和正式播报。将 WAV 存到地面站数据目录下的 `Speech/zh-CN-XiaoxiaoNeural/`。Windows 默认数据目录为「文档」下的 TitanPlanner，使用自定义数据目录时以其为准。语音包文件名为播报全文 NFC 规范化、UTF-8 编码后的 SHA-256 小写十六进制加 `.wav`。音频格式为标准 PCM RIFF/WAVE、24 kHz、16 位、单声道。

动态参数未命中录音、文件缺失或 WAV 无效时，沿用已安装的 Windows 音色。播报期间可以使用现有的正式告警、停止和试听控制。播放 WAV 只依赖本地语音包。

「中文(简体)2」的 {mode} 和 Home 航点使用已审核词汇。航点、模式、周期、电池及低高度首次编辑时提供中文模板。已保存模板保留原文和变量展开顺序。链路超时的原始 HUD 文字与播报分开。

| 固件／机型 | 中文播报覆盖 | 验证入口 |
| --- | --- | --- |
| ArduPilot 4.7.3 Copter / Heli | VTOL QuickTune 脚本与原生 QuickTune、Heli IM_COL 调参 | Lua 参数短名、原生参数名、限幅／调整、中止、总距参数 |
| ArduPilot 4.7.3 Plane / QuadPlane | 日志芯片已满、两种 QuickTune、轨迹与速率特技、精准降落、地形避障 | 起飞／特技阶段、目标状态、地形高度和告警 |
| ArduPilot 4.7.3 Rover / Boat | Rover QuickTune 启动、进度、参数、错误与完成状态 | 百分比、负数、小数和单位夹具 |
| ArduPilot 4.7.3 已适用机型 | 航线选择、云台相机模式、RUNCAM 录像、电池估算、坠机动作 | 任务文件名、电池数量、继电器编号、飞控来源 |
| ArduPilot 4.7.3 Sub / Tracker 及其他版本 | 地面站遥测播报。未列入规则的固件报文沿用原文和原播报条件 | 原文回退 |
| PX4 1.15.4、1.16.2、1.17.0、1.18.0 | 匹配正式构建的电池 STATUSTEXT。1.17.0 四旋翼另有 EKF2 磁力计参数告警 | 报文来源证明与播报去重夹具 |
| PX4 Events | 待接入事件元数据与译文。已有 STATUSTEXT 播报继续可用 | 传统消息回放 |

ArduPilot 脚本规则固定到 4.7.3 的八字节构建标识，并核对同一链路中的主飞控心跳、版本、机型、原始严重度和完整文本。飞控上的 Lua 文件可独立替换，译文以与官方源码匹配的正文为依据。Lua 的 “Tuning: RLL_D done” 与原生的 “Quicktune: Roll D done” 各用对应规则。重组按 MAVLink 上游语义处理，完整消息进入原来的 HUD 和播报门槛，日志保留原始报文。默认告警门槛为 WARNING。INFO／NOTICE 继续遵从门槛和上游的 Tuning、PreArm、Arm 前缀规则。缺片超时、无法证明来源的通用或自由文本、未列入规则的版本按原文处理。

官方来源均固定到 4.7.3 的同一提交。[VTOL QuickTune](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Scripting/applets/VTOL-quicktune.lua) · [Rover QuickTune](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Scripting/applets/rover-quicktune.lua) · [Plane 轨迹特技](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Scripting/applets/Aerobatics/FixedWing/plane_aerobatics.lua) · [Plane 速率特技](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Scripting/applets/Aerobatics/FixedWing/RateBased/sport_aerobatics.lua) · [原生 QuickTune](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Quicktune/AP_Quicktune.cpp)

新增 applet 来源。[MissionSelector.lua](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Scripting/applets/MissionSelector.lua) · [plane_precland.lua](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Scripting/applets/plane_precland.lua) · [Heli_IM_COL_Tune.lua](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Scripting/applets/Heli_IM_COL_Tune.lua) · [quadplane_terrain_avoid.lua](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Scripting/applets/quadplane_terrain_avoid.lua) · [Gimbal_Camera_Mode.lua](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Scripting/applets/Gimbal_Camera_Mode.lua) · [runcam_on_arm.lua](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Scripting/applets/runcam_on_arm.lua) · [BattEstimate.lua](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Scripting/applets/BattEstimate.lua) · [crash-actions.lua](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Scripting/applets/crash-actions.lua)

Windows 独立语音回归用 [WindowsSpeechProbe](../MissionPlannerTests/WindowsSpeechProbe/README.zh-CN.md)。完整应用的原生设置控件由 Windows 回归脚本加载。第二种中文音色、实际卸载音色包和操作员听感需要相应 Windows 环境。
