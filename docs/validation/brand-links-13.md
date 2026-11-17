# Brand links — Issue #13

Base: `b5bca65edef67e73cedc5ef0b7a8343e088b6a68` (`fix/10-14-integrated-ui`).
Scope: the desktop toolbar brand entry, source asset, and focused UI checks.

## Current follow-up: focus, hover and press without a center line

Remote source inspection confirms that `178463cf` still loads the original JPEG,
sets WhiteSmoke and creates two independent ToolStripButtons. It does not contain
this branch's transparent, atomic host. That candidate cannot validate the
current logo renderer; a complete application build must include the logo source
as well as the other intended UI changes.

The renderer at `561f64a2` already paints the PNG continuously across two accessible
hit regions, but its `Focused || (ShowFocusCues && IsDefault)` condition paints a
rectangle around a half even when Windows hides keyboard cues. The rectangle's
inside edge crosses the artwork close to the midpoint. Source
`7ef134a91bdc85fd7cc4bbcebee4db3404e4ed04` requires both actual focus and
ShowFocusCues, and confines the visible keyboard cue to the empty bottom padding
under the active half. Mouse focus, hover and press retain the complete image.
The two native accessible links, keyboard traversal, tooltips and URL destinations
are retained. The original image bytes, geometry and shared painting transform
are unchanged.

The new native test exercises each half in five states: hidden mouse-focus cues,
native mouse movement, button-down before button-up, visible keyboard focus and
cleared cues. It verifies effective WM_QUERYUISTATE and WinForms ShowFocusCues,
and confirms that native mouse events were dispatched. In all states it compares
every pixel against one full-image reference, allowing focus pixels only in the
active half's bottom padding. The center columns receive the same strict
comparison as the rest of the artwork. Keyboard cues must contain visible pixels;
removing focus indication does not pass. The native fixture also includes TCP,
baud, the second connection row and its statistics link for occlusion checks.

[Native run 43990215576](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/actions/runs/43990215576)
passed **31/31, zero skipped**, on exact source `7ef134a9`. All 30 interaction
captures (three sizes/backgrounds x two halves x five states) had zero unexpected
pixels. Visible cues had 51/84/115 changed pixels for heights 58/87/116; the other
states had none. Each state uses the same-frame native capture and HWND occlusion
checks described below. The existing narrow-window, connection, accessibility,
keyboard, repaint and contrast cases also passed.

The [verified artifact](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/actions/runs/43990215576/artifacts/14288080712)
is 3,604,425 bytes, SHA256
`2ad33f054fae76867e6e3189d84c7a40a749123e748b13daf103e57446a4ab96`.
Its downloaded ZIP hash, TRX counters and all 30 interaction metrics were checked
independently. It contains 293 PNGs, 68 frame timelines and ten probe reports.
The CI desktop was **96 DPI**; sized-toolbar cases are not proof of a real
144-DPI desktop result. Local Mono checks passed 15 and skipped 16 Windows-only
cases; actionlint and diff checks passed.

Native fixture crops, containing only the public logo and synthetic controls:

![Keyboard focus below ArduPilot](brand-links-interaction-left-focus.png)

![Keyboard focus below GitHub](brand-links-interaction-right-focus.png)

![Pressed link on the dark toolbar](brand-links-interaction-dark-pressed.png)

[Complete synthetic TCP toolbar and focused logo](brand-links-interaction-window.png).
These are original desktop captures from the test artifact. They contain no
actual vehicle, address, map or user desktop data and may be linked as **fixture
evidence**, not as a GPD application screenshot.

**Full-application acceptance remains pending.** Integrate the exact logo source
with the intended HUD fixes in a separate candidate, preserving both branches,
then build and observe it on GPD. Check both halves at rest, after mouse clicks,
during hover/press, with Tab/arrow focus, on dark/light backgrounds, narrow windows,
maximize/restore and the real display scale. TCP/baud/sysid controls must stay
reachable without covering the logo. Include version provenance with screenshots
and redact actual addresses, telemetry, maps and unrelated desktop content before
posting to Issue #13. The earlier 144-DPI seed discrepancy below also remains
unconfirmed until its exact updated-source rerun. No integrated candidate or GPD
result is claimed by this fixture pass; Issue #13 stays open.

## Previous follow-up: one desktop frame for pixels and screenshots

GPD tested `febb6e9dbb82848fb18ef874b576840bbcc116ac`: Release built,
21/27 checks passed and six stopped at the strict previous-page seed assertion.
The reported physical probe `(721,47)` was #F3F3F3 with native window DPI 144
and managed DeviceDpi 96; a subsequently saved client screenshot was magenta.
Those two observations were separate desktop reads, so they cannot establish
that the failed assertion and screenshot represented the same presentation.
The DPI disagreement alone also does not prove a wrong capture coordinate.
**The GPD cause and its 144-DPI recheck remain open.**

Test source `f2e899451672757d33105307b8a91f2a0a668991` closes that evidence gap:

- `BrandLogoDesktopFrame` captures one physical client rectangle with native
  GetDC/BitBlt into an owned bitmap. Assertions, logo crops, probe neighbourhoods
  and saved PNGs read that same buffer, using `physicalPoint - frame.Bounds.Location`.
  They do not take another desktop screenshot. Frame bounds, UTC and a SHA256
  of normalized RGB32 pixel bytes identify the asserted image.
- Capture still establishes PMv2 before creating fixture HWNDs. It checks the
  screen DC's MM_TEXT mapping, zero origins and identity LPtoDP at both corners;
  it logs logical/desktop resolutions and DC DPI rather than applying an assumed
  144/96 scale factor. PNG DPI metadata never rescales pixel coordinates.
- Pump synchronously invalidates, erases and updates the form's visible children
  with [RedrawWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-redrawwindow)
  and flushes the calling thread's GDI batch before the DWM fence. DwmFlush alone
  is insufficient evidence that all child WM_PAINT messages have completed.
- Capture records successive frames and accepts two consecutive identical
  pixel hashes and bounds, bounded to two seconds. Stability never depends on
  seeing the expected colour: two stable gray frames still fail exact magenta.
  The first frame, settled frame and timestamp/hash timeline are retained.
- The original physical client-y=2 magenta assertion remains. Additional checks
  require a new page Paint event and an exact hit on the exposed page HWND, as
  well as the existing client bounds, fixture ownership and logo occlusion checks.
  Logs include Paint Graphics DPI/transform, HWND awareness/PMv2 identity,
  WM_NCHITTEST, and diagnostic native GetPixel reads before/after BitBlt.

The native regression `CapturedFrameKeepsTheSamePhysicalPixelAfterTheDesktopChanges`
captures a red child patch at a nonzero client offset, repaints it blue, and
verifies that the first buffer and its PNG round trip still read red at the same
relative pixel. This exercises an actual desktop change and the capture offset.

### Verified result and its limit

[Native run 43936580098](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/actions/runs/43936580098)
checked the exact test source above: **28/28 passed, zero skipped**, on the
Windows runner's 96-DPI desktop. The artifact was downloaded and independently
verified: [TRX, PNGs and diagnostics](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/actions/runs/43936580098/artifacts/14260965729),
1,638,616-byte ZIP, SHA256
`ecc093f75c75e5d4d5799d58d5df1139c8b72740c7a541a62d7dc031a3a8322d`.
It contains 161 PNGs, 35 capture timelines, seven probe reports and the TRX.
All 35 captures settled on two identical frames; the latest second frame was
253 ms after capture started. All seven saved probe PNGs independently read
#FF00FF at their recorded relative coordinate `(496,2)`.

For the linked example, physical client origin `(8,31)` plus relative probe
`(496,2)` gives the asserted desktop coordinate `(504,33)`. Its PNG dimensions
are exactly 992 x 648, the native client size. DC mapping is identity; both
native GetPixel diagnostics agree with the immutable frame. Page Paint count
grew from 2 to 4 and the hit HWND is the page. These are **96-DPI CI observations**,
not a substitute for the GPD `(721,47)` observation at 144 DPI.

[Same asserted client PNG](brand-links-same-frame-probe.png),
[same-buffer neighbourhood](brand-links-same-frame-neighbourhood.png),
[probe report](brand-links-same-frame-probe.txt),
[capture timeline](brand-links-same-frame-frames.txt).
Across 25 actual-paint comparisons, stale-magenta and wrong-pixel counts were
zero and maximum mean RGB error was 0.01483/255. Local Mono verification passed
15 portable cases and skipped 13 Windows-only cases; actionlint and diff checks
passed. Neither original image nor the application renderer changed in this
follow-up.

### GPD recheck still required

Keep the actual display scale and run all 28 cases on source `f2e89945` or its
documentation-only successor. Retain all output, including failed seed cases:

```powershell
$env:BRAND_LOGO_SCREENSHOTS = Join-Path $PWD 'local-qa-logo-same-frame'
dotnet test MissionPlannerTests/Branding/Branding.csproj --configuration Release --logger 'trx;LogFileName=brand-links-gpd.trx' --results-directory "$env:BRAND_LOGO_SCREENSHOTS/results"
```

For any failure, compare `actual-paint/*-probe.txt`, `*-probe-frames.txt`,
`*-probe-first-client.png`, `*-probe-client.png`, `*-probe-window.png` and
`*-probe-neighbourhood.png`. Use the recorded relative coordinate directly in
the PNG. A stable gray pixel with page hit/paint evidence requires investigation
of the actual exposed surface; a caption or another HWND hit points to geometry
or occlusion. A changing frame timeline identifies presentation timing. Do not
infer the outcome from a later, separately captured screenshot or relax the
magenta assertion. Full-application visual acceptance remains a separate GPD
check coordinated by the other work line; Issue #13 stays open.

## Previous follow-up: contrast, spacing and physical capture coordinates

GPD full-application checks of `e99a4ca1` reported a visible transparent logo,
correct ArduPilot/GitHub browser destinations and no old-page residue after
maximize/restore. Those observations resolve the missing-painter symptom. Overall
visual acceptance remains open: the original black text had low contrast on the
dark toolbar and the logo had insufficient space beside Connect and the edge.
The four GPD desktop pixel cases also stopped at the previous-page probe
(expected magenta, actual #F3F3F3); they did not reach a logo-pixel assertion and
are not evidence of either a passing or failing logo image at that stage.

### Display treatment, original artwork retained

Runtime `8fc88eee7ac9fc54f874511352e259c41f33e8cf` retains both image files and draws
the original RGBA foreground last. Only a low-contrast backdrop activates a thin
neutral contour **behind the original dark ink**, derived from its alpha. The
source text, blue artwork and original opaque foreground RGB values are not
recolored or redrawn. This is a display contrast treatment, not an edited logo
asset or a rectangular light backing. The contour is limited to one paint pixel,
neutral RGB 190 with source-derived alpha. Light-background rendering uses only
the original PNG. The backdrop decision samples the actual composited toolbar
background, including its tiled image, rather than trusting BackColor alone.

The host now reserves horizontal gaps proportional to the live toolbar height:
8/12/16 pixels at heights 58/87/116. The image dimensions and the two click regions
retain their original proportions; native layout reserves the gaps alongside
connection items and the window edge. Narrow-window atomic overflow remains active.

### Strict probe retained; coordinate/DPI diagnosis added

The old screen tests mixed managed screen coordinates with a physical desktop
copy without explicitly setting the capture thread's DPI context. At display
scales above 100%, a logical client-y=2 coordinate can refer to another physical
pixel, including non-client chrome. The reported #F3F3F3 alone cannot establish
which coordinate or window was sampled. This is a coordinate-virtualization risk
in the old harness, not a confirmed diagnosis of that GPD sample without its
native bounds and DPI log. See Microsoft's [mixed-mode DPI API guidance](https://learn.microsoft.com/en-us/windows/win32/hidpi/high-dpi-improvements-for-desktop-applications)
and [ClientToScreen device-coordinate contract](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-clienttoscreen).

Test source `e3511f31278cca841302d101baf385e6ad9f987b` establishes per-monitor-v2
awareness **before creating any fixture windows**, scoped to the test thread and
restored afterward. Native GetClientRect/ClientToScreen and GetMonitorInfo define
the physical capture regions. This does not change the application's DPI policy,
the process default or the user's display scale.

All four live tests still assert exact magenta at physical client y=2. They also
assert that the hidden toolbar released the top client area, the previous page
fills the native client width, and the sampled root HWND belongs to the fixture.
No failing Windows case was skipped or converted into an inconclusive result.
Before the assertion they save a window capture and a diagnostic text file with:

- Physical and managed client/window rectangles and origins for form, page,
  header, menu and logo.
- Window DPI, managed DeviceDpi, capture-thread awareness and native/managed
  monitor rectangles.
- Physical probe coordinates, sampled RGB, owning HWND/root and native class.
- The old unaware-context probe coordinate and the RGB/owner found if that
  coordinate is used as a physical pixel.

The existing five-point before/after occlusion checks remain in place for the
logo itself. GPD must rerun the four cases to confirm the corrected mapping on its
actual scaled display; a 96-DPI CI pass does not substitute for that confirmation.

### Current verified native result

[Native run 43916310337](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/actions/runs/43916310337)
on runtime/test source `8fc88eee7ac9fc54f874511352e259c41f33e8cf` passed **27/27,
zero skipped**. This includes all four live cases with their strict preconditions,
20 reveal/resize/page/hide-show states and three additional dark-ink checks. Across
25 actual-paint comparisons there were zero stale-magenta pixels and maximum mean
RGB error 0.01483/255 against the original-PNG-plus-contour reference.

At toolbar heights 58/87/116, the independent contrast checks verified respectively
233/690/1,374 unchanged opaque dark-ink pixels, plus 101/133/179 visible neutral
contour pixels in the right half with contrast at least 4.5 relative to the
representative original dark ink. This is a rendering check, not a claim of full
WCAG conformance. Transparent top/bottom padding stayed at the actual toolbar
background color. Layout checks verified both reserved gaps and connection bounds.

The CI display was 96 DPI. The capture thread logged awareness 0 before the scope
and 2 inside it; native/managed client origins agreed, and every physical probe
read exact #FF00FF with a fixture-owned HWND. The old/unaware sample also read
#FF00FF at 96 DPI. Therefore this CI does **not** establish the cause or resolution
of the earlier GPD #F3F3F3 sample at its actual scale; the new logs are intended to
make that recheck decisive.

[Verified TRX, screenshots and diagnostics](https://github.com/reliable-ly0411/TitanPlanner-zh-CN/actions/runs/43916310337/artifacts/14250621020):
1,022,360-byte ZIP, SHA256
`e27c5ead9d2f6cfcd369be1acc969c4ed12198e6b04512d2843b8cbc1a48ea2d`.
The ZIP hash, 27/27 counters and 25 pixel-comparison outputs were independently
checked. The artifact contains 84 PNGs, seven probe-diagnostic text files and TRX.

Actual desktop crops of the current treatment:

![Current dark ink with thin neutral support](brand-links-contrast-dark.png)

![Current original foreground on a light toolbar](brand-links-spacing-light.png)

[Restored actual-desktop fixture with the reserved gaps](brand-links-contrast-window.png).
These are CI fixture captures, not GPD full-application acceptance images.

On GPD, keep all paint cases and retain their logs:

```powershell
$env:BRAND_LOGO_SCREENSHOTS = Join-Path $PWD 'local-qa-logo-next'
dotnet test MissionPlannerTests/Branding/Branding.csproj --configuration Release --filter 'FullyQualifiedName~BrandLogoPaintTests' --logger 'trx;LogFileName=brand-paint-gpd.trx' --results-directory "$env:BRAND_LOGO_SCREENSHOTS/results"
```

If a top-y=2 precondition fails again, inspect the corresponding
`actual-paint/*-probe.txt`, its `*-probe-window.png` and the TRX output before
attributing it to logo painting. The native client rectangles, window DPI,
physical/legacy probe points, sampled RGB and HWND ownership are all retained.

Both original image hashes above are unchanged. The lightweight regression suite
(59 tests), localization verification (2,710 dictionary entries and 96 resources),
actionlint and diff checks passed. **GPD dark-theme appearance, spacing and the
four scaled-display probe cases still require revalidation on this source.**
Issue #13 remains open. No release, promotion, force push or HUD change was made.

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
