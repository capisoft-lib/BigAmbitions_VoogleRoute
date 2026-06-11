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
        private static bool _destinationRecalcPending;
        private static PathResult _activePath;
        private static bool _warnedMissingLibrary;

        internal static void Initialize(ModContext context)
        {
            _ = context;

            ModLog.Info("VoogleRoute loop initializing (requires LIB_BaPlayerLocation).");
            PlayerLocationSession.Initialize();
            PlayerLocationSession.Changed += OnPlayerLocationChanged;
            PlayerLocationLogger.Initialize();
            RouteGraphStore.WarmUp();
            ModLog.Info("VoogleRoute loop initialized.");
        }

        internal static void Shutdown()
        {
            ModLog.Info("VoogleRoute loop shutting down.");
            IndoorNavigationService.Reset();
            PlayerLocationSession.Changed -= OnPlayerLocationChanged;
            PlayerLocationLogger.Shutdown();
            PlayerLocationSession.Shutdown();
            RouteGraphStore.Invalidate();
            _activePath = PathResult.None;
            _lastNavigationContextActive = false;
            _lastNavigationWanted = false;
            _lastDestinationChangeTime = -1f;
            _destinationRecalcPending = false;
            _warnedMissingLibrary = false;
            RouteRecalcBanner.ForceHide();
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
            RouteRecalcBanner.Tick();
            RouteSettingsUi.TickOverlay();

            if (ShouldRefreshHud())
                RouteToggleHud.UpdateVisibility();

            if (!GameState.IsPlayable())
            {
                IndoorNavigationService.Reset();
                OnNavigationContextEnded();
                _lastNavigationWanted = false;
                return;
            }

            if (GameState.IsIndoorNavigationContext())
            {
                if (_lastNavigationContextActive)
                    OnNavigationContextEnded();

                IndoorNavigationService.Tick();
                return;
            }

            IndoorNavigationService.Reset();

            if (ModConfig.WantsRouteComputation && GameState.IsWorldReady())
                SyncMapDestination();

            if (!GameState.ShouldRunNavigationSystems())
            {
                OnNavigationContextEnded();
                return;
            }

            if (!_lastNavigationContextActive)
            {
                RouteLineRenderer.InvalidateDisplayCache();
                _forceRouteRecalc = true;
                RouteRecalcDiagnostics.LogCacheInvalidated("navigation_context_started");
            }

            _lastNavigationContextActive = true;

            var navigationWanted = ModConfig.WantsRouteComputation;
            if (_lastNavigationWanted != navigationWanted)
            {
                InvalidateRouteCache(navigationWanted ? "navigation_enabled" : "navigation_disabled");
                _lastNavigationWanted = navigationWanted;
            }

            if (_destinationRecalcPending && TryRefreshDestinationRecalc())
                _destinationRecalcPending = false;

            var outdoor = GameState.IsOutdoor();
            if (outdoor != _wasOutdoor)
            {
                _wasOutdoor = outdoor;
                InvalidateRouteCache(outdoor ? "outdoor_entered" : "indoor_entered");
                RefreshRouteIfNavigating("outdoor_changed");
            }

            if (!ModConfig.RouteLineEnabled)
                RouteLineRenderer.Hide();

            if (_forceRouteRecalc && !PathFinderService.IsAsyncRecalcInProgress)
                RefreshRouteIfNavigating("navigation_resume");

            var canNavigate = CanNavigate();
            if (!canNavigate || !navigationWanted)
            {
                CleanupNavigationState();
                return;
            }

            if (PathFinderService.TickAsyncRecalc() || PathFinderService.ConsumeAsyncRefreshRequest())
                RefreshRouteDisplayFromCache();

            if (AutoWalkService.Tick(canNavigate, _activePath))
                RouteToggleHud.RefreshVisual();
        }

        private static void OnPlayerLocationChanged(PlayerLocationSnapshot snapshot)
        {
            _ = snapshot;

            if (!GameState.IsPlayable())
                return;

            if (GameState.IsIndoorNavigationContext())
            {
                if (MovementModeDetector.ModeChangedSinceLastApply)
                    IndoorPathFinderService.InvalidateCache();

                return;
            }

            if (!GameState.ShouldRunNavigationSystems())
                return;

            if (MovementModeDetector.ModeChangedSinceLastApply)
            {
                InvalidateRouteCache("movement_mode_changed");
                if (MovementModeDetector.CurrentMode != MovementMode.OnFoot)
                    AutoWalkService.Reset();
            }

            RefreshRouteIfNavigating("player_location");
        }

        private static void SyncMapDestination()
        {
            DestinationResolver.Poll();

            if (!ModConfig.WantsRouteComputation)
                return;

            if (NavigationTargetTracker.LastChangeTime == _lastDestinationChangeTime)
                return;

            _lastDestinationChangeTime = NavigationTargetTracker.LastChangeTime;
            InvalidateRouteCache("destination_changed");
            _destinationRecalcPending = true;
        }

        private static bool TryRefreshDestinationRecalc()
        {
            if (!ModConfig.WantsRouteComputation || !CanNavigate())
                return false;

            RefreshRouteIfNavigating("destination_changed");
            return true;
        }

        private static void RefreshRouteIfNavigating(string requestSource = "tick")
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

            if (PathFinderService.IsAsyncRecalcInProgress)
                return;

            if (_forceRouteRecalc && showLine &&
                PathFinderService.TryGetCachedRouteForDisplay(out var previewPath))
                RouteLineRenderer.ShowPath(previewPath);

            _activePath = PathFinderService.GetRoute(_forceRouteRecalc, requestSource);
            _forceRouteRecalc = false;

            if (showLine)
                RouteLineRenderer.ShowPath(_activePath);
            else
                RouteLineRenderer.Hide();
        }

        private static bool CanNavigate()
        {
            if (!PlayerLocationSession.IsLibraryActive)
            {
                if (!_warnedMissingLibrary)
                {
                    _warnedMissingLibrary = true;
                    ModLog.Info(
                        "[WARN] Navigation blocked: enable LIB_BaPlayerLocation in Mods. " +
                        "Voogle Route does not read player position on its own.");
                }

                return false;
            }

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

        private static void InvalidateRouteCache(string reason)
        {
            PathFinderService.InvalidateCache(reason);
            _forceRouteRecalc = true;
        }

        private static void CleanupNavigationState()
        {
            RouteLineRenderer.Hide();
            RouteRecalcBanner.ForceHide();
            AutoWalkService.Reset();
            _activePath = PathResult.None;
        }

        private static void RefreshRouteDisplayFromCache()
        {
            var navigationWanted = ModConfig.WantsRouteComputation;
            var canNavigate = CanNavigate();
            var showLine = canNavigate && navigationWanted && ModConfig.RouteLineEnabled;

            if (!canNavigate || !navigationWanted)
            {
                CleanupNavigationState();
                return;
            }

            _activePath = PathFinderService.TryGetCachedRouteForDisplay(out var path)
                ? path
                : PathResult.None;

            if (showLine)
                RouteLineRenderer.ShowPath(_activePath);
            else
                RouteLineRenderer.Hide();
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
