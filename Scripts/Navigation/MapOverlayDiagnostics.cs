using BaPlayerLocation.Subscriber;
using UnityEngine;
using VoogleRoute.Navigation;

namespace VoogleRoute
{
    /// <summary>Textual diagnostics for the city map (M) route overlay.</summary>
    internal static class MapOverlayDiagnostics
    {
        private static float _nextPeriodicLog;
        private static string _lastBlockReason = "";

        internal static void OnCityMapToggled(bool open)
        {
            ModLog.Info(
                "City map " + (open ? "opened" : "closed") +
                " | overlay_should_run=" + GameState.ShouldRunMapRouteOverlay() +
                " | " + CityMapLayerHelper.DescribeCameraMask() +
                " | " + CityMapLayerHelper.DescribeMapMask());

            LogNavigateState(open ? "map_opened" : "map_closed");
        }

        internal static void LogNavigateState(string context)
        {
            var destination = DescribeDestination();
            ModLog.Info(
                "Map overlay state | context=" + context +
                " | playable=" + GameState.IsPlayable() +
                " | map_open=" + GameState.IsCityMapOpen() +
                " | route_line=" + ModConfig.RouteLineEnabled +
                " | wants_route=" + ModConfig.WantsRouteComputation +
                " | lib_active=" + PlayerLocationSession.IsLibraryActive +
                " | " + destination +
                " | movement=" + MovementModeDetector.CurrentMode);
        }

        internal static void LogOverlayBlocked(string reason)
        {
            if (_lastBlockReason == reason && Time.unscaledTime - _nextPeriodicLog < 2f)
                return;

            _lastBlockReason = reason;
            _nextPeriodicLog = Time.unscaledTime;

            ModLog.Info(
                "Map route overlay blocked | reason=" + reason +
                " | " + DescribeDestination() +
                " | lib_active=" + PlayerLocationSession.IsLibraryActive +
                " | movement=" + MovementModeDetector.CurrentMode);
        }

        internal static void LogRouteShown(PathResult path, int layer, float lineWidth)
        {
            var points = path.Points ?? System.Array.Empty<Vector3>();
            var first = points.Length > 0 ? points[0].ToString() : "none";
            var last = points.Length > 1 ? points[points.Length - 1].ToString() : first;

            ModLog.Info(
                "Map route shown | success=" + path.Success +
                " | points=" + points.Length +
                " | width=" + lineWidth.ToString("0.##") +
                " | layer=" + layer + " (" + LayerMask.LayerToName(layer) + ")" +
                " | first=" + first +
                " | last=" + last +
                " | " + CityMapLayerHelper.DescribeCameraMask());
        }

        internal static void LogRouteHidden(string reason)
        {
            ModLog.Debug("Map route hidden | reason=" + reason);
        }

        internal static void MaybeLogPeriodicStatus(bool overlayActive, bool canNavigate)
        {
            if (!overlayActive)
                return;

            var now = Time.unscaledTime;
            if (now < _nextPeriodicLog)
                return;

            _nextPeriodicLog = now + 3f;
            ModLog.Debug(
                "Map overlay tick | can_navigate=" + canNavigate +
                " | " + DescribeDestination() +
                " | " + CityMapLayerHelper.DescribeMapMask());
        }

        private static string DescribeDestination()
        {
            if (!NavigationTargetTracker.HasMapGpsTarget)
                return "destination=none";

            return "destination=" + NavigationTargetTracker.ActiveTarget +
                   " source=" + NavigationTargetTracker.LastSource;
        }
    }
}
