import importlib.util
import os
from pathlib import Path
import subprocess
import tempfile
import unittest

SPEC = importlib.util.spec_from_file_location('align_development', Path(__file__).parents[1] / 'align_development.py')
align = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(align)


class AlignmentTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.cwd = os.getcwd()
        self.root = Path(self.temp.name)
        subprocess.run(['git', 'init', '--bare', str(self.root / 'remote')], check=True, capture_output=True)
        subprocess.run(['git', 'clone', str(self.root / 'remote'), str(self.root / 'work')], check=True, capture_output=True)
        os.chdir(self.root / 'work')
        align.git('config', 'user.name', 'Test')
        align.git('config', 'user.email', 'test@example.com')
        self.commit('initial', 'common')
        align.git('branch', 'zh-CN')
        align.git('branch', 'zh-CN-dev')
        align.git('push', 'origin', 'zh-CN', 'zh-CN-dev')

    def tearDown(self):
        os.chdir(self.cwd)
        self.temp.cleanup()

    def commit(self, content, path):
        Path(path).write_text(content)
        align.git('add', path)
        align.git('commit', '-m', content)
        return align.git('rev-parse', 'HEAD')

    def advance(self, branch, content, path):
        align.git('checkout', branch)
        sha = self.commit(content, path)
        align.git('push', 'origin', branch)
        return sha

    def remote(self, branch):
        return align.git('ls-remote', 'origin', 'refs/heads/' + branch).split()[0]

    def test_equal_noop(self):
        base, sha = align.prepare()
        self.assertEqual(base, sha)
        align.publish(base, sha)
        self.assertEqual(sha, self.remote('zh-CN-dev'))

    def test_stable_ahead_fast_forward(self):
        stable = self.advance('zh-CN', 'stable', 'stable')
        base, sha = align.prepare()
        self.assertEqual(stable, sha)
        self.assertNotEqual(base, sha)
        align.publish(base, sha)
        self.assertEqual(stable, self.remote('zh-CN-dev'))

    def test_development_ahead_noop(self):
        dev = self.advance('zh-CN-dev', 'dev', 'dev')
        self.assertEqual((dev, dev), align.prepare())

    def test_divergence_preserves_both_parents(self):
        stable = self.advance('zh-CN', 'stable', 'stable')
        dev = self.advance('zh-CN-dev', 'dev', 'dev')
        base, sha = align.prepare()
        self.assertEqual(dev, base)
        self.assertEqual([dev, stable], align.git('show', '-s', '--format=%P', sha).split())
        align.publish(base, sha)
        self.assertEqual(stable, self.remote('zh-CN'))
        self.assertEqual(sha, self.remote('zh-CN-dev'))

    def test_conflict_leaves_remote_untouched(self):
        self.advance('zh-CN', 'stable', 'common')
        dev = self.advance('zh-CN-dev', 'dev', 'common')
        with self.assertRaises(RuntimeError):
            align.prepare()
        self.assertEqual(dev, self.remote('zh-CN-dev'))
        self.assertEqual('', align.git('status', '--porcelain'))

    def test_concurrent_development_update_is_not_overwritten(self):
        self.advance('zh-CN', 'stable', 'stable')
        base, sha = align.prepare()
        dev = self.advance('zh-CN-dev', 'concurrent', 'dev')
        with self.assertRaisesRegex(RuntimeError, 'advanced'):
            align.publish(base, sha)
        self.assertEqual(dev, self.remote('zh-CN-dev'))
