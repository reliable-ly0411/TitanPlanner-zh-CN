# #39 verified optical-flow caption

One display-only addition for exact raw `158:Optflow Calibration` in Copter and Plane, only under enhanced Simplified Chinese. The [fixed ArduPilot metadata](https://github.com/ArduPilot/ardupilot/blob/7250f9d47a89c7859b63f09d18a1c401a9358657/libraries/RC_Channel/RC_Channel.cpp#L228) lists only those two families. The current ground station uses `RC6_OPTION`; its actual shipped AP backup includes the exact wording for both. Rover has no such row. PX4 uses a separate shipped metadata file without this parameter. The old Sub/Tracker fallback tables happen to contain the same Copter list; their raw labels stay raw rather than asserting unsupported firmware coverage. There is no separate helicopter/QuadPlane list introduced here: respect existing firmware source plus exact (ID, original label). This work does not overlap #180, #199 or #216.

Run on a disposable X display:
```sh
xvfb-run -a python3 MissionPlannerTests/AuxiliaryOptflowLabel/run-isolated.py
```
The runner compiles only the production AUX display helper, extracted owner-draw and sizing code, and an inert WinForms ComboBox fixture. Rebind the shipped raw option arrays; check three languages, 120/220 px input, actual popup opening and width, firmware switching, aliases, unchanged IDs/labels/items, and fallback for unsupported copied entries. 401 assertions passed each in Xvfb equivalents 96/120/144 DPI. The unmodified candidate fails the Copter caption assertion, as expected. These are not native Windows DPI or a flight/optical-flow operation test. Nothing in the binding, state, selected radio position, request, device, or new-option paths is called or changed.
