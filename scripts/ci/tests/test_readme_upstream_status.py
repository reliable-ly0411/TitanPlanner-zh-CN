import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import readme_upstream_status as status


class StatusSnapshotTests(unittest.TestCase):
    def setUp(self):
        self.refs = dict(zip(status.BRANCHES, ("stable", "dev", "titan", "mission")))
        self.calls = []

    def lookup(self, repo, base, head):
        self.calls.append((base, head))
        return {("titan", "stable"): (74, 0), ("mission", "stable"): (604, 58),
                ("titan", "dev"): (179, 0), ("mission", "dev"): (709, 58)}[(base, head)]

    def test_comparison_direction_and_visible_counts(self):
        counts = status.collect("repo", lambda: self.refs, self.lookup)
        self.assertEqual(self.calls, [("titan", "stable"), ("mission", "stable"),
                                      ("titan", "dev"), ("mission", "dev")])
        svg = ET.fromstring(status.render(counts))
        self.assertEqual([node.text for node in svg.iter("{http://www.w3.org/2000/svg}text")],
                         ["ahead / behind", "Titanmain", "missionmain",
                          "zh-CN", "74 / 0", "604 / 58", "zh-CN-dev", "179 / 0", "709 / 58"])
        self.assertIn("领先 74，落后 0", next(svg.iter("{http://www.w3.org/2000/svg}desc")).text)

    def test_moving_input_aborts_whole_snapshot(self):
        changed = {**self.refs, "zh-CN-dev": "new"}
        refs = iter((self.refs, changed))
        with self.assertRaisesRegex(RuntimeError, "changed during refresh"):
            status.collect("repo", lambda: next(refs), self.lookup)

    def test_api_schema_or_failure_does_not_return_snapshot(self):
        for body in (b'{"ahead_by":74,"behind_by":false}', b'{"ahead_by":74}',
                     b'{"ahead_by":-1,"behind_by":0}'):
            with self.subTest(body=body), patch.object(status.subprocess, "check_output", return_value=body):
                with self.assertRaises((ValueError, KeyError)):
                    status.compare("repo", "a", "b")
        with patch.object(status.subprocess, "check_output", side_effect=subprocess.CalledProcessError(1, "gh")):
            with self.assertRaises(subprocess.CalledProcessError):
                status.compare("repo", "a", "b")

    def test_orphan_status_branch_and_unchanged_tree_skip_write(self):
        with tempfile.TemporaryDirectory() as directory:
            old = Path.cwd()
            try:
                os.chdir(directory)
                status.git("init", "--quiet", "--bare", "remote.git")
                status.git("init", "--quiet", "work")
                os.chdir("work")
                status.git("remote", "add", "origin", "../remote.git")
                counts = status.collect("repo", lambda: self.refs, self.lookup)
                svg = status.render(counts)
                status.publish(svg)
                first = status.git("ls-remote", "--heads", "origin", status.STATUS_REF)
                self.assertEqual(len(first.splitlines()), 1)
                status.publish(svg)
                self.assertEqual(status.git("ls-remote", "--heads", "origin", status.STATUS_REF), first)
                counts[1][1] = (710, 58)
                status.publish(status.render(counts))
                second = status.git("ls-remote", "--heads", "origin", status.STATUS_REF)
                self.assertNotEqual(first, second)
                refs = status.git("--git-dir=../remote.git", "for-each-ref", "--format=%(refname)").decode().splitlines()
                self.assertEqual(refs, [status.STATUS_REF])
                self.assertEqual(status.git("--git-dir=../remote.git", "rev-list", "--count",
                                            status.STATUS_REF).strip(), b"2")
            finally:
                os.chdir(old)
