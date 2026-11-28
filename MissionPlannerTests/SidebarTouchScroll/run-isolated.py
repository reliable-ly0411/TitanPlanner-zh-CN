#!/usr/bin/env python3
"""Compile only the production scroll helper and run under private Mono/WinForms."""
import argparse
from pathlib import Path
import subprocess
import tempfile


ROOT = Path(__file__).resolve().parents[2]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--mcs', default='mcs')
    parser.add_argument('--mono', default='mono')
    args = parser.parse_args()
    with tempfile.TemporaryDirectory(prefix='titan-sidebar-touch-scroll-') as temp:
        exe = Path(temp) / 'TouchScrollChecks.exe'
        subprocess.run([args.mcs, '-nologo', '-langversion:latest', '-r:System.Drawing', '-r:System.Windows.Forms',
                        '-out:' + str(exe), str(ROOT / 'Controls/SidebarTouchScroll.cs'),
                        str(Path(__file__).with_name('Fixture.cs.txt'))], check=True)
        subprocess.run([args.mono, str(exe)], check=True)


if __name__ == '__main__':
    main()
