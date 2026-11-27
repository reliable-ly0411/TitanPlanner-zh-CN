using System;
using System.Collections.Generic;

namespace MissionPlanner.Utilities
{
    public enum ModeMenuPresentation
    {
        Unspecified,
        Hover,
        Circle,
        Rover,
        Tracker
    }

    /// <summary>Presentation only: the source mode remains available for commands and comparisons.</summary>
    public sealed class FlightModeDisplay
    {
        public string Primary { get; }
        public string Secondary { get; }
        public bool IsBilingual => Secondary.Length != 0;
        public string AccessibleText => IsBilingual ? Primary + "\n" + Secondary : Primary;

        private FlightModeDisplay(string primary, string secondary)
        {
            Primary = primary;
            Secondary = secondary;
        }

        // Use mode vocabulary only. A custom mode named like an unrelated UI key
        // (for example, "Settings") must not be translated by the general catalog.
        private static readonly HashSet<string> KnownMenuModes = new HashSet<string>(StringComparer.Ordinal)
        {
            "Acro", "Alt Hold", "Althold", "Altitude Control", "Auto", "Auto RTL",
            "Auto: Landing", "Auto: Ready", "Auto: Takeoff", "Autoland", "Autorotate", "Autotune",
            "Avoid Adsb", "Brake", "Circle", "Cruise", "Dock", "Drift",
            "FBWA", "FBWB", "Flip", "Flowhold", "Fly By Wire A", "Fly By Wire B",
            "Follow", "Guided", "Guided Nogps", "Heli Autorotate", "Hold", "Initialising",
            "Initializing", "Land", "Loiter", "Loiter Alt QLAND", "Loiter To QLAND", "Manual",
            "Motordetect", "Offboard Control", "Poshold", "Position Control", "QACRO", "QAUTOTUNE",
            "QHOVER", "QLAND", "QLOITER", "QRTL", "QSTABILIZE", "RTL",
            "Rattitude", "Scan", "Servo Test", "Simple", "Smart RTL", "Smartrtl",
            "Sport", "Stabalized", "Stabilize", "Steering", "Stop", "Surface",
            "Surftrak", "Systemid", "Takeoff", "Thermal", "Throw", "Training",
            "Turtle", "Zigzag",
        };

        /// <summary>Top-bar menu label only; callers must keep the raw mode for dispatch.</summary>
        public static string EnglishThenChinese(string mode, ModeMenuPresentation presentation = ModeMenuPresentation.Unspecified)
        {
            if (string.IsNullOrEmpty(mode) || !KnownMenuModes.Contains(mode))
                return mode;

            if (System.Globalization.CultureInfo.CurrentUICulture.Name == "zh-CN")
            {
                // AntennaTracker AUTO tracks a target, not a waypoint mission.
                if (presentation == ModeMenuPresentation.Tracker
                    && string.Equals(mode, "Auto", StringComparison.Ordinal))
                    return mode + " · 自动跟踪";

                if (presentation == ModeMenuPresentation.Rover
                    && string.Equals(mode, "Acro", StringComparison.Ordinal))
                    return mode + " · 转向率与速度控制";

                if (string.Equals(mode, "Loiter", StringComparison.Ordinal))
                {
                    switch (presentation)
                    {
                        case ModeMenuPresentation.Hover: return mode + " · 定点悬停";
                        case ModeMenuPresentation.Circle: return mode + " · 定点盘旋";
                        case ModeMenuPresentation.Rover: return mode + " · 定点驻留";
                    }
                }
            }

            var display = FromMode(mode);
            return display.IsBilingual ? mode + " · " + display.Primary : mode;
        }

        public static FlightModeDisplay FromMode(string mode)
        {
            if (string.IsNullOrEmpty(mode))
                return new FlightModeDisplay("---", "");

            var translated = UiText.Translate(mode);
            return string.Equals(translated, mode, StringComparison.Ordinal)
                ? new FlightModeDisplay(mode, "")
                : new FlightModeDisplay(translated, mode);
        }
    }
}
