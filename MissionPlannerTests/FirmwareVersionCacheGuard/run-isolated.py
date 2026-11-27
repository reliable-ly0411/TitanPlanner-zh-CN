"""Compile just GetAPMVERSIONFile and its cache fields over a deterministic fake client."""
import argparse
from pathlib import Path
import subprocess
import tempfile

HERE = Path(__file__).resolve().parent

def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--firmware", type=Path, default=HERE.parents[1] / "Utilities/Firmware.cs")
    p.add_argument("--mcs", default="mcs")
    p.add_argument("--mono", default="mono")
    args = p.parse_args()
    source = args.firmware.read_text(encoding="utf-8-sig")
    # Keep the exact field declarations, even in a baseline run.
    field = source.index("object urlcachelock")
    first = source.rfind("\n", 0, field)
    last = source.index(";", source.index("urlcache = ", field)) + 1
    meth_first = source.index("        public string GetAPMVERSIONFile(Uri url)")
    meth_last = source.index("\n        }", meth_first) + len("\n        }")
    with tempfile.TemporaryDirectory(prefix="titan-fw-cache-") as directory:
        dest = Path(directory)
        test = dest / "Firmware.cs"
        test.write_text("using System; using System.Collections.Generic; using System.IO; using System.Threading;\n"
                        "public class Firmware {private readonly FixtureLog log = new FixtureLog();"
                        + source[first:last] + "\n" + source[meth_first:meth_last] + "\n}\n", encoding="utf-8")
        exe = dest / "Probe.exe"
        subprocess.run([args.mcs, "-langversion:latest", "-out:" + str(exe), str(test),
                        str(HERE / "Probe.cs.txt")], check=True)
        subprocess.run([args.mono, str(exe)], check=True, timeout=15)

if __name__ == "__main__":
    main()
