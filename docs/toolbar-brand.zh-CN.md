# 顶栏品牌标志

在「地面站设置 → 顶栏品牌标志」选择预设。顶栏品牌和窗口 ICO、启动图互相独立。新安装和恢复默认显示随软件附带的现有标志；设置随原有应用配置保存，切换即时刷新。恢复默认不删除已导入文件。

可为每个品牌导入自己的浅色、深色 PNG，最多 4 MiB、每边最多 1024 px。程序根据实际工具栏背景选择浅／深色版；缺少有效对应图像时，尝试不分色的本地预设图（可选，由用户以不带后缀的预设名保存，如 `px4.png`），否则连同左侧链接一起回退为现有标志。待选预设和实际显示可能因此不相同。不同画幅等比放进固定预留区，不移动相邻遥测控件。

| 预设 | 左侧跳转 |
| --- | --- |
| 可靠的 ly0411 / ArduPilot | ArduPilot |
| TitanPlanner | Titan Dynamics |
| PX4 | PX4 |
| 奋斗者 MFE | MFE 4.4.4 原程序的 ArduPilot 跳转 |

右侧始终跳到 `https://github.com/reliable-ly0411`。不根据连接的设备选择外观，不影响串口或飞控行为。

MFE 来源为 [厂商公开下载目录](https://fw.makeflyeasy.com/Mission_Planner_VTOL_Survey/) 的 VTOL Survey V4.4.4。安装包 SHA-256 `7b165c4965633e82785122afdfba7951d21bb43a85fd986e0eaeff2e59690c9e`，安装后主程序 SHA-256 `9ba4db652ef9fc2058010310f7159412e809c69bd871f94a6ea94c4bf2b96ab0`。静态查看 `MainV2.MenuArduPilot_Click` 调用 `Process.Start`，参数为 `https://ardupilot.org/?utm_source=Menu&utm_campaign=MP`；没有运行该包。没有将第三方商标、安装器或原包素材添加到仓库。导入仅限用户拥有使用权的本地素材。
