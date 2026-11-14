## 本次同步与汉化差异

同步 TitanPlanner-1.3.9810.11050（上游提交 399939a98eaf9f31ee2ae1d003f918419838025b）。上游发行标签与程序内版本 1.3.83 是不同版本标识。

保留中文双版本及既有更新器、打包改进；修复设置页全宽快捷键区边距，扩展普通三列与全宽区回归检查；采用上游镜像、汉化开发与稳定分线。

# 上游发行说明

来源：https://github.com/Titan-Dynamics/TitanPlanner/releases/tag/TitanPlanner-1.3.9810.11050

Changes since [TitanPlanner 1.3.9802.40472](https://github.com/Titan-Dynamics/TitanPlanner/releases/tag/TitanPlanner-1.3.9802.40472).

### TitanPlanner

- Update images in README.md ([379f450](https://github.com/Titan-Dynamics/TitanPlanner/commit/379f45011b3d472b072a6c380119dac42ebeb5bc))
- Update image source in README.md ([0b3fa90](https://github.com/Titan-Dynamics/TitanPlanner/commit/0b3fa903e79378385ca3e8c7003333c0740fb489))
- PLAN terrain profile: rally points, the fence, zoom and pan ([60a444e](https://github.com/Titan-Dynamics/TitanPlanner/commit/60a444e504d47fbff90ae73666d5a687ff8f95a0))
- Maps: fade the vehicles that aren't selected, and show them all in 3D ([66d6241](https://github.com/Titan-Dynamics/TitanPlanner/commit/66d6241ccbad900b8a63973f37dd5bdaa9d835c6))
- 3D map: a beacon on other vehicles, and waypoint markers that fade ([01fe265](https://github.com/Titan-Dynamics/TitanPlanner/commit/01fe2650c3e3540373fcc0dafb58e714e2cf9825))
- 3D map: fade waypoint markers smoothly, default marker size 40 ([d07c037](https://github.com/Titan-Dynamics/TitanPlanner/commit/d07c037a875303db5b8fcb1251c721b4f851de12))
- 3D map: a yellow dot at the centre of numbered waypoint markers ([b49c5ba](https://github.com/Titan-Dynamics/TitanPlanner/commit/b49c5ba66be14248abe04fa69b5e6d0cefdb51d4))
- Release notes: compare the development build with the last release ([7b10994](https://github.com/Titan-Dynamics/TitanPlanner/commit/7b10994965656cb962b2eeaf703a70d0595cc6d9))
- Updater: wait for the planner to exit before replacing its files ([8e1a9a4](https://github.com/Titan-Dynamics/TitanPlanner/commit/8e1a9a48927dd3221ca205fa45f76138648b8b53))
- Updates: offer new dev builds, show how far behind the running build is ([f9ca0bd](https://github.com/Titan-Dynamics/TitanPlanner/commit/f9ca0bdc77eea1b873d3fb44ea88f82cd3d4ce42))
- HUD: resize on the UI thread, so a video stream can't freeze it ([738c86c](https://github.com/Titan-Dynamics/TitanPlanner/commit/738c86c9b8bd970428a67cc84e127162ba2c855e))
- Video tab: drop the appsink hint under the GStreamer pipeline ([a59553d](https://github.com/Titan-Dynamics/TitanPlanner/commit/a59553db57e50e892d76bb85bf2e0c147bf2052f))
- Joystick: live inputs, grouped layout, detect by most travel ([dcbe43a](https://github.com/Titan-Dynamics/TitanPlanner/commit/dcbe43a0e10d30d43ca63d70d44192ddf9589bce))
- Mode selector: Q modes only on quadplanes ([842215a](https://github.com/Titan-Dynamics/TitanPlanner/commit/842215ac978ec0cbeaedb1f207be8749ae65cf11))
- 3D map: Alt-F swaps between FPV and the chase view ([e687015](https://github.com/Titan-Dynamics/TitanPlanner/commit/e687015ef2782f8410ce68216b6b577d2ed3a26e))
- FLY screen: Alt-H for the HUD overlay, Alt-D and Alt-P for tabs ([7d957b5](https://github.com/Titan-Dynamics/TitanPlanner/commit/7d957b53bc8b8fad4253bbbc9fb94b429510aa4a))
- Keyboard shortcuts: declared once, listed in the Planner settings ([c931013](https://github.com/Titan-Dynamics/TitanPlanner/commit/c931013ae6c84098463b8e87f4a5f0000e4bd4e8))
- Joystick: send rate setting, readings sent at once, a quick setup page ([f41a63e](https://github.com/Titan-Dynamics/TitanPlanner/commit/f41a63ebf55049ff83594429bcd9a48c63281d24))

**Full diff:** [`bbbe226...399939a`](https://github.com/Titan-Dynamics/TitanPlanner/compare/bbbe226d993fd0ff8821b316137a4e6b79d81174...399939a98eaf9f31ee2ae1d003f918419838025b)

