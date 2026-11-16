"""Bilingual action painting must leave command selection strings untouched."""
import json
from pathlib import Path
import re
import unittest
ROOT = Path(__file__).resolve().parents[3]
class ActionDisplayTranslationTest(unittest.TestCase):
    def test_action_enum_has_translations(self):
        source = (ROOT / 'GCSViews/FlightData.cs').read_text()
        names = re.search(r'public enum actions\s*\{([^}]+)', source).group(1)
        catalog = json.loads((ROOT / 'localization/zh-Hans/code-translations.json').read_text())
        for name in re.findall(r'\b\w+\b', names):
            self.assertIn(name, catalog)
            self.assertNotEqual(name, catalog[name])
    def test_owner_drawing_does_not_replace_protocol_selection(self):
        source = (ROOT / 'Controls/FlightDataActions.cs').read_text()
        method = source[source.index('private static void ConfigureCommandDisplay'):source.index('private void ScrollPanel_Resize')]
        self.assertIn('DrawMode.OwnerDrawFixed', method)
        self.assertIn('combo.GetItemText(combo.Items[e.Index])', method)
        self.assertNotRegex(method, r'combo\.(Text|SelectedValue|SelectedItem|DataSource|DisplayMember)\s*=')
        self.assertNotIn('.Format +=', method)
        actions = (ROOT / 'GCSViews/FlightData.cs').read_text()
        self.assertIn('setMode(flightDataActions1.CMB_modes.Text)', actions)
        self.assertIn('flightDataActions1.CMB_action.Text == actions.Terminate_Flight.ToString()', actions)
    def test_download_and_dynamic_button_labels(self):
        sitl = (ROOT / 'GCSViews/SITL.cs').read_text()
        self.assertIn('Common.LoadingBox(UiText.Translate("Downloading"), UiText.Translate("Downloading sitl software"))', sitl)
        designer = (ROOT / 'Controls/FlightDataActions.Designer.cs').read_text()
        for term in ('Apply', 'Aux Functions', 'Servos', 'Relays'):
            self.assertNotIn('= "' + term + '";', designer)
        source = (ROOT / 'Controls/FlightDataActions.cs').read_text()
        self.assertIn('Text = UiText.Translate(AuxPositions[i].Text)', source)
        self.assertIn('MAVLink.MAV_CMD.DO_AUX_FUNCTION, function, position, 0, 0, 0, 0, 0', source)
if __name__ == '__main__': unittest.main()
