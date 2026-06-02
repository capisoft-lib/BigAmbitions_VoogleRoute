using VoogleRoute.Navigation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VoogleRoute.Rendering;

/// <summary>Délègue au rendu piéton ou véhicule selon le mode.</summary>
public static class RouteLineRenderer
{
    private static MovementMode _lastRenderMode = MovementMode.Unavailable;

    public static void EnsureCreated()
    {
        FootRouteLineRenderer.EnsureCreated();
        VehicleRouteLineRenderer.EnsureCreated();
    }

    public static void ApplyStyle()
    {
        FootRouteLineRenderer.ApplyStyle();
        VehicleRouteLineRenderer.ApplyStyle();
    }

    public static void ShowPath(PathResult path)
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

    public static void Hide()
    {
        FootRouteLineRenderer.Hide();
        VehicleRouteLineRenderer.Hide();
    }

    public static void Destroy()
    {
        FootRouteLineRenderer.Destroy();
        VehicleRouteLineRenderer.Destroy();

        foreach (var legacy in new[] { "VoogleRoute_RouteMesh", "VoogleRoute_RouteRoot" })
        {
            var go = GameObject.Find(legacy);
            if (go != null)
                Object.Destroy(go);
        }
    }
}
