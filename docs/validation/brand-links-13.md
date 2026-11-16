# Brand links — Issue #13

Base: `b5bca65edef67e73cedc5ef0b7a8343e088b6a68` (`fix/10-14-integrated-ui`).
Scope: the desktop toolbar brand entry, source asset, and focused UI checks.

## Original artwork

`Resources/LocalizedBrandLogo.jpg` is kept byte-for-byte as the supplied source
(original filename: `bluearchive.jfif`). Dimensions: 596 × 182 RGB.
SHA256: `db432ec916099133ef4cda90466df219cdc6230291c7d45f73073c74db521209`.

The application embeds `Resources/LocalizedBrandLogo.png`, a 596 × 182 RGBA image.
SHA256: `89dec6eca397638b20fd2bcb9b6bebdf6043cc8661220f365314c232d0b527c5`.

`python scripts/branding/remove_logo_background.py` reproduces the cutout with
Pillow. It checks the JPEG hash first. Dark/blue interiors are copied exactly;
white and neutral near-white JPEG ringing are removed, including enclosed letter
counters. Antialias boundary colors are unmixed from white using nearby interior
pixels. No drawing, resizing, shape generation, or generative image tool is used.
The source's blue and black colors, text, orbital lines and proportions are retained.
There are 79,059 fully transparent pixels, 10,661 antialias pixels and 18,752 fully
opaque pixels. Every fully opaque pixel retains the original JPEG RGB value exactly;
white-matte removal changes only transparency and the edge unmixing. Recomposition
over white differs from the JPEG by mean absolute RGB errors of 0.92/0.58/0.81 on
the 0–255 scale. This does not claim unchanged RGB values in the antialias boundary.

## Layout findings and correction

The previous implementation created two separate right-aligned ToolStripButtons,
sized from a button's transient Height, and forced both to WhiteSmoke on navigation
refresh. Removing the JPEG background alone could not remove that opaque surface.

The actual main toolbar derives from MenuStrip. Its default CanOverflow=false lets
left-side hosted controls and right-side items occupy overlapping bounds when the
combined widths exceed the available window. A WinForms fixture using the source
navigation, connection and vehicle panel dimensions reproduces brand overlap in a
narrow connected layout. The earlier tests used a plain ToolStrip without those
neighbours and therefore did not exercise this layout.

The replacement is one ToolStripControlHost containing the complete image and two
native accessible link buttons. It sits at the far right. Native menu overflow is
enabled; connection controls use Overflow=Never, and branding moves as one item when
space is insufficient. Image dimensions follow the current toolbar height after
layout changes. Painting always samples the full-resolution PNG, with one shared
transform for both hit regions. Navigation refresh restores a transparent surface.
Background painting copies only the toolbar background so transparent controls do
not repaint neighbouring menu text. Child mouse-leave events reach the toolbar host;
MainV2 applies its existing hide handler only while auto-hide is enabled.

## Focused validation

Run the isolated real WinForms suite without compiling the flight libraries:

```powershell
dotnet test MissionPlannerTests/Branding/Branding.csproj --configuration Release
```

It covers 100/125/150/200% equivalent control dimensions at physical window widths
847, 1231 and 1920; connected/disconnected neighbours; placement and complete
hit regions; connection visibility; PNG alpha and selected original core colors;
light/dark tiled backgrounds and navigation refresh; accessibility default actions;
keyboard Tab/Shift+Tab, arrows, Enter and Windows native Space dispatch; child
mouse-leave routing; live toolbar resizing. Both the ToolStripItem bounds and the
actual hosted connection/brand control bounds are checked for overlap. Hit testing
is performed after an overflowed logo is made visible by opening the overflow menu.
The scaling fixtures are not a per-monitor DPI transition test.

The scoped `Native Windows brand link checks` workflow runs this suite and retains
TRX results plus toolbar screenshots as CI artifacts. It does not build or publish
the application, create a release, or change existing release assets.

## Actual-window paint regression (2026-11-16)

Full-application GPD verification of `3e221779` **failed**: both accessible links
existed, but the toolbar image was invisible. Old parameter-checkbox pixels and,
after restoring to 1420 × 790, map wind-circle pixels remained in the logo region.
The earlier 18/18 result below verifies layout, accessibility and print rendering;
it does **not** establish successful on-screen painting.

The child BrandLinkButton inherited ButtonBase's Opaque style. Its OnPaint drew
only a focus rectangle, while the artwork and background were placed in
OnPaintBackground. Normal WM_PAINT skips that background method for an Opaque
control. The child HWNDs also clip the UserControl parent's paint, leaving no
complete painter for their visible pixels. Depending on the previous backing
surface, the result was blank or stale page contents. This is separate from the
previous split-item layout problem; correct bounds and accessible names cannot
prove visible pixels. See Microsoft's [Opaque style definition](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.controlstyles?view=netframework-4.8.1)
and [ButtonBase defaults](https://github.com/dotnet/winforms/blob/main/src/System.Windows.Forms/System/Windows/Forms/Controls/Buttons/ButtonBase.cs).
The native regression also reads Opaque=True from the actual net472 Windows controls.

Regression-only commit `7dccccbadb06007c10499e1e63f0b557849da901` runs the new tests
against the unchanged failing implementation. [Native run 43904532484](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/actions/runs/43904532484)
reproduced **18 passed / 6 failed**. The two WM_PAINT tests matched zero logo pixels
in both halves. All four actual-desktop tests detected old magenta page pixels
(1,202 stale pixels) after revealing the toolbar. Thus the new assertions fail on
the previously accepted code, instead of merely checking the replacement logic.

Fix `efcce3dba6d7b0d73160f96e3969e5932fa558bf` makes each child button paint its
complete backdrop and original artwork in OnPaint. The child background callback
does no separate painting. Explicit opaque/double-buffer/resize styles make the painter
responsible for every pixel on every WM_PAINT, including transparent PNG areas.
The backdrop resolves an opaque toolbar/ancestor color before compositing the
original PNG. Source artwork, the atomic layout and accessible link actions remain
the same.

The added visual regression sends WM_PAINT into a seeded bitmap DC **and** uses
CopyFromScreen to inspect actual desktop pixels. A positive-control probe first
verifies that screen capture sees the previous page. The fixture uses a double-
buffered form, a fill-docked menu inside a toolbar panel, and previous page controls
with a parameter checkbox and wind-circle surrogate. It tests docked and floating
headers, light/dark backgrounds and toolbar heights 58/87/116. Each desktop case
checks reveal, maximize, restore, page/theme change and hide/show. Pixel assertions
require the original artwork in both halves, no stale magenta, mean RGB error at
most 2/255 and at most 1% materially mismatched pixels against the PNG-composited
reference. These are actual-window fixture checks, still not full-application GPD
acceptance or a per-monitor DPI transition test.

## Corrected paint result (2026-11-16)

Runtime fix: `efcce3dba6d7b0d73160f96e3969e5932fa558bf`.
Final native test source: `b27ed9cb7bd83df1e998240652aaa0b805fca746`.
[Native run 43905914653](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/actions/runs/43905914653)
passed **24/24, zero skipped**, including the six added visual cases. The TRX records
22 pixel comparisons: two native WM_PAINT bitmap targets and 20 real desktop states.
All 22 had zero materially mismatched pixels, zero stale-magenta pixels and 100%
foreground matches in both image halves. Maximum mean RGB error was 0.01483/255.

The [verified artifact](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/actions/runs/43905914653/artifacts/14246366379)
contains TRX plus 71 PNGs (the 29 historical print renderings and 42 actual-paint
captures). ZIP size: 797,054 bytes; SHA256
`e47c4146c7c04f79ce255a49f63b5a8222ab7f0742d38d306a4879992e8d8684`.
The downloaded ZIP hash, 24/24 counters and all 22 pixel metrics were checked.

The live capture harness raises its own window immediately before capture and
checks native window ownership at five points before and after copying pixels.
An intermediate run correctly rejected screenshots partly covered by a hosted
runner console. This occlusion check prevents another window from becoming either
a false visual failure or false evidence of correct application pixels.
The observed CI desktop was 1024 pixels wide, so the restore test used a 944-pixel
client width; the 1420 × 790 request is capped by available desktop size. Exact GPD
1420 × 790 and maximized full-application validation therefore remain separate.

Native WM_PAINT before the correction (the unchanged failing painter):

![Failing native WM_PAINT](brand-links-wm-paint-before.png)

Native WM_PAINT after the correction:

![Corrected native WM_PAINT](brand-links-wm-paint-after.png)

Actual on-screen toolbar crops after reveal and after restore:

![Live light toolbar after reveal](brand-links-live-revealed-light.png)

![Live dark toolbar after restore](brand-links-live-restored-dark.png)

[Maximized actual-desktop fixture capture](brand-links-live-maximized.png) shows
the intentionally magenta previous page below the correctly composited toolbar.
These fixture images are not GPD screenshots.

The current Mono run is **15 passed / 0 failed / 9 skipped**; native desktop paint,
Windows accessibility and native input cases are Windows-only. Localization checks
(2,710 dictionary entries, 96 enhanced resources), all 59 offline CI regressions,
actionlint and CRLF-aware diff checks also passed after the correction. Both the
source JPEG and transparent PNG hashes are unchanged from the artwork section.
The runtime change touches only BrandLogoLinks.cs; HUD, connection and flight
logic were not edited. No release, tag, merge or force push was performed.

**GPD full-application revalidation is still pending.** The earlier 18/18 and the
new 24/24 fixture results do not override a failing full-application capture.

## Historical print/layout result (superseded for visual acceptance)

Runtime and UI test source: `90077de97b33c4d0f756f6edff677dc28ee6ff17`.
Later validation-document and parent-test-project exclusion changes do not alter
those runtime or UI test source files.

- Native Windows Server 2022, .NET Framework target net472: **18 passed, 0 failed,
  0 skipped**. [Exact CI run](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/actions/runs/43899786531).
- [TRX and 29 fixture screenshots](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/actions/runs/43899786531/artifacts/14241027117):
  artifact `native-brand-links`, 195,164 bytes, SHA256
  `44baa3592dfeaf4d81645da9051de2ed0f6885babbce9364f47306b198ce9772`.
  The downloaded ZIP hash and TRX counters were independently checked.
- Mono 6.12 / Xvfb, the same UI assertions: **15 passed, 0 failed, 3 skipped**
  (Windows accessibility bridge, native Space and native mouse dispatch).
- Localization verification: 2,710 dictionary entries and 96 enhanced Chinese
  resources passed; original Chinese resources, placeholders and links are intact.
- Offline CI regression suite: **59 passed**. Release-policy suite: **42 passed**.
- actionlint 1.7.12: the scoped workflow passed. CRLF-aware diff check passed;
  original CRLF source/project files were preserved.

The following are **off-screen DrawToBitmap/WM_PRINT fixture renderings**. They
are not actual-window screenshots and did not detect the missing WM_PAINT painter.
They must not be used as evidence that the GPD failure passed.

![Historical off-screen dark rendering](brand-links-dark.png)

![Historical off-screen light rendering](brand-links-light.png)

Full-application GPD verification, physical monitor DPI transitions and
flight/telemetry testing are outside these isolated checks and must not be inferred
from them. The original black lettering remains black in the transparent asset;
these tests do not claim a recolored high-contrast variant. Issue #13 remains open
for the separately coordinated full-application verification. No release, tag,
stable/development promotion or force push was performed.
