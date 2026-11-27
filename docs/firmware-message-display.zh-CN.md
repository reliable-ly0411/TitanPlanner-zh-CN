# 固件消息显示证据：ArduPilot EKF3 初始倾斜对准

| 来源 | 发送点与条件 | 支持显示 |
| --- | --- | --- |
| [ArduPilot d9c5097c](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_NavEKF3/AP_NavEKF3_Control.cpp#L511-L524) | `NavEKF3_core::checkAttitudeAlignmentStatus`，首次初始化时倾斜误差方差收敛后发 INFO。原模板 `EKF3 IMU%u tilt alignment complete`，参数为无符号 IMU 索引。 | 复用接收方已验证的 ArduPilot 4.7.3 版本与构建标记、车辆类型及完整数据包门槛；仅支持原文中 0–2 的精确拼写。 |

简体 2 的飞行数据消息列表保留整条英文，另显示「EKF3 IMU# 倾斜对准完成」。这不是偏航已对准、原点已建立或可解锁的声明。未知索引、其他版本/飞控、非 INFO、不完整或分片，以及拼写不同的消息都不追加这条译文。没有新增语音、高优先 HUD 或结构化 PX4 Events 的翻译。

相关测试位于 `MissionPlannerTests/Utilities/MessageDisplayContextTests.cs`。使用离线合成 MAVLink 报文对照原接收上下文，不连接飞控。
