#!/usr/bin/env python3
import json
import os
from pathlib import Path
import subprocess
import sys
from receipt import validate_run, verify
from evidence import api, artifacts, archive

repo = os.environ['GITHUB_REPOSITORY']
if repo != 'reliable-ly0411/TitanPlanner-zh-CN':
    raise ValueError('Wrong repository')
run_id = os.environ['CANDIDATE_RUN_ID']
sha = os.environ['SOURCE_SHA']
run = api(f'repos/{repo}/actions/runs/{run_id}')
validate_run(run, repo, sha, 'master', '.github/workflows/main.yml')
if subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip() != sha:
    raise ValueError('Checkout differs from requested source')
jobs = api(f'repos/{repo}/actions/runs/{run_id}/jobs?per_page=100')
if jobs['total_count'] != len(jobs['jobs']) or not {'validate','windows'} <= {j['name'] for j in jobs['jobs'] if j['conclusion'] == 'success'}:
    raise ValueError('Complete localization and Windows/GUI/MSI test evidence is required')
records = [a for a in artifacts(repo, run_id) if a['name'] == 'windows-release']
if len(records) != 1:
    raise ValueError('Expected one original windows-release artifact')
archive(repo, records[0])  # Verify immutable GitHub artifact digest before download/extraction.
subprocess.run(['gh','run','download',run_id,'--repo',repo,'--name','windows-release','--dir','release'],check=True)
verify(Path('release'), run, sha, 'windows')
os.environ['BUILD_SHA'] = sha
os.environ['BASE_SHA'] = sha
os.environ['CANDIDATE_BRANCH'] = ''
subprocess.run([sys.executable, 'scripts/ci/publish.py'],check=True)
