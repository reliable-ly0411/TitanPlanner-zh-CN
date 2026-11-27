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
        cls.control = (ROOT / "Controls/ToolStripVehicleState.cs").read_text(encoding="utf-8-sig")
        cls.menu = cls.control.split(
            "internal class ModeDropdownPopup : Form", 1)[1].split("internal class GpsStatusPopup : Form", 1)[0]
        cls.helper = (ROOT / "ExtLibs/Utilities/FlightModeDisplay.cs").read_text(encoding="utf-8")
        cls.known = set(re.findall(r'"([^"]+)"', cls.helper.split("KnownMenuModes =", 1)[1].split("};", 1)[0]))
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

    def test_mode_context_is_display_only(self):
        show = self.control.split("private void ShowDropdown()", 1)[1].split("var screenPoint", 1)[0]
        self.assertIn("var mav = MainV2.comPort?.MAV;", show)
        self.assertIn("_popup.MenuPresentation = GetModeMenuPresentation(mav?.cs?.firmware, mav?.aptype);", show)
        self.assertIn("ArduPilot.Common.getModesList(mav)", show)
        selector = self.control.split("internal static ModeMenuPresentation GetModeMenuPresentation", 1)[1].split(
            "private void ShowDropdown()", 1)[0]
        self.assertRegex(selector, r"case ArduPilot.Firmwares.ArduRover:\s*return ModeMenuPresentation.Rover;")
        self.assertIn("return ModeMenuPresentation.Unspecified;", selector)
        secondary = self.helper.split("public static string EnglishThenChinese", 1)[1].split(
            "public static FlightModeDisplay FromMode", 1)[0]
        self.assertIn('string.Equals(mode, "Loiter", StringComparison.Ordinal)', secondary)
        self.assertIn('string.Equals(mode, "Acro", StringComparison.Ordinal)', secondary)
        self.assertIn("presentation == ModeMenuPresentation.Rover", secondary)
        self.assertIn('CurrentUICulture.Name == "zh-CN"', secondary)
        self.assertIn("!KnownMenuModes.Contains(mode)", secondary)

    def test_px4_helicopter_uses_existing_hover_caption_without_vtol_guess(self):
        selector = self.control.split("internal static ModeMenuPresentation GetModeMenuPresentation", 1)[1].split(
            "private void ShowDropdown()", 1)[0]
        px4 = selector.split("case ArduPilot.Firmwares.PX4:", 1)[1]
        self.assertRegex(px4, r"MAV_TYPE\.FIXED_WING:\s*return ModeMenuPresentation\.Circle;")
        hover = px4.split("case MAVLink.MAV_TYPE.QUADROTOR:", 1)[1].split(
            "return ModeMenuPresentation.Hover;", 1)[0]
        self.assertIn("case MAVLink.MAV_TYPE.HELICOPTER:", hover)
        self.assertNotIn("case MAVLink.MAV_TYPE.VTOL_", px4)
        self.assertIn("return ModeMenuPresentation.Unspecified;", px4)

    def test_menu_keeps_raw_names_and_uses_bilingual_text_only_for_display(self):
        self.assertEqual(1, self.menu.count("FlightModeDisplay.EnglishThenChinese(mode.Value, MenuPresentation)"))
        self.assertIn("fltOrder.ContainsKey(m.Value)", self.menu)
        self.assertIn(".ThenBy(m => m.Value)", self.menu)
        self.assertGreaterEqual(self.menu.count("Tag = mode.Value"), 3)
        dispatch = self.menu.split("private void SelectModeRow(KeyboardModeRow row)", 1)[1].split("private sealed class", 1)[0]
        self.assertIn("ModeSelected?.Invoke(this, (string)row.Tag)", dispatch)
        self.assertEqual(1, self.menu.count("ModeSelected?.Invoke("))
        self.assertIn("row.Click += (s, e) => SelectModeRow(row);", self.menu)
        self.assertIn("modeLabel.Click += (s, e) => SelectModeRow(row);", self.menu)
        keyboard = self.menu.split("if (keyData == Keys.Enter)", 1)[1].split("if (rows.Count == 0)", 1)[0]
        self.assertIn("var index = rows.FindIndex(row => row.Focused);", self.menu)
        self.assertIn("if (index < 0)", keyboard)
        self.assertIn("SelectModeRow(rows[index]);", keyboard)
        self.assertRegex(self.menu, r"new KeyboardModeRow\s*\{\s*Tag = mode.Value,")
        self.assertIn("PinToggled?.Invoke(this, mode.Value)", self.menu)
        self.assertIn("FavoriteToggled?.Invoke(this, mode.Value)", self.menu)
        self.assertIn("TextFormatFlags.WordBreak", self.menu)


if __name__ == "__main__":
    unittest.main()
