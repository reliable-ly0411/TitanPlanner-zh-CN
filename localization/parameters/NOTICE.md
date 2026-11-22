The English records in `official-first-batch.xml` are selected, unmodified metadata
field values generated from the ArduPilot source annotations. ArduPilot and its
contributors retain their copyrights. The source project is licensed under
GPL-3.0-or-later; see the repository's [COPYING.txt](../../COPYING.txt) and
[ArduPilot's license](https://github.com/ArduPilot/ardupilot/blob/master/COPYING.txt).

`sources.json` records each original download URL, complete source XML SHA-256,
release tag and source commit. XML formatting and the small bundle wrapper are
local packaging; the digest identifies the complete upstream file, not the subset.
The old 4.5.7 files identify their tag without a SHA; their tag commits were resolved
through the ArduPilot GitHub tag refs. No wiki translation or third-party parameter
pack is included.

The independent Chinese notes in
`../zh-Hans/parameter-translations.json` and its generated `zh-CN.xml` are also
distributed under GPL-3.0-or-later. They are explanatory translations of the pinned
metadata, not additional flight-control rules or evidence of flight validation.
