using VoogleRoute.Rendering;
using VoogleRoute.UI;

namespace VoogleRoute.Navigation
{
    internal static class IndoorNavigationService
    {
        private static bool _wasIndoorActive;
        private static bool _forceRecalc;
        private static PathResult _activePath;

        internal static void Tick()
        {
            if (!GameState.IsIndoorNavigationContext())
            {
                if (_wasIndoorActive)
                    Reset();
                return;
            }

            if (!_wasIndoorActive)
            {
                _wasIndoorActive = true;
                _forceRecalc = true;
                IndoorPathFinderService.InvalidateCache();
            }

            var navigationWanted = ModConfig.IndoorWantsRouteComputation;
            if (!navigationWanted)
            {
                CleanupNavigationState();
                return;
            }

            if (!CanNavigate())
            {
                CleanupNavigationState();
                return;
            }

            var forceRecalc = _forceRecalc;
            _forceRecalc = false;
            _activePath = IndoorPathFinderService.GetRoute(forceRecalc);

            if (ModConfig.IndoorRouteLineEnabled && _activePath.Success)
                RouteLineRenderer.ShowPath(_activePath);
            else
                RouteLineRenderer.Hide();

            var exitTarget = IndoorPathFinderService.ActiveExitTarget;
            if (IndoorAutoWalkService.Tick(true, _activePath, exitTarget))
                RouteToggleHud.RefreshVisual();
        }

        internal static void Reset()
        {
            if (!_wasIndoorActive)
                return;

            _wasIndoorActive = false;
            _forceRecalc = false;
            var hadAutoWalk = ModConfig.IndoorAutoWalkEnabled;
            CleanupNavigationState();
            IndoorPathFinderService.InvalidateCache();

            if (hadAutoWalk)
            {
                ModConfig.SetIndoorAutoWalkEnabled(false);
                RouteToggleHud.RefreshVisual();
            }
        }

        private static bool CanNavigate()
        {
            if (!PlayerLocationSession.IsLibraryActive)
                return false;

            if (MovementModeDetector.CurrentMode != MovementMode.OnFoot)
                return false;

            return MovementModeDetector.TryGetPathOrigin(out _);
        }

        private static void CleanupNavigationState()
        {
            RouteLineRenderer.Hide();
            IndoorAutoWalkService.Reset();
            _activePath = PathResult.None;
        }
    }
}
