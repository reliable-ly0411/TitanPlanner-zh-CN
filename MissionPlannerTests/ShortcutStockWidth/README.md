# Narrow stock-language shortcut widths (#26)

At 120 DPI in an isolated Windows session, the exact `ac9ebb4` package's
850×540 settings probe showed the English and original Simplified Chinese
shortcut table at 647×1443px but with an unconstrained preferred width of
815px. Scroll extents reached the GroupBox; the last row escaped the table
and ended at screen y716 below the viewport's y596 bottom. Enhanced
Simplified Chinese was not affected. A 1230×710 window showed the final row.

Native A/B on the same package copied only the existing enhanced-Chinese
description width constraint to those two stock languages. At 850×540
the description cap was 445px, the table grew to 1579px and the parent
measured 1620px. The final row ended at y580 within the unchanged viewport.
Both stock languages passed 99/99 with the narrow row visible, versus
98/99 untouched. Docking the old table to Top alone increased the table
height but did not correct its parent/reporting. This is a controlled
runtime diagnostic on the old binary, not a test of a revised package.

The source change applies the same width cap before measuring the group
regardless of language. Translation selection, original key/description
text, orientation and all other settings groups are unchanged.

```sh
xvfb-run -a -s '-screen 0 1790x1080x24 -dpi 120 -extension GLX' \
  python MissionPlannerTests/ShortcutStockWidth/run-isolated.py --language en-US
```

The isolated source fixture extracts the production shortcut builder and
table/helpers, pins representative settings widths, and checks caps, label
widths and language formatting at 1125→667→450→667→1125px. It passes in
English and both Simplified Chinese variants at 96/120/144/192 Xvfb display
DPI. With `--planner-source <original-ConfigPlanner.cs>` both stock locales
fail because their description width is not constrained. This local test
does not reproduce Windows native scrolling or complete app startup.
