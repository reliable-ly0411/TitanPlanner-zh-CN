#!/usr/bin/env python3
"""Prepare a normal stable-to-development merge; validation precedes publication."""
import argparse
import os
import subprocess

STABLE = 'zh-CN'
DEVELOPMENT = 'zh-CN-dev'


def git(*args):
    return subprocess.check_output(['git', *args], text=True).strip()


def ancestor(old, new):
    return subprocess.run(['git', 'merge-base', '--is-ancestor', old, new],
                          check=False).returncode == 0


def prepare():
    git('fetch', '--no-tags', 'origin',
        f'+refs/heads/{STABLE}:refs/remotes/origin/{STABLE}',
        f'+refs/heads/{DEVELOPMENT}:refs/remotes/origin/{DEVELOPMENT}')
    stable = git('rev-parse', f'origin/{STABLE}')
    base = git('rev-parse', f'origin/{DEVELOPMENT}')
    git('checkout', '--detach', base)
    if ancestor(stable, base):
        return base, base
    if ancestor(base, stable):
        git('merge', '--ff-only', stable)
    else:
        try:
            git('merge', '--no-ff', '-m', 'Merge stable history back into development', stable)
        except subprocess.CalledProcessError:
            git('merge', '--abort')
            raise RuntimeError('Stable/development conflict: resolve by normal merge; no remote changed')
    return base, git('rev-parse', 'HEAD')


def publish(base, candidate):
    # A normal push protects against a concurrent development update, even after this fetch.
    git('fetch', '--no-tags', 'origin', DEVELOPMENT)
    current = git('rev-parse', 'FETCH_HEAD')
    if ancestor(candidate, current):
        return
    if current != base:
        raise RuntimeError('Development advanced during validation; retry from the new head')
    if not ancestor(base, candidate):
        raise RuntimeError('Candidate must preserve development history')
    git('push', 'origin', f'{candidate}:refs/heads/{DEVELOPMENT}')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=['prepare', 'publish'])
    parser.add_argument('--base')
    parser.add_argument('--sha')
    args = parser.parse_args()
    if args.action == 'prepare':
        base, sha = prepare()
        values = f'base={base}\nsha={sha}\n'
        print(values, end='')
        if os.environ.get('GITHUB_OUTPUT'):
            with open(os.environ['GITHUB_OUTPUT'], 'a', encoding='utf-8') as output:
                output.write(values)
    else:
        if not args.base or not args.sha:
            parser.error('publish requires --base and --sha')
        publish(args.base, args.sha)
