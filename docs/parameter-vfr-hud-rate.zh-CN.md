# VFR_HUD 遥测速率参数说明

本批仅补充简体中文 2 的说明，不修改参数值、消息、速率、发送、带宽或配置。所列遥测速率参数本身是可编辑参数，不是只读字段。未增加设置建议。

| 固件 | 固定官方版本 | 说明范围 |
| --- | --- | --- |
| Copter、Plane | 4.5.7、4.6.3 | SR0_EXTRA2 至 SR6_EXTRA2 |
| Copter、Plane、Sub | 4.7.1 | MAV1_EXTRA2 至 MAV32_EXTRA2 |

39 个遥测速率参数键、124 条分版说明；原文均为 `MAVLink Stream rate of VFR_HUD`，译为「VFR_HUD 消息的 MAVLink 发送频率。」。每一条仍按机型、版本、精确参数名及完整记录指纹匹配。同文不跨键套用；原目录有无标注单位均保持不变，范围、步长与重启标志原样保留。

另补一条独立只读说明：Copter 4.5.7 的 `FORMAT_VERSION`（EEPROM 格式变化时版本号递增）。不扩展至官方未标记只读的版本。

固定来源和 SHA-256 见 `localization/parameters/sources.json`。
