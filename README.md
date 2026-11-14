# TitanPlanner-zh-CN

> **中文双版本：** 保留“中文(简体)”并提供增强“中文(简体)2”。本仓库使用独立 .NET SDK 构建 Windows 程序，普通同步只运行轻量检查，新上游正式发布才自动构建候选，发布另受不可变源码与工件门禁约束；参见 [中文使用说明](README.zh-CN.md) 与 [上游同步及发布流程](AUTOMATION.zh-CN.md)。下文保留上游项目介绍。

## 双上游差异

`ahead` / `behind`：汉化分支相对上游镜像领先 / 落后的提交数。

| 汉化线 | 相对 TitanPlanner 镜像 `Titanmain` | 相对 MissionPlanner 镜像 `missionmain` |
| --- | --- | --- |
| `zh-CN` | [![zh-CN 相对 Titanmain ahead](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.github.com%2Frepos%2Freliable-ly0411%2FTitanPlanner-zh-CN%2Fcompare%2FTitanmain...zh-CN&query=%24.ahead_by&label=ahead&cacheSeconds=300&color=blue)](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/Titanmain...zh-CN) [![zh-CN 相对 Titanmain behind](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.github.com%2Frepos%2Freliable-ly0411%2FTitanPlanner-zh-CN%2Fcompare%2FTitanmain...zh-CN&query=%24.behind_by&label=behind&cacheSeconds=300&color=orange)](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/Titanmain...zh-CN) | [![zh-CN 相对 missionmain ahead](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.github.com%2Frepos%2Freliable-ly0411%2FTitanPlanner-zh-CN%2Fcompare%2Fmissionmain...zh-CN&query=%24.ahead_by&label=ahead&cacheSeconds=300&color=blue)](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/missionmain...zh-CN) [![zh-CN 相对 missionmain behind](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.github.com%2Frepos%2Freliable-ly0411%2FTitanPlanner-zh-CN%2Fcompare%2Fmissionmain...zh-CN&query=%24.behind_by&label=behind&cacheSeconds=300&color=orange)](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/missionmain...zh-CN) |
| `zh-CN-dev` | [![zh-CN-dev 相对 Titanmain ahead](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.github.com%2Frepos%2Freliable-ly0411%2FTitanPlanner-zh-CN%2Fcompare%2FTitanmain...zh-CN-dev&query=%24.ahead_by&label=ahead&cacheSeconds=300&color=blue)](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/Titanmain...zh-CN-dev) [![zh-CN-dev 相对 Titanmain behind](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.github.com%2Frepos%2Freliable-ly0411%2FTitanPlanner-zh-CN%2Fcompare%2FTitanmain...zh-CN-dev&query=%24.behind_by&label=behind&cacheSeconds=300&color=orange)](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/Titanmain...zh-CN-dev) | [![zh-CN-dev 相对 missionmain ahead](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.github.com%2Frepos%2Freliable-ly0411%2FTitanPlanner-zh-CN%2Fcompare%2Fmissionmain...zh-CN-dev&query=%24.ahead_by&label=ahead&cacheSeconds=300&color=blue)](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/missionmain...zh-CN-dev) [![zh-CN-dev 相对 missionmain behind](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.github.com%2Frepos%2Freliable-ly0411%2FTitanPlanner-zh-CN%2Fcompare%2Fmissionmain...zh-CN-dev&query=%24.behind_by&label=behind&cacheSeconds=300&color=orange)](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/missionmain...zh-CN-dev) |

徽章缓存至少 5 分钟；点击数字或下方链接查看 GitHub 当前比较结果。

| 汉化线 | 相对 TitanPlanner 镜像 | 相对 MissionPlanner 镜像 |
| --- | --- | --- |
| 稳定 `zh-CN` | [本线新增](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/Titanmain...zh-CN) · [镜像侧新增](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/zh-CN...Titanmain) | [本线新增](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/missionmain...zh-CN) · [镜像侧新增](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/zh-CN...missionmain) |
| 开发 `zh-CN-dev` | [本线新增](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/Titanmain...zh-CN-dev) · [镜像侧新增](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/zh-CN-dev...Titanmain) | [本线新增](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/missionmain...zh-CN-dev) · [镜像侧新增](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/compare/zh-CN-dev...missionmain) |

- “本线新增”与“镜像侧新增”分别查看双方独有提交；文件差异以共同祖先为起点。
- 统计包含功能、汉化和维护提交；MissionPlanner 对比还包含 TitanPlanner 自身改动。
- 镜像按[同步流程](AUTOMATION.zh-CN.md)定时或手动更新，进度见 [Actions](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/actions)。

## Mission Planner - Titan Dynamics Edition

<p align="center">
   <img width="300" height="300" alt="icon" src=".github/icon.png"/><br><br>
   A customized fork of <a href="https://github.com/ArduPilot/MissionPlanner">Mission Planner</a> with enhanced UI/UX and ease-of-use improvements
   <br><br>
   <img width="2560" height="1540" alt="image" src="https://github.com/user-attachments/assets/48d6ee95-7541-4f7f-8d8f-737ff875b205" /><br><br>
   <img width="1299" height="1056" alt="image" src="https://github.com/user-attachments/assets/ef922968-cc05-4402-bb51-fa37d8a365df" /><br><br>
   <img width="1278" height="809" alt="image" src="https://github.com/user-attachments/assets/d82c06ba-66a9-4a49-998e-9ab056a240e0" /><br><br>
   <img width="1718" height="1093" alt="image" src="https://github.com/user-attachments/assets/0afb4ca6-5123-49fb-bb2a-0c4430df698f" /><br><br>
   <img width="2560" height="1540" alt="image" src="https://github.com/user-attachments/assets/cdd2846e-2a46-4963-8d83-7ad75d65931f" /><br><br>
   <img width="2560" height="1540" alt="image" src="https://github.com/user-attachments/assets/6e3d361a-c47b-4b39-bf4b-3a560809b787" /><br>
</p>

---

## Enhancements

This fork implements every feature we've always yearned for with considerable UI/UX upgrades, a reworked HUD, a real-time 3D map with vehicle rendering (and ADSB), an overhauled parameter editor and tab layout, a Betaflight-style motor setup, USB auto-connect, offline map packs for air-gapped planning, and dozens of large improvements throughout the app.

**See [CHANGES.md](CHANGES.md) for the full list of features and fixes added in this fork.**

---

## Installation

### Windows (Recommended)

For this Chinese-localized fork, use [TitanPlanner-zh-CN releases](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/releases) and read the [Chinese installation and verification notes](README.zh-CN.md). Source updates on `zh-CN` or `zh-CN-dev` are not automatically published binaries.

The original Titan Dynamics packages remain available from [upstream releases](https://github.com/Titan-Dynamics/TitanPlanner/releases/latest); they do not include this fork's enhanced Chinese resources.

### Building from Source

Requires Visual Studio 2022.

```bash
git clone https://github.com/Titan-Dynamics/TitanPlanner.git
cd TitanPlanner
git submodule update --init
```

Open `MissionPlanner.sln` in Visual Studio 2022 and Build.

### Linux (Mono)

```bash
sudo apt install mono-complete mono-runtime libmono-system-windows-forms4.0-cil \
    libmono-system-core4.0-cil libmono-winforms4.0-cil libmono-corlib4.0-cil \
    libmono-system-management4.0-cil libmono-system-xml-linq4.0-cil

mono MissionPlanner.exe
```

See [Linux/README.md](Linux/README.md) for the upstream build (`./Linux/build.sh`) and launch (`./Linux/run.sh`) scripts. The prebuilt Linux and macOS downloads that document links to are published by upstream only; this fork ships Windows releases.

---

## Platform Support

| Platform | Status |
|----------|--------|
| Windows | ✅ Full Support |
| Linux (Mono) | ⚠️ WIP |
| macOS | ⚠️ WIP |
| Android | ⚠️ WIP |
| iOS | ⚠️ WIP |

---

## Data Sources

- Live ADS-B traffic: [adsb.fi](https://adsb.fi) open data (personal, non-commercial use)
- Motor frame diagrams: [ArduPilot documentation](https://ardupilot.org/copter/docs/connect-escs-and-motors.html) (CC BY-SA 3.0), shipped in `MotorFrames/`

## Upstream

- Upstream repository: https://github.com/ArduPilot/MissionPlanner
- ArduPilot website: http://ardupilot.org/planner/
- Forum: http://discuss.ardupilot.org/c/ground-control-software/mission-planner

## License

See [COPYING.txt](COPYING.txt).


## 分支导航 / Branches

- `Titanmain`：TitanPlanner 上游 `master` 精确镜像。
- `missionmain`：MissionPlanner 上游 `master` 精确镜像。
- `zh-CN`：汉化稳定维护线；分支存在不代表其中每个提交均已发包。
- `zh-CN-dev`：汉化开发与上游合并候选；验收后推进稳定线。
- `master`：迁移兼容历史；`release-policy-state`：发布状态记录，非产品分支。

镜像不含本仓改动。定时同步先刷新镜像，再检查开发线；不自动将开发候选升级为稳定版。详见 [分支和发布规范](RELEASING.repository.zh-CN.md)。
