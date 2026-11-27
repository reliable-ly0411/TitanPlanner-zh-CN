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
        extracted.write_text('using System; using System.Drawing; using System.Collections.Generic; using System.Windows.Forms; using MissionPlanner.Utilities;\n'
                             'partial class Fixture { private string _auxFunctionsFirmware;\n' + display_methods + '\n}\n',
                             encoding='utf-8')
        # Use the actual UiText resources rather than a lookup stub.
        writer_src = tmp / 'Writer.cs'
        writer_src.write_text('using System.Xml; using System.Resources; class Writer { '
                              'static void Main(string[] args) { var doc = new XmlDocument { XmlResolver = null }; '
                              'doc.Load(args[0]); using (var output = new ResourceWriter(args[1])) '
                              'foreach (XmlElement item in doc.SelectNodes("/root/data")) '
                              'output.AddResource(item.GetAttribute("name"), item.SelectSingleNode("value").InnerText); }}',
                              encoding='utf-8')
        writer = tmp / 'Writer.exe'
        subprocess.run([args.mcs, '-r:System.Xml', '-out:' + str(writer), str(writer_src)], check=True)
        resources = ROOT / 'ExtLibs/Utilities/Resources'
        neutral = tmp / 'UiText.resources'
        chinese = tmp / 'UiText.zh-CN.resources'
        for output in (neutral, chinese):
            resx = resources / output.with_suffix('.resx').name
            subprocess.run([args.mono, str(writer), str(resx), str(output)], check=True)
        satellite = tmp / 'zh-CN/AuxiliaryOptflowLabel.resources.dll'
        satellite.parent.mkdir()
        manifest = tmp / 'Satellite.cs'
        manifest.write_text('[assembly: System.Reflection.AssemblyCulture("zh-CN")]', encoding='utf-8')
        subprocess.run([args.mcs, '-target:library', '-out:' + str(satellite),
                        '-resource:' + str(chinese) + ',MissionPlanner.Utilities.Resources.UiText.zh-CN.resources',
                        str(manifest)], check=True)

        exe = tmp / 'AuxiliaryOptflowLabel.exe'
        subprocess.run([args.mcs, '-nologo', '-langversion:latest', '-r:System.Drawing', '-r:System.Windows.Forms', '-r:System.Xml',
                        '-out:' + str(exe), '-resource:' + str(neutral) + ',MissionPlanner.Utilities.Resources.UiText.resources',
                        str(ROOT / 'ExtLibs/Utilities/UiText.cs'), str(extracted),
                        str(Path(__file__).with_name('Fixture.cs.txt'))], check=True)
        subprocess.run([args.mono, str(exe), str(ROOT / 'ParameterMetaDataBackup.xml')], check=True)


if __name__ == '__main__':
    main()
