using System;

namespace MissionPlanner.Utilities
{
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
