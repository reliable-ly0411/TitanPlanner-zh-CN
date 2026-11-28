using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MissionPlanner.Utilities;
using Severity = MAVLink.MAV_SEVERITY;
using Vehicle = MAVLink.MAV_TYPE;

namespace MissionPlanner
{
    internal static class FirmwareScriptSpeech
    {
        private sealed class Rule
        {
            internal readonly Severity Severity;
            internal readonly Regex Text;
            internal readonly string Template;

            internal Rule(Severity severity, string pattern, string template)
            {
                Severity = severity;
                Text = new Regex(@"\A" + pattern + @"\z", RegexOptions.CultureInvariant);
                Template = template;
            }
        }

        // ArduPilot 4.7.3, d9c5097c. Keep script and native QuickTune's exact spelling and severity.
        private static readonly Rule[] Tune =
        {
            new Rule(Severity.EMERGENCY, @"Quicktune for quadplane loaded", @"Quicktune for quadplane loaded"),
            new Rule(Severity.EMERGENCY, @"Quicktune for multicopter loaded", @"Quicktune for multicopter loaded"),
            new Rule(Severity.EMERGENCY, @"Tuning: Must be flying to tune", @"Tuning: Must be flying to tune"),
            new Rule(Severity.EMERGENCY, @"Tuning: reverted", @"Tuning: reverted"),
            new Rule(Severity.CRITICAL, @"Tuning: attitude error (-?[0-9]+\.[0-9])deg - ABORTING", @"Tuning: attitude error {0}deg - ABORTING"),
            new Rule(Severity.NOTICE, @"Tuning: starting tune", @"Tuning: starting tune"),
            new Rule(Severity.NOTICE, @"Tuning: saved", @"Tuning: saved"),
            new Rule(Severity.NOTICE, @"Tuning: DONE", @"Tuning: DONE"),
            new Rule(Severity.NOTICE, @"Tuning: (RLL|PIT|YAW) done", @"Tuning: {0} done"),
            new Rule(Severity.INFO, @"Starting (RLL|PIT|YAW) tune", @"Starting {0} tune"),
            new Rule(Severity.INFO, @"Tuning: ((?:ATC|Q_A)_RAT_(?:RLL|PIT|YAW)_[DP]) done", @"Tuning: {0} done"),
            new Rule(Severity.WARNING, @"QuickTune disabled", @"QuickTune disabled"),
            new Rule(Severity.CRITICAL, @"QuickTune aborted", @"QuickTune aborted"),
            new Rule(Severity.CRITICAL, @"Quicktune: attitude error (-?[0-9]+\.[0-9])deg - ABORTING", @"Quicktune: attitude error {0}deg - ABORTING"),
            new Rule(Severity.WARNING, @"Quicktune: Must be flying to tune", @"Quicktune: Must be flying to tune"),
            new Rule(Severity.WARNING, @"Quicktune: Reverted", @"Quicktune: Reverted"),
            new Rule(Severity.NOTICE, @"Quicktune: Starting tune", @"Quicktune: Starting tune"),
            new Rule(Severity.NOTICE, @"Quicktune: DONE", @"Quicktune: DONE"),
            new Rule(Severity.NOTICE, @"Quicktune: Saved", @"Quicktune: Saved"),
            new Rule(Severity.NOTICE, @"Quicktune: (RLL|PIT|YAW) done", @"Quicktune: {0} done"),
            new Rule(Severity.INFO, @"Quicktune: ((?:ATC|Q_A)_RAT_(?:RLL|PIT|YAW)_[DP]) done", @"Quicktune: {0} done"),
        };

        private static readonly Rule[] Rover =
        {
            new Rule(Severity.INFO, @"Rover quiktune loaded", @"Rover quiktune loaded"),
            new Rule(Severity.CRITICAL, @"RTun: must be armed and moving to tune", @"RTun: must be armed and moving to tune"),
            new Rule(Severity.CRITICAL, @"RTun: gains reverted", @"RTun: gains reverted"),
            new Rule(Severity.INFO, @"RTun: starting (ATC_STR_RAT|ATC_SPEED) tune", @"RTun: starting {0} tune"),
            new Rule(Severity.INFO, @"RTun: (ATC_STR_RAT_FF|ATC_SPEED_FF) ([0-9]{1,3})% complete", @"RTun: {0} {1}% complete"),
            new Rule(Severity.NOTICE, @"RTun: (ATC_STR_RAT_FF|ATC_SPEED_FF) tuning done", @"RTun: {0} tuning done"),
            new Rule(Severity.NOTICE, @"RTun: Tuning DONE", @"RTun: Tuning DONE"),
            new Rule(Severity.NOTICE, @"RTun: tuning gains saved", @"RTun: tuning gains saved"),
            new Rule(Severity.INFO, @"RTun: adjusted ((?:ATC_STR_RAT|ATC_SPEED)_[PID]) (-?[0-9]+\.[0-9]{3}) -> (-?[0-9]+\.[0-9]{3})", @"RTun: adjusted {0} {1} -> {2}"),
            new Rule(Severity.WARNING, @"RTun: increase steering \(([0-9]+)% < ([0-9]+)%\)", @"RTun: increase steering ({0}% < {1}%)"),
            new Rule(Severity.WARNING, @"RTun: increase turn rate \(([0-9]+) deg/s < ([0-9]+)\)", @"RTun: increase turn rate ({0} deg/s < {1})"),
            new Rule(Severity.WARNING, @"RTun: increase throttle \(([0-9]+) < ([0-9]+)\)", @"RTun: increase throttle ({0} < {1})"),
            new Rule(Severity.WARNING, @"RTun: increase speed \((-?[0-9]+\.[0-9]) < (-?[0-9]+\.[0-9])\)", @"RTun: increase speed ({0} < {1})"),
        };

        private static readonly Rule[] Plane =
        {
            new Rule(Severity.INFO, @"Loaded plane_aerobatics\.lua", @"Loaded plane_aerobatics.lua"),
            new Rule(Severity.NOTICE, @"Loaded sport_aerobatics\.lua", @"Loaded sport_aerobatics.lua"),
            new Rule(Severity.ALERT, @"Enabled ([0-9]+) aerobatic tricks", @"Enabled {0} aerobatic tricks"),
            new Rule(Severity.NOTICE, @"Enabled ([0-9]+) aerobatic tricks", @"Enabled {0} aerobatic tricks"),
            new Rule(Severity.NOTICE, @"Starting ([0-9]+) Roll\(s\)", @"Starting {0} Roll(s)"),
            new Rule(Severity.NOTICE, @"Finished Roll ([0-9]+)", @"Finished Roll {0}"),
            new Rule(Severity.NOTICE, @"Starting ([0-9]+) Loop\(s\)", @"Starting {0} Loop(s)"),
            new Rule(Severity.NOTICE, @"Finished Loop ([0-9]+)", @"Finished Loop {0}"),
            new Rule(Severity.NOTICE, @"Starting Rolling Circle", @"Starting Rolling Circle"),
            new Rule(Severity.NOTICE, @"Finished Rolling Circle", @"Finished Rolling Circle"),
            new Rule(Severity.NOTICE, @"Starting Turnaround", @"Starting Turnaround"),
            new Rule(Severity.NOTICE, @"Finished Turnaround ", @"Finished Turnaround "),
            new Rule(Severity.NOTICE, @"Starting Split-S", @"Starting Split-S"),
            new Rule(Severity.NOTICE, @"Finished Split-S ", @"Finished Split-S "),
            new Rule(Severity.INFO, @"Trick ([0-9]+) started \(([A-Za-z][A-Za-z0-9 _-]{0,31})\)", @"Trick {0} started ({1})"),
            new Rule(Severity.ERROR, @"Trick ([0-9]+) not found", @"Trick {0} not found"),
            new Rule(Severity.ALERT, @"Trick aborted", @"Trick aborted"),
            new Rule(Severity.EMERGENCY, @"Trick aborted", @"Trick aborted"),
            new Rule(Severity.ALERT, @"No trick selected", @"No trick selected"),
            new Rule(Severity.EMERGENCY, @"No trick selected", @"No trick selected"),
            new Rule(Severity.EMERGENCY, @"Too low altitude, aborting", @"Too low altitude, aborting"),
        };

        internal static string Translate(MAVLink.MAV_AUTOPILOT autopilot, Vehicle vehicle, uint flightVersion,
            byte[] flightHash, byte severity, string completeRawText)
        {
            if (autopilot != MAVLink.MAV_AUTOPILOT.ARDUPILOTMEGA || flightVersion != 0x040703ffu ||
                flightHash?.Length != 8 || Encoding.ASCII.GetString(flightHash) != "d9c5097c" ||
                string.IsNullOrEmpty(completeRawText) || completeRawText.Length > 150)
                return null;

            switch (vehicle)
            {
                case Vehicle.FIXED_WING:
                    // ArduPlane can report FIXED_WING on QuadPlane. Match both distinct vocabularies.
                    return Match(Plane, severity, completeRawText) ?? Match(Tune, severity, completeRawText);
                case Vehicle.VTOL_DUOROTOR:
                case Vehicle.VTOL_QUADROTOR:
                case Vehicle.VTOL_TILTROTOR:
                    return Match(Tune, severity, completeRawText) ?? Match(Plane, severity, completeRawText);
                case Vehicle.QUADROTOR:
                case Vehicle.COAXIAL:
                case Vehicle.HELICOPTER:
                case Vehicle.HEXAROTOR:
                case Vehicle.OCTOROTOR:
                case Vehicle.TRICOPTER:
                case Vehicle.DODECAROTOR:
                    return Match(Tune, severity, completeRawText);
                case Vehicle.GROUND_ROVER:
                case Vehicle.SURFACE_BOAT:
                    return Match(Rover, severity, completeRawText);
                default:
                    return null;
            }
        }

        private static string Match(Rule[] rules, byte severity, string text)
        {
            foreach (var rule in rules)
            {
                if ((byte)rule.Severity != severity)
                    continue;
                var match = rule.Text.Match(text);
                if (!match.Success)
                    continue;
                var translated = UiText.Translate(rule.Template);
                if (translated == rule.Template)
                    return null;
                var arguments = match.Groups.Cast<Group>().Skip(1).Select(group => (object)group.Value).ToArray();
                return string.Format(CultureInfo.InvariantCulture, translated, arguments);
            }

            return null;
        }
    }
}
