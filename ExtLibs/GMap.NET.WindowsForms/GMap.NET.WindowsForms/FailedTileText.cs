using System;
using System.Globalization;

namespace GMap.NET.WindowsForms
{
    /// <summary>Display strings for the failed-tile overlay only; does not change the underlying exception.</summary>
    internal static class FailedTileText
    {
        internal const string DefaultEmptyTileText =
            "We are sorry, but we don't\nhave imagery at this zoom\nlevel for this region.";

        internal static string ExceptionText(Exception exception, CultureInfo culture)
        {
            if (culture.Name != "zh-CN")
                return "Exception: " + exception.Message;

            // GMaps.GetImageFrom returns this diagnostic in cache-only mode. Older builds
            // may omit "local tile" or the trailing dots.
            switch (exception.Message.TrimEnd('.'))
            {
                case "No data in local tile cache":
                case "No data in cache":
                    return "瓦片加载失败：本地缓存中没有此瓦片。";
                default:
                    // Server/native diagnostics are arbitrary. Preserve their exact text.
                    return "瓦片加载失败：" + exception.Message;
            }
        }

        internal static string ImageryText(string text, CultureInfo culture)
        {
            // Honor a custom EmptyTileText supplied by a caller, even in enhanced Chinese.
            return culture.Name == "zh-CN" && text == DefaultEmptyTileText
                ? "此区域在当前缩放级别\n暂无地图影像。"
                : text;
        }
    }
}
