# Settings shortcut group regression (#26)

The long bilingual keyboard-shortcut table must measure to its content. Pairing
`DockStyle.Fill` with an auto-sized parent let each repeat activation on Mono feed
the preceding filled height back into the shortcut group. Docking just this
table to the top decouples its content height from the previous parent height.

The fixture builds the existing Backstage control, verbatim production settings
column layout/helpers/shortcut group, and a fixture registry of 38 existing
shortcut descriptions, without starting the application. The feature's exact
bilingual strings are compiled from the production dictionary. It substitutes
the requested test DPI for the one unsupported Mono 6.x Control.DeviceDpi
getter and uses synthetic neighboring settings. It checks 12 warm activations,
narrow/wide resizing and restoring the initial width, complete row/table
containment, no neighboring overlap, final row reachable with vertical scroll,
and neighboring keyboard focus.

Requires Python 3, Mono (`mcs` and `mono`) and an isolated X display. Example:

```sh
xvfb-run -a -s '-screen 0 1790x1080x24 -dpi 120 -extension GLX' \
  python MissionPlannerTests/ShortcutLayout/run-isolated.py --dpi 120
```

Use matching display and `--dpi` values. Use `--language zh-Hans` or
`--language en-US` for the stock-language controls. To demonstrate the
regression, repeat using `--planner-source /path/to/the/prior/ConfigPlanner.cs`
from an earlier commit.

On Mono 6.8 at 120 DPI and root width 766 px the old source failed with group
height 880→1045 px on a same-width return (scroll height 2954→3119 px). With the
fix, the group remained at 1657 px and total scroll height 3731 px throughout
same-width returns and after restoring the original width. Enhanced Chinese
checks passed at 96/120/144/192 display DPI and stock Simplified Chinese and
English passed at 120. These are isolated layout measurements, not actual
Windows per-monitor DPI, full-product performance, or an explanation of the
separately observed native long pause.
