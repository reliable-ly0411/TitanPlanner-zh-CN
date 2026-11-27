# Auxiliary camera captions (#39)

This catalog slice is display-only. It adds a Chinese label after the exact
English `174:Camera Image Tracking` and `175:Camera Lens` options in Enhanced
Simplified Chinese on the previously supported Copter/Rover/Plane camera
caption path. It leaves other firmware unchanged, even if that firmware also
lists the option. The selected raw label and numeric option ID are unchanged.

The existing selector binds `GetParameterOptionsInt("RC6_OPTION", firmware)`.
Both entries match that exact parameter in these official release files
(SHA-256 of complete source XML):

| ArduPilot release | SHA-256 |
| --- | --- |
| [Copter 4.7.1](https://autotest.ardupilot.org/Parameters/versioned/Copter/stable-4.7.1/apm.pdef.xml) | `9e5e674c6029bd9f680540c62119086c26991d3ba586c585f4e98484036c0518` |
| [Plane 4.7.1](https://autotest.ardupilot.org/Parameters/versioned/Plane/stable-4.7.1/apm.pdef.xml) | `924fdf56b972719bde381082adb29fd415731aa8cd899ec111eb4a5eda5192d6` |
| [Rover 4.6.3](https://autotest.ardupilot.org/Parameters/versioned/Rover/stable-4.6.3/apm.pdef.xml) | `0c009e0a35c0ab8f3455f1bb323c29f6a59152dc54cb5e0edcc95afd48127de5` |

The review also matched the [pinned RC metadata](https://github.com/ArduPilot/ardupilot/blob/7250f9d47a89c7859b63f09d18a1c401a9358657/libraries/RC_Channel/RC_Channel.cpp#L241-L242)
and checked the meaning of Camera Lens against the [official auxiliary function documentation](https://docs.ardupilot.org/copter/docs/common-auxiliary-functions.html).
This evidence does not add either option to an older catalog.

Run the production display code in a standalone Mono/WinForms combo box:

```sh
xvfb-run -a -s '-screen 0 1024x768x24 -dpi 120 -extension GLX' \
  python MissionPlannerTests/AuxiliaryCameraLabels/run-isolated.py
```

The same fixture can be run at other display DPI, or against old code
using `--actions-source /path/to/older/FlightDataActions.cs`. The fixture
binds synthetic rows with the verified IDs and raw labels, opens the actual
WinForms dropdown, and checks width, current display, and retained raw
list/selection after firmware rebinding. Test output on Mono 6.8:
363 assertions each at 96, 120 and 144 display DPI, 120/220 px control widths,
`zh-CN`, `zh-Hans`, and `en-US`. No action buttons or transport are present.
Not tested: native Windows DPI matrix, full ground station build, SITL or devices.
