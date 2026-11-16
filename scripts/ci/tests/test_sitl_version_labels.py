"""SITL release labels may translate; enum values and selection wiring must not."""
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[3]


class SitlVersionLabelsTest(unittest.TestCase):
    def test_display_labels_preserve_release_values(self):
        source = (ROOT / 'GCSViews/SITL.cs').read_text(encoding='utf-8-sig')
        catalog = json.loads((ROOT / 'localization/zh-Hans/code-translations.json').read_text())
        pairs = [('Latest (Dev)', 'APFirmware.RELEASE_TYPES.DEV'),
                 ('Beta', 'APFirmware.RELEASE_TYPES.BETA'),
                 ('Stable', 'APFirmware.RELEASE_TYPES.OFFICIAL'),
                 ('Skip Download', 'null')]
        for label, value in pairs:
            self.assertIn('{ UiText.Translate("' + label + '"), ' + value + ' }', source)
            self.assertNotEqual(catalog[label], label)
        self.assertEqual(len({catalog[label] for label, _ in pairs}), 4)
        self.assertIn('cmb_version.DisplayMember = "Key";', source)
        self.assertIn('cmb_version.ValueMember = "Value";', source)
        self.assertIn('cmb_version.SelectedIndex = Settings.Instance.GetInt32("sitl_download_version");', source)


if __name__ == '__main__':
    unittest.main()
