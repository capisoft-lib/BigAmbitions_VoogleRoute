using MelonLoader;
using VoogleRoute.Localization;
using VoogleRoute.Navigation;
using VoogleRoute.Rendering;
using VoogleRoute.UI;
using VoogleRoute.Update;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VoogleRoute;

public sealed class Plugin : MelonMod
{
    private static bool _wasOutdoor = true;
    private static bool _lastNavigationContextActive;
    private static float _nextGuidanceRefresh;
    private static float _nextDebugOverlayRefresh;
    private static float _nextHudRefresh;
    private static TurnGuidanceState _cachedGuidance;
    private static bool _lastNavigationWanted;

    public override void OnInitializeMelon()
    {
        ModConfig.Init();
        ModLocalization.EnsureInitialized();
        ModLocalization.LanguageChanged += OnGameLanguageChanged;

        MelonEvents.OnSceneWasLoaded.Subscribe(OnSceneWasLoaded, 0, false);
        RouteLineRenderer.EnsureCreated();
        RouteToggleHud.EnsureCreated();
        TurnNavigationHud.EnsureCreated();
        IntersectionArrowRenderer.EnsureCreated();
        RouteSettingsUi.EnsureCreated();
        UpdateService.Initialize();

        MelonLogger.Msg($"{ModInfo.Name} v{ModInfo.Version} loaded.");
    }

    public override void OnUpdate()
    {
        ModLocalization.PollLanguageChange();
        UpdateService.Tick();

        if (!GameState.IsPlayable())
        {
            if (ShouldRefreshHud())
                RouteToggleHud.UpdateVisibility();
            OnNavigationContextEnded();
            _lastNavigationWanted = false;
            return;
        }

        if (ShouldRefreshHud())
            RouteToggleHud.UpdateVisibility();

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
            _cachedGuidance = default;
            if (MovementModeDetector.CurrentMode != MovementMode.OnFoot)
                AutoWalkService.Reset();
            if (MovementModeDetector.CurrentMode != MovementMode.Vehicle)
                TurnNavigationHud.Clear();
        }

        var canNavigate = CanNavigate(out _);
        var routeLineEnabled = ModConfig.RouteLineEnabled.Value;

        if (!routeLineEnabled)
            RouteLineRenderer.Hide();

        var showLine = canNavigate && routeLineEnabled;
        var needsRoute = canNavigate && navigationWanted;
        PathResult path = PathResult.None;
        TurnGuidanceState guidance = default;
        MovementModeDetector.TryGetPathOrigin(out var playerPos);

        if (!needsRoute)
        {
            CleanupNavigationState();
        }
        else
        {
            path = PathFinderService.GetRoute();

            if (showLine)
                RouteLineRenderer.ShowPath(path);
            else
                RouteLineRenderer.Hide();

            var inVehicle = MovementModeDetector.CurrentMode == MovementMode.Vehicle;

            if (inVehicle && routeLineEnabled && ShouldRefreshGuidance())
                _cachedGuidance = TurnGuidanceService.Update(playerPos, NavigationTargetTracker.ActiveTarget);

            if (inVehicle && routeLineEnabled)
            {
                TurnNavigationHud.Update(_cachedGuidance, true);
                IntersectionArrowRenderer.Update(_cachedGuidance, playerPos, _cachedGuidance.HasGuidance);
            }
            else
            {
                TurnNavigationHud.Clear();
                IntersectionArrowRenderer.Update(default, playerPos, false);
            }

            guidance = _cachedGuidance;
            if (AutoWalkService.Tick(canNavigate, path))
                RouteToggleHud.RefreshVisual();
        }

        if (ShouldRefreshDebugOverlay())
        {
            DebugOverlayHud.Update(
                true,
                MovementModeDetector.CurrentMode,
                outdoor,
                NavigationTargetTracker.HasMapGpsTarget,
                NavigationTargetTracker.HasMapGpsTarget ? NavigationTargetTracker.ActiveTarget : Vector3.zero,
                path,
                guidance);
        }
    }

    private static void OnGameLanguageChanged()
    {
        RouteToggleHud.RefreshLocalizedText();
        RouteSettingsUi.RefreshLocalizedText();
        TurnNavigationHud.Clear();
        _cachedGuidance = default;
        _nextGuidanceRefresh = 0f;
    }

    public override void OnDeinitializeMelon()
    {
        ModLocalization.LanguageChanged -= OnGameLanguageChanged;
        MelonEvents.OnSceneWasLoaded.Unsubscribe(OnSceneWasLoaded);
        UpdateService.Shutdown();
        RouteLineRenderer.Destroy();
        RouteToggleHud.Destroy();
        RouteSettingsUi.Destroy();
        TurnNavigationHud.Destroy();
        IntersectionArrowRenderer.Destroy();
        DebugOverlayHud.Destroy();
        NavigationTargetTracker.ClearMapGpsTarget("mod unload");
        DestinationResolver.Clear();
    }

    private static bool CanNavigate(out string blockReason)
    {
        if (!NavigationTargetTracker.HasMapGpsTarget)
        {
            blockReason = "";
            return false;
        }

        if (NavigationTargetTracker.LastSource != NavigationTargetTracker.MapSource)
        {
            blockReason = "";
            return false;
        }

        if (MovementModeDetector.CurrentMode == MovementMode.Subway)
        {
            blockReason = "";
            return false;
        }

        if (MovementModeDetector.CurrentMode is not (MovementMode.OnFoot or MovementMode.Vehicle))
        {
            blockReason = "";
            return false;
        }

        blockReason = "";
        return true;
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
        TurnNavigationHud.Clear();
        IntersectionArrowRenderer.Update(default, Vector3.zero, false);
        AutoWalkService.Reset();
        _cachedGuidance = default;
    }

    private static bool ShouldRefreshGuidance()
    {
        var now = Time.unscaledTime;
        if (now < _nextGuidanceRefresh)
            return false;

        var interval = MovementModeDetector.CurrentMode == MovementMode.Vehicle ? 0.25f : 0.5f;
        _nextGuidanceRefresh = now + interval;
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

    private static bool ShouldRefreshDebugOverlay()
    {
        if (!ModConfig.ShowDebugOverlay.Value)
            return false;

        var now = Time.unscaledTime;
        if (now < _nextDebugOverlayRefresh)
            return false;

        _nextDebugOverlayRefresh = now + 1f;
        return true;
    }

    private static void OnSceneWasLoaded(Scene scene, int buildIndex)
    {
        TrafficWaypointProvider.OnSceneChanged();
        PathFinderService.InvalidateCache();
        RouteLineRenderer.EnsureCreated();
        RouteToggleHud.EnsureCreated();
        TurnNavigationHud.EnsureCreated();
        IntersectionArrowRenderer.EnsureCreated();
    }
}
