using BAModAPI;
using BaPlayerLocation.Subscriber;
using UnityEngine;
using VoogleRoute.Live;
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
        private static bool _forceRouteRecalc;
        private static float _nextHudRefresh;
        private static bool _legacyTurnHudPurged;
        private static float _lastDestinationChangeTime = -1f;
        private static PathResult _activePath;

        internal static void Initialize(ModContext context)
        {
            _ = context;

            ModLog.Info("VoogleRoute loop initializing.");
            PlayerLocationSession.Initialize();
            PlayerLocationSession.Changed += OnPlayerLocationChanged;
            PlayerLocationLogger.Initialize();
            ModLog.Info("VoogleRoute loop initialized.");
        }

        internal static void Shutdown()
        {
            ModLog.Info("VoogleRoute loop shutting down.");
            PlayerLocationSession.Changed -= OnPlayerLocationChanged;
            PlayerLocationLogger.Shutdown();
            PlayerLocationSession.Shutdown();
            _activePath = PathResult.None;
            _lastNavigationContextActive = false;
            _lastNavigationWanted = false;
            _lastDestinationChangeTime = -1f;
            ModLog.Info("VoogleRoute loop shut down.");
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
            {
                RouteLineRenderer.InvalidateDisplayCache();
                _forceRouteRecalc = true;
            }

            _lastNavigationContextActive = true;

            var navigationWanted = ModConfig.WantsRouteComputation;
            if (_lastNavigationWanted != navigationWanted)
            {
                InvalidateRouteCache();
                _lastNavigationWanted = navigationWanted;
            }

            if (navigationWanted && GameState.IsWorldReady())
                PollDestination();

            var outdoor = GameState.IsOutdoor();
            if (outdoor != _wasOutdoor)
            {
                _wasOutdoor = outdoor;
                InvalidateRouteCache();
                RefreshRouteIfNavigating();
            }

            if (!ModConfig.RouteLineEnabled)
                RouteLineRenderer.Hide();

            if (_forceRouteRecalc)
                RefreshRouteIfNavigating();

            var canNavigate = CanNavigate();
            if (!canNavigate || !navigationWanted)
            {
                CleanupNavigationState();
                return;
            }

            if (AutoWalkService.Tick(canNavigate, _activePath))
                RouteToggleHud.RefreshVisual();
        }

        private static void OnPlayerLocationChanged(PlayerLocationSnapshot snapshot)
        {
            _ = snapshot;

            if (!GameState.IsPlayable() || !GameState.ShouldRunNavigationSystems())
                return;

            if (MovementModeDetector.ModeChangedSinceLastApply)
            {
                InvalidateRouteCache();
                if (MovementModeDetector.CurrentMode != MovementMode.OnFoot)
                    AutoWalkService.Reset();
            }

            RefreshRouteIfNavigating();
        }

        private static void PollDestination()
        {
            DestinationResolver.Poll();

            if (!ModConfig.WantsRouteComputation)
                return;

            if (NavigationTargetTracker.LastChangeTime == _lastDestinationChangeTime)
                return;

            _lastDestinationChangeTime = NavigationTargetTracker.LastChangeTime;
            InvalidateRouteCache();
            RefreshRouteIfNavigating();
        }

        private static void RefreshRouteIfNavigating()
        {
            var navigationWanted = ModConfig.WantsRouteComputation;
            var canNavigate = CanNavigate();
            var showLine = canNavigate && navigationWanted && ModConfig.RouteLineEnabled;

            if (!canNavigate || !navigationWanted)
            {
                CleanupNavigationState();
                _activePath = PathResult.None;
                return;
            }

            if (_forceRouteRecalc && showLine &&
                PathFinderService.TryGetCachedRouteForDisplay(out var previewPath))
                RouteLineRenderer.ShowPath(previewPath);

            _activePath = PathFinderService.GetRoute(_forceRouteRecalc);
            _forceRouteRecalc = false;

            if (showLine)
                RouteLineRenderer.ShowPath(_activePath);
            else
                RouteLineRenderer.Hide();
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
        }

        private static void InvalidateRouteCache()
        {
            PathFinderService.InvalidateCache();
            _forceRouteRecalc = true;
        }

        private static void CleanupNavigationState()
        {
            RouteLineRenderer.Hide();
            AutoWalkService.Reset();
            _activePath = PathResult.None;
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
