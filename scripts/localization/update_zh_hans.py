#!/usr/bin/env python3
"""补齐 WinForms 简体中文资源；只改显示文本，不改布局、参数或协议字段。"""
import argparse
import json
from pathlib import Path
import re
import xml.etree.ElementTree as ET
from xml.sax.saxutils import escape, quoteattr

ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / 'localization/zh-Hans/ui-translations.json'
# 可替换为其他语言后缀，例如 zh-Hant；须同时提供对应语言词典。
LANGUAGE = 'zh-CN'
DISPLAY_KEY = re.compile(r'\.(Text|ToolTip|ToolTipText|HeaderText|Title|Items\d*)$')


def resources():
    for path in sorted(ROOT.rglob('*.resx')):
        rel = path.relative_to(ROOT)
        if '.git' in rel.parts or '.' in path.stem:
            continue
        if rel.parts[0] == 'ExtLibs' and rel.parts[1] not in ('Controls', 'Strings'):
            continue
        for entry in ET.parse(path).findall('data'):
            key, value = entry.get('name'), entry.findtext('value', '')
            if entry.get('type') or entry.get('mimetype'):
                continue
            if re.search('[a-zA-Z]', value) and (DISPLAY_KEY.search(key) or path.parent.name == 'Strings'):
                yield path, key, value


def localized_path(path):
    expected = path.with_name(path.stem + '.' + LANGUAGE + '.resx')
    # 上游 GeoRef/ResEdit 的资源名大小写不一致，避免生成重复卫星资源。
    return next((p for p in path.parent.glob('*.resx') if p.name.lower() == expected.name.lower()), expected)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true', help='只检查，不写入')
    args = parser.parse_args()
    catalog = json.loads(CATALOG.read_text(encoding='utf-8'))
    contexts = json.loads((CATALOG.parent / 'context-translations.json').read_text(encoding='utf-8'))
    documents, changed, count = {}, set(), 0
    for source, key, english in resources():
        override = contexts.get(source.relative_to(ROOT).as_posix(), {}).get(key)
        if english not in catalog and override is None:
            continue
        target = localized_path(source)
        if target not in documents:
            if target.exists():
                text = target.read_bytes().decode('utf-8-sig')
            else:
                # 沿用标准 RESX 文件头；新卫星资源只包含译文并继承中立布局。
                root = ET.parse(source).getroot()
                root.text = '\n'
                for child in list(root):
                    if child.tag in ('data', 'metadata', 'assembly'):
                        root.remove(child)
                text = '<?xml version="1.0" encoding="utf-8"?>\n' + ET.tostring(root, encoding='unicode') + '\n'
            documents[target] = text
        text = documents[target]
        entries = {e.get('name'): e.findtext('value', '') for e in ET.fromstring(text).findall('data')}
        # 保留上游已完成的译文；不以跨页面词典覆盖上下文译法。
        if override is not None and entries.get(key) == override:
            continue
        if override is None and key in entries and entries[key] != english and entries[key].strip():
            continue
        translation = override if override is not None else catalog[english]
        node = f'  <data name={quoteattr(key)} xml:space="preserve"><value>{escape(translation)}</value></data>'
        pattern = r'  <data\s+name="' + re.escape(key) + r'"[^>]*>.*?</data>'
        if key in entries:
            text, n = re.subn(pattern, lambda _: node, text, count=1, flags=re.S)
            if n != 1:
                raise ValueError(f'无法定位资源: {target}:{key}')
        else:
            text = text.replace('</root>', node + '\n</root>')
        documents[target] = text
        changed.add(target)
        count += 1
    if not args.check:
        for path in changed:
            path.write_bytes(documents[path].encode('utf-8'))
    print(f'{len(changed)} 个资源文件，{count} 个待补齐条目' if args.check else f'已补齐 {len(changed)} 个资源文件、{count} 个条目')
    if args.check and changed:
        raise SystemExit(1)


if __name__ == '__main__':
    main()
