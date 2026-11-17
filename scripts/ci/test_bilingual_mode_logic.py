#!/usr/bin/env python3
"""Run the isolated display/geometry checks with a .NET 8 SDK, without NuGet or a GUI."""
import argparse
import json
from pathlib import Path
import re
import shutil
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]


def version(path):
    return tuple(int(part) for part in re.findall(r'\d+', path.name))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet', default='dotnet')
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    executable = shutil.which(args.dotnet)
    if not executable:
        raise SystemExit('A .NET 8 SDK is required; pass --dotnet with its executable path.')
    dotnet = Path(executable).resolve()
    sdks = subprocess.check_output([str(dotnet), '--list-sdks'], text=True)
    candidates = [Path(root) / name for name, root in re.findall(r'^(8\.\S+) \[(.+)\]$', sdks, re.M)]
    if not candidates:
        raise SystemExit('No .NET 8 SDK found.')
    sdk = max(candidates, key=version)
    installation = sdk.parent.parent
    references = max((installation / 'packs/Microsoft.NETCore.App.Ref').glob('8.*'), key=version)
    runtime = max((installation / 'shared/Microsoft.NETCore.App').glob('8.*'), key=version)
    out = args.output or Path(tempfile.mkdtemp(prefix='titan-mode-checks-'))
    out.mkdir(parents=True, exist_ok=True)
    reference_args = ['-r:' + str(path) for path in sorted((references / 'ref/net8.0').glob('*.dll'))]
    runtime_config = {'runtimeOptions': {'tfm': 'net8.0', 'framework': {
        'name': 'Microsoft.NETCore.App', 'version': runtime.name}}}

    def compile_source(target, sources, extra=()):
        kind = '-target:library' if target.name.endswith('.resources.dll') else '-target:exe'
        subprocess.run([str(dotnet), str(sdk / 'Roslyn/bincore/csc.dll'), '-nologo', '-noconfig',
                        kind, '-out:' + str(target), *reference_args, *extra,
                        *(str(path) for path in sources)], check=True)
        target.with_suffix('.runtimeconfig.json').write_text(json.dumps(runtime_config), encoding='utf-8')

    generator = out / 'BuildResources.cs'
    generator.write_text('''using System.Resources; using System.Xml.Linq;
class BuildResources {
    static void Main(string[] args) {
        using (var writer = new ResourceWriter(args[1]))
            foreach (var item in XDocument.Load(args[0]).Root.Elements("data"))
                writer.AddResource((string)item.Attribute("name"), (string)item.Element("value"));
    }
}''', encoding='utf-8')
    compile_source(out / 'BuildResources.dll', [generator])
    neutral = []
    for culture in ('', 'zh-CN', 'zh-Hans'):
        suffix = '.' + culture if culture else ''
        resource = out / ('UiText' + suffix + '.resources')
        source = ROOT / 'ExtLibs/Utilities/Resources' / ('UiText' + suffix + '.resx')
        subprocess.run([str(dotnet), str(out / 'BuildResources.dll'), str(source), str(resource)], check=True)
        embed = '-resource:' + str(resource) + ',MissionPlanner.Utilities.Resources.UiText' + suffix + '.resources'
        if culture:
            directory = out / culture
            directory.mkdir(exist_ok=True)
            attribute = directory / 'Culture.cs'
            attribute.write_text('[assembly: System.Reflection.AssemblyCulture("' + culture + '")]', encoding='utf-8')
            compile_source(directory / 'BilingualFlightModeChecks.resources.dll', [attribute], [embed])
        else:
            neutral = [embed]
    compile_source(out / 'BilingualFlightModeChecks.dll', [ROOT / path for path in (
        'MissionPlannerTests/FlightModeDisplay/Program.cs.txt',
        'ExtLibs/Utilities/UiText.cs', 'ExtLibs/Utilities/FlightModeDisplay.cs',
        'ExtLibs/Controls/FlightModeLayout.cs')], neutral)
    subprocess.run([str(dotnet), str(out / 'BilingualFlightModeChecks.dll')], check=True)


if __name__ == '__main__':
    main()
