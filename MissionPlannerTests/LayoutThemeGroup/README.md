# Layout/Theme group same-size activation regression (#190)

The inner Layout/Theme table now docks to the top, without changing the shared
table factory, neighboring groups or the original layout/appearance options.
A Fill-docked table inside this auto-sized group previously fed the last layout
height back into the next one on Mono, even without the firmware checkbox or
appearance settings.

The offline runner uses the real Backstage and extracts the production
Layout/Theme group, settings column container and helper methods. Only the host,
neighboring groups, label texts and the Mono-missing `DeviceDpi` getter are
fixture replacements. It checks 12 repeated entries, narrower/wider settings
and restoration, row wrapping/containment, long scroll, and keyboard focus.
To check compatibility with #189, pass the planner source from a disposable
combination of the two changes and the matching `FirmwareListRequest.cs`; the
runner compiles the exact production toggle with an in-memory settings store.

```sh
xvfb-run -a -s '-screen 0 1790x1080x24 -dpi 120 -extension GLX' \
  python MissionPlannerTests/LayoutThemeGroup/run-isolated.py --language zh-Hans --dpi 120
```

Options `--planner-source` and `--firmware-settings-source` allow source-only
A/B and #189 compatibility runs. At the same 766px effective viewport width the
unchanged source grew 1570→1750px on the first checked return (scroll extent
3643→3823px); the new group stays 278px with a 2366px scroll extent. The #189
combo also stays 278px and retains its unchecked value and focusability.
Stock Simplified Chinese at 120, English at 96 and enhanced Simplified Chinese
at 144 display DPI passed; the #189 combination passed at 120.

This result does not diagnose native Windows slow-open timings or the separate
last shortcut-row clipping seen at 125% (#26). No complete build, Windows
foreground, vehicle, firmware request, or real settings write was performed.
