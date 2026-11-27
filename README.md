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
