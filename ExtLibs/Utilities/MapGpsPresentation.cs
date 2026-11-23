using System;
using System.Globalization;

namespace MissionPlanner.Utilities
{
    public static class MapGpsPresentation
    {
        public static bool Enhanced =>
            string.Equals(CultureInfo.CurrentUICulture.Name, "zh-CN", StringComparison.Ordinal);

        // Preserve legacy formatting for locales not using the enhanced display.
        public static string LegacyFixLabel(int code)
        {
            switch (code)
            {
                case 0: return "No GPS";
                case 1: return "No Fix";
                case 2: return "2D Fix";
                case 3: return "3D Fix";
                case 4: return "DGPS";
                case 5: return "RTK Float";
                case 6: return "RTK Fixed";
                default: return code.ToString();
            }
        }

        public static string RawFixHelp(int code)
        {
            // Vendored MAVLink common.xml GPS_FIX_TYPE: STATIC=7, PPP=8.
            switch (code)
            {
                case 7: return "STATIC";
                case 8: return "PPP";
                default: return LegacyFixLabel(code);
            }
        }

        public static string FixLabel(int code, bool compact = false)
        {
            string raw = LegacyFixLabel(code);
            if (!Enhanced) return raw;
            if (code < 0 || code > 8) return raw;
            string name = RawFixHelp(code);
            if (compact)
            {
                string shortName = Translate("GPS compact: ", name, raw);
                if (shortName != raw) return shortName;
            }
            return Translate("GPS fix: ", name, raw);
        }

        public static string LegendLabel(string raw) => Translate("Map legend: ", raw, raw);
        public static string ScaleTitle(string raw) => Translate("Map altitude scale: ", raw, raw);

        public static string CoordinateLabel(string raw)
        {
            if (raw != "GEO" && raw != "UTM" && raw != "MGRS") return raw;
            string local = Translate("Map coordinates: ", raw, raw);
            return local == raw ? raw : raw + " · " + local;
        }

        private static string Translate(string scope, string raw, string fallback)
        {
            if (!Enhanced || string.IsNullOrEmpty(raw)) return fallback;
            string key = scope + raw;
            string local = UiText.Translate(key);
            return local == key ? fallback : local;
        }
    }
}
