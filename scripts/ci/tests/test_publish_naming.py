"""Publishing uses stable names while retaining the existing immutable tag policy."""
from contextlib import chdir
import json
import os
from pathlib import Path
import runpy
import sys
import tempfile
import unittest
from unittest.mock import patch
import zipfile

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import upstream
from release_artifacts import artifact_names, legacy_artifact_names, release_tag, write_manifest

PUBLISH = Path(__file__).resolve().parents[1]/'publish.py'


class PublishNamingTests(unittest.TestCase):
    def run_publish(self, existing, target=None, legacy=False):
        sha = 'a'*40
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)/'release'; root.mkdir()
            names = artifact_names(sha, '1.3.83')
            with zipfile.ZipFile(root/names[0], 'w') as archive:
                archive.writestr('BUILD-INFO.json', json.dumps({'commit': sha, 'version': '1.3.83'}))
            (root/names[1]).write_bytes(b'msi')
            write_manifest(root, sha, '1.3.83')
            if legacy:
                old_names = legacy_artifact_names(sha, '1.3.83')
                data = json.loads((root/'build-info.json').read_text())
                for entry, old in zip(data['artifacts'], old_names):
                    (root/entry['name']).rename(root/old)
                    entry['name'] = old
                (root/'build-info.json').write_text(json.dumps(data))
                (root/'SHA256SUMS').write_text(''.join(f"{e['sha256']}  {e['name']}\n" for e in data['artifacts']))
            with chdir(tmp), patch.dict(os.environ, {'BUILD_SHA': sha, 'BASE_SHA': sha,
                                        'GITHUB_REPOSITORY': 'owner/repo', 'CANDIDATE_BRANCH': ''}), \
                 patch.object(upstream, 'promote') as promote, \
                 patch.object(upstream, 'git', return_value=f'{target or sha}\trefs/tags/{release_tag(sha, "1.3.83")}'), \
                 patch.object(upstream, 'run') as run:
                from subprocess import CompletedProcess
                run.return_value = CompletedProcess([], 0 if existing else 1, stdout='release URL')
                runpy.run_path(str(PUBLISH), run_name='__main__')
                promote.assert_called_once_with(sha, sha)
                return run.call_args_list

    def test_new_release_uses_fixed_names_and_unchanged_tag(self):
        calls = self.run_publish(False)
        self.assertEqual(2, len(calls))
        args = calls[1].args
        self.assertEqual(('gh', 'release', 'create', 'v1.3.83-zh2-aaaaaaaaaaaa'), args[:4])
        self.assertEqual(('release/TitanPlanner-zh-CN.zip', 'release/TitanPlanner-zh-CN.msi'), args[4:6])

    def test_rerun_does_not_create_or_upload_again_for_either_naming_scheme(self):
        for legacy in (False, True):
            with self.subTest(legacy=legacy):
                calls = self.run_publish(True, legacy=legacy)
                self.assertEqual(1, len(calls))
                self.assertEqual(('gh', 'release', 'view'), calls[0].args[:3])

    def test_existing_tag_on_different_commit_is_rejected(self):
        with self.assertRaisesRegex(RuntimeError, 'another commit'):
            self.run_publish(True, target='b'*40)
