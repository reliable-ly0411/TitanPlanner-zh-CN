"""Source and offline metadata contract for the bounded #52 caption change."""
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
PAGE = ROOT / "GCSViews/ConfigurationView/ConfigRadioOutput.cs"
PRESENTATION = PAGE.with_name("ServoFunctionCaptions.cs")


class ServoFunctionCaptionsTest(unittest.TestCase):
    def test_reviewed_metadata_is_shared_by_three_supported_firmwares(self):
        root = ET.parse(ROOT / "ParameterMetaDataBackup.xml")
        for firmware in ("ArduCopter2", "ArduPlane", "ArduRover"):
            raw = root.findtext(f".//{firmware}/SERVO1_FUNCTION/Values")
            with self.subTest(firmware=firmware):
                options = dict(item.split(":", 1) for item in raw.split(","))
                self.assertEqual(["Disabled", "Motor2", "Alarm"], [options[key] for key in ("0", "34", "138")])

    def test_format_hook_uses_one_original_metadata_list_at_existing_binding(self):
        page = PAGE.read_text(encoding="utf-8-sig")
        source = PRESENTATION.read_text(encoding="utf-8-sig")
        self.assertIn('GetParameterOptionsInt(servo + "_FUNCTION", firmware.ToString())', page)
        self.assertIn('ServoFunctionCaptions.Attach(func1, functions, firmware);', page)
        self.assertIn('func1.setup(functions, servo + "_FUNCTION", MainV2.comPort.MAV.param);', page)
        self.assertEqual(1, page.count("func1.setup("))
        self.assertIn("combo.Format +=", source)
        self.assertNotIn("DataSource =", source)
        self.assertNotIn("SelectedValue =", source)
        self.assertNotIn("setParam", source)
        self.assertNotIn("ParamWriteQueue", source)


if __name__ == "__main__":
    unittest.main()
