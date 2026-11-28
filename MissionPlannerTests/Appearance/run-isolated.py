#!/usr/bin/env python3
"""Isolated Mono/WinForms check. Requires mcs, mono and a private DISPLAY (e.g. xvfb-run).
Host substitutes are explicit in IsolatedHost.cs.txt. Not a full MissionPlanner build or Windows test.
"""
import argparse
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[2]

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', required=True)
    parser.add_argument('--mcs', default='mcs')
    parser.add_argument('--mono', default='mono')
    parser.add_argument('--framework-references', help='Optional .NET Framework 4.7.2 reference assembly directory')
    parser.add_argument('--desktop-appearance-source', help='Optional earlier bridge source for a negative regression run')
    args = parser.parse_args()
    output = Path(args.output).resolve()
    output.mkdir(parents=True, exist_ok=True)
    def compile(*items):
        references = ['-r:System.Drawing', '-r:System.Windows.Forms', '-r:System.Core']
        if args.framework_references:
            references = ['-noconfig', '-nostdlib'] + ['-r:' + str(Path(args.framework_references) / (name + '.dll'))
                for name in ['mscorlib', 'System', 'System.Core', 'System.Drawing', 'System.Windows.Forms', 'System.Xml']]
        subprocess.run([args.mcs, '-nologo', *references, *map(str, items)], check=True, cwd=ROOT)
    def run(*items):
        subprocess.run([args.mono, *map(str, items)], check=True, cwd=ROOT)
    helper = output / 'BuildResources.exe'
    compile('-out:' + str(helper), ROOT / 'MissionPlannerTests/Appearance/BuildResources.cs.txt')
    for source, name in [('Splash.resx', 'Splash'), ('ExtLibs/Utilities/Resources/UiText.resx', 'UiText'), ('ExtLibs/Utilities/Resources/UiText.zh-Hans.resx', 'UiText.zh-Hans'), ('ExtLibs/Utilities/Resources/UiText.zh-CN.resx', 'UiText.zh-CN')]:
        run(helper, ROOT / source, output / (name + '.resources'))
    theme = (ROOT / 'Utilities/ThemeManager.cs').read_text(encoding='utf-8-sig')
    fragment = theme[theme.index('        [DllImport("user32.dll")]\n        private static extern bool DestroyIcon'):theme.index('        #region Windows Dark Mode Title Bar Support')]
    shim = output / 'ThemeManagerFragment.cs'
    shim.write_text('using System; using System.Drawing; using System.Runtime.InteropServices; using MissionPlanner; namespace MissionPlanner.Utilities { public static class ThemeManager { public static Color BannerColor2=Color.Green;\n' + fragment + '\n} }\n')
    neutral = '-resource:' + str(output / 'UiText.resources') + ',MissionPlanner.Utilities.Resources.UiText.resources'
    for culture in ['zh-CN', 'zh-Hans']:
        directory = output / culture
        directory.mkdir(exist_ok=True)
        attributes = output / (culture + '.cs')
        attributes.write_text('using System.Reflection; [assembly: AssemblyCulture("' + culture + '")]')
        compile('-target:library', '-out:' + str(directory / 'AppearanceIsolated.resources.dll'), attributes,
                '-resource:' + str(output / ('UiText.' + culture + '.resources')) + ',MissionPlanner.Utilities.Resources.UiText.' + culture + '.resources')
    sources = ['Utilities/AppearanceAssetStore.cs', 'Utilities/DesktopAppearance.cs',
               'GCSViews/ConfigurationView/ConfigAppearance.cs', 'Splash.cs', 'Splash.Designer.cs',
               'ExtLibs/Utilities/UiText.cs', 'MissionPlannerTests/Utilities/AppearanceAssetStoreTests.cs',
               'MissionPlannerTests/Utilities/WindowIconRefreshTests.cs',
               'MissionPlannerTests/Appearance/IsolatedHost.cs.txt']
    if args.desktop_appearance_source:
        sources[1] = str(Path(args.desktop_appearance_source).resolve())
    executable = output / 'AppearanceIsolated.exe'
    artwork = sorted((ROOT / 'Resources' / 'Appearance').glob('built-in-*'))
    embedded = ['-resource:' + str(path) + ',MissionPlanner.Appearance.' + path.name for path in artwork]
    compile('-out:' + str(executable), neutral,
            '-resource:' + str(output / 'Splash.resources') + ',MissionPlanner.Splash.resources', shim,
            *embedded, *[ROOT / source for source in sources])
    # New desktop-only files must also compile to an empty library under LIB.
    compile('-target:library', '-define:LIB', '-out:' + str(output / 'AppearanceLibGuard.dll'),
            *[ROOT / source for source in sources[:3]])
    run(executable, output)
    for phase in ['seed', 'verify', 'damage', 'fallback', 'reset', 'defaults']:
        run(executable, output / 'persistence', phase)

if __name__ == '__main__':
    main()
