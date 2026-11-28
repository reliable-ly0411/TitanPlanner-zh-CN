#!/usr/bin/env python3
"""Propose reuse of reviewed descriptions from locally pinned complete PDEF files."""
import argparse
from collections import Counter, defaultdict
import json
from pathlib import Path
import xml.etree.ElementTree as ET

from audit_parameter_coverage import audit
from build_parameter_catalog import ROOT, expanded_descriptions, index_bundle, records_by_key, record_fingerprint, resource


def release_order(version):
    return tuple(int(part) for part in version.split('.'))


def stable_targets(sources):
    targets = {}
    for family, version in sources:
        series = (family, *release_order(version)[:2])
        if series not in targets or release_order(version) > release_order(targets[series]):
            targets[series] = version
    return {(series[0], version) for series, version in targets.items()}


def append_record(paramfile, section, group, record):
    container = paramfile.find(section)
    if container is None:
        container = ET.SubElement(paramfile, section)
    parent = next((item for item in container.findall('parameters') if item.get('name') == group), None)
    if parent is None:
        parent = ET.SubElement(container, 'parameters', name=group)
    parent.append(record)


def propose(catalog, registry, bundle, source_dir):
    releases = {(r.get('family'), r.get('version')): r for r in bundle}
    sources = {(s['family'], s['version']): s for s in registry['releases']}
    if len(sources) != len(registry['releases']) or releases.keys() != sources.keys():
        raise ValueError('Source registry and embedded release scopes disagree')
    embedded = index_bundle(bundle)
    existing = expanded_descriptions(catalog, embedded)
    occupied = {(note['family'], v, note['parameter']) for note in existing if note['field'] == 'Description'
                for v in note['versions']}
    audited = {}
    donors = defaultdict(list)
    for scope, source in sources.items():
        family, version = scope
        raw = (source_dir / f'{family}-{version}.xml').read_bytes()
        coverage = audit(raw, family, version, source, existing)
        audited[scope] = (records_by_key(ET.fromstring(raw)), coverage)

    for note in catalog['translations']:
        if note['field'] == 'Description':
            for version in note['versions']:
                section, group, param = audited[note['family'], version][0][note['parameter']][0]
                donors[section, group, note['parameter'], record_fingerprint(param)].append(
                    (note['family'], version, note['chinese']))

    targets = stable_targets(sources)
    pending = defaultdict(list)
    stats = Counter()
    # Add independently verified target records. Archived sources already carry the complete tree.
    for family, version in sorted(targets, key=lambda s:
                                  (s[0] not in ('Plane', 'Copter'), s[0], release_order(s[1]))):
        release = releases[family, version]
        records, _ = audited[family, version]
        for key, definitions in records.items():
            if len(definitions) != 1 or (family, version, key) in occupied:
                continue
            section, group, record = definitions[0]
            if not group or not record.get('documentation', '').strip():
                continue
            fingerprint = record_fingerprint(record)
            candidates = [(f, v, text) for f, v, text in donors[section, group, key, fingerprint]
                          if f == family or section == 'libraries']
            if len({text for _, _, text in candidates}) != 1:
                continue
            # Prefer a donor from the same vehicle and then its highest known stable version.
            donor_family, donor_version, _ = max(
                candidates, key=lambda donor: (donor[0] == family, release_order(donor[1]), donor[0]))
            pending[family, key, fingerprint, donor_family, donor_version].append(version)
            if key not in embedded[family, version]:
                append_record(release.find('paramfile'), section, group, record)
            stats[family, version] += 1
        sources[family, version]['parameters'] = sum(1 for _ in release.iter('param'))

    entries = [dict(family=f, parameter=key, versions=sorted(versions, key=release_order), recordSha256=digest,
                    **{'from': dict(family=donor_f, version=donor_v)})
               for (f, key, digest, donor_f, donor_v), versions in sorted(pending.items())]
    catalog.setdefault('descriptionReuse', []).extend(entries)
    generated = resource(catalog, bundle)
    expanded = expanded_descriptions(catalog, index_bundle(bundle))
    coverage = []
    for (family, version), (_, before) in audited.items():
        count = stats[family, version]
        raw = (source_dir / f'{family}-{version}.xml').read_bytes()
        after = audit(raw, family, version, sources[family, version], expanded)
        if after['exact_descriptions'] != before['exact_descriptions'] + count:
            raise ValueError(f'Unexpected coverage change: {family} {version}')
        coverage.append(dict(after, added=count, latest_patch=(family, version) in targets))
    descriptions = {(n['family'], v, n['parameter']) for n in expanded if n['field'] == 'Description' for v in n['versions']}
    return generated, dict(
        new_reused=sum(stats.values()), authored_entries=len(catalog['translations']),
        reuse_entries=len(catalog['descriptionReuse']), versioned_descriptions=len(descriptions),
        distinct_family_keys=len({(f, key) for f, _, key in descriptions}), releases=coverage)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source_directory', type=Path, help='Full PDEF files named Family-version.xml')
    parser.add_argument('proposal_directory', type=Path, help='Location for reviewable generated files')
    args = parser.parse_args()
    catalog = json.loads((ROOT / 'localization/zh-Hans/parameter-translations.json').read_bytes())
    registry = json.loads((ROOT / 'localization/parameters/sources.json').read_bytes())
    bundle = ET.parse(ROOT / 'localization/parameters/official-first-batch.xml').getroot()
    generated, report = propose(catalog, registry, bundle, args.source_directory)
    ET.indent(bundle, space='  ')
    bundle_bytes = ET.tostring(bundle, encoding='utf-8', xml_declaration=True) + b'\n'
    if max(len(bundle_bytes), len(generated)) > 64 * 1024 * 1024:
        raise ValueError('Embedded parameter XML exceeds the application parser limit')
    args.proposal_directory.mkdir(parents=True, exist_ok=True)
    for name, data in (
        ('parameter-translations.json', (json.dumps(catalog, ensure_ascii=False, indent=2) + '\n').encode()),
        ('sources.json', (json.dumps(registry, ensure_ascii=False, indent=2) + '\n').encode()),
        ('coverage.json', (json.dumps(report, indent=2) + '\n').encode()),
        ('official-first-batch.xml', bundle_bytes),
        ('zh-CN.xml', generated),
    ):
        (args.proposal_directory / name).write_bytes(data)
    print(json.dumps({key: count for key, count in report.items() if key != 'releases'}))


if __name__ == '__main__':
    main()
