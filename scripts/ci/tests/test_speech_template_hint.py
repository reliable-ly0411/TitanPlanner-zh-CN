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

    def test_dialog_prompt_and_saved_template_are_unchanged(self):
        planner = (ROOT / 'GCSViews/ConfigurationView/ConfigPlanner.cs').read_text(encoding='utf-8-sig')
        handler = planner.split('private void CHK_speechcustom_CheckedChanged(', 1)[1].split('private void ', 1)[0]
        self.assertIn('var speechstring = "Heading to Waypoint {wpn}, altitude is {alt}, Ground speed is {gsp} ";', handler)
        self.assertIn('speechstring = Settings.Instance["speechcustom"].ToString();', handler)
        self.assertIn('InputBox.Show(UiText.Translate("Notification"), UiText.Translate("What do you want it to say?"), ref speechstring)', handler)
        self.assertIn('Settings.Instance["speechcustom"] = speechstring;', handler)


if __name__ == '__main__':
    unittest.main()
