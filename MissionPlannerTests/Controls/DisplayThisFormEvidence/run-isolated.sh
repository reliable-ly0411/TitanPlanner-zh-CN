#!/usr/bin/env bash
# Isolated real controls with synthetic model/theme dependencies; requires Mono/X11.
set -euo pipefail
: "${DISPLAY_NET472_REFS:?Path to .NET Framework 4.7.2 reference assemblies}"
: "${DISPLAY_MSTEST_DLL:?Path to MSTest 2.2.10 net45 framework DLL}"
display_mono="${DISPLAY_MONO:-mono}"
display_mcs="${DISPLAY_MCS_EXE:-/usr/lib/mono/4.5/mcs.exe}"
display_root="$(cd "$(dirname "$0")/../../.." && pwd)"
display_evidence="$display_root/MissionPlannerTests/Controls/DisplayThisFormEvidence"
display_output="${DISPLAY_TEST_OUTPUT:-$(mktemp -d)}"
mkdir -p "$display_output"
cd "$display_root"
display_references=(-nostdlib "-lib:$DISPLAY_NET472_REFS" -r:mscorlib -r:System -r:System.Core -r:System.Windows.Forms -r:System.Drawing)
display_sources=(Controls/DisplayThisForm.cs Controls/DisplayThisForm.Designer.cs Controls/PlaceholderTextBox.cs Controls/TelemetryFieldPresentation.cs "$display_evidence/dependencies.cs.txt")
display_test_refs=("-r:$DISPLAY_NET472_REFS/Facades/System.Runtime.dll" "-r:$DISPLAY_NET472_REFS/Facades/System.Collections.dll" "-r:$DISPLAY_MSTEST_DLL")
"$display_mono" "$display_mcs" "${display_references[@]}" "${display_test_refs[@]}" "-out:$display_output/Tests.exe" "${display_sources[@]}" MissionPlannerTests/Controls/DisplayThisFormLayoutTests.cs "$display_evidence/runner.cs.txt"
cp "$DISPLAY_MSTEST_DLL" "$display_output/"
"$display_mono" "$display_mcs" "${display_references[@]}" -d:LIB -target:library "-out:$display_output/PopupLibGuard.dll" "${display_sources[@]}"
# Run the unchanged status half of the earlier combined status/chart geometry test.
python3 - "$display_output/legacy.cs" <<'PY'
from pathlib import Path
import sys
s=Path('MissionPlannerTests/Controls/TelemetryFieldPresentationTests.cs').read_text()
start=s.index('        [TestMethod]\n        public void StatusBilingualLabelReserves')
brace=s.index('{',start); depth=1; end=brace+1
while depth:
    if s[end]=='{': depth+=1
    elif s[end]=='}': depth-=1
    end+=1
imports='using System;\nusing System.Collections;\nusing System.Drawing;\nusing System.Reflection;\nusing System.Threading;\nusing System.Windows.Forms;\nusing Microsoft.VisualStudio.TestTools.UnitTesting;\nusing MissionPlanner.Controls;\n'
Path(sys.argv[1]).write_text(imports+'[TestClass] public class LegacyLayoutRegression {\n'+s[start:end]+'\n}\n')
PY
"$display_mono" "$display_mcs" "${display_references[@]}" "${display_test_refs[@]}" "-out:$display_output/LegacyTests.exe" "${display_sources[@]}" Controls/StatusControl.cs "$display_output/legacy.cs" "$display_evidence/runner.cs.txt"
for display_file in Controls/DisplayThisForm.cs Controls/DisplayThisForm.Designer.cs; do
    git show "b5bca65edef67e73cedc5ef0b7a8343e088b6a68:$display_file" | sed 's/DisplayThisForm/BeforeDisplayThisForm/g' > "$display_output/$(basename "$display_file" | sed 's/DisplayThisForm/BeforeDisplayThisForm/')"
done
"$display_mono" "$display_mcs" "${display_references[@]}" "-out:$display_output/Visual.exe" "${display_sources[@]}" "$display_output/BeforeDisplayThisForm.cs" "$display_output/BeforeDisplayThisForm.Designer.cs" "$display_evidence/visual.cs.txt"
"$display_mono" "$display_output/Tests.exe" | tee "$display_output/targeted.log"
"$display_mono" "$display_output/LegacyTests.exe" | tee "$display_output/legacy.log"
"$display_mono" "$display_output/Visual.exe" "$display_evidence/catalog.tsv" "$display_output" | tee "$display_output/visual.log"
printf 'Evidence directory: %s\n' "$display_output"
