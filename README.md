# TitanPlanner UI verification figures

All images are actual WinForms control renders in an isolated Windows test session, at genuine 120 DPI. They are only control crops with synthetic data, not desktop or actual mission/profile screenshots. No real user settings, slot/path names, map, devices or telemetry appear. PNG files contain only IHDR/sRGB/gAMA/pHYs/IDAT/IEND; no text, EXIF, timestamps or identifiable metadata. Original and final SHA identical.

Scope labels:
- #18: native DataGridView source-derived **fixture**, not full application before/after. PLAN constructor hunk: before 9bed38ab0d724155521c16edd192e8cd904cbcdd; after 90b05585b11378777f50a474ea943f92e0056809. Raw parameters: before 146f282d262d3454fb1cb081aa462e83426e0507; after f47f7bbb853c8b0ec2cee0def41ef0c6b9aa4a57. Synthetic rows and options; fixed narrow raw column still clips the synthetic parameter identifier.
- #26: exact packaged 706cd0c050f807195d908d0aaa40a0bf3de6c7c6 ConfigPlanner controls in an isolated host with no Activate; enhanced zh-CN; earliest and final shortcut rows cropped from 933×558 client. First crop may include a sliver of adjacent row. No shortcut executed.
- #40/#42: dfbe3531 source-only isolated chooser, enhanced zh-CN, 573 client width (fixture min outer width 591), large synthetic 22 pt font; sample pressure 1009.25 is synthetic. Not a packaged app screenshot.

| Image | Host/client width | PNG size | Source/served SHA256 (same) |
| --- | ---: | ---: | --- |
| `issue-18/params-1230-after.png` | 1230 | 1230×156 | `c92b616e557901167c9be0e5100b9a109e6dcd1fb694249b1d8b6dc16079258f` |
| `issue-18/params-1230-before.png` | 1230 | 1230×81 | `4219c060c53c1a03dbb88be7269e271f79222e54571421c3f12db51eba1773e9` |
| `issue-18/params-850-after.png` | 850 | 850×156 | `c23e6ca6dbf783704ad22f26b58ec8cf6a164f9d1ce065fc5d09422c20373ec8` |
| `issue-18/params-850-before.png` | 850 | 850×81 | `ab84fde6388c39edf48cb780845b5d6ccc0840aa26cb17f11f2c166d8e09fe4c` |
| `issue-18/plan-1230-after.png` | 1230 | 1230×107 | `dd35e060dcbfafdcddf0824fc7d7fad981006e4d27b23cbd084f40c9aefa433a` |
| `issue-18/plan-1230-before.png` | 1230 | 1230×78 | `800a5eb52ac3a60fe1a0e02a2c4d593bb6902bee1999a954ed0197c1180cb390` |
| `issue-18/plan-850-after.png` | 850 | 850×107 | `02bc2109bd7e01f058e44c3f9890e1699b45c5e9e90b4b7e5a146cef2b9d883e` |
| `issue-18/plan-850-before.png` | 850 | 850×78 | `36ffe7efde4f5ac3a39fc3367e9cfafd1efa66a9b27375e9fd955702272c7897` |
| `issue-26/shortcut-first.png` | 933 | 385×59 | `5e476f72c52583053cd2b289be4f48cca2ff33da12b11729e1dcf4edcf16aa47` |
| `issue-26/shortcut-last.png` | 933 | 329×59 | `ba3f903f8aba821e2ab42475abd2d451e86c5c9cf15d883eab927b26d3bd2b53` |
| `issues-40-42/qnh-573.png` | 573 | 573×312 | `c2006ddeb540370ba457c2cf558f018b3d09a3e442ae5497a05bcab072f014b6` |

## Additional controls

These source-level captures use synthetic data; they are not full product or user desktop captures.

- **#34**, exact `5a8247c0`: Windows WinForms, actual 120 DPI. The 933/1230 px toolbar and 933 px overflow case use inert port, baud and Connect controls. The emblem is the existing repository logo. The full navigation and device paths are not used.
- **#36**, exact `6b37cbb7`: actual Windows 120 DPI, compact controls at 420 and 670 px. Synthetic preview and no system file picker. The 420 px image shows truncation in the closed startup-image selector; it is retained as a known issue, not a pass.
- **#40**, exact `dfbe3531`: actual Windows 120 DPI native options-list window, synthetic values. The earlier fixture incorrectly mapped all fields containing “airspeed” to “空速”. This legacy evidence image is withdrawn from the issue and PR; the text in the image does not represent the actual lowairspeed mapping.
- **#188**: actual Mono WinForms tooltip in isolated X11 (requested 120 DPI; logical width 558). Screenshot demonstrates the two lines, not Windows, the complete checkbox, or persistence.

| File | SHA-256 |
| --- | --- |
| `issue-188/tooltip-mono-x11.png` | `cb2f0e2553e1b106e6a9b10a93a08745a9d874535ef9ca8b5a572cbc48bba1fd` |
| `issue-34/brand34-min-933.png` | `245d3261552f9f7a808e617531b38c8a6621cc584d7de143cb1c3cff6c5a3183` |
| `issue-34/brand34-overflow-menu.png` | `9f2b524176e8529202aefedfaa571bd17f0dabff2312437d13c19b823872125d` |
| `issue-34/brand34-overflow-panel.png` | `27cfa4a57679726c27dc0d811d531467e855882e3a23ce20d1bd8bc7a8ca081a` |
| `issue-34/brand34-wide-1230.png` | `67bd60c0e7c2021442c61eeaa96a277e9fdc3df2389d5e3c96bdccd1ff565d70` |
| `issue-36/appearance36-420.png` | `57ee418c2e430aeb1cf024e22e7c0060918b96cd54c2af576e615ec9fa844c29` |
| `issue-36/appearance36-670.png` | `78c3d7bb07b743d50813a07dbfe5390be15dc1081b2d9aeed0c64b78695a8cef` |
| `issues-40-42/quickview-options-popup.png` | `1bd3d6b8d42808bf3c6b7d7538f0fd474a05a42c4b5a95e3864a18951d7203b1` |

## Opening image #22 / #225

Same synthetic 3840×480 input, genuine Windows GDI+ production renderer into a 660×400 bitmap tagged **144 DPI**. This is offscreen render DPI, not the computer's scaling. The old source already did not paint a background; the comparison isolates the change from zero to three visible outline credit lines on the same ultra-wide image. Before `ac9ebb4`; after `405a7bd`. No full application or vendor imagery.

## Startup-image dropdown #36

Windows WinForms, true 120 DPI, enhanced Chinese, the same 420 px isolated control. Before `6b37cbb7`, the real list width was 260 px; after `36cfa698`, the font measured the widest label at 269 px and the list grew to 300 px. The main selector and adjacent button did not grow. The two PNGs show the **actual dropdown list**, not a file picker, mock screen or tooltip capture.

## Corrected #40 Options evidence

True 120 DPI Windows list, 628×255, exact production QuickView options projected with the real source metadata. Synthetic rows and selected item. The two low-speed flags are verified against their full Chinese captions. Other rows do not assert full metadata coverage. This single photograph replaces the earlier incorrectly modeled fixture image.

## #188 checkbox and #190 layout outcome

Actual Windows 120 DPI, only isolated controls. #188 shows the checked background-list option from package `706cd0c` in a 933×558 host. Other controls intentionally have no activated device/user settings. #190 shows the final shortcut row after a 933→1230→933 resize using source `4129782f`; system text colour is set for the isolated image. The still image does not prove speed, real application activation, or persisted settings.

## #184 tile failure and #186 HUD translation

Windows offscreen GDI rendering using the same exact packaged commit `7dab547c` for both languages. The 256×256 tile uses an intentionally missing synthetic tile without network requests; there is no real map. The 466×354 HUD uses a synthetic waypoint (7, 123 m) and wind (3.5 m/s). en-US versus enhanced zh-CN is a **language** comparison, not a comparison of different software builds. No hardware or OpenGL capture.

## #38 PX4 Hold and #39 optical flow selection

Actual isolated Windows WinForms 120-DPI control crops with synthetic inputs, no hardware or transmitted command. #38 exact `2b8f5cc3` shows the same raw Loiter name for helicopter, fixed wing and generic fallback; the Chinese context differs. #39 exact `2f6cec47` shows the verified ID 158 label in Copter and Plane and raw-English fallback in the ArduSub test case; raw option ID and name unchanged. These figures are source-level, not a complete application or general firmware-coverage claim.

## Actual portable-app test in the Acer Windows VM

Original application-window captures, source package `706cd0c` (portable 1.3.83), captured in the interactive Windows console on 2026-11-27. The actual application window API reported 96 DPI, screen crops from the real 1280×768 session exclude the taskbar and desktop. Red ellipses indicate observations; file paths and coordinate fields are masked. No map imagery was fetched and no physical device was attached.

First-run map notice and the untouched test profile show the default map and Simplified Chinese 2. The cold Settings entry capture at 10.5 s shows the earlier package unresponsive; the window had recovered by the next observed frame at 62.1 s, not a measured total. #188 is a genuine lifecycle test: initial checked, clicked off, ordinary window close with no separate save, then still off in a different process. The photo set deliberately excludes a scroll diagnostic whose picture did not agree with the control bounds; #26 remains open.
