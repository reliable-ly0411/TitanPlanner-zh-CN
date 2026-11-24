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
            if (string.IsNullOrWhiteSpace(description) || description == key)
                description = MissingDisplayDescription(key);
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

        private static string MissingDisplayDescription(string key)
        {
            switch (key)
            {
                case "altd100": return UiText.Translate("Relative altitude hundreds component (fractional)");
                case "altd1000": return UiText.Translate("Relative altitude thousands component (fractional)");
                case "armed": return UiText.Translate("Vehicle armed state");
                case "ahrs2_alt": return UiText.Translate("Secondary AHRS altitude (MSL, m)");
                case "ahrs2_lat": return UiText.Translate("Secondary AHRS latitude (°)");
                case "ahrs2_lng": return UiText.Translate("Secondary AHRS longitude (°)");
                case "ahrs2_pitch": return UiText.Translate("Secondary AHRS pitch (°)");
                case "ahrs2_roll": return UiText.Translate("Secondary AHRS roll (°)");
                case "ahrs2_yaw": return UiText.Translate("Secondary AHRS yaw (°)");
                case "gen_status": return UiText.Translate("Generator status flags");
                case "gen_speed": return UiText.Translate("Generator speed (rpm)");
                case "gen_current": return UiText.Translate("Generator load current (A)");
                case "gen_voltage": return UiText.Translate("Generator bus voltage (V)");
                case "gen_runtime": return UiText.Translate("Generator run time since restart (s)");
                case "gen_maint_time": return UiText.Translate("Time until generator maintenance (s)");
            }


            const string cellPrefix = "battery_cell";
            if (key.StartsWith(cellPrefix, StringComparison.Ordinal) &&
                int.TryParse(key.Substring(cellPrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out int cell) &&
                cell >= 1 && cell <= 14 && key.Length == cellPrefix.Length + (cell < 10 ? 1 : 2))
                return UiText.Format($"Primary battery cell {cell} voltage (V)");

            if (!key.StartsWith("esc", StringComparison.Ordinal))
                return key;
            int separator = key.IndexOf('_', 3);
            if (separator < 4 || separator > 5 ||
                !int.TryParse(key.Substring(3, separator - 3), NumberStyles.None, CultureInfo.InvariantCulture, out int number) ||
                number < 1 || number > 16 ||
                separator != (number < 10 ? 4 : 5))
                return key;

            switch (key.Substring(separator + 1))
            {
                case "curr": return UiText.Format($"ESC {number} current (A)");
                case "rpm": return UiText.Format($"ESC {number} electrical RPM");
                case "temp": return UiText.Format($"ESC {number} temperature (°C)");
                case "volt": return UiText.Format($"ESC {number} voltage (V)");
                default: return key;
            }
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
