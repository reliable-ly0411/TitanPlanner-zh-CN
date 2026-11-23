"""Offline coverage of the top-bar mode label catalog (not a transport test)."""
import hashlib
import json
import re
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]


class TopBarFlightModesTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.common = (ROOT / "ExtLibs/ArduPilot/Common.cs").read_text(encoding="utf-8-sig")
        cls.acronyms = set(re.findall(r'"([^"]+)"', cls.common.split("ModeAcronyms", 1)[1].split("};", 1)[0]))
        cls.mavlink = (ROOT / "ExtLibs/Mavlink/Mavlink.cs").read_text(encoding="utf-8-sig")
        cls.params = ET.parse(ROOT / "ParameterMetaDataBackup.xml").getroot()
        cls.catalog = json.loads((ROOT / "localization/zh-Hans/code-translations.json").read_text(encoding="utf-8"))
        cls.menu = (ROOT / "Controls/ToolStripVehicleState.cs").read_text(encoding="utf-8-sig").split(
            "internal class ModeDropdownPopup : Form", 1)[1].split("internal class GpsStatusPopup : Form", 1)[0]
        helper = (ROOT / "ExtLibs/Utilities/FlightModeDisplay.cs").read_text(encoding="utf-8")
        cls.known = set(re.findall(r'"([^"]+)"', helper.split("KnownMenuModes =", 1)[1].split("};", 1)[0]))
        cls.satellite = {v.attrib["name"]: v.findtext("value") for v in
                         ET.parse(ROOT / "ExtLibs/Utilities/Resources/UiText.zh-CN.resx").getroot().findall("data")}

    def formatted(self, raw):
        # Mirror the present Common.FormatModeName algorithm only for offline fixture extraction.
        return " ".join(p.upper() if p.upper() in self.acronyms else p[:1].upper() + p[1:].lower()
                        for p in raw.replace("_", " ").split(" "))

    def test_firmware_modes_have_known_labels(self):
        modes = set()
        for firmware, param in (("ArduCopter2", "FLTMODE1"), ("ArduPlane", "FLTMODE1"), ("ArduRover", "MODE1")):
            values = self.params.findtext(f"{firmware}/{param}/Values")
            self.assertTrue(values)
            options = [mode.split(":", 1) for mode in values.split(",")]
            self.assertTrue(all(key.isdigit() for key, _ in options))
            modes.update(self.formatted(raw) for _, raw in options)
        for name in ("COPTER_MODE", "PLANE_MODE", "ROVER_MODE", "SUB_MODE"):
            body = re.search(r"public enum " + name + r"\b.*?\{(.*?)^    \}", self.mavlink, re.M | re.S)[1]
            signals = re.findall(r"^\s*([A-Z][A-Z0-9_]+)\s*=\s*\d+\s*,", body, re.M)
            self.assertTrue(signals)
            modes.update(self.formatted(s) for s in signals if s != "END")
        px4 = self.common.split("if (firmware == Firmwares.PX4)", 1)[1].split("else if", 1)[0]
        tracker = self.common.split("else if (firmware == Firmwares.ArduTracker)", 1)[1].split("return null;", 1)[0]
        for group in (px4, tracker):
            signals = re.findall(r',\s*"([^"]+)"\)', group)
            self.assertGreaterEqual(len(signals), 6)
            modes.update(self.formatted(s) for s in signals)
        # Plane adds a fixed spelling to its metadata-derived options.
        modes.add(self.formatted("INITIALISING"))
        self.assertGreaterEqual(len(modes), 68)
        self.assertEqual(set(), modes - self.known)
        self.assertNotIn("Settings", self.known)

    def test_generated_resource_contains_real_chinese_for_modes_and_headings(self):
        for value in self.known | {"Configured modes", "Flight modes", "Pin All"}:
            with self.subTest(value=value):
                localized = self.catalog[value]
                self.assertRegex(localized, r"[\u4e00-\u9fff]")
                self.assertEqual(localized, self.satellite[hashlib.sha256(value.encode("utf-8")).hexdigest()])

    def test_menu_keeps_raw_names_and_uses_bilingual_text_only_for_display(self):
        self.assertEqual(1, self.menu.count("FlightModeDisplay.EnglishThenChinese(mode.Value)"))
        self.assertIn("fltOrder.ContainsKey(m.Value)", self.menu)
        self.assertIn(".ThenBy(m => m.Value)", self.menu)
        self.assertGreaterEqual(self.menu.count("Tag = mode.Value"), 3)
        self.assertEqual(2, self.menu.count("ModeSelected?.Invoke(this, mode.Value)"))
        self.assertIn("PinToggled?.Invoke(this, mode.Value)", self.menu)
        self.assertIn("FavoriteToggled?.Invoke(this, mode.Value)", self.menu)
        self.assertIn("TextFormatFlags.WordBreak", self.menu)


if __name__ == "__main__":
    unittest.main()
