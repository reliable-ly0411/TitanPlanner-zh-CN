# Auxiliary logging caption (#39)

The Aux Function dropdown obtains existing entries from
`GetParameterOptionsInt("RC6_OPTION", firmware)`. These official release
XMLs each provide the exact option `164:Pause Stream Logging`:

| ArduPilot release | SHA-256 of source XML |
| --- | --- |
| [Copter 4.7.1](https://autotest.ardupilot.org/Parameters/versioned/Copter/stable-4.7.1/apm.pdef.xml) | `9e5e674c6029bd9f680540c62119086c26991d3ba586c585f4e98484036c0518` |
| [Plane 4.7.1](https://autotest.ardupilot.org/Parameters/versioned/Plane/stable-4.7.1/apm.pdef.xml) | `924fdf56b972719bde381082adb29fd415731aa8cd899ec111eb4a5eda5192d6` |
| [Rover 4.6.3](https://autotest.ardupilot.org/Parameters/versioned/Rover/stable-4.6.3/apm.pdef.xml) | `0c009e0a35c0ab8f3455f1bb323c29f6a59152dc54cb5e0edcc95afd48127de5` |

See the [pinned ArduPilot metadata](https://github.com/ArduPilot/ardupilot/blob/7250f9d47a89c7859b63f09d18a1c401a9358657/libraries/RC_Channel/RC_Channel.cpp#L234)
and [logger declaration](https://github.com/ArduPilot/ardupilot/blob/7250f9d47a89c7859b63f09d18a1c401a9358657/libraries/AP_Logger/AP_Logger.h#L214-L219).
The caption preserves “stream logging”; it does not imply all logging stops.
This change only decorates exact matches on the reviewed vehicle families in
Enhanced Simplified Chinese. It does not add options or invoke them.

```sh
xvfb-run -a -s '-screen 0 1024x768x24 -dpi 120 -extension GLX' \
  python MissionPlannerTests/AuxiliaryLogLabel/run-isolated.py
```

Uses the production formatting and drawing methods in a standalone WinForms
combo box. It exercises narrow and normal controls and three UI cultures. Pass
`--actions-source /path/to/older/FlightDataActions.cs` to demonstrate the old
label. No vehicle, action button, protocol, or logging command is present.
