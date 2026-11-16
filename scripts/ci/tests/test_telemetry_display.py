"""Source contracts for display-only telemetry localization; rendered QA is separate."""
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[3]


class TelemetryDisplayTest(unittest.TestCase):
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
        self.assertEqual(len([k for k in catalog if k.startswith('Telemetry group: ')]), 26)


if __name__ == '__main__':
    unittest.main()
