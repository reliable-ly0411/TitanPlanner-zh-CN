# #41：ArduPlane 4.7.3 的 GPS 2 u-blox INFO（显示说明）

- 固定来源：[ArduPlane 4.7.3 版本提交](https://github.com/ArduPilot/ardupilot/commit/d9c5097c3d8333435712ffed86bca8ce00c4bb46)；[AP_GPS.cpp 862–866](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_GPS/AP_GPS.cpp#L862-L866) 首次收到可解析数据时以 INFO 发送 `GPS %d: detected %s`，传入 `instance + 1` 与后端名；[后端名称](https://github.com/ArduPilot/ardupilot/blob/d9c5097c3d8333435712ffed86bca8ce00c4bb46/libraries/AP_GPS/AP_GPS_UBLOX.h#L180-L184) 为 `u-blox`。本条要求第二接收器（内部索引 1）、匹配后端且尚未公告，不表示已有定位、当前使用该接收器或任何频段。
- 仅给精确 `GPS 2: detected u-blox` 增加「检测到 GPS 2：u-blox」。接收入口仍只记主组件 `compid=1`；来源守卫仍要求同一会话/消息列表的已知固定翼 ArduPilot 心跳、精确 4.7.3 版本串及无冲突。只有完整 INFO（级别 6）才匹配。新规则不改 GPS 1 行、不推广模板；其它编号、拼写、厂家、版本、飞控、分片及不完整报文保留原文。版本标识提供显示语境，不是身份认证。
- 仅简体中文 2 显示附加说明。原消息对象、复制／记录、排序、级别、时间、语音和协议未改。与 #212（气压计/L5 能力）、#196（EKF 倾斜对准）、#182（4.6.3 原点）不同；组合时保留各自严格来源判断。未接入 PX4 Events 或 STATUSTEXT 分片处理。
- `Gps2DetectionStatusDisplayTests` 使用实际 MAVLink 编解码合成帧和候选生产显示上下文。私有本地补测使用实际 WinForms 消息列表检查三语行显示、原文复制文本和未知回退。未启动地面站、实际连接、Windows 实包或硬件，不计作完整覆盖。
