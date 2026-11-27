# 机型日志类型位掩码的分版说明

`LOG_BITMASK` 选择飞控记录哪些日志类型。在七份固定 ArduPilot PDEF 中，它都位于机型自己的 `vehicles/parameters` 节点。Copter、Plane、Sub 的原始名称分别是 `ArduCopter:LOG_BITMASK`、`ArduPlane:LOG_BITMASK` 和 `ArduSub:LOG_BITMASK`。参数表沿用原键 `LOG_BITMASK`，每个固件版本单独匹配英文和整条官方记录。

| 中文说明适用范围 | 已审 `Description` |
| --- | --- |
| Copter 4.5.7，Plane / QuadPlane 4.5.7、4.6.3、4.7.1 | 选择飞控要记录的日志类型。参数值等于所选各项对应的掩码值之和。官方说明建议用 65535 启用基本日志类型。 |
| Copter 4.6.3、4.7.1 | 选择飞控要记录的日志类型。参数值等于所选各项对应的掩码值之和。官方说明建议用 65535 启用基本日志类型。若想减小日志文件，官方建议优先考虑用 LOG_FILE_RATEMAX 限制流式日志消息写入文件的频率。 |
| Sub 4.7.1 | 用 4 字节位掩码选择飞控要记录的日志类型。 |

七份 PDEF 均无结构化默认值、范围、单位或普通枚举字段。Copter 和 Plane 英文中的 `65535` 是官方给出的基本日志掩码建议。Copter 两个较新版本还提到文件后端的流式日志限速。Sub 的英文只说明 4 字节掩码，位 14 保留官方标签 `INAV`。该版源码的日志位宏跳过了 14，单凭标签无法断定这一位会生成记录。[Copter 参数定义](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/ArduCopter/Parameters.cpp#L207-L212) · [Plane 参数定义](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/ArduPlane/Parameters.cpp#L628-L633) · [Sub 参数定义](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/ArduSub/Parameters.cpp#L270-L275) · [Sub 位宏](https://github.com/ArduPilot/ardupilot/blob/dbe792162d06cab66c3475fd5556bf7a120f119e/ArduSub/defines.h#L50-L68)

## 来源与覆盖分母

每份完整 PDEF 均只有一条对应记录。基线在七版均未登记此键的中文说明。本批新增 7/7 条，五份目录记录覆盖七版。Copter 和 Plane 的 4.5.7 只有 `Bitmask` 文本，其余五份同时包含结构化位节点，两种表示共存时编号、标签和顺序一致。[基线](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/commit/c2e41fd4864acb3ea5c3fd8217e6967390081d2c) · [标签、源码提交及官方文件 SHA-256](../localization/parameters/sources.json)

| 固件 | 固定 PDEF | 原始行号 | 位掩码表示 | 原位标签数 | 全文件已译变化 / 不同键 | 新增后未译 |
| --- | --- | ---: | --- | ---: | ---: | ---: |
| Copter | [4.5.7](https://autotest.ardupilot.org/Parameters/versioned/Copter/stable-4.5.7/apm.pdef.xml) | 394–396 | 仅文本 | 20 | 96 → 97 / 4,426 | 4,329 |
| Copter | [4.6.3](https://autotest.ardupilot.org/Parameters/versioned/Copter/stable-4.6.3/apm.pdef.xml) | 413–437 | 文本及位节点 | 20 | 98 → 99 / 4,776 | 4,677 |
| Copter | [4.7.1](https://autotest.ardupilot.org/Parameters/versioned/Copter/stable-4.7.1/apm.pdef.xml) | 346–370 | 文本及位节点 | 20 | 104 → 105 / 5,688 | 5,583 |
| Plane / QuadPlane | [4.5.7](https://autotest.ardupilot.org/Parameters/versioned/Plane/stable-4.5.7/apm.pdef.xml) | 609–611 | 仅文本 | 18 | 107 → 108 / 4,485 | 4,377 |
| Plane / QuadPlane | [4.6.3](https://autotest.ardupilot.org/Parameters/versioned/Plane/stable-4.6.3/apm.pdef.xml) | 660–682 | 文本及位节点 | 18 | 109 → 110 / 4,851 | 4,741 |
| Plane / QuadPlane | [4.7.1](https://autotest.ardupilot.org/Parameters/versioned/Plane/stable-4.7.1/apm.pdef.xml) | 658–680 | 文本及位节点 | 18 | 117 → 118 / 5,722 | 5,604 |
| Sub | [4.7.1](https://autotest.ardupilot.org/Parameters/versioned/Sub/stable-4.7.1/apm.pdef.xml) | 260–283 | 文本及位节点 | 19 | 65 → 66 / 5,369 | 5,303 |
| 合计 | 七份完整文件 | | | 133 | 696 → 703 / 35,317 | 34,614 |

`LOG_` 原键前缀在七版合计 98 条，基线已译 91 条，本批补齐七条后为 98/98。官方 `libraries/parameters[@name='LOG']` 参数组合计 91 条，在本批开始前已全部覆盖。PX4 v1.16.0 的固定参数参考中没有 `LOG_BITMASK` 原键。以上全文件分母仅覆盖表中七份 ArduPilot 元数据，PX4 各构建的完整分母尚未核实。[PX4 v1.16.0 参数参考](https://github.com/PX4/PX4-Autopilot/blob/6ea3539157ca358c70a515878b77077af7d4611d/docs/en/advanced_config/parameter_reference.md)

| 固件范围 | 英文 SHA-256 | 完整记录 SHA-256 |
| --- | --- | --- |
| Copter 4.5.7 | `e05957d9b84b10457744038d26d3fd9c9f325124b21d146c1c7d371faba96c9f` | `b11cc07fbe423cad92d9138dcc1b09f763d2a671ee47d84a67bd00231101c095` |
| Copter 4.6.3、4.7.1 | `5d668a442048e3857c1ce5b0bd331f307cd691783d9f623b46aa46735db5bed8` | `c83cee4f491fc9e2046ff7c6971ab630f16bfe233f5695103611e16d62defb58` |
| Plane 4.5.7 | `eff6fd690bdcd71224a903ce8ec1db8a63222db93fbb9a1a88b7f153fe97b6c2` | `5d67787b1147b50c7db174c78e49d32c79be0c20d23aadc919eb1be47e0ce501` |
| Plane 4.6.3、4.7.1 | `eff6fd690bdcd71224a903ce8ec1db8a63222db93fbb9a1a88b7f153fe97b6c2` | `252d2b524724372951b69fa095e921b28a574e5945f71101ae81abd09881c02d` |
| Sub 4.7.1 | `7db23d2a7c61971ab6f852c5a13a8a35639e8a0ee2c3363a3bd8375dcf76e300` | `821c54e8f49dcd8ebb6ba29e26f9851daabdad5efd1d8d1d42a33c658c8fc32c` |

## 只读代表行

同一个内存样例 `1024` 对应位 10。Copter 的原标签为 `RC output`，Plane 为 `Compass`，Sub 为 `RCOUT`。三者的单位和范围均为空。选项格按 `bit N: 原标签` 的形式逐行显示，原始位号含空档，数字和英文大小写均需保留。

| 机型 | 选项格文本 SHA-256（UTF-8、LF 分隔、末行无 LF） |
| --- | --- |
| Copter | `0deb2d803476fc279e105f6f09d03d99b1795fc4f3c176db641053daabe60b8b` |
| Plane | `ffe8a870fcdaa074b26a42ded3b88654fce0867bd425163121124bc67f95f26a` |
| Sub | `1634b6ff44ee29c0978458746018f05b1d3bae03ce2fb48ba9afbc00f9a27fde` |

Plane 4.5.7 与 4.6.3 的英文和 18 行选项完全相同，整条记录指纹不同。在 4.6.3 会话中用 4.5.7 的官方节点做内存对照时，预期保留英文及 18 个原位标签，显示“待复核”并撤下已审译文。未登记的 4.7.2 需沿用原有版本回退。

对单份完整 PDEF 可在仓库根目录复核来源、精确已译数和两个独立参数组。

```sh
python scripts/localization/audit_parameter_coverage.py Plane 4.6.3 Plane-4.6.3.xml --group ArduPlane --group LOG
```
