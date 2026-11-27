# 飞行数据 → 消息：已保存诊断文件（#41）

只给正式 ArduPlane 4.4.4 的 NOTICE 5 `Saved crash_dump.bin` 添加「已保存 crash_dump.bin」。文件名不改；原文仍可见并按原样复制。没有新增语音或修改日志、HUD、消息级别和触发条件。别的固件、来源或等级即使写了同一句，仍只显示原文。其他文件、看门狗、失败和操作提示未纳入。

[Plane-4.4.4 官方源码 16b78382](https://github.com/ArduPilot/ardupilot/blob/16b783822905668b160b0eb1ef5599ce0d3a0231/libraries/AP_Logger/AP_Logger.cpp#L1370-L1384) 确认原句及级别；它报告固件保存诊断文件，不验证文件内容或下载。用[官方 CubeOrange 固件](https://firmware.ardupilot.org/Plane/stable-4.4.4/CubeOrange/arduplane.apj)静态核对了完整原句与构建标识；未执行、安装或附带固件。读取 APJ 的 SHA-256 是 `01dc7507dd96424438ee803f75a3e61a7c469f68d8efb2892a40989132cf011c`，`git_identity` 为 `16b78382`，解压 image 恰有一处以 NUL 结尾的 `Saved crash_dump.bin`。这不是对所有板卡或所有日志消息的证明。

现行来源门不改：固定翼 ArduPilot 运行时版本与 Git 标识、主组件、同一接收代与消息列表、完整未分片的精确 UTF‑8 文本。简体1、英文、缺简体2资源维持原文。定向测试覆盖错版本/发送方、不同等级、分片、不同文件名、剪裁、原始文本复制和语音不受影响。Windows 实包未运行。
