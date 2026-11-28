# 固件告警的离线回放样本

以下两条是根据固定版本源码构造的 MAVLink 报文。接收端用同一系统主组件的心跳和版本报文确认来源。消息列表在原文旁显示译文，复制和日志使用原文。

| 来源 | 原文和等级 | 触发条件 |
| --- | --- | --- |
| ArduPlane 4.7.3，固定翼 | `Chip full, logging stopped`，WARNING（4） | 使用块存储日志后端，日志芯片已满，固件停止记录。[发送位置](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Logger/AP_Logger_Block.cpp#L296-L325) |
| PX4 v1.17.0，四旋翼 | `EKF2_MAG_TYPE invalid, resetting to default`，CRITICAL（2） | 编入磁力计支持的 EKF2 检查到参数值无效，正在恢复默认值。同处还有独立的 Warning 事件，带一个浮点参数。[发送位置](https://github.com/PX4/PX4-Autopilot/blob/d6f12ad1c4f70ad3230afd7d86e971421e02fef4/src/modules/ekf2/EKF2.cpp#L866-L885) |

两条文字都没有变量。`EKF2_MAG_TYPE` 是参数名。PX4 的传统文字流为每条消息分配递增 ID，完整的短消息也可能有非零 ID。回放覆盖首个独立短包及非零 ID 的完整首片，核对正文和零填充。[PX4 单帧与分片编码](https://github.com/PX4/PX4-Autopilot/blob/d6f12ad1c4f70ad3230afd7d86e971421e02fef4/src/modules/mavlink/streams/STATUSTEXT.hpp#L88-L118)

ArduPlane 使用组件 1 的固定翼心跳、4.7.3 正式版本号和 `d9c5097c`。是否能产生这条告警取决于板上的日志后端。PX4 使用组件 1 的四旋翼心跳、1.17.0 正式版本号、零厂商字段和该版本的提交标识。[ArduPilot 日志后端配置](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Logger/AP_Logger_config.h#L15-L40) · [PX4 版本字段](https://github.com/PX4/PX4-Autopilot/blob/d6f12ad1c4f70ad3230afd7d86e971421e02fef4/src/modules/mavlink/mavlink_main.cpp#L1088-L1097)

本批只覆盖表中两种来源的传统 `STATUSTEXT`。Copter／Heli、QuadPlane、Rover／Boat、Sub、Tracker、Blimp、其他 PX4 机型、脚本、长消息重组及 PX4 Events 仍待独立核对。其他组件、版本冲突、缺参或未知消息沿用原文。

## 地面站的播报入口

| 入口 | 已核对的路径 |
| --- | --- |
| 固件告警、脚本文字 | 接收器先判断 `STATUSTEXT` 的严重度、既有前缀和 PreArm 静音条件。直接播报与主循环回读高优先级消息共用同一条已核实告警的入队结果。接收端忙时仍可由原有回读路径补播。[接收器](../ExtLibs/ArduPilot/Mavlink/MAVLinkInterface.cs) · [高优先级消息](../ExtLibs/ArduPilot/CurrentState.cs) |
| 模式、航点 | `CurrentState` 从心跳或 `MISSION_CURRENT` 更新状态，按原有分类、选中车辆和解锁状态判断是否播报，使用用户保存的模板。[状态更新](../ExtLibs/ArduPilot/CurrentState.cs) |
| 周期提醒、电池、低速、高度、链路 | 主循环按各项条件调用语音引擎。模板仍由 `Common.speechConversion` 按原顺序展开；自定义文字、花括号、变量名和单位沿用已有实现。[主循环](../MainV2.cs) · [模板展开](../ExtLibs/ArduPilot/Common.cs) |
| Windows 音色、试听 | 增强简中从本机 `System.Speech` 筛选已启用的微软中文音色。音色选择随本地设置保存，缺失时使用自动选项。试听要求开启语音并且当前空闲；后来的正式播报可以打断试听。[设置页](../GCSViews/ConfigurationView/ConfigPlanner.SpeechVoice.cs) · [Windows 语音](../Utilities/Speech.Windows.cs) |

官方 Copter、Plane、Rover、Sub 和 Tracker 的 4.7.3 标签指向同一 ArduPilot 提交。各机型和板级编译开关仍需分别核对。PX4 单独核对 v1.17.0 及四旋翼心跳。[ArduPilot 固定提交](https://github.com/ArduPilot/ardupilot/commit/d9c5097c3d8333435712ffed86bca8ce00c4bb46) · [PX4 v1.17.0](https://github.com/PX4/PX4-Autopilot/releases/tag/v1.17.0)

两条样本可以在 `FirmwareDiagnosticReplayTests` 中重放，使用实际 MAVLink 编码器和解码器。夹具模拟接收器外层的主组件判断，检查同一条解码记录的显示、语音、原文、等级和时间。`FirmwareSpeechAlertTests` 用接受或拒绝请求的语音替身检查直接播报与回读。[回放测试](../MissionPlannerTests/Utilities/FirmwareDiagnosticReplayTests.cs) · [入队和回读测试](../MissionPlannerTests/Utilities/FirmwareSpeechAlertTests.cs)
