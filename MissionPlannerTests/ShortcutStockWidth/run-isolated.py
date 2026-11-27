#!/usr/bin/env python3
"""Use Mono under a private X/WinForms display; does not build or start the application."""
import argparse
from pathlib import Path
import subprocess
import tempfile


ROOT = Path(__file__).resolve().parents[2]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--mcs', default='mcs')
    parser.add_argument('--mono', default='mono')
    parser.add_argument('--planner-source', type=Path, default=ROOT / 'GCSViews/ConfigurationView/ConfigPlanner.cs',
                        help='Point at the earlier C# source for the negative regression run.')
    parser.add_argument('--language', default='zh-CN')
    args = parser.parse_args()

    planner = args.planner_source.read_text(encoding='utf-8-sig')

    def between(start, end):
        begin = planner.index(start)
        return planner[begin:planner.index(end, begin)]

    # The production builder, bilingual dictionary and layout helpers remain verbatim.
    planner_helpers = between('        private static void PrepareLayoutControls', '        private FlowLayoutPanel CreateWrapFlow')
    shortcuts = between('        private static readonly Dictionary<string, string> ShortcutHelpChinese', '        private Control CreateSpeechAndAudioGroup')
    with tempfile.TemporaryDirectory(prefix='titan-shortcut-stock-width-') as temp:
        tmp = Path(temp)
        extracted = tmp / 'PlannerExcerpt.cs'
        extracted.write_text('using System; using System.Linq; using System.Collections.Generic; using System.Drawing; '
                             'using System.Globalization; using System.Windows.Forms; using MissionPlanner.Utilities;\n'
                             'partial class StockWidthChecks {\n' + planner_helpers + shortcuts + '\n}\n',
                             encoding='utf-8')
        exe = tmp / 'StockWidthChecks.exe'
        subprocess.run([args.mcs, '-nologo', '-langversion:latest', '-r:System.Drawing', '-r:System.Windows.Forms',
                        '-out:' + str(exe), str(extracted), str(Path(__file__).with_name('Fixture.cs.txt'))], check=True)
        subprocess.run([args.mono, str(exe), args.language], check=True)


if __name__ == '__main__':
    main()
