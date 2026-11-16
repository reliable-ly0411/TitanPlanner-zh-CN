"""Connection failures are translated only at the UI boundary."""
import hashlib
import json
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]

class ConnectionErrorTranslationTest(unittest.TestCase):
    def test_error_display_translates_on_ui_thread_and_keeps_details(self):
        source = (ROOT / 'ExtLibs/Controls/ProgressReporterDialogue.cs').read_text()
        method = source[source.index('private void ShowDoneWithError('):source.index('private void btnCancel_Click(')]
        self.assertLess(method.index('this.Invoke((MethodInvoker)delegate'), method.index('UiText.Translate(doWorkArgs)'))
        self.assertIn('UiText.Format($"There was an unexpected error ({exception.Message})")', method)
        self.assertIn('this.workerException = exception;', method)
        self.assertIn('this.linkLabel1.Visible = exception != null;', method)

    def test_connection_error_catalog_and_generated_resources_agree(self):
        catalog = json.loads((ROOT / 'localization/zh-Hans/code-translations.json').read_text())
        resource = ET.parse(ROOT / 'ExtLibs/Utilities/Resources/UiText.zh-CN.resx')
        values = {item.attrib['name']: item.findtext('value') for item in resource.findall('data')}
        for term in ('No Heartbeat Packets Received', 'This appears to be a CAN port, please use the DroneCAN screen',
                     'Timeout getting parameters', 'User Canceled', 'There was an unexpected error ({0})'):
            self.assertNotEqual(catalog[term], term)
            self.assertEqual(values[hashlib.sha256(term.encode()).hexdigest()], catalog[term])
        self.assertEqual(catalog['There was an unexpected error ({0})'].count('{0}'), 1)
        self.assertIn('DroneCAN', catalog['This appears to be a CAN port, please use the DroneCAN screen'])

if __name__ == '__main__':
    unittest.main()
