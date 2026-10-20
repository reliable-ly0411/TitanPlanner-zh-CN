import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch
import zipfile

SCRIPTS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(SCRIPTS))
import upstream
from package_windows import package
from check_audit import findings


class PackageTests(unittest.TestCase):
    def test_package_requires_both_languages(self):
        with tempfile.TemporaryDirectory() as directory:
            with self.assertRaises(ValueError):
                package(Path(directory)/'empty', Path(directory)/'output', 'a'*40)

    def test_package_excludes_user_state_and_preserves_languages(self):
        with tempfile.TemporaryDirectory() as directory:
            source = Path(directory)/'input'
            for name in ['MissionPlanner.exe', 'MissionPlanner.exe.config', 'MissionPlanner.Utilities.dll',
                         'zh-Hans/MissionPlanner.resources.dll', 'zh-CN/MissionPlanner.resources.dll',
                         'zh-CN/MissionPlanner.Utilities.resources.dll', 'ProDotNetZip.dll', 'x64/libSkiaSharp.dll', 'x86/libSkiaSharp.dll',
                         'config.xml', 'config_Default.xml', 'profile.txt', 'logs/flight.tlog', 'GMapCache/private.db']:
                target=source/name;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(b'test')
            package(source, Path(directory)/'output', 'a'*40)
            with zipfile.ZipFile(next((Path(directory)/'output').glob('*.zip'))) as z:
                self.assertIn('zh-Hans/MissionPlanner.resources.dll', z.namelist())
                self.assertIn('zh-CN/MissionPlanner.resources.dll', z.namelist())
                self.assertFalse(any('private' in f or f.startswith(('config', 'logs/', 'GMapCache/')) for f in z.namelist()))
            for stale in ('DotNetZip.dll', 'SixLabors.ImageSharp.dll'):
                with self.subTest(assembly=stale):
                    (source/stale).write_bytes(b'old')
                    with self.assertRaises(ValueError):
                        package(source, Path(directory)/'output2', 'a'*40)
                    (source/stale).unlink()

    def test_audit_accepts_windows_and_cross_platform_encodings(self):
        with tempfile.TemporaryDirectory() as directory:
            audit = Path(directory)/'audit.json'
            for encoding in ('utf-8-sig', 'utf-16'):
                audit.write_text(json.dumps({'projects': [{'frameworks': []}]}), encoding=encoding)
                result = subprocess.run([sys.executable, str(SCRIPTS/'check_audit.py'), str(audit)], capture_output=True)
                self.assertEqual(0, result.returncode, result.stderr)

    def test_transitive_vulnerabilities_block_release(self):
        report={'projects':[{'frameworks':[{'transitivePackages':[{'id':'legacy','resolvedVersion':'1','vulnerabilities':[{'severity':'High','advisoryurl':'https://example.test/advisory'}]}]}]}]}
        self.assertEqual([('legacy','1','High','https://example.test/advisory')], findings(report))
        self.assertEqual([], findings({'projects':[{'frameworks':[]}]}))


class UpstreamTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name);self.original=Path.cwd();self.addCleanup(os.chdir,self.original)
        self.origin=self.root/'origin.git';self.up=self.root/'upstream.git';self.repo=self.root/'work'
        subprocess.run(['git','init','--bare','--initial-branch=master',str(self.origin)],check=True,capture_output=True)
        subprocess.run(['git','init','--bare','--initial-branch=master',str(self.up)],check=True,capture_output=True)
        subprocess.run(['git','clone',str(self.origin),str(self.repo)],check=True,capture_output=True)
        os.chdir(self.repo)
        upstream.git('config','user.email','ci@example.invalid');upstream.git('config','user.name','CI test')
        Path('base.txt').write_text('base');upstream.git('add','.');upstream.git('commit','-m','base')
        self.base=upstream.git('rev-parse','HEAD')
        upstream.git('push','origin','master');upstream.git('push',str(self.up),'master')

    def add_upstream(self, path, content):
        upstream.git('checkout','-B','upstream-work',self.base)
        p=Path(path);p.parent.mkdir(parents=True,exist_ok=True);p.write_text(content)
        upstream.git('add','.');upstream.git('commit','-m','upstream change');upstream.git('push',str(self.up),'HEAD:master')
        upstream.git('checkout','master')

    def fork_change(self, path='fork.txt', content='中文(简体)2'):
        Path(path).write_text(content);upstream.git('add','.');upstream.git('commit','-m','keep fork translation')
        upstream.git('push','origin','master')
        return upstream.git('rev-parse','HEAD')

    def test_no_change_does_not_create_candidate(self):
        self.fork_change()
        with patch.object(upstream,'output') as output:
            upstream.prepare(True,str(self.up))
            self.assertEqual('false',output.call_args.kwargs['changed'])
        self.assertEqual('',upstream.git('ls-remote','origin','refs/heads/automation/*'))

    def test_merge_preserves_fork_then_promotes_only_tested_candidate(self):
        self.add_upstream('new.txt','new upstream');base=self.fork_change()
        with patch.object(upstream,'output') as output:
            upstream.prepare(True,str(self.up));values=output.call_args.kwargs
        self.assertEqual('true',values['changed'])
        self.assertEqual('中文(简体)2',Path('fork.txt').read_text())
        self.assertEqual(base,upstream.git('ls-remote','origin','refs/heads/master').split()[0])
        upstream.promote(base,values['sha'])
        self.assertEqual(values['sha'],upstream.git('ls-remote','origin','refs/heads/master').split()[0])
        upstream.promote(base,values['sha'])  # Retry is idempotent.

    def test_conflict_keeps_master_without_pushing_unreviewed_workflows(self):
        self.add_upstream('base.txt','upstream');base=self.fork_change('base.txt','fork')
        with patch.object(upstream,'proposal') as proposal:
            with self.assertRaises(RuntimeError):upstream.prepare(True,str(self.up))
            proposal.assert_not_called()
        self.assertEqual('', upstream.git('ls-remote', 'origin', 'refs/heads/automation/*'))
        self.assertEqual(base,upstream.git('rev-parse','HEAD'))
        self.assertEqual(base,upstream.git('ls-remote','origin','refs/heads/master').split()[0])

    def test_concurrent_master_change_prevents_promotion(self):
        self.add_upstream('new.txt','new');base=self.fork_change()
        with patch.object(upstream,'output') as output:
            upstream.prepare(True,str(self.up));candidate=output.call_args.kwargs['sha']
        upstream.git('checkout','-B','master',base);self.fork_change('later.txt','user work')
        with self.assertRaises(RuntimeError):upstream.promote(base,candidate)

    def test_validated_push_can_publish_without_overwriting_newer_master(self):
        tested = self.fork_change()
        latest = self.fork_change('later.txt', 'parallel user work')
        upstream.promote(tested, tested)
        self.assertEqual(latest, upstream.git('ls-remote', 'origin', 'refs/heads/master').split()[0])

    def test_upstream_cannot_replace_release_detector_before_push(self):
        self.add_upstream('.github/release_policy/policy.py', 'foreign policy');base=self.fork_change()
        with self.assertRaisesRegex(RuntimeError, 'no candidate pushed'):
            upstream.prepare(True,str(self.up))
        self.assertEqual('', upstream.git('ls-remote','origin','refs/heads/automation/*'))
        self.assertEqual(base,upstream.git('ls-remote','origin','refs/heads/master').split()[0])

    def test_upstream_cannot_replace_release_policy_automatically(self):
        self.add_upstream('.github/workflows/foreign.yml','policy change');base=self.fork_change()
        with patch.object(upstream,'proposal') as proposal:
            with self.assertRaises(RuntimeError):upstream.prepare(True,str(self.up))
            proposal.assert_not_called()
        self.assertEqual('', upstream.git('ls-remote', 'origin', 'refs/heads/automation/*'))
        self.assertEqual(base,upstream.git('ls-remote','origin','refs/heads/master').split()[0])


if __name__ == '__main__':
    unittest.main()
