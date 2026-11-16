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

## Results (2026-11-16)

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

These are exact native WinForms **fixture** screenshots, not full-application or
GPD captures. The fixture uses the toolbar/connection dimensions and placeholder
navigation/vehicle controls.

![Native dark toolbar fixture](brand-links-dark.png)

![Native light toolbar fixture](brand-links-light.png)

Full-application GPD verification, physical monitor DPI transitions and
flight/telemetry testing are outside these isolated checks and must not be inferred
from them. The original black lettering remains black in the transparent asset;
these tests do not claim a recolored high-contrast variant. Issue #13 remains open
for the separately coordinated full-application verification. No release, tag,
stable/development promotion or force push was performed.
