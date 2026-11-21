using System;
using System.Collections.Generic;
using System.Linq;

namespace MissionPlanner.Utilities
{
    /// <summary>Display-only mission labels. Keys and parameter metadata stay in their original form.</summary>
    public static class MissionCommandDisplay
    {
        public static string CommandName(string command)
        {
            if (string.IsNullOrEmpty(command))
                return command;
            var key = "Mission command: " + command;
            var label = UiText.Translate(key);
            return label == key ? command : command + " (" + label + ")";
        }

        public static List<KeyValuePair<string, string>> CreateOptions(IEnumerable<string> commands)
        {
            return commands.Select(command =>
                new KeyValuePair<string, string>(command, CommandName(command))).ToList();
        }

        public static string ParameterHeader(string header)
        {
            if (string.IsNullOrEmpty(header))
                return header;
            var key = "Mission parameter: " + header;
            var label = UiText.Translate(key);
            if (label != key)
                return label;

            // readCMDXML appends the chosen display unit after the canonical label.
            // Translate that label, retaining the unit text and all scaling metadata.
            var unitStart = header.LastIndexOf(" (", StringComparison.Ordinal);
            if (unitStart > 0 && header.EndsWith(")", StringComparison.Ordinal))
            {
                key = "Mission parameter: " + header.Substring(0, unitStart);
                label = UiText.Translate(key);
                if (label != key)
                    return label + header.Substring(unitStart);
            }
            return header;
        }
    }
}
