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
            string description;
            // These fields expose the original HYGROMETER_SENSOR values and units.
            switch (key)
            {
                case "hygrotemp1": description = "1 号温湿度传感器温度 (cdegC)"; break;
                case "hygrohumi1": description = "1 号温湿度传感器湿度 (c%)"; break;
                case "hygrotemp2": description = "2 号温湿度传感器温度 (cdegC)"; break;
                case "hygrohumi2": description = "2 号温湿度传感器湿度 (c%)"; break;
                default: description = CurrentState.GetDisplayText(key); break;
            }
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
                // These are the ground station's booleans, not the MAVLink landed state code.
                case "landed": return UiText.Translate("Ground station on-ground flag");
                case "safetyactive": return UiText.Translate("Ground station safety flag (motor control not enabled)");
                case "prearmstatus": return UiText.Translate("Ground station pre-arm flag (connected and checks healthy or disabled)");
                case "ahrs2_alt": return UiText.Translate("Secondary AHRS altitude (MSL, m)");
                case "ahrs2_lat": return UiText.Translate("Secondary AHRS latitude (°)");
                case "ahrs2_lng": return UiText.Translate("Secondary AHRS longitude (°)");
                case "ahrs2_pitch": return UiText.Translate("Secondary AHRS pitch (°)");
                case "ahrs2_roll": return UiText.Translate("Secondary AHRS roll (°)");
                case "ahrs2_yaw": return UiText.Translate("Secondary AHRS yaw (°)");
                case "ekfcompv": return UiText.Translate("EKF compass variance indicator");
                case "ekfflags": return UiText.Translate("EKF status flags");
                case "ekfposhor": return UiText.Translate("EKF horizontal position variance indicator");
                case "ekfposvert": return UiText.Translate("EKF vertical position variance indicator");
                case "ekfstatus": return UiText.Translate("Ground station EKF health indicator");
                case "ekfteralt": return UiText.Translate("EKF terrain altitude variance indicator");
                case "ekfvelv": return UiText.Translate("EKF velocity variance indicator");
                case "pidachieved": return UiText.Translate("PID actual rate");
                case "pidaxis": return UiText.Translate("PID tuning axis code");
                case "pidD": return UiText.Translate("PID derivative contribution");
                case "piddesired": return UiText.Translate("PID target rate");
                case "pidff": return UiText.Translate("PID feedforward contribution");
                case "pidI": return UiText.Translate("PID integral contribution");
                case "pidP": return UiText.Translate("PID proportional contribution");
                case "pidPDmod": return UiText.Translate("PID P/D oscillation modifier");
                case "pidSRate": return UiText.Translate("PID reported slew rate");
                case "pidSRateAccZ": return UiText.Translate("PID vertical acceleration slew rate");
                case "pidSRateLanding": return UiText.Translate("PID landing slew rate");
                case "pidSRatePitch": return UiText.Translate("PID pitch slew rate");
                case "pidSRateRoll": return UiText.Translate("PID roll slew rate");
                case "pidSRateSteer": return UiText.Translate("PID steering slew rate");
                case "pidSRateYaw": return UiText.Translate("PID yaw slew rate");
                case "rateattitude": return UiText.Translate("Requested attitude and flight data stream rate (Hz)");
                case "rateposition": return UiText.Translate("Requested position stream rate (Hz)");
                case "raterc": return UiText.Translate("Requested RC channel stream rate (Hz)");
                case "ratesensors": return UiText.Translate("Requested sensor stream rate (Hz)");
                case "ratestatus": return UiText.Translate("Requested extended status stream rate (Hz)");
                case "i2cerrors": return UiText.Translate("Reported I2C error count");
                case "rxerrors": return UiText.Translate("Radio receive error count");
                // SYS_STATUS.drop_rate_comm is copied without scaling in CurrentState.
                case "packetdropremote": return UiText.Translate("Remote-reported communication drop rate (c%)");
                case "txbuffer": return UiText.Translate("Free radio transmit buffer (%)");
                case "fixedp": return UiText.Translate("Error-corrected radio packet count");
                case "brklevel": return UiText.Translate("Reported heap top");
                case "freemem": return UiText.Translate("Reported free memory (bytes)");
                case "rpm1": return UiText.Translate("RPM sensor 1 speed (rpm)");
                case "rpm2": return UiText.Translate("RPM sensor 2 speed (rpm)");
                case "uid": return UiText.Translate("Autopilot hardware UID");
                case "landed_state": return UiText.Translate("Reported landed state code");
                case "capabilities": return UiText.Translate("Reported MAVLink protocol capability flags");
                case "connected": return UiText.Translate("Link open or log playback active");
                case "vehicleClass": return UiText.Translate("Ground station vehicle class");
                case "gimballat": return UiText.Translate("Gimbal point latitude (°)");
                case "gimballng": return UiText.Translate("Gimbal point longitude (°)");
                case "timesincelastshot": return UiText.Translate("Interval between camera trigger records (s)");
                case "linkqualitygcs": return UiText.Translate("Ground station packet reception quality (%)");
                case "groundspeed2": return UiText.Translate("Secondary GPS ground speed (m/s)");
                case "lowairspeed": return UiText.Translate("Ground station low airspeed flag");
                case "lowgroundspeed": return UiText.Translate("Ground station low ground speed flag");
                case "vlen": return UiText.Translate("Three-dimensional velocity magnitude (m/s)");
                case "HomeAlt": return UiText.Translate("Home location altitude");
                case "targetalt": return UiText.Translate("Navigation target altitude");
                case "targetaltd100": return UiText.Translate("Target altitude hundreds component (fractional)");
                case "load": return UiText.Translate("Reported autopilot processing load (%)");
                case "KIndex": return UiText.Translate("Geomagnetic K-index");
                case "QNH": return UiText.Translate("Estimated sea-level pressure");
                case "speedup": return UiText.Translate("Estimated IMU time to wall clock ratio");
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
