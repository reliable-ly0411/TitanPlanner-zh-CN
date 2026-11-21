"""Check the real mission metadata, including numeric choices in dynamic headers."""
import json
from pathlib import Path
import re
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]


class MissionCommandTranslationsTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.metadata = ET.parse(ROOT / 'mavcmd.xml').getroot()
        cls.catalog = json.loads((ROOT / 'localization/zh-Hans/code-translations.json').read_text())

    def test_all_vehicle_command_choices_have_scoped_chinese_explanations(self):
        commands = {command.tag for firmware in self.metadata for command in firmware}
        commands.update(('FENCE_RETURN_POINT', 'FENCE_POLYGON_VERTEX_INCLUSION',
                         'FENCE_POLYGON_VERTEX_EXCLUSION', 'FENCE_CIRCLE_INCLUSION',
                         'FENCE_CIRCLE_EXCLUSION', 'RALLY_POINT', 'UNKNOWN'))
        for command in sorted(commands):
            with self.subTest(command=command):
                self.assertRegex(self.catalog['Mission command: ' + command], r'[\u4e00-\u9fff]')

    def test_all_nonempty_parameter_headers_are_covered_without_rewriting_numeric_choices(self):
        headers = {p.text for firmware in self.metadata for command in firmware for p in command if p.text}
        headers.update(('Points', 'Radius (m)', 'setme'))
        protocol_labels = {'ID', 'ISO', 'P1', 'P2', 'P3', 'PWM'}
        for header in sorted(headers):
            with self.subTest(header=header):
                translated = self.catalog['Mission parameter: ' + header]
                self.assertEqual(re.findall(r'-?\d+(?:\.\d+)?', header),
                                 re.findall(r'-?\d+(?:\.\d+)?', translated))
                if header in protocol_labels:
                    self.assertEqual(header, translated)
                else:
                    self.assertRegex(translated, r'[\u4e00-\u9fff]')


if __name__ == '__main__':
    unittest.main()
