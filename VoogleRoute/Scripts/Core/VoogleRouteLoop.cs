using BAModAPI;
using UnityEngine;
using VoogleRoute.Navigation;
using VoogleRoute.Rendering;
using VoogleRoute.UI;

namespace VoogleRoute
{
    internal static class VoogleRouteLoop
    {
        private static bool _wasOutdoor = true;
        private static bool _lastNavigationContextActive;
        private static bool _lastNavigationWanted;
        private static float _nextHudRefresh;
        private static bool _legacyTurnHudPurged;

        internal static void Initialize(ModContext context)
        {
            _ = context;
        }

        internal static void Shutdown()
        {
            _lastNavigationContextActive = false;
            _lastNavigationWanted = false;
        }

        internal static void Tick()
        {
            if (!_legacyTurnHudPurged)
            {
                LegacyTurnHudCleanup.DestroyAll();
                _legacyTurnHudPurged = true;
            }

            ModUiText.PollLanguageChange();
            RouteSettingsUi.TickOverlay();

            if (ShouldRefreshHud())
                RouteToggleHud.UpdateVisibility();

            if (!GameState.IsPlayable())
            {
                OnNavigationContextEnded();
                _lastNavigationWanted = false;
                return;
            }

            if (!GameState.ShouldRunNavigationSystems())
            {
                OnNavigationContextEnded();
                return;
            }

            if (!_lastNavigationContextActive)
                PathFinderService.InvalidateCache();
            _lastNavigationContextActive = true;

            var navigationWanted = ModConfig.WantsRouteComputation;
            if (_lastNavigationWanted && !navigationWanted)
                PathFinderService.InvalidateCache();
            _lastNavigationWanted = navigationWanted;

            if (navigationWanted)
                DestinationResolver.Poll();

            var outdoor = GameState.IsOutdoor();
            if (outdoor != _wasOutdoor)
            {
                _wasOutdoor = outdoor;
                PathFinderService.InvalidateCache();
            }

            MovementModeDetector.Tick();
            if (MovementModeDetector.ModeChangedSinceLastTick())
            {
                PathFinderService.InvalidateCache();
                if (MovementModeDetector.CurrentMode != MovementMode.OnFoot)
                    AutoWalkService.Reset();
            }

            var canNavigate = CanNavigate();
            var routeLineEnabled = ModConfig.RouteLineEnabled;

            if (!routeLineEnabled)
                RouteLineRenderer.Hide();

            var showLine = canNavigate && routeLineEnabled;

            if (!canNavigate || !navigationWanted)
            {
                CleanupNavigationState();
                return;
            }

            var path = PathFinderService.GetRoute();

            if (showLine)
                RouteLineRenderer.ShowPath(path);
            else
                RouteLineRenderer.Hide();

            if (AutoWalkService.Tick(canNavigate, path))
                RouteToggleHud.RefreshVisual();
        }

        private static bool CanNavigate()
        {
            if (!NavigationTargetTracker.HasMapGpsTarget)
                return false;

            if (NavigationTargetTracker.LastSource != NavigationTargetTracker.MapSource)
                return false;

            if (MovementModeDetector.CurrentMode == MovementMode.Subway)
                return false;

            return MovementModeDetector.CurrentMode is MovementMode.OnFoot or MovementMode.Vehicle;
        }

        private static void OnNavigationContextEnded()
        {
            if (!_lastNavigationContextActive)
                return;

            _lastNavigationContextActive = false;
            CleanupNavigationState();
            PathFinderService.InvalidateCache();
        }

        private static void CleanupNavigationState()
        {
            RouteLineRenderer.Hide();
            AutoWalkService.Reset();
        }

        private static bool ShouldRefreshHud()
        {
            var now = Time.unscaledTime;
            if (now < _nextHudRefresh)
                return false;

            _nextHudRefresh = now + 0.25f;
            return true;
        }
    }
}
