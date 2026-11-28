#!/usr/bin/env python3
"""Compile scoped Chinese parameter notes. No network and no automatic source reapproval."""
import argparse
from collections import defaultdict
import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]


def sha(text):
    return hashlib.sha256(text.encode('utf-8')).hexdigest()


def generated_matches(actual, expected):
    """Git may check text resources out with CRLF. Allow only that EOL difference.

    This is never used for official source digests or authored English/record hashes.
    Whitespace, content, field ordering and bare carriage returns still have to agree.
    """
    return actual.replace(b'\r\n', b'\n') == expected.replace(b'\r\n', b'\n')


def record_fingerprint(param):
    parts = ['attr:' + key + '\x1f' + value for key, value in sorted(param.attrib.items())]
    parts += ['field:' + field.get('name', '') + '\x1f' + (field.text or '')
              for field in sorted(param.findall('field'), key=lambda field: field.get('name', ''))]
    for path, prefix in (('./values/value', 'value:'), ('./bitmask/bit', 'bit:')):
        parts += [prefix + value.get('code', '') + '\x1f' + (value.text or '') for value in param.findall(path)]
    return sha('\0'.join(parts))


def resource(catalog, bundle):
    result = ET.Element('parameterTranslations', schemaVersion='1', locale='zh-CN', license='GPL-3.0-or-later')
    releases = {}
    for release in bundle:
        records = defaultdict(list)
        for record in release.iter('param'):
            records[record.get('name', '').rsplit(':', 1)[-1]].append(record)
        releases[release.get('family'), release.get('version')] = records
    seen = set()
    for note in catalog['translations']:
        if note['field'] not in ('Description', 'Value', 'Bitmask'):
            raise ValueError('Only display descriptions and labels may be translated')
        if not note['chinese'].strip() or note['englishSha256'] != sha(note['english']):
            raise ValueError('Missing Chinese or changed English fingerprint: ' + note['parameter'])
        for version in note['versions']:
            key = (note['family'], version, note['parameter'], note['field'], note.get('code', ''), note['englishSha256'])
            if key in seen:
                raise ValueError('Duplicate translation scope: ' + repr(key))
            seen.add(key)
            params = releases[note['family'], version].get(note['parameter'], ())
            if len(params) != 1 or note['recordSha256'] != record_fingerprint(params[0]):
                raise ValueError('Changed/ambiguous source record requires manual review: ' + repr(key))
            param = params[0]
            if note['field'] == 'Description':
                english = param.get('documentation', '')
            else:
                path = './values/value' if note['field'] == 'Value' else './bitmask/bit'
                english = next(v.text for v in param.findall(path) if v.get('code') == note['code'])
            if note['english'] != english:
                raise ValueError('Source English does not match: ' + repr(key))
        attrs = {key: str(note[key]) for key in ('family', 'parameter', 'field')}
        attrs.update(versions=','.join(note['versions']), englishSha256=note['englishSha256'], recordSha256=note['recordSha256'])
        if 'code' in note:
            attrs['code'] = note['code']
        translation = ET.SubElement(result, 'translation', **attrs)
        ET.SubElement(translation, 'english').text = note['english']
        ET.SubElement(translation, 'chinese').text = note['chinese']
    ET.indent(result, space='  ')
    return ET.tostring(result, encoding='utf-8', xml_declaration=True) + b'\n'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    catalog = json.loads((ROOT / 'localization/zh-Hans/parameter-translations.json').read_text(encoding='utf-8'))
    bundle = ET.parse(ROOT / 'localization/parameters/official-first-batch.xml').getroot()
    expected = resource(catalog, bundle)
    output = ROOT / 'localization/parameters/zh-CN.xml'
    if args.check:
        if not generated_matches(output.read_bytes(), expected):
            raise SystemExit('Parameter translation resource needs regeneration')
    else:
        output.write_bytes(expected)
    print(f'Parameter catalog: {len(catalog["translations"])} scoped entries; ' + ('check passed' if args.check else 'generated'))


if __name__ == '__main__':
    main()
