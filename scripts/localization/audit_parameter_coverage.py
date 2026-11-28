#!/usr/bin/env python3
"""Report exact description coverage against a locally supplied, pinned release PDEF."""
import argparse
from collections import defaultdict
import hashlib
import json
from pathlib import Path
import re
import xml.etree.ElementTree as ET

from build_parameter_catalog import ROOT, expanded_descriptions, index_bundle, record_fingerprint, sha


def audit(raw, family, version, source, notes, groups=()):
    if (source['family'], source['version']) != (family, version):
        raise ValueError('Registry entry belongs to another release')
    if hashlib.sha256(raw).hexdigest() != source['sourceSha256']:
        raise ValueError('PDEF SHA-256 does not match the pinned release')
    root = ET.fromstring(raw)
    if source.get('sourceKind') == 'reconstructed':
        commit = source['gitSha']
        archive = 'https://codeload.github.com/ArduPilot/ardupilot/tar.gz/'
        evidence = source['reconstruction']
        if (not re.fullmatch(r'[0-9a-f]{40}', commit)
                or source['sourceUrl'] != archive + commit
                or not re.fullmatch(r'[0-9a-f]{64}', evidence['sourceArchiveSha256'])):
            raise ValueError('Invalid pinned reconstruction archive')
        prefix = 'https://firmware.ardupilot.org/'
        directory = 'AntennaTracker' if family == 'Tracker' else family
        prefix += f'{directory}/stable-{version}/'
        board_pairs = evidence['boards']
        tags = evidence['tags']
        names = {family, {'Copter': 'ArduCopter', 'Plane': 'ArduPlane', 'Rover': 'APMrover2',
                          'Sub': 'ArduSub', 'Tracker': 'AntennaTracker'}[family]}
        if (not (tags or board_pairs)
                or any(tag not in {f'{name}-{version}' for name in names} for tag in tags)
                or any(pair['gitSha'] != commit
                       or pair['releaseUrl'] != prefix + pair['board'] + '/firmware-version.txt'
                       or pair['commitUrl'] != prefix + pair['board'] + '/git-version.txt'
                       for pair in board_pairs)):
            raise ValueError('Missing or inconsistent reconstruction release evidence')
        groups_in_file = [g.get('name') for g in root.findall('./vehicles/parameters')]
        if groups_in_file != [evidence['vehicleGroup']] or root.find('firmware') is not None:
            raise ValueError('Reconstructed PDEF vehicle group or release marker changed')
    else:
        if source.get('sourceKind') is not None:
            raise ValueError('Unknown metadata source kind')
        stamp = source.get('legacySourceStamp', f"Generated from git tag {source['gitTag']} ({source['gitSha']})")
        if f"<!-- {stamp} -->".encode() not in raw:
            raise ValueError('Missing exact release/build source stamp')
    official = defaultdict(list)
    for record in root.iter('param'):
        official[record.attrib['name'].rsplit(':', 1)[-1]].append(record)
    reviewed = set()
    for note in notes:
        if (note['family'] != family or version not in note['versions']
                or note['field'] != 'Description'):
            continue
        key = note['parameter']
        matches = official[key] if key in official else []
        if (key in reviewed or len(matches) != 1
                or record_fingerprint(matches[0]) != note['recordSha256']
                or matches[0].get('documentation') != note['english']
                or sha(note['english']) != note['englishSha256']):
            raise ValueError(f'Unverified/ambiguous translation claim: {key}')
        reviewed.add(key)
    if source.get('sourceKind') == 'reconstructed' and len(official) != source['reconstruction']['uniqueKeys']:
        raise ValueError('Reconstructed unique parameter count changed')
    duplicates = [k for k, records in official.items() if len(records) > 1]
    conflicts = [k for k in duplicates if len({record_fingerprint(r) for r in official[k]}) > 1]
    report = dict(
        family=family, version=version, source_sha256=source['sourceSha256'],
        source_kind=source.get('sourceKind', 'official'),
        official_parameter_keys=len(official), exact_descriptions=len(reviewed),
        not_localized=len(official) - len(reviewed), duplicate_keys=len(duplicates),
        conflicting_record_keys=len(conflicts),
    )

    if groups:
        report['groups'] = {}
        for name in sorted(set(groups)):
            matches = root.findall(".//parameters")
            keys = {r.attrib['name'].rsplit(':', 1)[-1] for group in matches if group.get('name') == name
                    for r in group.findall('param')}
            if not keys:
                raise ValueError(f'Unknown or empty official parameter group: {name}')
            group_duplicates = keys.intersection(duplicates)
            report['groups'][name] = dict(
                keys=len(keys), translated=sorted(keys & reviewed), untranslated=sorted(keys - reviewed),
                duplicate_keys=sorted(group_duplicates), conflicting_record_keys=sorted(keys.intersection(conflicts)),
                untranslated_unique_read_only=sorted(
                    k for k in keys - reviewed - group_duplicates
                    if official[k][0].findtext("field[@name='ReadOnly']") == 'True'
                ),
            )
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('family')
    parser.add_argument('version')
    parser.add_argument('official_xml', type=Path)
    parser.add_argument('--group', action='append', default=[], help='Exact PDEF group; repeat for multiple groups')
    args = parser.parse_args()
    records = json.loads((ROOT / 'localization/parameters/sources.json').read_text(encoding='utf-8'))['releases']
    entries = [record for record in records if (record['family'], record['version']) == (args.family, args.version)]
    if len(entries) != 1:
        parser.error('No unique pinned registry entry for that release')
    catalog = json.loads((ROOT / 'localization/zh-Hans/parameter-translations.json').read_text(encoding='utf-8'))
    try:
        bundle = ET.parse(ROOT / 'localization/parameters/official-first-batch.xml').getroot()
        notes = expanded_descriptions(catalog, index_bundle(bundle))
        print(json.dumps(audit(args.official_xml.read_bytes(), args.family, args.version, entries[0], notes, args.group), indent=2))
    except (OSError, ValueError, ET.ParseError) as exc:
        parser.error(str(exc))


if __name__ == '__main__':
    main()
