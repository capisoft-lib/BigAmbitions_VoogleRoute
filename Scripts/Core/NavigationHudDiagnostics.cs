using BaPlayerLocation.Subscriber;
using VoogleRoute.Navigation;

namespace VoogleRoute
{
    /// <summary>Logs when the action panel stays hidden despite an active outdoor navigation context.</summary>
    internal static class NavigationHudDiagnostics
    {
        private static float _nextCheck;
        private static string _lastReason = string.Empty;

        internal static void Tick()
        {
            var now = UnityEngine.Time.unscaledTime;
            if (now < _nextCheck)
                return;

            _nextCheck = now + 8f;

            if (!ModConfig.DisplayOutsideEnabled)
                return;

            var mode = MovementModeDetector.CurrentMode;
            if (mode is not (MovementMode.OnFoot or MovementMode.Vehicle))
                return;

            if (GameState.IsIndoorNavigationContext() || GameState.IsInsideInteriorForDiagnostics())
                return;

            if (GameState.ShouldShowNavigationPanel() && MovementModeDetector.ShouldShowActionPanel())
            {
                _lastReason = string.Empty;
                return;
            }

            var reason = DescribeHiddenReason();
            if (reason == _lastReason)
                return;

            _lastReason = reason;
            ModLog.Info("Navigation HUD hidden: " + reason);
        }

        private static string DescribeHiddenReason()
        {
            if (!GameState.IsWorldReady())
                return "world_not_ready";

            if (!PlayerLocationSession.IsLibraryActive)
                return "missing_LIB_BaPlayerLocation";

            if (!PlayerLocationSession.IsAvailable)
                return "player_location_unavailable";

            if (!ModConfig.DisplayOutsideEnabled)
                return "display_outside_disabled";

            if (GameState.IsOverlayBlockingNavigation())
                return "overlay_blocking";

            if (MovementModeDetector.CurrentMode == MovementMode.Unavailable)
                return "movement_mode_unavailable";

            return "panel_visibility_gate";
        }
    }
}
