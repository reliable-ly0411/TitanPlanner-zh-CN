# Flight Data sidebar: contained Video touch scroll pilot (#47)

In the Video tab the existing `FlightPlannerVideoOptions` user control owns
vertical `AutoScroll`; its flow layout groups are not separate viewports.
This change is scoped to that control. Empty areas of the viewport and the
explicitly listed flow/group/table backgrounds are eligible; the status/start
strip, buttons, disabled controls, dropdowns, text input and its native child
handles are not. The hook takes only a primary Windows touch that starts in an
empty eligible client area and only while a vertical scrollbar is needed.
It waits for a vertical drag threshold, clamps both ends, preserves horizontal
position, and cancels the motion on tab hide or pointer capture loss. It does
not install a mouse filter or alter wheel, scrollbar, focus, input, or keyboard
handlers. The selected background has no tap action to preserve.

The adapter uses Windows 8+ pointer messages and implicit touch capture:
[WM_POINTERDOWN](https://learn.microsoft.com/en-us/windows/win32/inputmsg/wm-pointerdown)
and [WM_POINTERCAPTURECHANGED](https://learn.microsoft.com/en-us/windows/win32/inputmsg/wm-pointercapturechanged).
A safely owned interaction consumes its full pointer sequence, but unrelated
messages pass through normally. No global touch registration or Windows-only
hook is made on Mono/Linux.

## Targeted checks

```sh
xvfb-run -a -s '-screen 0 1024x768x24 -dpi 120 -extension GLX' \
  python MissionPlannerTests/SidebarTouchScroll/run-isolated.py
```

49 assertions passed at each of 96, 120, 144 and 192 display DPI on Mono.
The fixture compiles the real helper and uses a short overflowing WinForms
viewport with separate button, disabled button, dropdown and text input.
It covers the vertical direction and threshold, stationary/horizontal motions,
reverse at the scroll limits, hidden/non-overflowing tabs, retained horizontal
scroll and neighboring keyboard focus. It neither loads Flight Data nor
starts a stream; it does not execute the Windows pointer adapter.

## Native gate for this pilot

Use a separate Windows test session (no vehicle, no capture/stream), initially
at native 100% and 125%, then 150% if available. In a reduced-height Video tab,
inject or make upward/downward paths over empty space beside rows, reach both
end stops, reverse, and switch away mid-path. In the same fixture verify a
gesture starting on a synthetic input doesn't touch the scroll state and
normal clicking, wheel, scrollbar and keyboard still work. Include Start/Stop
and Record regions in hit-test checks without invoking their actions. When
content fits the viewport, verify a background swipe does not scroll. Log
Windows touch injection and physical touchscreen results separately.

This pilot does not add hooks to Quick, Actions, Messages, Params, Tuning,
Inspector, Preflight, the tab strip, map or HUD. The first groups have
different child/viewports (quick table, Actions panel, MessagesList, raw
parameter grid, three tuning controls and inspector); each needs its own
overflow/hit-target review instead of forwarding a gesture over the whole
window. No full build, native injection, physical touch, stream, or hardware
was run for this slice.
