using UnityEngine;
using VoogleRoute.UI;

namespace VoogleRoute.VisualTests
{
    internal readonly struct VisualTestScreenBounds
    {
        internal VisualTestScreenBounds(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        internal int X { get; }
        internal int Y { get; }
        internal int Width { get; }
        internal int Height { get; }
        internal bool IsValid => Width > 0 && Height > 0;
    }

    internal static class VisualTestUiTargets
    {
        internal static bool TryResolveScreenBounds(
            string captureTarget,
            int marginPixels,
            out VisualTestScreenBounds bounds)
        {
            bounds = default;
            if (string.IsNullOrWhiteSpace(captureTarget))
                return false;

            switch (captureTarget.Trim().ToLowerInvariant())
            {
                case "routeactionpanel":
                    return TryPanelBounds(RouteActionPanel.GetVisualTestPanelRect(), marginPixels, out bounds);

                case "mapbookmarkspanel":
                    return TryPanelBounds(CityMapBookmarksPanel.GetVisualTestPanelRect(), marginPixels, out bounds);

                case "visithistorypanel":
                    return TryPanelBounds(VisitHistoryPanel.GetVisualTestPanelRect(), marginPixels, out bounds);

                case "settingspanel":
                    return TryPanelBounds(RouteSettingsUi.GetVisualTestPanelRect(), marginPixels, out bounds);

                case "dualmappanels":
                    return TryDualMapPanelBounds(marginPixels, out bounds);

                default:
                    ModLog.Info("[VisualTest] Unknown captureTarget: " + captureTarget);
                    return false;
            }
        }

        private static bool TryPanelBounds(
            RectTransform panelRect,
            int marginPixels,
            out VisualTestScreenBounds bounds)
        {
            bounds = default;
            if (!VisualTestCapture.TryGetScreenBounds(
                    panelRect,
                    marginPixels,
                    out var x,
                    out var y,
                    out var width,
                    out var height))
                return false;

            bounds = new VisualTestScreenBounds(x, y, width, height);
            return bounds.IsValid;
        }

        private static bool TryDualMapPanelBounds(int marginPixels, out VisualTestScreenBounds bounds)
        {
            bounds = default;
            var bookmarks = CityMapBookmarksPanel.GetVisualTestPanelRect();
            var history = VisitHistoryPanel.GetVisualTestPanelRect();

            if (bookmarks == null && history == null)
                return false;

            if (bookmarks != null && history == null)
                return TryPanelBounds(bookmarks, marginPixels, out bounds);

            if (history != null && bookmarks == null)
                return TryPanelBounds(history, marginPixels, out bounds);

            if (!VisualTestCapture.TryGetScreenBounds(bookmarks, 0, out var lx, out var ly, out var lw, out var lh))
                return TryPanelBounds(history, marginPixels, out bounds);

            if (!VisualTestCapture.TryGetScreenBounds(history, 0, out var rx, out var ry, out var rw, out var rh))
                return TryPanelBounds(bookmarks, marginPixels, out bounds);

            var minX = Mathf.Min(lx, rx) - marginPixels;
            var minY = Mathf.Min(ly, ry) - marginPixels;
            var maxX = Mathf.Max(lx + lw, rx + rw) + marginPixels;
            var maxY = Mathf.Max(ly + lh, ry + rh) + marginPixels;

            minX = Mathf.Clamp(minX, 0, Screen.width - 1);
            minY = Mathf.Clamp(minY, 0, Screen.height - 1);
            maxX = Mathf.Clamp(maxX, minX + 1, Screen.width);
            maxY = Mathf.Clamp(maxY, minY + 1, Screen.height);

            bounds = new VisualTestScreenBounds(minX, minY, maxX - minX, maxY - minY);
            return bounds.IsValid;
        }
    }
}
