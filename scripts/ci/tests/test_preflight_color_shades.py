"""The enhanced checklist color captions keep each original System.Drawing token."""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[3]
LABELS = {'DarkGreen': '深绿色', 'DarkRed': '深红色', 'Gold': '金色',
          'LightBlue': '浅蓝色', 'LightGreen': '浅绿色', 'OrangeRed': '橙红色'}


class PreflightColorShadesTests(unittest.TestCase):
    def setUp(self):
        self.source = (ROOT / 'Controls/PreFlight/CheckListInput.cs').read_text(encoding='utf-8-sig')

    def test_six_new_captions_preserve_english_tokens_and_unknown_fallback(self):
        chooser = self.source.split('static string PresentColor(', 1)[1]
        chooser = chooser.split('private static int ColorSwatchWidth(', 1)[0]
        entries = re.findall(r'case "(\w+)": return token \+ " · ([^"]+)";', chooser)
        labels = dict(entries)
        self.assertEqual(len(entries), len(labels))
        self.assertEqual(22, len(labels))
        self.assertEqual(LABELS, {key: labels[key] for key in LABELS})
        self.assertIn('default: return token;', chooser)

    def test_formatting_only_changes_enhanced_chinese_display(self):
        setup = self.source.split('void SetupColorPresentation()', 1)[1].split('static string PresentColor(', 1)[0]
        self.assertIn('CultureInfo.CurrentUICulture.Name, "zh-CN", StringComparison.Ordinal', setup)
        self.assertIn('args.Value = PresentColor(token);', setup)
        for index in (1, 2):
            self.assertIn(f'CMB_colour{index}.DataSource = Enum.GetNames(typeof(KnownColor));', self.source)
        self.assertIn('Color.FromName((string)item)', self.source)
        self.assertIn('CMB_colour1.SelectedItem = CheckListItem.TrueColor;', self.source)
        self.assertIn('CMB_colour2.SelectedItem = CheckListItem.FalseColor;', self.source)


if __name__ == '__main__':
    unittest.main()
