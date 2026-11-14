#!/usr/bin/env python3
"""Refresh two exact upstream refs; diverged histories require review, never force."""
import subprocess

MIRRORS = (
    ('Titanmain', 'https://github.com/Titan-Dynamics/TitanPlanner.git'),
    ('missionmain', 'https://github.com/ArduPilot/MissionPlanner.git'),
)

def git(*args):
    return subprocess.check_output(['git', *args], text=True).strip()

def main():
    for branch, remote in MIRRORS:
        git('fetch', '--no-tags', remote, 'master')
        target = git('rev-parse', 'FETCH_HEAD')
        current = git('ls-remote', 'origin', 'refs/heads/' + branch)
        if current:
            git('fetch', '--no-tags', 'origin', branch)
            old = git('rev-parse', 'FETCH_HEAD')
            if old == target:
                print(branch, target, 'already exact')
                continue
            subprocess.run(['git', 'merge-base', '--is-ancestor', old, target], check=True)
        git('push', 'origin', target + ':refs/heads/' + branch)
        actual = git('ls-remote', 'origin', 'refs/heads/' + branch).split()[0]
        if actual != target:
            raise RuntimeError('Mirror changed concurrently: ' + branch)
        print(branch, actual, 'exact upstream commit')

if __name__ == '__main__':
    main()
