using VoogleRoute.Rendering;
using UnityEngine;
using UnityEngine.AI;

namespace VoogleRoute.Navigation;

/// <summary>Ligne voiture : chemin Gley léger, ou projection route pour repli NavMesh/piéton.</summary>
internal static class VehiclePathPipeline
{
    private const float GleyDecimateSpacing = 22f;
    private const int GleyMaxDisplayPoints = 42;

    internal static Vector3[] BuildTurnCorners(Vector3[] navCorners, NavMeshQueryFilter filter)
    {
        if (VehicleRouteCalculator.LastPathFromGley)
            return DecimateForDisplay(navCorners);

        var road = VehiclePathHelper.ProjectOntoRoadNetwork(CopyCorners(navCorners), filter);
        return GroundProjector.ProjectToGround(road, ModConfig.GroundOffset.Value, filter);
    }

    internal static Vector3[] BuildLinePoints(
        Vector3[] navCorners,
        Vector3 vehicleOrigin,
        Vector3 worldTarget,
        NavMeshQueryFilter filter)
    {
        Vector3[] projected;

        if (VehicleRouteCalculator.LastPathFromGley)
        {
            projected = BuildGleyDisplayLine(navCorners, vehicleOrigin, worldTarget);
        }
        else
        {
            var cornerPoints = CopyCorners(navCorners);
            var smoothed = PathGeometry.SmoothCorners(cornerPoints, 8f);
            var onRoad = VehiclePathHelper.ProjectOntoRoadNetwork(smoothed.ToArray(), filter);
            projected = GroundProjector.ProjectToGround(onRoad, ModConfig.GroundOffset.Value, filter);
        }

        var lineOrigin = vehicleOrigin;
        if (VehiclePathHelper.TryGetRoadOrigin(out var roadStart))
            lineOrigin = roadStart;

        var line = ModConfig.ShowFullRouteLine.Value
            ? projected
            : PathGeometry.TrimBehindOrigin(projected, lineOrigin, 6f);

        if (line.Length < 2)
            line = projected;

        line = VehiclePathArrival.ApplyDisplayLine(vehicleOrigin, worldTarget, line);
        return line.Length >= 2 ? line : projected;
    }

    /// <summary>Waypoints Gley déjà sur la chaussée — pas de snap NavMesh massif.</summary>
    private static Vector3[] BuildGleyDisplayLine(Vector3[] navCorners, Vector3 vehicleOrigin, Vector3 worldTarget)
    {
        var decimated = DecimateForDisplay(navCorners);
        var elevated = ApplyLightGroundOffset(decimated);
        return VehiclePathArrival.ApplyDisplayLine(vehicleOrigin, worldTarget, elevated);
    }

    private static Vector3[] DecimateForDisplay(Vector3[] navCorners) =>
        PathGeometry.Decimate(navCorners, GleyDecimateSpacing, GleyMaxDisplayPoints);

    private static Vector3[] ApplyLightGroundOffset(Vector3[] points)
    {
        if (points.Length == 0)
            return points;

        var yOff = ModConfig.GroundOffset.Value;
        var result = new Vector3[points.Length];
        for (var i = 0; i < points.Length; i++)
        {
            var p = points[i];
            if (i % 4 == 0)
                p = GroundProjector.ProjectToGround(new[] { p }, yOff)[0];
            else
                p.y += yOff;
            result[i] = p;
        }

        return result;
    }

    private static Vector3[] CopyCorners(Vector3[] corners)
    {
        var copy = new Vector3[corners.Length];
        for (var i = 0; i < corners.Length; i++)
            copy[i] = corners[i];
        return copy;
    }
}
