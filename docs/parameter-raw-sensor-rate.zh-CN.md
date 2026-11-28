# 原始传感器读数遥测速率参数说明

本批只增加简体中文 2 的说明。参数本身可编辑，但本次不变更数值、发送频率、带宽、消息、配置或设备行为，不提供频率建议。

- Copter 和 Plane 4.5.7、4.6.3：`SR0_RAW_SENS` 至 `SR6_RAW_SENS`。
- Copter、Plane 和 Sub 4.7.1：`MAV1_RAW_SENS` 至 `MAV32_RAW_SENS`。
- 39 个原键、124 条分版说明。Copter 和 Plane 4.5.7 的原文都不列出 `AIRSPEED`，共 14 条；其余五个正式版的 110 条保留原文中的 `AIRSPEED`。IMU 与压力消息标识原样保留。
- 只对登记的机型、正式版、精确参数键和完整记录指纹生效，不跨流复用。原单位、范围、步长和重启标志保持原样。官方来源与完整 SHA-256 见[来源登记](../localization/parameters/sources.json)。
