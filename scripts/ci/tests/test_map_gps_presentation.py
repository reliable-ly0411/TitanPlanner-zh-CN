"""Offline source and generated-resource contracts; rendered WinForms QA is separate."""
import json
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]

def source(path):
    return (ROOT / path).read_text(encoding="utf-8-sig")

class MapGpsPresentationTest(unittest.TestCase):
    def test_fix_labels_match_vendored_dialect_and_remain_distinct(self):
        xml = ET.parse(ROOT / "ExtLibs/Mavlink/message_definitions/common.xml")
        entries = xml.findall("./enums/enum[@name='GPS_FIX_TYPE']/entry")
        self.assertEqual(list(range(9)), [int(e.get('value')) for e in entries][:9])
        self.assertEqual(["GPS_FIX_TYPE_DGPS", "GPS_FIX_TYPE_RTK_FLOAT", "GPS_FIX_TYPE_RTK_FIXED",
                          "GPS_FIX_TYPE_STATIC", "GPS_FIX_TYPE_PPP"], [e.get('name') for e in entries[4:9]])
        data = json.loads(source("localization/zh-Hans/code-translations.json"))
        labels = [data["GPS fix: " + name] for name in ("DGPS", "RTK Float", "RTK Fixed", "STATIC", "PPP")]
        self.assertEqual(len(set(labels)), len(labels))
        self.assertIn("静态", labels[3])
        self.assertNotIn("RTK", labels[3])

    def test_exact_culture_and_unknown_fallback_present_in_source(self):
        s = source("ExtLibs/Utilities/MapGpsPresentation.cs")
        self.assertIn('string.Equals(CultureInfo.CurrentUICulture.Name, "zh-CN", StringComparison.Ordinal)', s)
        self.assertIn('if (!Enhanced) return raw;', s)
        self.assertIn('if (code < 0 || code > 8) return raw;', s)
        self.assertIn('default: return code.ToString();',s)
        self.assertIn('if (raw != "GEO" && raw != "UTM" && raw != "MGRS") return raw;',s)
        self.assertIn('if (!Enhanced || string.IsNullOrEmpty(raw)) return fallback;',s)

    def test_toolbar_keeps_both_prechange_width_budgets_and_popup_uses_raw_ids(self):
        s = source("Controls/ToolStripVehicleState.cs")
        self.assertEqual(2, s.count('MeasureText(_gpsLayoutText ?? _gpsSatsLabel.Text'))
        self.assertIn('_gpsLayoutText = legacy;', s)
        self.assertIn('_gpsSatsLabel.Size = new Size(limit, size.Height);', s)
        self.assertIn('<= limit ? name : compact', s)
        self.assertIn('_gpsSatsLabel.AutoEllipsis = true;', s)
        self.assertIn('GetFixColor(bestFix)', s)
        self.assertIn('Math.Max((int)gpsStatus, (int)gpsStatus2)', s)
        self.assertIn('public void UpdateValues(int satCount, int gpsFix, int satCount2, int gpsFix2', s)
        self.assertIn('satCount2 > 0 || gpsFix2 != 0', s)

    def test_coordinate_binding_and_scale_arithmetic_stay_on_original_path(self):
        s = source("ExtLibs/Controls/Coords.cs")
        self.assertIn('get { return CMB_coordsystem.Text; }', s)
        self.assertIn('set { CMB_coordsystem.Text = value; }', s)
        self.assertIn('CMB_coordsystem.DataSource = Enum.GetNames(typeof(CoordsSystems));', s)
        self.assertNotIn('combo.DropDownStyle =',s)
        self.assertIn('combo.ItemHeight = rowHeight;',s)
        self.assertIn('DrawItemState.ComboBoxEdit) != 0 ? raw',s)
        self.assertIn('MGRS mgrs = (MGRS)point;',s)
        self.assertIn('UTM utm = (UTM)point;',s)
        s = source("ExtLibs/Maps/GMapMarkerElevation.cs")
        self.assertIn('scaleHigh = high;',s)
        self.assertIn('scaleLow = low;',s)
        self.assertIn('MapGpsPresentation.ScaleTitle(scaleText)',s)
        self.assertIn('g.DrawImage(test, 0, 60, 300, 20);',s)

    def test_only_enhanced_satellite_contains_contextual_map_gps_labels(self):
        catalogs = {language: {
            json.loads(e.findtext("comment")): e.findtext("value") for e in
            ET.parse(ROOT / ("ExtLibs/Utilities/Resources/UiText" + language + ".resx")).findall("data")
        } for language in ("", ".zh-Hans", ".zh-CN")}
        expected = [k for k in catalogs[".zh-CN"] if k.startswith(("Map legend: ", "Map coordinates: ", "Map altitude scale: ", "GPS fix: ", "GPS compact: "))]
        self.assertEqual(27, len(expected))
        self.assertTrue(all(k not in catalogs[""] and k not in catalogs[".zh-Hans"] for k in expected))

if __name__ == "__main__":
    unittest.main()
