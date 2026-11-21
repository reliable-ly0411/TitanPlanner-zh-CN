"""Metadata regression only; does not execute FlightPlanner or a vehicle."""
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
FIELDS = ["P1", "P2", "P3", "P4", "X", "Y", "Z"]


class MissionCommandMetadataTest(unittest.TestCase):
    def test_desktop_commands_are_unique_per_vehicle(self):
        root = ET.parse(ROOT / "mavcmd.xml").getroot()
        for vehicle in root:
            keys = [command.tag for command in vehicle]
            with self.subTest(vehicle=vehicle.tag):
                self.assertEqual(len(keys), len(set(keys)), "duplicate command metadata")
                for command in vehicle:
                    self.assertEqual([f.tag for f in command], FIELDS)

    def test_return_path_preserves_current_effective_metadata(self):
        root = ET.parse(ROOT / "mavcmd.xml").getroot()
        plane = root.find("APM")
        commands = list(plane)
        node = plane.find("DO_RETURN_PATH_START")
        self.assertIsNotNone(node)
        self.assertEqual([f.text or "" for f in node],
                         ["", "", "", "", "Lat", "Long", ""])
        self.assertTrue(all(not f.attrib for f in node))
        # Keep the original first-insertion menu position, not just field values.
        keys = [c.tag for c in commands]
        self.assertEqual(keys.index("DO_RETURN_PATH_START"),
                         keys.index("DO_GRIPPER") + 1)
        copter = root.find("AC2/DO_RETURN_PATH_START")
        self.assertEqual([f.text or "" for f in copter], [""] * 7)
        self.assertIsNone(root.find("APRover/DO_RETURN_PATH_START"))


if __name__ == "__main__":
    unittest.main()
