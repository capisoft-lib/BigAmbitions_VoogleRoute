using Il2Cpp;
using Il2CppHelpers;
using UnityEngine;
using UnityEngine.AI;

namespace VoogleRoute.Navigation;

/// <summary>
/// Chemin véhicule sur la chaussée (API jeu + filtre route), segment visible devant la voiture.
/// </summary>
internal static class VehiclePathHelper
{
    internal static bool TryGetRoadOrigin(out Vector3 roadOrigin)
    {
        roadOrigin = default;
        if (!MovementModeDetector.TryGetVehiclePose(out var pose, out _))
            return false;

        roadOrigin = SnapToVehicleNavMesh(pose);
        return roadOrigin.sqrMagnitude > 0.01f;
    }

    internal static bool TryGetRoadTarget(Vector3 worldTarget, out Vector3 roadTarget)
    {
        roadTarget = SnapToVehicleNavMesh(worldTarget);
        return roadTarget.sqrMagnitude > 0.01f;
    }

    internal static Vector3 SnapToVehicleNavMesh(Vector3 worldPosition) =>
        SnapToVehicleNavMeshInternal(worldPosition);

    internal static Vector3[] PrependRoadOrigin(Vector3[] path, Vector3 roadOrigin)
    {
        if (path.Length == 0)
            return new[] { roadOrigin, roadOrigin };

        if ((path[0] - roadOrigin).sqrMagnitude < 0.25f)
        {
            path[0] = roadOrigin;
            return path.Length >= 2 ? path : new[] { roadOrigin, path[^1] };
        }

        var merged = new Vector3[path.Length + 1];
        merged[0] = roadOrigin;
        Array.Copy(path, 0, merged, 1, path.Length);
        return merged;
    }

    internal static Vector3[] ClampToVehicleNavMesh(Vector3[] points, NavMeshQueryFilter filter)
    {
        if (points.Length == 0)
            return points;

        var list = new List<Vector3>(points.Length);
        foreach (var p in points)
            list.Add(SnapToVehicleNavMesh(p));

        return DeduplicateClose(list, 0.75f);
    }

    /// <summary>Projette chaque point du chemin piéton vers le réseau route le plus proche.</summary>
    internal static Vector3[] ProjectOntoRoadNetwork(Vector3[] points, NavMeshQueryFilter filter)
    {
        if (points.Length == 0)
            return points;

        var result = new List<Vector3>(points.Length) { SnapToVehicleNavMesh(points[0]) };

        for (var i = 1; i < points.Length; i++)
        {
            var snapped = SnapToVehicleNavMesh(points[i]);
            var prev = result[^1];
            var step = snapped - prev;
            step.y = 0f;
            var dist = step.magnitude;

            if (dist > 12f)
            {
                var segments = Mathf.CeilToInt(dist / 8f);
                for (var s = 1; s <= segments; s++)
                {
                    var interp = Vector3.Lerp(prev, snapped, s / (float)segments);
                    result.Add(SnapToVehicleNavMesh(interp));
                }
            }
            else if ((snapped - prev).sqrMagnitude >= 0.5f)
            {
                result.Add(snapped);
            }
        }

        return DeduplicateClose(result, 0.6f);
    }

    private static Vector3 SnapToVehicleNavMeshInternal(Vector3 worldPosition)
    {
        var filter = NavMeshFilterProvider.GetVehicleRouteFilter();
        Vector3 roadPos = worldPosition;
        var hasRoad = NavMesh.SamplePosition(worldPosition, out var roadHit, 12f, filter);
        if (hasRoad)
            roadPos = roadHit.position;

        try
        {
            var vehicle = GameManager.Instance?.selectedVehicle;
            if (vehicle != null)
            {
                var gameSnap = vehicle.GetClosestNavMeshTargetPosition(worldPosition);
                if (gameSnap.sqrMagnitude > 0.01f)
                {
                    if (!hasRoad)
                        return gameSnap;

                    var gameFlat = gameSnap - worldPosition;
                    gameFlat.y = 0f;
                    var roadFlat = roadPos - worldPosition;
                    roadFlat.y = 0f;
                    if (gameFlat.sqrMagnitude <= roadFlat.sqrMagnitude + 4f)
                        return gameSnap;
                }
            }
        }
        catch
        {
            // ignore
        }

        return hasRoad ? roadPos : worldPosition;
    }

    private static Vector3[] DeduplicateClose(List<Vector3> points, float minDist)
    {
        if (points.Count == 0)
            return Array.Empty<Vector3>();

        var minSq = minDist * minDist;
        var result = new List<Vector3> { points[0] };
        for (var i = 1; i < points.Count; i++)
        {
            if ((points[i] - result[^1]).sqrMagnitude >= minSq)
                result.Add(points[i]);
        }

        return result.Count >= 2 ? result.ToArray() : new[] { points[0], points[^1] };
    }
}
