"""MAVFTP localization must wrap display text without changing mount operations."""
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[3]


class MavFtpTranslationTest(unittest.TestCase):
    def test_mount_values_and_cancel_flow_are_preserved(self):
        source = (ROOT / 'Controls/MavFTPUI.cs').read_text(encoding='utf-8-sig')
        self.assertIn('MavFtpDokan.Mount(_mavftp, mountPoint);', source)
        self.assertIn('MavFtpDokan.Unmount(DefaultMountPoint);', source)
        self.assertIn('string mountPoint = DefaultMountPoint;', source)
        self.assertIn('if (dr != DialogResult.OK)\n                    return;', source)
        self.assertIn(' + mountPoint;', source)
        self.assertIn(' + ex.Message', source)
        designer = (ROOT / 'Controls/MavFTPUI.Designer.cs').read_text()
        self.assertIn('this.btnMountFuse.Text = UiText.Translate("Mount as Drive");', designer)

    def test_mount_labels_have_enhanced_translations(self):
        catalog = json.loads((ROOT / 'localization/zh-Hans/code-translations.json').read_text())
        for term in ('Mount as Drive', 'Unmount Drive', 'Mount Point', 'FUSE drive unmounted',
                     'Failed to unmount: ', 'FUSE drive mounted at ', 'Failed to mount: ',
                     'Enter drive letter or path (e.g. M:\\)', '\n\nMake sure Dokan driver is installed.'):
            self.assertNotEqual(catalog[term], term)
        self.assertIn('M:\\', catalog['Enter drive letter or path (e.g. M:\\)'])
        self.assertIn('Dokan', catalog['\n\nMake sure Dokan driver is installed.'])


if __name__ == '__main__':
    unittest.main()
