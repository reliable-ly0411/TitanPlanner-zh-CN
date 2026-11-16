using System;
using System.Globalization;
using MissionPlanner.ArduPilot;
using MissionPlanner.Utilities;

namespace MissionPlanner.Controls
{
    /// <summary>Display labels only; telemetry lookup and selection retain the original key.</summary>
    public static class TelemetryFieldPresentation
    {
        public static string FieldLabel(string key)
        {
            // Custom names are authored by the operator, not catalog entries.
            if (CurrentState.custom_field_names.TryGetValue(key, out var customName))
                return customName;
            if (!string.Equals(CultureInfo.CurrentUICulture.Name, "zh-CN", StringComparison.OrdinalIgnoreCase))
                return key;
            var description = CurrentState.GetDisplayText(key);
            // Attribute placeholders do not guarantee the units of every input path.
            // Keep the key and description, but never infer a physical unit from them.
            description = description.Replace(" (dist)", "").Replace(" (alt)", "")
                .Replace(" (speed)", "").Replace(" (speed/s)", "");
            // GLOBAL_POSITION_INT velocity components are centimetres/second on the wire
            // and stored as metres/second; the legacy attribute spells this ambiguously as ms.
            if (key == "vx" || key == "vy" || key == "vz")
                description = description.Replace("(ms)", "(m/s)");
            return Combine(key, description);
        }

        public static string Combine(string original, string description)
        {
            return string.IsNullOrWhiteSpace(description) || description == original
                ? original : original + " (" + description + ")";
        }

        public static string GroupLabel(string group)
        {
            var source = "Telemetry group: " + group;
            var translated = UiText.Translate(source);
            return translated == source ? group : Combine(group, translated);
        }

        public static bool Matches(string query, params string[] labels)
        {
            foreach (var label in labels)
                if (label != null && label.IndexOf(query ?? "", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }
    }
}
