#!/usr/bin/env python3
"""Prepare a merge candidate. Never overwrite master or discard fork changes."""
import argparse
import json
import os
from pathlib import Path
import subprocess
import tempfile

UPSTREAM = 'https://github.com/Titan-Dynamics/TitanPlanner.git'


def run(*args, check=True):
    return subprocess.run(args, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=check)


def git(*args, check=True):
    return run('git', *args, check=check).stdout.strip()


def ancestor(old, new):
    return run('git', 'merge-base', '--is-ancestor', old, new, check=False).returncode == 0


def output(**values):
    text = ''.join(f'{key}={value}\n' for key, value in values.items())
    if os.environ.get('GITHUB_OUTPUT'):
        with open(os.environ['GITHUB_OUTPUT'], 'a', encoding='utf-8') as stream:
            stream.write(text)
    print(text, end='')


def proposal(branch, reason):
    repo = os.environ['GITHUB_REPOSITORY']
    existing = run('gh', 'pr', 'list', '--repo', repo, '--head', branch, '--state', 'open', '--json', 'url')
    if json.loads(existing.stdout):
        print(existing.stdout)
        return
    body = ('自动上游同步需要人工处理。\n\n' + reason +
            '\n\n主分支及已发布版本保持不变。解决冲突并通过 Windows 构建、中文资源和回归测试后再合并。')
    with tempfile.NamedTemporaryFile(mode='w', encoding='utf-8', suffix='.md') as file:
        file.write(body)
        file.flush()
        result = run('gh', 'pr', 'create', '--repo', repo, '--base', 'master', '--head', branch,
                     '--title', 'chore: review upstream TitanPlanner update', '--body-file', file.name, check=False)
    if result.returncode:
        raise RuntimeError('Candidate branch retained, but PR creation failed. Enable Actions PR creation in repository settings.\n' + result.stderr)
    print(result.stdout)


def prepare(sync, upstream=UPSTREAM):
    base = git('rev-parse', 'HEAD')
    if not sync:
        output(sha=base, base=base, candidate='', upstream='', changed='false')
        return
    git('fetch', '--no-tags', upstream, 'master')
    upstream_sha = git('rev-parse', 'FETCH_HEAD')
    if ancestor(upstream_sha, base):
        output(sha=base, base=base, candidate='', upstream=upstream_sha, changed='false')
        return
    branch = f'automation/upstream-{upstream_sha[:12]}-{base[:12]}'
    existing = git('ls-remote', 'origin', 'refs/heads/' + branch)
    if existing:
        git('fetch', 'origin', branch)
        candidate = git('rev-parse', 'FETCH_HEAD')
        if not ancestor(base, candidate) or not ancestor(upstream_sha, candidate):
            proposal(branch, '此前的同步分支仍有冲突，需审查。')
            raise RuntimeError('Existing candidate requires review')
    else:
        result = run('git', 'merge', '--no-ff', '--no-edit', upstream_sha, check=False)
        if result.returncode:
            conflicts = git('diff', '--name-only', '--diff-filter=U')
            git('merge', '--abort')
            # A PR from upstream's commit exposes real conflicts without changing master.
            git('push', 'origin', upstream_sha + ':refs/heads/' + branch)
            proposal(branch, '发生合并冲突：\n\n```text\n' + conflicts + '\n```')
            raise RuntimeError('Upstream merge conflicts; master unchanged')
        candidate = git('rev-parse', 'HEAD')
        git('push', 'origin', candidate + ':refs/heads/' + branch)
    # Automation must not silently replace its own validation/release policy.
    protected = git('diff', '--name-only', base, candidate, '--', '.github/workflows', '.github/release_policy', 'scripts/ci')
    if protected:
        proposal(branch, '上游修改涉及自动化策略，需要审查：\n\n```text\n' + protected + '\n```')
        raise RuntimeError('Automation policy changed; master unchanged')
    output(sha=candidate, base=base, candidate=branch, upstream=upstream_sha, changed='true')


def promote(base, sha):
    git('fetch', 'origin', 'master')
    current = git('rev-parse', 'FETCH_HEAD')
    if current == sha:
        return  # Safe retry after a release upload failure.
    if base == sha and ancestor(sha, current):
        # A push build already belongs to master. Publish its exact immutable
        # tag without moving the branch, even if unrelated work arrived later.
        print('Validated push commit is already in master history; retaining newer master')
        return
    if current != base:
        raise RuntimeError('master advanced during validation; rerun against its new head')
    if not ancestor(base, sha):
        raise RuntimeError('Candidate is not a descendant of tested base')
    # Ordinary fast-forward push also protects against a race after the fetch.
    git('push', 'origin', sha + ':refs/heads/master')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=['prepare', 'promote', 'propose'])
    parser.add_argument('--sync', action='store_true')
    parser.add_argument('--base')
    parser.add_argument('--sha')
    parser.add_argument('--branch')
    args = parser.parse_args()
    if args.action == 'prepare':
        prepare(args.sync)
    elif args.action == 'promote':
        promote(args.base, args.sha)
    else:
        proposal(args.branch, '候选版本未通过自动验证。请查看对应 GitHub Actions 日志。')
