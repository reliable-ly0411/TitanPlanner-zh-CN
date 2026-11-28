# 增强简体中文语音播报

在 Windows 的「地面站设置 → 语音播报」可选实际后端枚举到的微软中文音色。自动模式优先匹配简体中文语言，缺少可用音色时回退为系统默认。开启语音后可在空闲时试听。正式播报进入语音后端时会取消当前试听。已保存的音色随地面站设置读写。普通简中和英文沿用其原有声音。Linux/Mono 沿用 Festival。

「中文(简体)2」的 {mode} 和 Home 航点使用已审核词汇。航点、模式、周期、电池及低高度首次编辑时提供中文模板。已保存模板保留原文和变量展开顺序。链路超时的原始 HUD 文字与播报分开。

| 固件／机型 | 中文播报覆盖 | 验证入口 |
| --- | --- | --- |
| ArduPilot 4.7.3 Copter | VTOL QuickTune 脚本及原生 QuickTune 的已核对状态 | 固定消息、轴名及含小数的中止消息夹具 |
| ArduPilot 4.7.3 Plane / QuadPlane | 日志芯片已满、已核对的快速调参状态、轨迹及速率特技脚本消息 | 固定告警、状态、次数及带参数消息夹具 |
| ArduPilot 4.7.3 Rover / Boat | Rover QuickTune 启动、进度、参数、错误与完成状态 | 百分比、负数、小数和单位夹具 |
| ArduPilot 4.7.3 Sub / Tracker 及其他版本 | 地面站遥测播报。未列入规则的固件报文沿用原文和原播报条件 | 原文回退 |
| PX4 1.15.4、1.16.2、1.17.0、1.18.0 | 匹配正式构建的电池 STATUSTEXT。1.17.0 四旋翼另有 EKF2 磁力计参数告警 | 报文来源证明与播报去重夹具 |
| PX4 Events | 待接入事件元数据与译文。已有 STATUSTEXT 播报继续可用 | 传统消息回放 |

ArduPilot 脚本规则固定到 4.7.3 的八字节构建标识，并要求同一接收环境下的主飞控心跳、版本、机型、消息级别和完整文本匹配。固件脚本与原生 QuickTune 的文字分别审核。脚本 NOTICE/INFO 仅在增强简中且符合语音开关和仅解锁时播报条件时追加到语音路径，原始级别和日志保留。

官方来源均固定到 4.7.3 的同一提交。[VTOL QuickTune](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Scripting/applets/VTOL-quicktune.lua) · [Rover QuickTune](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Scripting/applets/rover-quicktune.lua) · [Plane 轨迹特技](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Scripting/applets/Aerobatics/FixedWing/plane_aerobatics.lua) · [Plane 速率特技](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Scripting/applets/Aerobatics/FixedWing/RateBased/sport_aerobatics.lua) · [原生 QuickTune](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Quicktune/AP_Quicktune.cpp)

Windows 独立语音回归用 [WindowsSpeechProbe](../MissionPlannerTests/WindowsSpeechProbe/README.zh-CN.md)。完整应用的原生设置控件由 Windows 回归脚本加载。第二种中文音色、实际卸载音色包和操作员听感需要相应 Windows 环境。
