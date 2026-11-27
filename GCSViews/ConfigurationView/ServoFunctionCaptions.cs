using MissionPlanner.ArduPilot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace MissionPlanner.GCSViews.ConfigurationView
{
    // Presentation only. Keep the metadata list, key and original value unchanged in ComboBox binding.
    internal static class ServoFunctionCaptions
    {
        internal static void Attach(ComboBox combo, IEnumerable<KeyValuePair<int, string>> options, Firmwares firmware)
        {
            if (!string.Equals(CultureInfo.CurrentUICulture.Name, "zh-CN", StringComparison.Ordinal) ||
                (firmware != Firmwares.ArduCopter2 && firmware != Firmwares.ArduPlane && firmware != Firmwares.ArduRover) ||
                !options.Any(option => Translation(option) != null))
                return;

            // Attach before setup selects the cached key. Formatting changes only the text shown by WinForms.
            combo.Format += (sender, args) =>
            {
                if (args.ListItem is KeyValuePair<int, string> option)
                {
                    var translation = Translation(option);
                    if (translation != null)
                        args.Value = option.Value + " · " + translation;
                }
            };
            combo.FormattingEnabled = true;
        }

        private static string Translation(KeyValuePair<int, string> option)
        {
            // The exact names and IDs below are shared by Plane, Copter and Rover in SRV_Channel.cpp.
            switch (option.Key)
            {
                case 0: if (option.Value == "Disabled") return "未分配功能"; break;
                case 34: if (option.Value == "Motor2") return "电机 2"; break;
                case 138: if (option.Value == "Alarm") return "告警"; break;
            }
            return null;
        }
    }
}
