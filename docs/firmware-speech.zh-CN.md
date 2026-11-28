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
| ArduPilot 4.x Copter / Heli / Plane / QuadPlane / Rover / Boat | 经对应正式构建源码确认的导航、传感器、失联保护、调参、任务和解锁检查消息 | 固定与动态模板、原始门槛和模式回退 |
| ArduPilot 4.x Sub / AntennaTracker | 深度、渗漏、推进器和测距，跟踪器位置及目标角度 | 有符号数、定点小数、长消息分片 |
| Blimp 4.8.0 开发构建 | 官方 SITL 发布标识 6034266b 的失联保护、EKF、机架布局与共用消息 | 独立机型、DEV 标志和哈希 |
| PX4 1.15.4、1.16.2、1.17.0、1.18.0 | 匹配正式构建的电池 STATUSTEXT。1.17.0 四旋翼另有 EKF2 磁力计参数告警 | 报文来源证明与播报去重夹具 |
| PX4 Events | 待接入事件元数据与译文。已有 STATUSTEXT 播报继续可用 | 传统消息回放 |

ArduPilot 脚本规则固定到 4.7.3 的八字节构建标识，并核对同一链路中的主飞控心跳、版本、机型、原始严重度和完整文本。飞控上的 Lua 文件可独立替换，译文以与官方源码匹配的正文为依据。Lua 的 “Tuning: RLL_D done” 与原生的 “Quicktune: Roll D done” 各用对应规则。重组按 MAVLink 上游语义处理，完整消息进入原来的 HUD 和播报门槛，日志保留原始报文。默认告警门槛为 WARNING。INFO／NOTICE 继续遵从门槛和上游的 Tuning、PreArm、Arm 前缀规则。缺片超时、无法证明来源的通用或自由文本、未列入规则的版本按原文处理。

### ArduPilot 固件消息目录

目录覆盖六个固件家族，按同一连接主飞控的 HEARTBEAT 和 AUTOPILOT_VERSION 匹配机型、四段版本及八字节构建标识。记录包括已核对的 4.x 正式构建、保留源码 Beta 标志的 ArduSub 4.1.1 和 Blimp 4.8.0 的官方开发 SITL。版本号、构建标志和模板都从源码核对。[ArduPilot 官方标签](https://github.com/ArduPilot/ardupilot/tags) · [Blimp 官方构建记录](https://firmware.ardupilot.org/Blimp/latest/sitl/git-version.txt)

每条原始消息只登记一次经核对的附加译文。消息页在原文旁显示，复制和 TLog 保留原文。HUD 和语音沿用配置的严重度、前缀、来源及忙时覆盖，先组装分片再判断。PreArm 继续按上游规则显示并静音。来源身份发生冲突或接收代重置后，附加译文随之失效。

动态字段按源码 printf 格式编译，签号、小数位、单位和固定百分号都参与匹配。译文保留实际数值和字段。共享电池状态只接受核对过的状态词，机型模式及标识符有明确字符范围。缺片、格式不一致、未知构建和未审查文本沿用原文。目录每条规则列出源码路径与适用构建集合。可用 [生成脚本](../scripts/localization/build_ardupilot_status_catalog.py) 的 --check 参数校验目录与生产代码一致，用 --fixtures FILE 导出逐构建合成 MAVLink 样例用于离线回放。


原有 applet 规则的源码以 4.7.3 的同一提交为准。[VTOL QuickTune](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Scripting/applets/VTOL-quicktune.lua) · [Rover QuickTune](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Scripting/applets/rover-quicktune.lua) · [Plane 轨迹特技](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Scripting/applets/Aerobatics/FixedWing/plane_aerobatics.lua) · [Plane 速率特技](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Scripting/applets/Aerobatics/FixedWing/RateBased/sport_aerobatics.lua) · [原生 QuickTune](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Quicktune/AP_Quicktune.cpp)

新增 applet 来源。[MissionSelector.lua](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Scripting/applets/MissionSelector.lua) · [plane_precland.lua](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Scripting/applets/plane_precland.lua) · [Heli_IM_COL_Tune.lua](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Scripting/applets/Heli_IM_COL_Tune.lua) · [quadplane_terrain_avoid.lua](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Scripting/applets/quadplane_terrain_avoid.lua) · [Gimbal_Camera_Mode.lua](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Scripting/applets/Gimbal_Camera_Mode.lua) · [runcam_on_arm.lua](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Scripting/applets/runcam_on_arm.lua) · [BattEstimate.lua](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Scripting/applets/BattEstimate.lua) · [crash-actions.lua](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Scripting/applets/crash-actions.lua)

Windows 独立语音回归用 [WindowsSpeechProbe](../MissionPlannerTests/WindowsSpeechProbe/README.zh-CN.md)。完整应用的原生设置控件由 Windows 回归脚本加载。第二种中文音色、实际卸载音色包和操作员听感需要相应 Windows 环境。
