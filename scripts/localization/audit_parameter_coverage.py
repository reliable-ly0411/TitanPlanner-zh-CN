#!/usr/bin/env python3
"""Report exact description coverage against a locally supplied, pinned official PDEF."""
import argparse
from collections import defaultdict
import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET

from build_parameter_catalog import ROOT, record_fingerprint, sha


def audit(raw, family, version, source, notes, groups=()):
    if hashlib.sha256(raw).hexdigest() != source['sourceSha256']:
        raise ValueError('Official XML SHA-256 does not match the pinned release')
    stamp = source.get('legacySourceStamp', f"Generated from git tag {source['gitTag']} ({source['gitSha']})")
    stamp = f"<!-- {stamp} -->".encode()
    if stamp not in raw:
        raise ValueError('Missing exact release/build source stamp')
    official = defaultdict(list)
    root = ET.fromstring(raw)
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
    duplicates = [k for k, records in official.items() if len(records) > 1]
    conflicts = [k for k in duplicates if len({record_fingerprint(r) for r in official[k]}) > 1]
    report = dict(
        family=family, version=version, source_sha256=source['sourceSha256'],
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
    notes = json.loads((ROOT / 'localization/zh-Hans/parameter-translations.json').read_text(encoding='utf-8'))['translations']
    try:
        print(json.dumps(audit(args.official_xml.read_bytes(), args.family, args.version, entries[0], notes, args.group), indent=2))
    except (OSError, ValueError, ET.ParseError) as exc:
        parser.error(str(exc))


if __name__ == '__main__':
    main()
