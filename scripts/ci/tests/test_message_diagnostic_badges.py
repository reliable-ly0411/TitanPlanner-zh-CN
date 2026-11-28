"""Diagnostic severity localization affects only the painted badge, never copied text."""
import hashlib
from pathlib import Path
import re
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
LABELS = {
    'EMERGENCY': ('Emergency', '紧急', 'EMERG'),
    'ALERT': ('Alert', '警报', 'ALERT'),
    'CRITICAL': ('Critical', '严重', 'CRIT'),
    'ERROR': ('Error', '错误', 'ERROR'),
    'WARNING': ('Warning', '警告', 'WARN'),
    'NOTICE': ('Notice', '通知', 'NOTICE'),
    'INFO': ('Info', '信息', 'INFO'),
    'DEBUG': ('Debug', '调试', 'DEBUG'),
}


class MessageDiagnosticBadgesTests(unittest.TestCase):
    def setUp(self):
        self.source = (ROOT / 'Controls/MessagesList.cs').read_text(encoding='utf-8-sig')

    def test_badges_use_exact_resource_keys_with_fallbacks(self):
        method = self.source.split('static string LocalizedDiagnosticBadge(', 1)[1]
        method = method.split('private void CopyItem_Click(', 1)[0]
        options = re.findall(r'case MAV_SEVERITY\.(\w+): key = "(\w+)"; break;', method)
        self.assertEqual({k: labels[0] for k, labels in LABELS.items()}, dict(options))
        self.assertEqual(len(LABELS), len(options))
        self.assertIn('CultureInfo.CurrentUICulture.Name, "zh-CN",', method)
        self.assertIn('default: return original;', method)
        self.assertIn('string.Equals(translated, key, StringComparison.Ordinal) ? original : translated', method)
        resources = ROOT / 'ExtLibs/Utilities/Resources'
        chinese = ET.parse(resources / 'UiText.zh-CN.resx')
        stock = [ET.parse(resources / name) for name in ('UiText.resx', 'UiText.zh-Hans.resx')]
        for english, zh, _ in LABELS.values():
            with self.subTest(english=english):
                key = hashlib.sha256(english.encode()).hexdigest()
                selector = f"data[@name='{key}']"
                self.assertEqual(zh, chinese.findtext(selector + '/value'))
                self.assertTrue(all(xml.find(selector) is None for xml in stock))

    def test_paint_uses_the_label_but_copy_keeps_original_code_and_body(self):
        self.assertEqual(2, self.source.count('LocalizedDiagnosticBadge('))
        self.assertIn('g.DrawString(LocalizedDiagnosticBadge(msg.severity, severityText), badgeFont', self.source)
        self.assertIn('[{GetSeverityText(msg.severity)}] {msg.message}', self.source)
        self.assertIn('Color badgeColor = GetSeverityBadgeColor(msg.severity);', self.source)
        raw = self.source.split('string GetSeverityText(', 1)[1]
        for enum, (_, _, short) in LABELS.items():
            self.assertRegex(raw, fr'case MAV_SEVERITY\.{enum}:\s*return "{short}";')


if __name__ == '__main__':
    unittest.main()
