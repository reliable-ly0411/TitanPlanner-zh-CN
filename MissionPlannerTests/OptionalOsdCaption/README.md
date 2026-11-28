# Optional Hardware OSD sidebar caption (#54)

The entry registered for `ConfigHWOSD` used the unlocalized `OSD` caption in
all three tested languages. The independently reviewed #143 only changed text
inside the OSD page, and left that title unchanged.

Initial Setup restores the last selected page by `BackstageViewPage.LinkText`,
so it stays `OSD`. The generic, opt-in `MenuCaption` changes only the string
drawn in the sidebar; section, tag/page type, sizing/indentation/scroll
algorithm, keyboard focus, tracking text, pop-out title and activation
callbacks keep the original path. Only a registered OSD page in enhanced
Simplified Chinese opts in. Neutral and ordinary Simplified Chinese remain
`OSD`; a missing enhanced catalog entry falls back to `LinkText` as before.

```sh
xvfb-run -a -s '-screen 0 1024x768x24 -dpi 120 -extension GLX' \
  python MissionPlannerTests/OptionalOsdCaption/run-isolated.py
python scripts/localization/build_ui_catalog.py --check
```

The test compiles actual Backstage controls, the precise Initial Setup caption
function, and actual neutral/zh-Hans/zh-CN resources. It registers dummy pages
only: application startup, real `ConfigHWOSD` activation, all
telemetry/OSD actions and devices are excluded. It asserts expected display and
unchanged raw/markup captions, parent and state identity, caption fit at
172/250px sidebar widths, keyboard focusability, selection/tracking, and lookup
by saved `OSD` after resizing. 60 assertions pass per display at 96/120/144/192
DPI under isolated Mono/Xvfb. This is not a full build, shared #127 native DPI
acceptance, or a claim of coverage for other optional hardware pages.
