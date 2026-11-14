# TitanPlanner-zh-CN

> **中文双版本：** 保留“中文(简体)”并提供增强“中文(简体)2”。本仓库使用独立 .NET SDK 构建 Windows 程序，普通同步只运行轻量检查，新上游正式发布才自动构建候选，发布另受不可变源码与工件门禁约束；参见 [中文使用说明](README.zh-CN.md) 与 [上游同步及发布流程](AUTOMATION.zh-CN.md)。下文保留上游项目介绍。

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

For this Chinese-localized fork, use [TitanPlanner-zh-CN releases](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/releases) and read the [Chinese installation and verification notes](README.zh-CN.md). Source updates on `master` are not automatically published binaries.

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
