#!/usr/bin/env python3
"""Compile only the production auxiliary label and dropdown display path; no application or device access."""
import argparse
from pathlib import Path
import subprocess
import tempfile


ROOT = Path(__file__).resolve().parents[2]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--mcs', default='mcs')
    parser.add_argument('--mono', default='mono')
    parser.add_argument('--actions-source', type=Path, default=ROOT / 'Controls/FlightDataActions.cs')
    args = parser.parse_args()
    px4_metadata = ROOT / 'ParameterFactMetaData.xml'
    if 'RC6_OPTION' in px4_metadata.read_text(encoding='utf-8-sig'):
        raise ValueError("PX4 fixture assumption changed; review exact option source.")
    source = args.actions_source.read_text(encoding='utf-8-sig')
    start = source.index('        private static readonly Dictionary<(int, string), string> AuxiliaryOptionLabels')
    end = source.index('        // DO_AUX_FUNCTION switch positions', start)
    display_methods = source[start:end]

    with tempfile.TemporaryDirectory(prefix='titan-aux-optflow-label-') as temp:
        tmp = Path(temp)
        extracted = tmp / 'Display.cs'
        extracted.write_text('using System; using System.Drawing; using System.Collections.Generic; using System.Windows.Forms;\n'
                             'partial class Fixture { private string _auxFunctionsFirmware;\n' + display_methods + '\n}\n',
                             encoding='utf-8')
        exe = tmp / 'AuxiliaryOptflowLabel.exe'
        subprocess.run([args.mcs, '-nologo', '-langversion:latest', '-r:System.Drawing', '-r:System.Windows.Forms', '-r:System.Xml',
                        '-out:' + str(exe), str(extracted), str(Path(__file__).with_name('Fixture.cs.txt'))], check=True)
        subprocess.run([args.mono, str(exe), str(ROOT / 'ParameterMetaDataBackup.xml')], check=True)


if __name__ == '__main__':
    main()
