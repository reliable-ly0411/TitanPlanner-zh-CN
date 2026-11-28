#!/usr/bin/env python3
"""Compile real Backstage and the OSD caption helper; no app startup or device."""
import argparse
from pathlib import Path
import subprocess
import tempfile


ROOT = Path(__file__).resolve().parents[2]
FIXTURE = Path(__file__).with_name('Fixture.cs.txt')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--mcs', default='mcs')
    parser.add_argument('--mono', default='mono')
    args = parser.parse_args()

    def compile(*items):
        subprocess.run([args.mcs, '-nologo', '-langversion:latest', '-r:System.Windows.Forms', '-r:System.Drawing',
                        *map(str, items)], check=True)

    def run(*items):
        subprocess.run([args.mono, *map(str, items)], check=True)

    with tempfile.TemporaryDirectory(prefix='titan-optional-osd-caption-') as directory:
        tmp = Path(directory)
        setup = (ROOT / 'GCSViews/InitialSetup.cs').read_text(encoding='utf-8-sig')
        start = setup.index('        private static void SetOptionalOsdMenuCaption')
        end = setup.index('        private void HardwareConfig_FormClosing', start)
        helper = tmp / 'Caption.cs'
        helper.write_text('using System; using MissionPlanner.Utilities; using MissionPlanner.Controls.BackstageView;\n'
                          'partial class OptionalOsdProbe {\n' + setup[start:end] + '\n}\n', encoding='utf-8')

        resource_example = tmp / 'ResourcesCompiler.cs'
        resource_class = FIXTURE.read_text(encoding='utf-8').split('internal static class ResourcesCompiler', 1)[1]
        resource_example.write_text('using System.Resources; using System.Xml;\n'
                                    'internal static class ResourcesCompiler' + resource_class, encoding='utf-8')
        resource_builder = tmp / 'ResourcesCompiler.exe'
        compile('-main:ResourcesCompiler', '-out:' + str(resource_builder), resource_example)
        assembly = 'OptionalOsdProbe'
        for culture in ('', 'zh-CN', 'zh-Hans'):
            suffix = '.' + culture if culture else ''
            name = 'UiText' + suffix
            resources = tmp / (name + '.resources')
            run(resource_builder, ROOT / 'ExtLibs/Utilities/Resources' / (name + '.resx'), resources)
            if culture:
                target = tmp / culture
                target.mkdir()
                marker = target / 'Culture.cs'
                marker.write_text('using System.Reflection; [assembly: AssemblyCulture("' + culture + '")]', encoding='utf-8')
                compile('-target:library', '-out:' + str(target / (assembly + '.resources.dll')), marker,
                        '-resource:' + str(resources) + ',MissionPlanner.Utilities.Resources.' + name + '.resources')
        exe = tmp / (assembly + '.exe')
        backstage = sorted((ROOT / 'ExtLibs/Controls/BackstageView').glob('*.cs'))
        compile('-main:OptionalOsdProbe', '-out:' + str(exe), FIXTURE, helper, ROOT / 'ExtLibs/Utilities/UiText.cs',
                '-resource:' + str(tmp / 'UiText.resources') + ',MissionPlanner.Utilities.Resources.UiText.resources',
                *backstage)
        run(exe)


if __name__ == '__main__':
    main()
