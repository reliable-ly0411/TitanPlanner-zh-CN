using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using MissionPlanner.Utilities;
using Autopilot = MAVLink.MAV_AUTOPILOT;
using Vehicle = MAVLink.MAV_TYPE;

namespace MissionPlanner
{
    internal static partial class ArduPilotStatusTextCatalog
    {
        private enum Family
        {
            Unknown, Copter, Plane, Rover, Sub, Tracker, Blimp
        }

        private sealed class StatusRule
        {
            internal readonly Regex Pattern;
            internal readonly string ResourceKey;
            internal readonly int[] Builds;

            internal StatusRule(string pattern, string resourceKey, int[] builds)
            {
                Pattern = new Regex(@"\A" + pattern + @"\z", RegexOptions.CultureInvariant,
                    TimeSpan.FromMilliseconds(25));
                ResourceKey = resourceKey;
                Builds = builds;
            }
        }

        private static readonly Dictionary<(Family, uint, string), int> BuildIndex = CreateBuildIndex();
        private static readonly StatusRule[][] BySeverity = CreateRules();

        internal static string Translate(Autopilot autopilot, Vehicle vehicle, uint version, byte[] hash,
            byte severity, string raw)
        {
            if (autopilot != Autopilot.ARDUPILOTMEGA || hash?.Length != 8 || severity >= BySeverity.Length ||
                string.IsNullOrEmpty(raw) || raw.Length > 200 ||
                !BuildIndex.TryGetValue((VehicleFamily(vehicle), version, Encoding.ASCII.GetString(hash)), out var build))
                return null;

            foreach (var rule in BySeverity[severity])
            {
                if (Array.BinarySearch(rule.Builds, build) < 0)
                    continue;

                Match match;
                try { match = rule.Pattern.Match(raw); }
                catch (RegexMatchTimeoutException) { return null; }
                if (!match.Success) continue;

                var translated = UiText.Translate(rule.ResourceKey);
                if (translated == rule.ResourceKey) return null;
                var values = new object[match.Groups.Count - 1];
                for (int i = 0; i < values.Length; i++)
                    values[i] = match.Groups[i + 1].Value;
                return string.Format(CultureInfo.InvariantCulture, translated, values);
            }
            return null;
        }

        private static Family VehicleFamily(Vehicle vehicle)
        {
            switch (vehicle)
            {
                case Vehicle.QUADROTOR:
                case Vehicle.COAXIAL:
                case Vehicle.HELICOPTER:
                case Vehicle.HEXAROTOR:
                case Vehicle.OCTOROTOR:
                case Vehicle.TRICOPTER:
                case Vehicle.DODECAROTOR:
                case Vehicle.DECAROTOR:
                case Vehicle.GENERIC_MULTIROTOR:
                    return Family.Copter;
                case Vehicle.FIXED_WING:
                case Vehicle.VTOL_DUOROTOR:
                case Vehicle.VTOL_QUADROTOR:
                case Vehicle.VTOL_TILTROTOR:
                case Vehicle.VTOL_RESERVED2:
                case Vehicle.VTOL_RESERVED3:
                case Vehicle.VTOL_RESERVED4:
                case Vehicle.VTOL_RESERVED5:
                    return Family.Plane;
                case Vehicle.GROUND_ROVER:
                case Vehicle.SURFACE_BOAT:
                    return Family.Rover;
                case Vehicle.SUBMARINE:
                    return Family.Sub;
                case Vehicle.ANTENNA_TRACKER:
                    return Family.Tracker;
                case Vehicle.AIRSHIP:
                    return Family.Blimp;
                default:
                    return Family.Unknown;
            }
        }
    }
}
