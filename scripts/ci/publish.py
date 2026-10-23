#!/usr/bin/env python3
"""Publish only the exact artifact validated by this workflow; never replace a release."""
import os
import json
from pathlib import Path
import re
from upstream import git, promote, run
from release_artifacts import verify_manifest, release_tag

sha, base = os.environ['BUILD_SHA'], os.environ['BASE_SHA']
if not all(re.fullmatch(r'[0-9a-f]{40}', value) for value in (sha, base)):
    raise RuntimeError('Invalid commit SHA')
root = Path('release')
artifacts = verify_manifest(root, sha)
version = json.loads((root/'build-info.json').read_text(encoding='utf-8'))['version']
if os.environ.get('CANDIDATE_BRANCH'):
    git('fetch', 'origin', os.environ['CANDIDATE_BRANCH'])
promote(base, sha)
repo = os.environ['GITHUB_REPOSITORY']
tag = release_tag(sha, version)
refs = git('ls-remote', 'origin', 'refs/tags/' + tag, 'refs/tags/' + tag + '^{}').splitlines()
target = next((line.split()[0] for line in refs if line.endswith('^{}')), refs[0].split()[0] if refs else '')
if target and target != sha:
    raise RuntimeError('Existing release tag points to another commit; refusing to overwrite it')
exists = run('gh', 'release', 'view', tag, '--repo', repo, '--json', 'url', check=False)
if exists.returncode == 0:
    refs = git('ls-remote', 'origin', 'refs/tags/' + tag, 'refs/tags/' + tag + '^{}').splitlines()
    target = next((line.split()[0] for line in refs if line.endswith('^{}')), refs[0].split()[0] if refs else '')
    if target != sha:
        raise RuntimeError('Existing release tag points to another commit; refusing to overwrite it')
    print('Release already exists; immutable assets retained: ' + exists.stdout)
else:
    notes = root / 'release-notes.md'
    notes.write_text(f'程序版本：**{version}**。\n\n自动构建的 Windows 安装版和便携版（测试版）。\n\n'
                     '- 保留“中文(简体)”与增强“中文(简体)2”，选择后重启生效。\n'
                     '- 已通过中文资源、翻译运行、Windows 控件及离线回归测试、Release/Debug 构建和依赖漏洞检查。\n'
                     '- MSI：适用于 64 位 Windows，安装到当前用户目录并添加开始菜单入口，卸载保留个人配置和日志。\n'
                     '- ZIP：解压完整文件后启动 MissionPlanner.exe；不要仅复制 EXE。\n'
                     '- MSI 已通过安装、文件一致性、修复和卸载保留数据测试。\n'
                     '- 包含 SHA256SUMS 校验文件。未签名；真实飞控、飞行、高 DPI 与所有外部服务尚需实机验收。\n\n'
                     f'构建提交：`{sha}`\n', encoding='utf-8')
    print(run('gh', 'release', 'create', tag, *(str(path) for path in artifacts), str(root / 'SHA256SUMS'), str(root / 'build-info.json'),
              '--repo', repo, '--target', sha, '--title', f'TitanPlanner-zh-CN v{version}（中文双版本 · {sha[:12]}）',
              '--prerelease', '--notes-file', str(notes)).stdout)
