"""Compile only the actual host/coordinator and inert outer-navigation fixture."""
import os
from pathlib import Path
import shutil
import subprocess
import tempfile

def main():
    fixture = Path(__file__).resolve().parent
    repo = fixture.parents[1]
    controls = repo / "ExtLibs/Controls"
    sources = [
        controls / "MainSwitcher.cs",
        controls / "MyUserControl.cs",
        repo / "GCSViews/ConfigurationView/FirmwareListRequest.cs",
        *sorted((controls / "BackstageView").glob("*.cs")),
    ]
    with tempfile.TemporaryDirectory(prefix="tp-firmware-in-flight-") as directory:
        work = Path(directory)
        for f in sources:
            shutil.copyfile(f, work / f.name)
        for name in ("Stubs", "Scenario"):
            shutil.copyfile(fixture / (name + ".cs.txt"), work / (name + ".cs"))
        # Exact handler body, without constructing InitialSetup and its hardware-backed children.
        setup = (repo / "GCSViews/InitialSetup.cs").read_text(encoding="utf-8-sig")
        first = setup.index("        private void HardwareConfig_FormClosing(")
        end = setup.index("\n        }", first) + len("\n        }")
        close = setup[first:end]
        if "backstageView.Close();" not in close:
            raise ValueError("The close hook changed; review the fixture against the candidate.")
        (work / "ActualClose.cs").write_text(
            "using System.Windows.Forms;\npublic partial class SimulatedSetup {\n" + close + "\n}\n",
            encoding="utf-8",
        )
        args = [os.environ.get("MCS", "mcs"), "-langversion:latest", "-r:System.Windows.Forms",
                "-r:System.Drawing", "-r:System.Data", "-out:Scenario.exe"]
        subprocess.run(args + [str(p) for p in sorted(work.glob("*.cs"))], cwd=work, check=True)
        subprocess.run([os.environ.get("MONO", "mono"), "Scenario.exe"], cwd=work, check=True, timeout=15)

if __name__ == "__main__":
    main()
