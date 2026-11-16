"""Source contracts for display-only telemetry localization; rendered QA is separate."""
import json
import re
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[3]


class TelemetryDisplayTest(unittest.TestCase):
    def test_every_current_state_description_has_chinese_and_retains_units(self):
        source = (ROOT / 'ExtLibs/ArduPilot/CurrentState.cs').read_text()
        entries = {}
        for name in ('ui-translations.json', 'code-translations.json'):
            entries.update(json.loads((ROOT / 'localization/zh-Hans' / name).read_text()))
        descriptions = re.findall(r'DisplayText\("([^"]+)"', source)
        self.assertGreaterEqual(len(descriptions), 200)
        for description in descriptions:
            translated = entries[description]
            self.assertRegex(translated, r'[\u4e00-\u9fff]', description)
            # Preserve upstream placeholders and numeric unit spellings, even unusual ones.
            for unit in re.findall(r'\([^)]*\)', description):
                aliases = {"(min.sec)": "(分:秒)", "(Deg)": "(°)", "(mah)": "(mAh)",
                           "(Amps)": "(A)", "(mah/km)": "(mAh/km)"}
                self.assertTrue(unit in translated or aliases.get(unit, unit) in translated, description)

    def test_abstract_unit_labels_do_not_infer_unit_settings(self):
        source = (ROOT / 'Controls/TelemetryFieldPresentation.cs').read_text()
        for setting in ('CurrentState.DistanceUnit', 'CurrentState.SpeedUnit', 'CurrentState.AltUnit'):
            self.assertNotIn(setting, source)
        for placeholder in ('dist', 'alt', 'speed', 'speed/s'):
            self.assertIn('Replace(" (' + placeholder + ')", "")', source)

    def test_binding_and_selection_stay_on_original_key(self):
        status = (ROOT / 'Controls/StatusControl.cs').read_text()
        chart = (ROOT / 'Controls/DisplayThisForm.cs').read_text()
        self.assertIn('GetProperty(item.Name)', status)
        self.assertIn('Name = field.Name,', chart)
        self.assertIn('IsChecked = _isChecked(field.Name)', chart)
        self.assertIn('Name = item.Name,', chart)
        for source in (status, chart):
            self.assertIn('TelemetryFieldPresentation.Matches(searchText', source)
            self.assertIn('g.DrawString(item.DisplayName,', source)
            self.assertIn('MeasureString(item.DisplayName', source)
            self.assertIn('GetHeight(graphics)', source)
            self.assertIn('_contentPanel.AutoScrollMinSize', source)
        self.assertIn('lastOccupiedColumn + 1', chart)

    def test_group_catalog_does_not_reuse_distance_terrain(self):
        catalog = json.loads((ROOT / 'localization/zh-Hans/code-translations.json').read_text())
        self.assertEqual(catalog['Telemetry group: Terrain'], '地形')
        self.assertEqual(catalog['Telemetry group: Other'], '其他')
        self.assertEqual(catalog['Sat Count Blend'], '两个 GPS 卫星数量之和')
        self.assertEqual(len([k for k in catalog if k.startswith('Telemetry group: ')]), 26)


if __name__ == '__main__':
    unittest.main()
