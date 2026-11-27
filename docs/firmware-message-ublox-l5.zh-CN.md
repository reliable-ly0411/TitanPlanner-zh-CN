# 固件消息显示证据：u-blox 支持 L5 频段
在 [ArduPilot d9c5097c 固定源码](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_GPS/AP_GPS_UBLOX.cpp#L1459-L1478)中，驱动从固件版本响应的扩展字段识别 L1/L5 能力后发出固定 INFO 文本 `u-blox supports L5 Band`。它只表示经识别的硬件能力，不表示 L5 正在使用、接收了特定信号或获得了定位。

仅在简体 2 的飞行数据消息列表，沿用现有 ArduPilot 4.7.3 发送源与完整包门槛，保留整条原文并另附「u-blox 支持 L5 频段」。不改语音、日志、诊断严重度或 GPS 功能。测试使用离线合成 MAVLink 报文，不连接设备。
