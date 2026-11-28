"""The enhanced custom speech hint must not change the template or its dialog key."""
import hashlib
import json
from pathlib import Path
import re
import unittest
import xml.etree.ElementTree as ET


ROOT = Path(__file__).resolve().parents[3]
RESOURCES = ROOT / 'ExtLibs/Utilities/Resources'


class CustomSpeechTemplateHintTests(unittest.TestCase):
    def test_catalog_keeps_exact_existing_variable_names(self):
        planner = (ROOT / 'GCSViews/ConfigurationView/ConfigPlanner.cs').read_text(encoding='utf-8-sig')
        helper = planner.split('private static void AddCustomSpeechTemplateHelp(', 1)[1].split('private Control ', 1)[0]
        text = re.search(r'UiText.Translate\(\s*"([^"]+)"\)', helper).group(1)
        tools = json.loads((ROOT / 'localization/zh-Hans/code-translations.json').read_text(encoding='utf-8'))
        tokens = ['{wpn}', '{alt}', '{gsp}']
        self.assertEqual(tokens, re.findall(r'\{[^}]+\}', text))
        self.assertEqual(tokens, re.findall(r'\{[^}]+\}', tools[text]))
        key = hashlib.sha256(text.encode()).hexdigest()
        self.assertEqual(tools[text], ET.parse(RESOURCES / 'UiText.zh-CN.resx').findtext(f"data[@name='{key}']/value"))
        for resource in ['UiText.resx', 'UiText.zh-Hans.resx']:
            self.assertIsNone(ET.parse(RESOURCES / resource).find(f"data[@name='{key}']"))
        self.assertIn('CultureInfo.CurrentUICulture.Name, "zh-CN", StringComparison.Ordinal', helper)
        self.assertNotIn('Settings', helper)
        self.assertIn('InitializeComponent();\n            AddCustomSpeechTemplateHelp(CHK_speechcustom);', planner)
        common = (ROOT / 'ExtLibs/ArduPilot/Common.cs').read_text(encoding='utf-8-sig')
        self.assertIn('input.Replace("{wpn}", MAV.cs.wpno.ToString())', common)
        self.assertIn('input.Replace("{wpn}", "Home")', common)
        self.assertIn('input.Replace("{alt}", MAV.cs.alt.ToString("0"))', common)
        self.assertIn('input.Replace("{gsp}", MAV.cs.groundspeed.ToString("0"))', common)

    def test_default_template_is_only_used_when_no_saved_template_exists(self):
        planner = (ROOT / 'GCSViews/ConfigurationView/ConfigPlanner.cs').read_text(encoding='utf-8-sig')
        helper = planner.split('private static string PeriodicSpeechEditorDefault(string saved)', 1)[1]
        helper = helper.split('private void CHK_speechcustom_CheckedChanged(', 1)[0].strip()
        default = "Heading to Waypoint {wpn}, altitude is {alt}, Ground speed is {gsp} "
        enhanced_default = "正在前往航点 {wpn}，高度 {alt}，地速 {gsp} "
        self.assertEqual('{ return saved ?? UiText.Translate("' + default + '"); }', ' '.join(helper.split()))
        entries = json.loads((ROOT / 'localization/zh-Hans/code-translations.json').read_text(encoding='utf-8'))
        self.assertEqual(enhanced_default, entries[default])
        key = hashlib.sha256(default.encode()).hexdigest()
        self.assertEqual(enhanced_default, ET.parse(RESOURCES / 'UiText.zh-CN.resx').findtext(f"data[@name='{key}']/value"))
        for resource in ['UiText.resx', 'UiText.zh-Hans.resx']:
            self.assertIsNone(ET.parse(RESOURCES / resource).find(f"data[@name='{key}']"))

    def test_editor_uses_saved_template_and_saves_only_after_acceptance(self):
        planner = (ROOT / 'GCSViews/ConfigurationView/ConfigPlanner.cs').read_text(encoding='utf-8-sig')
        handler = planner.split('private void CHK_speechcustom_CheckedChanged(', 1)[1].split('private void ', 1)[0]
        self.assertIn(
            'var speechstring = PeriodicSpeechEditorDefault(Settings.Instance["speechcustom"]); '
            'if (DialogResult.Cancel == InputBox.Show(UiText.Translate("Notification"), '
            'UiText.Translate("What do you want it to say?"), ref speechstring)) return; '
            'Settings.Instance["speechcustom"] = speechstring;',
            ' '.join(handler.split()),
        )
        self.assertEqual(1, handler.count('Settings.Instance["speechcustom"] ='))


if __name__ == '__main__':
    unittest.main()
