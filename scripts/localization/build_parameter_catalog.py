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


def records_by_key(paramfile):
    """Keep the original section and group alongside each parameter record."""
    records = defaultdict(list)
    for section in paramfile:
        if section.tag in ('vehicles', 'libraries'):
            for group in section.findall('parameters'):
                for record in group.findall('param'):
                    key = record.get('name', '').rsplit(':', 1)[-1]
                    records[key].append((section.tag, group.get('name'), record))
    return records


def index_bundle(bundle):
    return {(release.get('family'), release.get('version')): records_by_key(release.find('paramfile'))
            for release in bundle}


def unique_record(releases, family, version, key):
    records = releases.get((family, version), {}).get(key, ())
    if len(records) != 1:
        raise ValueError(f'Changed/ambiguous source record requires manual review: {family} {version} {key}')
    return records[0]


def expanded_descriptions(catalog, releases):
    """Resolve explicit reuse from authored descriptions. Runtime entries stay version-scoped."""
    direct = catalog['translations']
    reuse = catalog.get('descriptionReuse', ())
    if not reuse:
        return direct

    authored = {}
    occupied = set()
    for note in direct:
        if note['field'] != 'Description':
            continue
        for version in note['versions']:
            identity = (note['family'], version, note['parameter'])
            if identity in authored:
                raise ValueError(f'Duplicate translation scope: {identity}')
            authored[identity] = note
            occupied.add(identity)

    expanded = list(direct)
    for item in reuse:
        family, key = item['family'], item['parameter']
        donor_family, donor_version = item['from']['family'], item['from']['version']
        donor = authored.get((donor_family, donor_version, key))
        if donor is None or item['recordSha256'] != donor['recordSha256'] or not item['versions']:
            raise ValueError(f'No matching authored description for reuse: {family} {key}')
        source_section, source_group, _ = unique_record(releases, donor_family, donor_version, key)
        for version in item['versions']:
            identity = (family, version, key)
            if identity in occupied:
                raise ValueError(f'Duplicate translation scope: {identity}')
            occupied.add(identity)
            section, group, record = unique_record(releases, *identity)
            if (not group or (section, group) != (source_section, source_group)
                    or (family != donor_family and section != 'libraries')
                    or record_fingerprint(record) != item['recordSha256']
                    or record.get('documentation') != donor['english']):
                raise ValueError(f'Reuse source fields or parameter group changed: {identity}')
        expanded.append(dict(donor, family=family, versions=item['versions']))
    return expanded


def resource(catalog, bundle):
    result = ET.Element('parameterTranslations', schemaVersion='1', locale='zh-CN', license='GPL-3.0-or-later')
    releases = index_bundle(bundle)
    seen = set()
    for note in expanded_descriptions(catalog, releases):
        if note['field'] not in ('Description', 'Value', 'Bitmask'):
            raise ValueError('Only display descriptions and labels may be translated')
        if not note['chinese'].strip() or note['englishSha256'] != sha(note['english']):
            raise ValueError('Missing Chinese or changed English fingerprint: ' + note['parameter'])
        for version in note['versions']:
            key = (note['family'], version, note['parameter'], note['field'], note.get('code', ''), note['englishSha256'])
            if key in seen:
                raise ValueError('Duplicate translation scope: ' + repr(key))
            seen.add(key)
            _, _, param = unique_record(releases, note['family'], version, note['parameter'])
            if note['recordSha256'] != record_fingerprint(param):
                raise ValueError('Changed/ambiguous source record requires manual review: ' + repr(key))
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
    print(f'Parameter catalog: {len(catalog["translations"])} authored entries, '
          f'{len(catalog.get("descriptionReuse", ()))} reuse entries. '
          + ('Check passed' if args.check else 'Generated'))


if __name__ == '__main__':
    main()
