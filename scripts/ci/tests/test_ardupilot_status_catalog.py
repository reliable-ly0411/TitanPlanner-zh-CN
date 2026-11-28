"""Verify that the checked-in matcher agrees with the localization catalog."""
from pathlib import Path
import subprocess
import sys
import unittest


class ArduPilotStatusCatalogTests(unittest.TestCase):
    def test_generated_catalog_is_current(self):
        root = Path(__file__).resolve().parents[3]
        subprocess.run([sys.executable, "scripts/localization/build_ardupilot_status_catalog.py", "--check"],
                       cwd=root, check=True, capture_output=True, text=True)


if __name__ == "__main__":
    unittest.main()
