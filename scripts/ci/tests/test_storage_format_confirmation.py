"""Source integration invariants; executable C# behavior is covered by MSTest separately."""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[3]

class StorageFormatSourceTests(unittest.TestCase):
    def test_format_command_keeps_target_and_parameters(self):
        source = (ROOT / 'GCSViews/FlightData.cs').read_text(encoding='utf-8-sig')
        block = source.split('if (flightDataActions1.CMB_action.Text == actions.Format_SD_Card.ToString())', 1)[1].split('if (flightDataActions1.CMB_action.Text == actions.Trigger_Camera.ToString())', 1)[0]
        self.assertEqual(block.count('MAVLink.MAV_CMD.STORAGE_FORMAT'), 1)
        self.assertIn('StorageFormatConfirmation.Execute(', block)
        self.assertIn('MissionPlanner.MsgBox.CustomMessageBox.ShowDestructiveConfirmation(', block)
        self.assertIn('MainV2.comPort.doCommandInt(MainV2.comPort.MAV.sysid, MainV2.comPort.MAV.compid, MAVLink.MAV_CMD.STORAGE_FORMAT, 1, 1, 0, 0, 0, 0, 0)', block)
        self.assertLess(block.index('ShowDestructiveConfirmation'), block.index('doCommandInt'))

    def test_dialog_tests_bind_the_windows_implementation(self):
        source = (ROOT / 'MissionPlannerTests/Controls/StorageFormatDialogTests.cs').read_text()
        self.assertIn('using CustomMessageBox = MissionPlanner.MsgBox.CustomMessageBox;', source)
        self.assertNotIn('using MissionPlanner.MsgBox;', source)

    def test_safe_default_is_opt_in_and_applied_before_dialog(self):
        source = (ROOT / 'ExtLibs/Controls/CustomMessageBox.cs').read_text(encoding='utf-8-sig')
        self.assertEqual(source.count('bool defaultCancel = false'), 3)
        self.assertIn('"Yes", "Cancel", true, null, true)', source)
        self.assertIn('answer = defaultCancel ? test : _state;', source)
        self.assertIn('if (defaultCancel) msgBoxFrm.DialogResult = DialogResult.Yes;', source)
        self.assertIn('if (defaultCancel) msgBoxFrm.DialogResult = DialogResult.Cancel;', source)
        self.assertRegex(source, r'if \(defaultCancel\)\s*\{\s*msgBoxFrm.AcceptButton = msgBoxFrm.CancelButton;\s*msgBoxFrm.ActiveControl = msgBoxFrm.CancelButton as Control;\s*\}\s*DialogResult test = msgBoxFrm.ShowDialog\(\);')

    def test_only_yes_can_reach_transport(self):
        source = (ROOT / 'ExtLibs/Controls/StorageFormatConfirmation.cs').read_text()
        self.assertRegex(source, r'if \(confirm\(\) != DialogResult.Yes\)\s*return;\s*send\(\);')
        self.assertEqual(source.count('send();'), 1)

if __name__ == '__main__':
    unittest.main()
