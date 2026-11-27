"""Offline source and packaged Copter metadata contract for #46."""
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
FENCE = ROOT / "GCSViews/ConfigurationView/ConfigAC_Fence.cs"

class FenceTypeReadbackTest(unittest.TestCase):
    def test_packaged_copter_metadata_is_explicitly_a_bitmask(self):
        node = ET.parse(ROOT / "ParameterMetaDataBackup.xml").find(".//ArduCopter2/FENCE_TYPE")
        self.assertIsNotNone(node)
        self.assertEqual("0:Max altitude,1:Circle,2:Polygon,3:Min altitude", node.findtext("Bitmask"))
        self.assertIsNone(node.find("Values"))

    def test_writable_combo_keeps_legacy_values_binding(self):
        text = FENCE.read_text(encoding="utf-8-sig")
        self.assertEqual(text.count("mavlinkComboBox1.setup("), 1)
        self.assertIn('ParameterMetaDataRepository.GetParameterOptionsInt("FENCE_TYPE",', text)
        self.assertIn('MainV2.comPort.MAV.cs.firmware.ToString()), "FENCE_TYPE", MainV2.comPort.MAV.param);', text)
        readback = text.split("private void UpdateFenceTypeReadback", 1)[1].split("private void LayoutFenceTypeReadback", 1)[0]
        self.assertIn("Firmwares.ArduCopter2", readback)
        self.assertIn('CurrentUICulture.Name, "zh-CN"', readback)
        self.assertIn('GetParameterBitMaskInt("FENCE_TYPE"', readback)
        self.assertIn("1 << match.Key", readback)
        self.assertNotIn("mavlinkComboBox", readback)
        self.assertNotIn("setParam", readback)
        self.assertNotIn("getParamList", readback)

if __name__ == "__main__":
    unittest.main()
