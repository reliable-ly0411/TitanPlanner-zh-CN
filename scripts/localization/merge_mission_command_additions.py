#!/usr/bin/env python3
"""Merge the reviewed mission-name additions and regenerate Chinese UI resources."""
import argparse
import json
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--apply', action='store_true', help='write dictionary and regenerate resources')
    args = parser.parse_args()
    mapping = json.loads((HERE / 'mission-command-source-map.json').read_text(encoding='utf-8'))['commands']
    additions = json.loads((HERE / 'mission-command-additions.zh-CN.json').read_text(encoding='utf-8'))
    target = ROOT / 'localization/zh-Hans/code-translations.json'
    catalog = json.loads(target.read_text(encoding='utf-8'))
    if len(mapping) != 71 or len({c['id'] for c in mapping}) != 71 or len({c['name'] for c in mapping}) != 71:
        raise SystemExit('Mission map must contain 71 unique IDs and full names')
    expected = {'Mission command: DO_GO_AROUND', 'Mission command: DO_PAUSE_CONTINUE'}
    expected.update('Mission command: ' + c['name'] for c in mapping)
    if set(additions) != expected:
        raise SystemExit('Unexpected or missing addition keys')
    review = json.loads((HERE / 'mission-command-semantic-corrections.json').read_text(encoding='utf-8'))
    corrections = {c['full_name']: c for c in review['corrections']}
    if review.get('schema_version') != 1 or len(review['corrections']) != 8 or len(corrections) != 8:
        raise SystemExit('Expected eight reviewed semantic corrections')
    if not corrections.keys() <= {c['name'] for c in mapping}:
        raise SystemExit('Unknown reviewed command')
    updates = dict(additions)
    allowed = {}
    for command in mapping:
        full = 'Mission command: ' + command['name']
        short = 'Mission command: ' + command['short']
        if additions[full] != command['zh']:
            raise SystemExit('Full-name translation differs: ' + full)
        correction = corrections.get(command['name'])
        accepted = {command['zh']}
        if correction:
            if (correction['id'], correction['short'], correction['expected_translation']) != (
                    command['id'], command['short'], command['zh']):
                raise SystemExit('Reviewed correction does not match source map: ' + full)
            replacement = correction['replacement']
            if not replacement.strip() or replacement == command['zh']:
                raise SystemExit('Empty or unchanged correction: ' + full)
            accepted.add(replacement)
            updates[short] = updates[full] = replacement
            allowed[short] = allowed[full] = accepted
        if catalog.get(short, additions.get(short)) not in accepted:
            raise SystemExit('Existing translation changed; review before merging: ' + short)
    for key, value in updates.items():
        if key in catalog and catalog[key] not in allowed.get(key, {value}):
            raise SystemExit('Conflicting existing translation: ' + key)
    pending = sum(key not in catalog for key in additions)
    print(f'{len(mapping)} command identities; {len(additions)} reviewed additions; {len(corrections)} paired corrections; {pending} new entries')
    if not args.apply:
        return
    changed = any(catalog.get(key) != value for key, value in updates.items())
    catalog.update(updates)
    if changed:
        target.write_text(json.dumps(catalog, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    for name in ('build_ui_catalog.py', 'update_zh_hans.py', 'verify_zh_hans.py'):
        subprocess.run([sys.executable, str(HERE / name)], check=True)

if __name__ == '__main__':
    main()
