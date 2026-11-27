"""Source contracts for display-only telemetry localization; rendered QA is separate."""
import json
import re
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET

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

    def test_velocity_unit_correction_is_limited_to_three_fields(self):
        source = (ROOT / 'Controls/TelemetryFieldPresentation.cs').read_text()
        self.assertIn('if (key == "vx" || key == "vy" || key == "vz")', source)
        self.assertIn('description.Replace("(ms)", "(m/s)")', source)
        state = (ROOT / 'ExtLibs/ArduPilot/CurrentState.cs').read_text()
        for field in ('vx', 'vy', 'vz'):
            self.assertIn(field + ' = loc.' + field + ' * 0.01;', state)
        # Genuine time labels keep milliseconds, rather than a global string replacement.
        catalogs = {}
        for name in ('ui-translations.json', 'code-translations.json'):
            catalogs.update(json.loads((ROOT / 'localization/zh-Hans' / name).read_text()))
        for label in ('Delay (ms)', 'Shutter lag (ms)', 'Max Window (ms)'):
            self.assertIn('(ms)', catalogs[label])

    def test_gimbal_caption_axes_match_both_telemetry_sources(self):
        definitions = ROOT / 'ExtLibs/Mavlink/message_definitions'
        mount = ET.parse(definitions / 'ardupilotmega.xml').find("./messages/message[@name='MOUNT_STATUS']")
        self.assertIsNotNone(mount)
        state = (ROOT / 'ExtLibs/ArduPilot/CurrentState.cs').read_text(encoding='utf-8-sig')
        code = (ROOT / 'Controls/TelemetryFieldPresentation.cs').read_text()
        catalog = json.loads((ROOT / 'localization/zh-Hans/code-translations.json').read_text())

        def case(name):
            start = state.index('case (uint)MAVLink.MAVLINK_MSG_ID.' + name + ':')
            return state[start:state.index('case (uint)MAVLink.MAVLINK_MSG_ID.', start + 1)]

        for suffix, axis, zh_axis in (('a', 'pitch', '俯仰'), ('b', 'roll', '横滚'), ('c', 'yaw', '偏航')):
            key = 'campoint' + suffix
            legacy_field = mount.find("./field[@name='pointing_" + suffix + "']")
            self.assertIsNotNone(legacy_field)
            self.assertEqual(('cdeg', axis), (legacy_field.get('units'), legacy_field.text.strip('.').lower()))
            self.assertIn(key + ' = status.pointing_' + suffix + ' / 100.0f;', case('MOUNT_STATUS'))
            self.assertIn(key + ' = (float)(q.get_euler_' + axis + '() * (180.0 / Math.PI));',
                          case('GIMBAL_DEVICE_ATTITUDE_STATUS'))
            label = 'Gimbal ' + axis + ' angle (°)'
            self.assertIn('case "' + key + '": return UiText.Translate("' + label + '");', code)
            self.assertEqual('云台' + zh_axis + '角（°）', catalog[label])

    def test_binding_and_selection_stay_on_original_key(self):
        status = (ROOT / 'Controls/StatusControl.cs').read_text()
        chart = (ROOT / 'Controls/DisplayThisForm.cs').read_text()
        self.assertIn('GetProperty(item.Name)', status)
        self.assertIn('Name = field.Name,', chart)
        self.assertIn('IsChecked = _isChecked(field.Name)', chart)
        self.assertIn('Name = item.Name,', chart)
        # Status draws a measured label derived from the original field and keeps
        # the complete label and value available in the tooltip.
        self.assertIn('TelemetryFieldPresentation.Matches(searchText', status)
        self.assertIn('Name = field, DisplayName = TelemetryFieldPresentation.FieldLabel(field)', status)
        self.assertIn('column.Labels[row] = FitName(item.DisplayName ?? item.Name ?? "", nameWidths[start + row]', status)
        self.assertIn('g.DrawString(column.Labels[row],', status)
        self.assertIn('g.DrawString(item.Value ?? "",', status)
        self.assertIn('item.DisplayName + Environment.NewLine + item.Value', status)
        self.assertIn('_contentPanel.AutoScrollPosition = Point.Empty;', status)
        self.assertIn('TelemetryFieldPresentation.Matches(text, item.DisplayName, item.Name, item.Group, item.GroupLabel)', chart)
        self.assertIn('g.DrawString(row.Label, _normalFont, nameBrush, row.NameBounds, format)', chart)
        self.assertIn('g.DrawString(row.Item.Value ?? "", _normalFont, valueBrush, row.ValueBounds, format)', chart)
        self.assertIn('_contentPanel.AutoScrollPosition = Point.Empty;', chart)
        for source in (status, chart):
            self.assertIn('MeasureString(item.DisplayName', source)
            self.assertIn('GetHeight(graphics)', source)
            self.assertIn('_contentPanel.AutoScrollMinSize', source)
        self.assertIn('new Size(x - _columnGap + _padding, bottom + _padding)', chart)

    def test_group_catalog_does_not_reuse_distance_terrain(self):
        catalog = json.loads((ROOT / 'localization/zh-Hans/code-translations.json').read_text())
        self.assertEqual(catalog['Telemetry group: Terrain'], '地形')
        self.assertEqual(catalog['Telemetry group: Other'], '其他')
        self.assertEqual(catalog['Sat Count Blend'], '两个 GPS 卫星数量之和')
        self.assertEqual(len([k for k in catalog if k.startswith('Telemetry group: ')]), 26)


if __name__ == '__main__':
    unittest.main()
