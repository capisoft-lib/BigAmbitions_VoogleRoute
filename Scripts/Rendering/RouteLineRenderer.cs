using VoogleRoute.Navigation;

namespace VoogleRoute.Rendering
{
    internal static class RouteLineRenderer
    {
        private static MovementMode _lastRenderMode = MovementMode.Unavailable;

        internal static void EnsureCreated()
        {
            FootRouteLineRenderer.EnsureCreated();
            VehicleRouteLineRenderer.EnsureCreated();
            CityMapRouteLineRenderer.EnsureCreated();
            RouteLineDetectionRenderer.EnsureCreated();
            ModLog.Info("Route line renderers initialized (foot + vehicle + city map).");
        }

        internal static void ApplyStyle()
        {
            FootRouteLineRenderer.ApplyStyle();
            VehicleRouteLineRenderer.ApplyStyle();
            CityMapRouteLineRenderer.ApplyStyle();
        }

        internal static void ShowPath(PathResult path)
        {
            if (GameState.IsCityMapOpen())
            {
                FootRouteLineRenderer.Hide();
                VehicleRouteLineRenderer.Hide();
                RouteLineDetectionRenderer.Hide();
                CityMapRouteLineRenderer.ShowPath(path);
                return;
            }

            CityMapRouteLineRenderer.Hide();

            var mode = MovementModeDetector.CurrentMode;
            if (mode != _lastRenderMode)
            {
                _lastRenderMode = mode;
                LineRendererPathCache.Reset();
            }

            if (mode == MovementMode.Vehicle)
            {
                FootRouteLineRenderer.Hide();
                VehicleRouteLineRenderer.ShowPath(path);
            }
            else
            {
                VehicleRouteLineRenderer.Hide();
                FootRouteLineRenderer.ShowPath(path);
            }

            RouteLineDetectionRenderer.ShowPath(path);
        }

        internal static void Hide()
        {
            FootRouteLineRenderer.Hide();
            VehicleRouteLineRenderer.Hide();
            CityMapRouteLineRenderer.Hide();
            RouteLineDetectionRenderer.Hide();
        }

        internal static void InvalidateDisplayCache()
        {
            _lastRenderMode = MovementMode.Unavailable;
            LineRendererPathCache.Reset();
        }

        internal static void Destroy()
        {
            FootRouteLineRenderer.Destroy();
            VehicleRouteLineRenderer.Destroy();
            CityMapRouteLineRenderer.Destroy();
            RouteLineDetectionRenderer.Destroy();
        }
    }
}
