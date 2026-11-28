"""An unmatched search shows feedback without implying a change to field selection."""
import hashlib
import re
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]


class DisplayFieldSearchFeedbackTests(unittest.TestCase):
    def test_empty_feedback_is_drawn_in_the_viewport_for_nonblank_queries_only(self):
        source = (ROOT / 'Controls/DisplayThisForm.cs').read_text(encoding='utf-8-sig')
        paint = source.split('void ContentPanel_Paint(', 1)[1].split('private void ', 1)[0]
        condition = '_filteredItems.Count == 0 && !string.IsNullOrWhiteSpace(_searchBox.Text)'
        body = re.search(r'if \(' + re.escape(condition) + r'\)\s*\{([^}]+)\}', paint).group(1)
        self.assertIn('TextRenderer.DrawText(g, UiText.Translate("No matching fields")', body)
        self.assertIn('_contentPanel.ClientRectangle, ChooseTextColor(Color.Gray)', body)
        self.assertIn('TextFormatFlags.EndEllipsis', body)
        self.assertIn('return;', body)
        self.assertLess(paint.index(condition), paint.index('g.TranslateTransform'))
        self.assertNotIn('_checkChangedHandler', body)

    def test_simplified_2_translation_and_english_fallback(self):
        path = ROOT / 'ExtLibs/Utilities/Resources'
        key = hashlib.sha256(b'No matching fields').hexdigest()
        selector = f"data[@name='{key}']"
        translated = ET.parse(path / 'UiText.zh-CN.resx')
        self.assertEqual('未找到匹配字段', translated.findtext(selector + '/value'))
        for file in ('UiText.resx', 'UiText.zh-Hans.resx'):
            self.assertIsNone(ET.parse(path / file).find(selector))


if __name__ == '__main__':
    unittest.main()
