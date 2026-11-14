import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import unittest
from unittest.mock import patch
import subprocess
import mirror_upstreams as mirror

class MirrorTests(unittest.TestCase):
    def test_exact_ref_is_not_pushed(self):
        with patch.object(mirror, 'MIRRORS', [('Titanmain', 'upstream')]), patch.object(mirror, 'git', side_effect=['', 'a', 'a refs/heads/Titanmain', '', 'a']) as git:
            mirror.main()
        self.assertFalse(any(call.args[0] == 'push' for call in git.call_args_list))

    def test_divergence_is_not_force_pushed(self):
        with patch.object(mirror, 'MIRRORS', [('Titanmain', 'upstream')]), patch.object(mirror, 'git', side_effect=['', 'b', 'a refs/heads/Titanmain', '', 'a']) as git, patch.object(mirror.subprocess, 'run', side_effect=subprocess.CalledProcessError(1, 'merge-base')):
            with self.assertRaises(subprocess.CalledProcessError):
                mirror.main()
        self.assertFalse(any(call.args[0] == 'push' for call in git.call_args_list))
