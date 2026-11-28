using Vehicle = MAVLink.MAV_TYPE;
using Severity = MAVLink.MAV_SEVERITY;

namespace MissionPlanner
{
    internal static partial class FirmwareScriptSpeech
    {
        private static readonly Rule[] AppletCommon =
        {
            new Rule(Severity.NOTICE, @"Loaded\ MissionSelector\.lua", @"Loaded MissionSelector.lua"),
            new Rule(Severity.INFO, @"loaded mission: (mission[LMH]\.txt)", @"loaded mission: {0}"),
            new Rule(Severity.INFO, @"LUA:\ Gimbal\-Cam\ mode\ manual", @"LUA: Gimbal-Cam mode manual"),
            new Rule(Severity.INFO, @"LUA:\ Gimbal\-Cam\ mode\ auto/manual", @"LUA: Gimbal-Cam mode auto/manual"),
            new Rule(Severity.INFO, @"LUA:\ Gimbal\-Cam\ mode\ auto", @"LUA: Gimbal-Cam mode auto"),
            new Rule(Severity.INFO, @"LUA:\ Gimbal\-Cam\ mode\ started", @"LUA: Gimbal-Cam mode started"),
            new Rule(Severity.ERROR, @"CAM_RC_\*\ parameters\ missing;\ camera\ autoarming\ unavailable", @"CAM_RC_* parameters missing; camera autoarming unavailable"),
            new Rule(Severity.NOTICE, @"Arming\ controls\ RUNCAM\ recording", @"Arming controls RUNCAM recording"),
            new Rule(Severity.INFO, @"RUNCAM\ on", @"RUNCAM on"),
            new Rule(Severity.INFO, @"RUNCAM\ off", @"RUNCAM off"),
            new Rule(Severity.INFO, @"Loaded BattEstimate for ([0-9]+) batteries", @"Loaded BattEstimate for {0} batteries"),
            new Rule(Severity.NOTICE, @"Crash\ Actions:\ Disarming", @"Crash Actions: Disarming"),
            new Rule(Severity.NOTICE, @"Crash\ Actions:\ Recovered", @"Crash Actions: Recovered"),
            new Rule(Severity.EMERGENCY, @"Crash\ Actions:\ Executing\.\.\.", @"Crash Actions: Executing..."),
            new Rule(Severity.WARNING, @"Crash\ Actions:\ FS_CRASH_CHECK\ is\ disabled", @"Crash Actions: FS_CRASH_CHECK is disabled"),
            new Rule(Severity.WARNING, @"Crash\ Actions:\ CRASH_DETECT\ is\ disabled", @"Crash Actions: CRASH_DETECT is disabled"),
            new Rule(Severity.INFO, @"Crash\ Actions:\ Script\ loaded", @"Crash Actions: Script loaded"),
            new Rule(Severity.ERROR, @"Crash Actions: RELAY([1-9]|[12][0-9]|3[0-2]) not configured", @"Crash Actions: RELAY{0} not configured"),
            new Rule(Severity.ERROR, @"Crash Actions: SERVO([1-9]|[12][0-9]|3[0-2]) not configured", @"Crash Actions: SERVO{0} not configured"),
            new Rule(Severity.NOTICE, @"Crash Actions: Setting RELAY([1-9]|[12][0-9]|3[0-2]) (OFF|ON)", @"Crash Actions: Setting RELAY{0} {1}"),
            new Rule(Severity.ERROR, @"Crash Actions: Failed to get SERVO([1-9]|[12][0-9]|3[0-2])_(MIN|TRIM|MAX)", @"Crash Actions: Failed to get SERVO{0}_{1}"),
            new Rule(Severity.NOTICE, @"Crash Actions: Setting SERVO([1-9]|[12][0-9]|3[0-2]) (MIN|TRIM|MAX)", @"Crash Actions: Setting SERVO{0} {1}"),
            new Rule(Severity.ERROR, @"Crash Actions: Unknown action ([0-9]+)", @"Crash Actions: Unknown action {0}"),
        };

        private static readonly Rule[] AppletPlane =
        {
            new Rule(Severity.INFO, @"PLND:\ Disabled", @"PLND: Disabled"),
            new Rule(Severity.INFO, @"PLND:\ Target\ Acquired", @"PLND: Target Acquired"),
            new Rule(Severity.INFO, @"PLND:\ Target\ Lost", @"PLND: Target Lost"),
            new Rule(Severity.INFO, @"PLND:\ PrecLoiter\ enabled", @"PLND: PrecLoiter enabled"),
            new Rule(Severity.INFO, @"PLND:\ PrecLoiter\ disabled", @"PLND: PrecLoiter disabled"),
            new Rule(Severity.INFO, @"PLND:\ Loaded", @"PLND: Loaded"),
            new Rule(Severity.NOTICE, @"TerrAvoid:\ airbraking\ stopping", @"TerrAvoid: airbraking stopping"),
            new Rule(Severity.NOTICE, @"TerrAvoid:\ airbraking\ Starting", @"TerrAvoid: airbraking Starting"),
            new Rule(Severity.NOTICE, @"TerrAvoid:\ current_location\ NIL", @"TerrAvoid: current_location NIL"),
            new Rule(Severity.NOTICE, @"TerrAvoid:\ wp_distance\ NIL", @"TerrAvoid: wp_distance NIL"),
            new Rule(Severity.NOTICE, @"TerrAvoid:\ activated", @"TerrAvoid: activated"),
            new Rule(Severity.NOTICE, @"TerrAvoid:\ deactivated", @"TerrAvoid: deactivated"),
            new Rule(Severity.NOTICE, @"TerrAvoid:\ exit\ Quading\ NOT\ QLoiter", @"TerrAvoid: exit Quading NOT QLoiter"),
            new Rule(Severity.NOTICE, @"TerrAvoid:\ Must\ run\ on\ QuadPlane\ with\ terrain\ follow", @"TerrAvoid: Must run on QuadPlane with terrain follow"),
            new Rule(Severity.INFO, @"TerrAvoid:\ Pitching\ started", @"TerrAvoid: Pitching started"),
            new Rule(Severity.INFO, @"TerrAvoid:\ high\ terrain\ detected", @"TerrAvoid: high terrain detected"),
            new Rule(Severity.INFO, @"TerrAvoid:\ terrain\ Ok", @"TerrAvoid: terrain Ok"),
            new Rule(Severity.INFO, @"TerrAvoid:\ Pitching\ DONE", @"TerrAvoid: Pitching DONE"),
            new Rule(Severity.INFO, @"TerrAvoid:\ Quading\ started", @"TerrAvoid: Quading started"),
            new Rule(Severity.INFO, @"TerrAvoid:\ Stop\ Pitching", @"TerrAvoid: Stop Pitching"),
            new Rule(Severity.INFO, @"TerrAvoid:\ Quading\ DONE", @"TerrAvoid: Quading DONE"),
            new Rule(Severity.INFO, @"TerrAvoid:\ avoid_terrain\ no\ current_location", @"TerrAvoid: avoid_terrain no current_location"),
            new Rule(Severity.INFO, @"TerrAvoid:\ close\ to\ home", @"TerrAvoid: close to home"),
            new Rule(Severity.INFO, @"TerrAvoid:\ away\ from\ home", @"TerrAvoid: away from home"),
            new Rule(Severity.INFO, @"Terrain\ Avoid\ 4\.6\.0\-023\ script\ loaded", @"Terrain Avoid 4.6.0-023 script loaded"),
            new Rule(Severity.CRITICAL, @"TerrAvoid: (Pitching|Quading|CMTC) (-?[0-9]+(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?) above: (-?[0-9]+(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?)", @"TerrAvoid: {0} {1} above: {2}"),
            new Rule(Severity.INFO, @"TerrAvoid: reset Alt: (-?[0-9]+(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?)", @"TerrAvoid: reset Alt: {0}"),
            new Rule(Severity.INFO, @"TerrAvoid: Quading repeat: (-?[0-9]+(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?)", @"TerrAvoid: Quading repeat: {0}"),
            new Rule(Severity.ERROR, @"TerrAvoid: Quading overrides CMTC: (-?[0-9]+(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?)", @"TerrAvoid: Quading overrides CMTC: {0}"),
            new Rule(Severity.ERROR, @"TerrAvoid: CMTC to ALREADY ACTIVE: (-?[0-9]+(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?)", @"TerrAvoid: CMTC to ALREADY ACTIVE: {0}"),
            new Rule(Severity.ERROR, @"TerrAvoid: failed to set alt: (-?[0-9]+(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?)", @"TerrAvoid: failed to set alt: {0}"),
            new Rule(Severity.INFO, @"TerrAvoid: Obstacle down: (-?[0-9]+\.[0-9]{2}) m", @"TerrAvoid: Obstacle down: {0} m"),
            new Rule(Severity.INFO, @"TerrAvoid: Obstacle forward: (-?[0-9]+\.[0-9]{2}) m", @"TerrAvoid: Obstacle forward: {0} m"),
            new Rule(Severity.NOTICE, @"TerrAvoid: airbrake current (-?[0-9]+\.[0-9]{6}) error (-?[0-9]+\.[0-9]{6}) new (-?[0-9]+\.[0-9]{6})", @"TerrAvoid: airbrake current {0} error {1} new {2}"),
            new Rule(Severity.ERROR, @"TerrAvoid: failed to set alt: (-?[0-9]+) frame: ([0-9]+) current frame: ([0-9]+)", @"TerrAvoid: failed to set alt: {0} frame: {1} current frame: {2}"),
            new Rule(Severity.INFO, @"TerrAvoid: CMTC loiter (left|right) to (-?[0-9]+) alt", @"TerrAvoid: CMTC loiter {0} to {1} alt"),
            new Rule(Severity.INFO, @"TerrAvoid: CMTC Done alt: (-?[0-9]+)", @"TerrAvoid: CMTC Done alt: {0}"),
            new Rule(Severity.INFO, @"TerrAvoid: CMTC STOP alt curr (-?[0-9]+) trg (-?[0-9]+) max (-?[0-9]+\.[0-9])", @"TerrAvoid: CMTC STOP alt curr {0} trg {1} max {2}"),
        };

        private static readonly Rule[] AppletHeli =
        {
            new Rule(Severity.CRITICAL, @"LUA:\ IM_COL\ tune\ complete", @"LUA: IM_COL tune complete"),
            new Rule(Severity.CRITICAL, @"LUA:\ IM_COL\ setter\ stopped", @"LUA: IM_COL setter stopped"),
            new Rule(Severity.ERROR, @"LUA: (IM_STB_COL_[23]) = ([0-9]+)", @"LUA: {0} = {1}"),
            new Rule(Severity.ERROR, @"LUA: Failed get (IM_STB_COL_[1234])", @"LUA: Failed get {0}"),
        };

        // Call only after the common pinned-firmware and complete-message checks.
        private static string MatchOfficialApplets(Vehicle vehicle, byte severity, string text)
        {
            switch (vehicle)
            {
                case Vehicle.FIXED_WING:
                case Vehicle.VTOL_DUOROTOR:
                case Vehicle.VTOL_QUADROTOR:
                case Vehicle.VTOL_TILTROTOR:
                    return Match(AppletPlane, severity, text) ?? Match(AppletCommon, severity, text);
                case Vehicle.HELICOPTER:
                    return Match(AppletHeli, severity, text) ?? Match(AppletCommon, severity, text);
                case Vehicle.QUADROTOR:
                case Vehicle.COAXIAL:
                case Vehicle.HEXAROTOR:
                case Vehicle.OCTOROTOR:
                case Vehicle.TRICOPTER:
                case Vehicle.DODECAROTOR:
                case Vehicle.GROUND_ROVER:
                case Vehicle.SURFACE_BOAT:
                    return Match(AppletCommon, severity, text);
                default:
                    return null;
            }
        }
    }
}
