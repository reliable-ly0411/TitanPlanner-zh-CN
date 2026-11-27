"""Ensure HUD readout strings are contextual and gated to enhanced Chinese."""
import json
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET


ROOT = Path(__file__).resolve().parents[3]


class HudReadoutTranslationTest(unittest.TestCase):
    def test_only_enhanced_locale_has_readout_translations(self):
        expected = {"HUD readout: WP": "航点", "HUD readout: Wind": "风"}
        for culture in ("", ".zh-Hans", ".zh-CN"):
            path = ROOT / f"ExtLibs/Utilities/Resources/UiText{culture}.resx"
            labels = {
                json.loads(item.findtext("comment")): item.findtext("value")
                for item in ET.parse(path).findall("data")
                if json.loads(item.findtext("comment")).startswith("HUD readout: ")
            }
            self.assertEqual(expected if culture == ".zh-CN" else {}, labels)

    def test_live_readout_keeps_units_indices_and_directions(self):
        hud = (ROOT / "ExtLibs/Controls/HUD.cs").read_text(encoding="utf-8-sig")
        self.assertIn('if (!MapGpsPresentation.Enhanced) return label;', hud)
        self.assertIn('return localized == key ? label : localized;', hud)
        self.assertIn('var wpLabel = HudReadoutLabel("WP");', hud)
        self.assertIn('wpLabel += " " + _wpno;', hud)
        self.assertIn('DrawArrowReadout(wpLabel, newdist > 0 ? _bearingtowp : (float?)null, newdist + newdistunit,', hud)
        self.assertIn('DrawArrowReadout(HudReadoutLabel("Wind"), _windSpeed < 0.05f ? (float?)null : _windDirection + 180,', hud)
        self.assertIn('_windSpeed.ToString("0.0") + speedunit, x, y, fontsize);', hud)
        for marker in ("↻", "H", "G"):
            self.assertIn(f'wpLabel = "{marker}";', hud)


if __name__ == "__main__":
    unittest.main()
