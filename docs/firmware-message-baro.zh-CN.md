# 固件消息显示证据：ArduPilot 气压计校准完成

[ArduPilot d9c5097c 的 AP_Baro 源码](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_Baro/AP_Baro.cpp#L369-L376)在校准末段按传感器检查已校准标记，并给相应编号发 INFO。模板是 `Barometer %u calibration complete`，传感器编号从 1 起。本次只收录第一个气压计的精确原文；不推断其他气压计、整体系统状态或是否可以飞行。

简体 2 在飞行数据消息列表保留整条英文原文，另附「1 号气压计校准完成」。沿用已支持的 4.7.3 版本、构建、发送来源和完整包门槛。未知版本、设备编号、严重度、分片、不完整或不同文案不追加该译文。没有改动现有语音、校准、报文和日志。测试参见 `MissionPlannerTests/Utilities/BarometerStatusDisplayTests.cs`，不连接设备。
