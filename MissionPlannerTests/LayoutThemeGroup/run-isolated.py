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
    parser.add_argument('--firmware-settings-source', type=Path, help='Optional production FirmwareListRequest.cs from #189.')
    parser.add_argument('--dpi', default=120, type=int)
    parser.add_argument('--language', default='zh-CN')
    args = parser.parse_args()

    planner = args.planner_source.read_text(encoding='utf-8-sig')

    def between(start, end):
        begin = planner.index(start)
        return planner[begin:planner.index(end, begin)]

    # Keep production container and row builder verbatim. Stub only host settings, labels and unrelated groups.
    planner_helpers = between('        private static void PrepareLayoutControls', '        private Control CreateDesktopAppearanceGroup').rstrip()
    if planner_helpers.endswith('#if !LIB'):
        planner_helpers = planner_helpers[:-len('#if !LIB')]
    columns = between('        private class GroupColumnsPanel', '        private static void PrepareLayoutControls')
    toggle = ''
    if args.firmware_settings_source:
        firmware = args.firmware_settings_source.read_text(encoding='utf-8-sig')
        start = firmware.index('    internal static class FirmwareListSettings')
        end = firmware.index('        internal static string FailureText', start)
        toggle = firmware[start:end] + '\n}\n'
    # Mono 6.x does not implement Control.DeviceDpi. Feed the test's scale into that property only.
    assert columns.count('DeviceDpi') == 1
    columns = columns.replace('DeviceDpi', 'ThemeRegression.FixtureDpi')

    with tempfile.TemporaryDirectory(prefix='titan-theme-layout-') as temp:
        tmp = Path(temp)
        extracted = tmp / 'PlannerExcerpt.cs'
        extracted.write_text('using System; using System.Linq; using System.Collections.Generic; using System.Drawing; '
                             'using System.Globalization; using System.Windows.Forms; using MissionPlanner.Utilities;\n'
                             'partial class ThemeRegression {\n' + columns + planner_helpers + '\n}\n' + toggle,
                             encoding='utf-8')
        backstage = sorted((ROOT / 'ExtLibs/Controls/BackstageView').glob('*.cs'))
        exe = tmp / 'ThemeRegression.exe'
        subprocess.run([args.mcs, '-nologo', '-langversion:latest', '-r:System.Drawing', '-r:System.Windows.Forms',
                        '-out:' + str(exe), str(extracted), str(Path(__file__).with_name('Fixture.cs.txt')),
                        *map(str, backstage)], check=True)
        subprocess.run([args.mono, str(exe), str(args.dpi), args.language], check=True)


if __name__ == '__main__':
    main()
