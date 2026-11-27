# Parameter description validation, 2026-11-16

Follow-up, 2026-11-17: `Present()` now preserves the complete English first, followed by the Chinese explanation or review/missing/conflict notice. The exact-release firmware-server metadata endpoint is tried after the versioned autotest endpoint; the fallback must identify the same release and build in its document. Cache pointers and source tooltips retain the endpoint actually selected. The follow-up core run passed **374 assertions** against all six complete original XML files. Targeted tests cover index gaps, unproven fallback rejection, mismatched primary builds, cached fallback identity, duplicate attribute order and presentation order. Framework partial/probe compilation also passed again. The native Windows probe was updated to check the order; its runtime results remain pending.

- Core: **361 assertions passed** on .NET 8.0, including byte-digest checks of all six complete downloaded official XML files and record-by-record comparison with the 36 bundled records. Default offline execution omits 42 full-source comparisons; it uses the same committed test program and embedded catalog.
- Catalog/CI: **38 Python tests passed**, including three new catalog tests. Source changes to English, range or units cannot regenerate approved notes automatically.
- Existing localization: **2267 UI strings, 96 enhanced Chinese resource files passed**; all original Chinese baseline hashes, placeholders and links are preserved.
- The new core and UI partial compiled against **.NET Framework 4.7.2** reference assemblies, with and without `LIB`. The UI partial's application host was replaced by the explicit `UiCompilationStubs.cs.txt`; this verifies API/type compatibility of those parts, not a complete application build. The Windows reflection probe also compiled as a .NET Framework executable.
- Diff checks passed with CR treated as the existing line ending. No original `.zh-Hans.resx` resource, workflow, status view, planner view, control/write algorithm, or numeric parameter key was edited.

The Linux workspace has no Windows UI runtime. Full Windows application compilation, the native parameter-table probe, actual system DPI/themes and connected SITL inspection remain pending. Font-scale coverage declared by `ParameterTableProbe.cs.txt` is a test to run, not a claimed Windows result. The probe uses synthetic values and does not activate connections or write parameters.

The cloud .NET SDK CLI's first-use migration mutex was unavailable in this sandbox. The core test executable was compiled with SDK 8.0.428's Roslyn C# compiler, language version 7.3, SDK .NET 8 reference assemblies, and the two resources named in `ParameterDescriptions.csproj`, then run with the .NET 8 host. It executed `Program.cs.txt --source-directory <six-original-XML-directory>` successfully. `dotnet run --project MissionPlannerTests/ParameterDescriptions/ParameterDescriptions.csproj -c Release` is the standard reproduction on Windows or an unrestricted SDK host; no special test dependency is required.

The Framework compatibility compile used the same core and UI partial, `UiCompilationStubs.cs.txt`, and the `mscorlib`, `System`, `System.Core`, `System.Drawing`, `System.Windows.Forms`, `System.Xml`, `System.Xml.Linq`, and `System.Net.Http` reference assemblies from Microsoft's net472 reference package. Only the stub's unused field warning was emitted. This does not substitute for compiling the actual application host.

Version-source evidence: direct HTTPS read of the official Copter stable metadata release tag returned `Copter-4.7.2`. Reads of the Copter/Plane `stable-4.7.3/__METADATA__/apm.pdef.xml` endpoints returned 404. The tests verify that a 4.7.2-tagged response cannot be cached or translated as 4.7.3. They do not establish which file an already running installed ground station loaded; its firmware identity, logs, directories and loaded metadata still need to be inspected.

## 2026-11-27: exact Sub 4.7.1 statistics subset (#21)

- Downloaded the seven versioned official XML files and verified their complete
  SHA-256 digests. For Sub 4.7.1, the XML provenance comment's commit
  `dbe792162d06cab66c3475fd5556bf7a120f119e` also matches the official Git tag.
- The targeted, unmodified-runtime C# core and `Program.cs.txt` compiled on
  Mono 6.8 with both catalog resources embedded. Running with
  `--source-directory` passed **3,365 assertions**, including exact source,
  attributes and field fingerprints, family/build/version isolation,
  unchanged English/units, and Chinese search. Seven focused Python checks
  and the generator's `--check` also passed.
- This is a display/catalog-only change covering four officially marked
  read-only statistics. It does not add `STAT_RESET`, infer flight-time
  handling in Sub, or change what can be edited. No full build, GUI probe,
  SITL, connected device or parameter write was performed.
