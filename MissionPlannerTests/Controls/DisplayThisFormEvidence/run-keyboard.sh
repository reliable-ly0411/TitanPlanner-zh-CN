#!/usr/bin/env bash
set -euo pipefail
root=$(git rev-parse --show-toplevel)
cd "$root"
src=MissionPlannerTests/Controls/DisplayThisFormEvidence
tmp=$(mktemp -d)
trap 'rm -rf -- "$tmp"' EXIT
cp "$src/keyboard.cs.txt" "$tmp/keyboard.cs"
# Keep the older shared evidence fixture unchanged. Current field presentation also calls Format.
sed '/public static class UiText/s/ { / { public static string Format(string fmt, params object[] args) { return string.Format(fmt,args); } /' \
    "$src/dependencies.cs.txt" > "$tmp/dependencies.cs"
mono_exe=${DISPLAY_MONO:-mono}
refs=${DISPLAY_NET472_REFS:?set the .NET Framework 4.7.2 reference assembly directory}
compiler=${DISPLAY_ROSLYN_CSC:?set the path to Roslyn net472 csc.exe}
"$mono_exe" "$compiler" -nologo -noconfig -nostdlib+ -define:LIB \
    -r:"$refs/mscorlib.dll" -r:"$refs/System.dll" -r:"$refs/System.Core.dll" -lib:"$refs" \
    -r:System.Drawing.dll -r:System.Windows.Forms.dll -out:"$tmp/keyboard.exe" \
    "$tmp/keyboard.cs" "$tmp/dependencies.cs" Controls/DisplayThisForm.cs \
    Controls/DisplayThisForm.Designer.cs Controls/TelemetryFieldPresentation.cs \
    Controls/ThemeTextContrast.cs Controls/PlaceholderTextBox.cs
# DISPLAY must name a disposable isolated X server: this test sends actual input events there.
[[ "${DISPLAY_KEYBOARD_ISOLATED:-}" == "yes" && -n "${DISPLAY:-}" ]]
"$mono_exe" "$tmp/keyboard.exe"
