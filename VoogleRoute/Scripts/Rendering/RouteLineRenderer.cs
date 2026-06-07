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
        }

        internal static void ApplyStyle()
        {
            FootRouteLineRenderer.ApplyStyle();
            VehicleRouteLineRenderer.ApplyStyle();
        }

        internal static void ShowPath(PathResult path)
        {
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
        }

        internal static void Hide()
        {
            FootRouteLineRenderer.Hide();
            VehicleRouteLineRenderer.Hide();
        }

        internal static void Destroy()
        {
            FootRouteLineRenderer.Destroy();
            VehicleRouteLineRenderer.Destroy();
        }
    }
}
