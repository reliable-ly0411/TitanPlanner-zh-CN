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
                case "ter_load": return UiText.Translate("Terrain blocks loaded in memory");
                case "ter_pend": return UiText.Translate("Terrain blocks awaiting receipt or disk read");
                case "ter_space": return UiText.Translate("Terrain grid spacing");
                case "terrainactive": return UiText.Translate("Terrain subsystem present, enabled and healthy");
                case "vibeclip0": return UiText.Translate("Accelerometer 1 clipping count");
                case "vibeclip1": return UiText.Translate("Accelerometer 2 clipping count");
                case "vibeclip2": return UiText.Translate("Accelerometer 3 clipping count");
                case "vibex": return UiText.Translate("X-axis vibration level");
                case "vibey": return UiText.Translate("Y-axis vibration level");
                case "vibez": return UiText.Translate("Z-axis vibration level");
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

            if (TryBatteryId(key, "battery_temp", out int batteryId))
                return UiText.Format($"Battery ID {batteryId} temperature (°C)");
            if (TryBatteryId(key, "battery_remainmin", out batteryId))
                return UiText.Format($"Battery ID {batteryId} estimated time remaining (min)");

            if (TryRawChannel(key, "in", 16, out int channel))
                return UiText.Format($"RC input channel {channel} pulse width (µs)");
            if (TryRawChannel(key, "out", 32, out channel))
                return UiText.Format($"Servo output channel {channel} pulse width (µs)");

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

        private static bool TryBatteryId(string key, string prefix, out int id)
        {
            id = 0;
            if (!key.StartsWith(prefix, StringComparison.Ordinal))
                return false;
            if (key.Length == prefix.Length)
                return true;
            if (key.Length != prefix.Length + 1 || key[prefix.Length] < '2' || key[prefix.Length] > '9')
                return false;

            // The unsuffixed property is BATTERY_STATUS ID 0; suffix N maps to ID N-1.
            id = key[prefix.Length] - '1';
            return true;
        }

        private static bool TryRawChannel(string key, string suffix, int maximum, out int channel)
        {
            channel = 0;
            int digits = key.Length - 2 - suffix.Length;
            return (digits == 1 || digits == 2) && key.StartsWith("ch", StringComparison.Ordinal) &&
                key.EndsWith(suffix, StringComparison.Ordinal) && key[2] != '0' &&
                int.TryParse(key.Substring(2, digits), NumberStyles.None, CultureInfo.InvariantCulture, out channel) &&
                channel >= 1 && channel <= maximum;
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
