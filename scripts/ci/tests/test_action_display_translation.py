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
    def test_actual_metadata_and_builtin_formatted_mode_names(self):
        import xml.etree.ElementTree as ET
        source = (ROOT / 'ExtLibs/ArduPilot/Common.cs').read_text()
        acronyms = set(re.findall(r'"([^"\n]+)"', source.split('ModeAcronyms =')[1].split('};')[0]))
        def formatted(name):
            return ' '.join(part.upper() if part.upper() in acronyms else part[:1].upper() + part[1:].lower()
                            for part in name.replace('_', ' ').split(' '))
        names = []
        metadata = ET.parse(ROOT / 'ParameterMetaDataBackup.xml')
        for firmware, key in [('ArduCopter2', 'FLTMODE1'), ('ArduPlane', 'FLTMODE1'), ('ArduRover', 'MODE1')]:
            values = metadata.findtext(firmware + '/' + key + '/Values')
            self.assertTrue(values, firmware)
            names.extend(item.split(':', 1)[1] for item in values.split(','))
        builtin = source.split('public static List<KeyValuePair<int, string>> getModesList(Firmwares firmware)')[1].split('// ArduPlane')[0]
        names.extend(re.findall(r',\s*"([^"\n]+)"\)', builtin))
        catalog = json.loads((ROOT / 'localization/zh-Hans/code-translations.json').read_text())
        self.assertGreater(len(names), 60)
        for name in names:
            self.assertIn(formatted(name), catalog, name)

    def test_confirmation_only_translates_display_not_dispatch(self):
        source = (ROOT / 'GCSViews/FlightData.cs').read_text()
        self.assertIn('FlightDataActions.DisplayCommandLabel(flightDataActions1.CMB_action.Text)', source)
        self.assertIn('MessageBoxButtons.YesNo) == (int) DialogResult.Yes)', source)
        self.assertIn('flightDataActions1.CMB_action.Text == actions.Preflight_Reboot_Shutdown.ToString()', source)
        self.assertIn('Enum.Parse(typeof(MAVLink.MAV_CMD), flightDataActions1.CMB_action.Text.ToUpper(CultureInfo.InvariantCulture))', source)

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
