# 固件告警的离线回放样本

以下两条是根据固定版本源码构造的 MAVLink 报文。接收端用同一系统主组件的心跳和版本报文确认来源。消息列表在原文旁显示译文，复制和日志使用原文。

| 来源 | 原文和等级 | 触发条件 |
| --- | --- | --- |
| ArduPlane 4.7.3，固定翼 | `Chip full, logging stopped`，WARNING（4） | 使用块存储日志后端，日志芯片已满，固件停止记录。[发送位置](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Logger/AP_Logger_Block.cpp#L296-L325) |
| PX4 v1.17.0，四旋翼 | `EKF2_MAG_TYPE invalid, resetting to default`，CRITICAL（2） | 编入磁力计支持的 EKF2 检查到参数值无效，正在恢复默认值。同处还有独立的 Warning 事件，带一个浮点参数。[发送位置](https://github.com/PX4/PX4-Autopilot/blob/d6f12ad1c4f70ad3230afd7d86e971421e02fef4/src/modules/ekf2/EKF2.cpp#L866-L885) |

两条文字都没有变量。`EKF2_MAG_TYPE` 是参数名。PX4 的传统文字流为每条消息分配递增 ID，完整的短消息也可能有非零 ID。回放覆盖首个独立短包及非零 ID 的完整首片，核对正文和零填充。[PX4 单帧与分片编码](https://github.com/PX4/PX4-Autopilot/blob/d6f12ad1c4f70ad3230afd7d86e971421e02fef4/src/modules/mavlink/streams/STATUSTEXT.hpp#L88-L118)

ArduPlane 使用组件 1 的固定翼心跳、4.7.3 正式版本号和 `d9c5097c`。是否能产生这条告警取决于板上的日志后端。PX4 使用组件 1 的四旋翼心跳、1.17.0 正式版本号、零厂商字段和该版本的提交标识。[ArduPilot 日志后端配置](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Logger/AP_Logger_config.h#L15-L40) · [PX4 版本字段](https://github.com/PX4/PX4-Autopilot/blob/d6f12ad1c4f70ad3230afd7d86e971421e02fef4/src/modules/mavlink/mavlink_main.cpp#L1088-L1097)

本批只覆盖表中两种来源的传统 `STATUSTEXT`。Copter／Heli、QuadPlane、Rover／Boat、Sub、Tracker、Blimp、其他 PX4 机型、脚本、长消息重组及 PX4 Events 仍待独立核对。其他组件、版本冲突、缺参或未知消息沿用原文。
