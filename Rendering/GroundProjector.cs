using UnityEngine;
using UnityEngine.AI;

namespace VoogleRoute.Rendering;

public static class GroundProjector
{
    private const float RayStartHeight = 60f;
    private const float RayDistance = 120f;

    public static Vector3[] ProjectToGround(IReadOnlyList<Vector3> points, float yOffset)
    {
        if (points.Count == 0)
            return Array.Empty<Vector3>();

        var result = new Vector3[points.Count];
        for (var i = 0; i < points.Count; i++)
            result[i] = ProjectPointPedestrian(points[i], yOffset);
        return result;
    }

    public static Vector3[] ProjectToGround(
        IReadOnlyList<Vector3> points,
        float yOffset,
        NavMeshQueryFilter roadFilter) =>
        ProjectToGround(points, yOffset, useRoadFilter: true, roadFilter);

    private static Vector3[] ProjectToGround(
        IReadOnlyList<Vector3> points,
        float yOffset,
        bool useRoadFilter,
        NavMeshQueryFilter roadFilter)
    {
        if (points.Count == 0)
            return Array.Empty<Vector3>();

        var result = new Vector3[points.Count];
        for (var i = 0; i < points.Count; i++)
            result[i] = ProjectPoint(points[i], yOffset, useRoadFilter, roadFilter);
        return result;
    }

    private static Vector3 ProjectPoint(
        Vector3 point,
        float yOffset,
        bool useRoadFilter,
        NavMeshQueryFilter roadFilter)
    {
        var lift = Vector3.up * yOffset;

        if (useRoadFilter)
        {
            if (NavMesh.SamplePosition(point, out var roadHit, 10f, roadFilter))
                return roadHit.position + lift;
        }
        var origin = point + Vector3.up * RayStartHeight;
        if (Physics.Raycast(origin, Vector3.down, out var hit, RayDistance, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
            return hit.point + lift;

        return point + lift;
    }

    private static Vector3 ProjectPointPedestrian(Vector3 point, float yOffset)
    {
        var lift = Vector3.up * yOffset;
        if (NavMesh.SamplePosition(point, out var navHit, 8f, NavMesh.AllAreas))
            return navHit.position + lift;

        var origin = point + Vector3.up * RayStartHeight;
        if (Physics.Raycast(origin, Vector3.down, out var hit, RayDistance, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
            return hit.point + lift;

        return point + lift;
    }
}
