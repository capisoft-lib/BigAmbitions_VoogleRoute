using System;
using BAModAPI;
using BaPlayerLocation.Subscriber;
using Buildings;
using Streets;
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
        private static float _lastDestinationChangeTime = -1f;
        private static bool _destinationRecalcPending;
        private static PathResult _activePath;
        private static bool _warnedMissingLibrary;
        private static bool _wasMapOverlayActive;
        private static float _nextFootLineRefresh;
        private static float _nextFootPathRecalc;
        private static Action<bool> _onCityMapToggled;
        private static Action<Address> _onEnterBuilding;
        private static Action<Address> _onEnterBuildingDelayed;
        private static Action<Address> _onExitBuilding;

        internal static void Initialize(ModContext context)
        {
            _ = context;

            ModLog.Info("VoogleRoute loop initializing (requires LIB_BaPlayerLocation).");
            PlayerLocationSession.Initialize();
            PlayerLocationSession.Changed += OnPlayerLocationChanged;
            PlayerLocationLogger.Initialize();
            RouteGraphStore.WarmUp();
            SubwayStationStore.WarmUp();
            _onCityMapToggled = MapOverlayDiagnostics.OnCityMapToggled;
            GlobalEvents.onCityMapToggle =
                (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, _onCityMapToggled);
            _onEnterBuilding = OnEnterBuilding;
            _onEnterBuildingDelayed = OnEnterBuildingDelayed;
            _onExitBuilding = OnExitBuilding;
            GlobalEvents.onEnterBuilding =
                (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuilding, _onEnterBuilding);
            GlobalEvents.onEnterBuildingDelayed =
                (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuildingDelayed, _onEnterBuildingDelayed);
            GlobalEvents.onExitBuilding =
                (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, _onExitBuilding);
            ModLog.Info("VoogleRoute loop initialized.");
        }

        internal static void Shutdown()
        {
            ModLog.Info("VoogleRoute loop shutting down.");
            if (_onCityMapToggled != null)
            {
                GlobalEvents.onCityMapToggle =
                    (Action<bool>)Delegate.Remove(GlobalEvents.onCityMapToggle, _onCityMapToggled);
                _onCityMapToggled = null;
            }

            if (_onEnterBuilding != null)
            {
                GlobalEvents.onEnterBuilding =
                    (Action<Address>)Delegate.Remove(GlobalEvents.onEnterBuilding, _onEnterBuilding);
                _onEnterBuilding = null;
            }

            if (_onEnterBuildingDelayed != null)
            {
                GlobalEvents.onEnterBuildingDelayed =
                    (Action<Address>)Delegate.Remove(GlobalEvents.onEnterBuildingDelayed, _onEnterBuildingDelayed);
                _onEnterBuildingDelayed = null;
            }

            if (_onExitBuilding != null)
            {
                GlobalEvents.onExitBuilding =
                    (Action<Address>)Delegate.Remove(GlobalEvents.onExitBuilding, _onExitBuilding);
                _onExitBuilding = null;
            }

            IndoorNavigationService.Reset();
            PlayerLocationSession.Changed -= OnPlayerLocationChanged;
            PlayerLocationLogger.Shutdown();
            PlayerLocationSession.Shutdown();
            RouteGraphStore.Invalidate();
            SubwayStationStore.Invalidate();
            _activePath = PathResult.None;
            _lastNavigationContextActive = false;
            _lastNavigationWanted = false;
            _lastDestinationChangeTime = -1f;
            _destinationRecalcPending = false;
            _warnedMissingLibrary = false;
            RouteRecalcBanner.ForceHide();
            NavigationArrivalService.Reset();
            _wasSubwayRidingForNav = false;
            ModLog.Info("VoogleRoute loop shut down.");
        }

        internal static void Tick()
        {
            ModUiText.PollLanguageChange();
            RouteRecalcBanner.Tick();
            RouteSettingsUi.TickOverlay();
            AutoDriveConfirmPopup.TickOverlay();
            BookmarkDataSaveStore.Tick();
            VisitHistoryPanel.Tick();
            VisitHistoryPanel.TickOverlay();
            CityMapBookmarksPanel.Tick();
            CityMapBuildingNavBar.Tick();
            CityMapClickService.Tick();
            NavigationScreenRecovery.Tick();

            if (GameState.IsWorldReady() && !SubwayStationStore.TryEnsureLoaded())
                SubwayStationStore.WarmUp();

            BookmarkRouteDistanceService.TickMainThread();

            if (ShouldRefreshHud())
            {
                RouteActionPanel.UpdateVisibility();
                NavigationHudDiagnostics.Tick();
            }

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

                if (ModConfig.DisplayInsideEnabled)
                    IndoorNavigationService.Tick();
                else
                    IndoorNavigationService.Reset();

                return;
            }

            IndoorNavigationService.Reset();

            if (ModConfig.WantsRouteComputation && GameState.IsWorldReady())
                SyncMapDestination();

            var mapOverlayActive = GameState.ShouldRunMapRouteOverlay();
            if (mapOverlayActive)
            {
                if (!_wasMapOverlayActive)
                {
                    _wasMapOverlayActive = true;
                    PathFinderService.EnsureCacheMatchesMovementMode();
                    _forceRouteRecalc = true;
                    MapOverlayDiagnostics.LogNavigateState("overlay_started");
                }

                TickMapRouteOverlay();
            }
            else if (_wasMapOverlayActive)
            {
                _wasMapOverlayActive = false;
                MapOverlayDiagnostics.LogRouteHidden("overlay_ended");
                CityMapRouteLineRenderer.Hide();
            }

            if (!GameState.ShouldRunNavigationSystems())
            {
                if (!mapOverlayActive)
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
                NavigationArrivalService.Tick();
                CleanupNavigationState();
                return;
            }

            if (PathFinderService.TickAsyncRecalc() || PathFinderService.ConsumeAsyncRefreshRequest())
                RefreshRouteDisplayFromCache();

            TickFootRouteRefresh(canNavigate, navigationWanted);

            if (AutoWalkService.Tick(canNavigate, _activePath))
                RouteActionPanel.RefreshVisual();

            NavigationArrivalService.Tick();
        }

        private static void TickFootRouteRefresh(bool canNavigate, bool navigationWanted)
        {
            if (!canNavigate || !navigationWanted ||
                MovementModeDetector.CurrentMode != MovementMode.OnFoot)
                return;

            var now = Time.unscaledTime;

            if (ModConfig.RouteLineEnabled && _activePath.Success && now >= _nextFootLineRefresh)
            {
                _nextFootLineRefresh = now + 0.12f;
                if (NavigationProximityService.IsNearActiveDestination())
                {
                    RouteLineRenderer.Hide();
                    NavigationArrivalService.TryCompleteNearbyDestination();
                }
                else
                {
                    RouteLineRenderer.ShowPath(_activePath);
                }
            }

            if (now < _nextFootPathRecalc)
                return;

            _nextFootPathRecalc = now + ModConfig.RecalcIntervalSeconds;
            RefreshRouteIfNavigating("foot_interval");
        }

        private static void OnEnterBuilding(Address address)
        {
            _ = address;
            AutoWalkService.Reset();
            if (ModConfig.AutoWalkEnabled)
                ModConfig.SetAutoWalkEnabled(false, persist: false);

            if (ModConfig.RouteLineEnabled && !ModConfig.IndoorRouteLineEnabled)
                ModConfig.SetIndoorRouteLineEnabled(true, persist: false);

            RouteSettingsUi.Close();
            VisitHistoryPanel.Close();
            RouteActionPanel.RefreshVisual();
        }

        private static void OnEnterBuildingDelayed(Address address)
        {
            QuickBookmarkStore.OnEnterBuildingDelayed(address);
            VisitHistoryStore.OnEnterBuildingDelayed(address);
        }

        private static void OnExitBuilding(Address address)
        {
            _ = address;
            IndoorNavigationService.OnBuildingExited();
            PlayerNavigationRelease.Release();
            NavigationArrivalService.TryCompleteNearbyDestination();
            RouteSettingsUi.Close();
        }

        private static void OnPlayerLocationChanged(PlayerLocationSnapshot snapshot)
        {
            _ = snapshot;

            if (!GameState.IsPlayable())
                return;

            // City map pauses the game — position-based work runs once when the map opens.
            if (GameState.IsCityMapOpen())
                return;

            if (GameState.IsIndoorNavigationContext())
            {
                if (MovementModeDetector.ModeChangedSinceLastApply)
                    IndoorPathFinderService.InvalidateCache();

                return;
            }

            if (!GameState.ShouldRunNavigationSystems())
            {
                if (GameState.ShouldRunMapRouteOverlay() && MovementModeDetector.ModeChangedSinceLastApply)
                {
                    InvalidateRouteCache("movement_mode_changed_map");
                    _forceRouteRecalc = true;
                }

                return;
            }

            if (MovementModeDetector.ModeChangedSinceLastApply)
            {
                if (MovementModeDetector.PreviousMode == MovementMode.Vehicle &&
                    MovementModeDetector.CurrentMode == MovementMode.OnFoot)
                {
                    PlayerNavigationRelease.Release();
                    ModLog.Info("Released mod navigation after exiting vehicle.");
                }

                InvalidateRouteCache("movement_mode_changed");
                if (MovementModeDetector.CurrentMode != MovementMode.OnFoot)
                    AutoWalkService.Reset();
                else
                    AutoWalkService.ResetSubwayState();
            }

            if (MovementModeDetector.CurrentMode == MovementMode.OnFoot &&
                TryDetectSubwayRideCompleted())
            {
                AutoWalkService.OnSubwayRideCompleted();
                InvalidateRouteCache("subway_ride_complete");
                _forceRouteRecalc = true;
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

            if (NavigationProximityService.IsNearActiveDestination())
            {
                RouteLineRenderer.Hide();
                _activePath = PathResult.None;
                NavigationArrivalService.TryCompleteNearbyDestination();
                return;
            }

            if (PathFinderService.IsAsyncRecalcInProgress)
                return;

            if (_forceRouteRecalc && showLine &&
                PathFinderService.TryGetCachedRouteForDisplay(out var previewPath))
                RouteLineRenderer.ShowPath(previewPath);

            _activePath = PathFinderService.GetRoute(_forceRouteRecalc, requestSource);
            _forceRouteRecalc = false;

            if (showLine && !NavigationProximityService.IsNearActiveDestination())
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

            if (!NavigationTargetTracker.IsModNavigationSource)
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
            PathFinderService.InvalidateCacheAndForceRecalc(reason);
            _forceRouteRecalc = true;
            _activePath = PathResult.None;
            RouteLineRenderer.Hide();
        }

        /// <summary>Called when mod options change routing — invalidates cache and recomputes immediately if possible.</summary>
        internal static void RequestRouteRecalc(string reason)
        {
            InvalidateRouteCache(reason);
            BookmarkRouteDistanceService.Cancel();

            if (!ModConfig.WantsRouteComputation)
                return;

            if (PathFinderService.IsAsyncRecalcInProgress)
                return;

            if (CanNavigate())
                RefreshRouteIfNavigating(reason);

            if (_wasMapOverlayActive && CanNavigate())
                RefreshMapRouteIfNavigating(reason + "|map");
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

            if (showLine && !NavigationProximityService.IsNearActiveDestination())
                RouteLineRenderer.ShowPath(_activePath);
            else
            {
                RouteLineRenderer.Hide();
                if (NavigationProximityService.IsNearActiveDestination())
                    NavigationArrivalService.TryCompleteNearbyDestination();
            }
        }

        private static void TickMapRouteOverlay()
        {
            if (!ModConfig.WantsRouteComputation)
            {
                MapOverlayDiagnostics.LogOverlayBlocked("route_computation_disabled");
                CityMapRouteLineRenderer.Hide();
                return;
            }

            if (_destinationRecalcPending && TryRefreshDestinationRecalc())
                _destinationRecalcPending = false;

            if (!ModConfig.RouteLineEnabled)
            {
                MapOverlayDiagnostics.LogOverlayBlocked("route_line_disabled");
                CityMapRouteLineRenderer.Hide();
                return;
            }

            var canNavigate = CanNavigate();

            if (!canNavigate)
            {
                MapOverlayDiagnostics.LogOverlayBlocked(DescribeNavigateBlockReason());
                CityMapRouteLineRenderer.Hide();
                return;
            }

            if (PathFinderService.TickAsyncRecalc() || PathFinderService.ConsumeAsyncRefreshRequest())
                RefreshMapRouteDisplayFromCache();
            else if (_forceRouteRecalc && !PathFinderService.IsAsyncRecalcInProgress)
                RefreshMapRouteIfNavigating("map_overlay_open");
        }

        private static string DescribeNavigateBlockReason()
        {
            if (!PlayerLocationSession.IsLibraryActive)
                return "lib_ba_player_location_inactive";

            if (!NavigationTargetTracker.HasMapGpsTarget)
                return "no_map_gps_target";

            if (!NavigationTargetTracker.IsModNavigationSource)
                return "destination_source_not_map(" + NavigationTargetTracker.LastSource + ")";

            if (MovementModeDetector.CurrentMode == MovementMode.Subway)
                return "movement_subway";

            if (MovementModeDetector.CurrentMode is not (MovementMode.OnFoot or MovementMode.Vehicle))
                return "movement_unavailable(" + MovementModeDetector.CurrentMode + ")";

            return "unknown";
        }

        private static void RefreshMapRouteIfNavigating(string requestSource = "map_overlay")
        {
            if (!CanNavigate() || !ModConfig.WantsRouteComputation)
            {
                CityMapRouteLineRenderer.Hide();
                return;
            }

            if (PathFinderService.IsAsyncRecalcInProgress)
                return;

            var showLine = ModConfig.RouteLineEnabled;

            if (_forceRouteRecalc &&
                showLine &&
                PathFinderService.TryGetCachedRouteForDisplay(out var previewPath))
                RouteLineRenderer.ShowPath(previewPath);

            var path = PathFinderService.GetRoute(_forceRouteRecalc, requestSource);
            _forceRouteRecalc = false;

            if (!showLine)
            {
                CityMapRouteLineRenderer.Hide();
                return;
            }

            if (path.Success)
                RouteLineRenderer.ShowPath(path);
            else if (!PathFinderService.IsAsyncRecalcInProgress)
                CityMapRouteLineRenderer.Hide();
        }

        private static void RefreshMapRouteDisplayFromCache()
        {
            if (!CanNavigate() || !ModConfig.WantsRouteComputation)
            {
                CityMapRouteLineRenderer.Hide();
                return;
            }

            if (ModConfig.RouteLineEnabled &&
                PathFinderService.TryGetCachedRouteForDisplay(out var path))
                RouteLineRenderer.ShowPath(path);
            else
                CityMapRouteLineRenderer.Hide();
        }

        private static bool _wasSubwayRidingForNav;

        private static bool TryDetectSubwayRideCompleted()
        {
            var riding = false;
            try
            {
                riding = SubwaySystem.IsRiding;
            }
            catch
            {
                return false;
            }

            if (riding)
            {
                _wasSubwayRidingForNav = true;
                return false;
            }

            if (!_wasSubwayRidingForNav)
                return false;

            _wasSubwayRidingForNav = false;
            return true;
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
