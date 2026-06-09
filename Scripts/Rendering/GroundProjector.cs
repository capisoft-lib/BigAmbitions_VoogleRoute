using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace VoogleRoute.Rendering
{
    
    internal static class GroundProjector
    {
        private const float RayStartHeight = 60f;
        private const float RayDistance = 120f;
    
        internal static Vector3[] ProjectToGround(IReadOnlyList<Vector3> points, float yOffset)
        {
            if (points.Count == 0)
                return System.Array.Empty<Vector3>();

            var result = new Vector3[points.Count];
            for (var i = 0; i < points.Count; i++)
                result[i] = ProjectPointPedestrian(points[i], yOffset);
            return result;
        }

        internal static Vector3[] ProjectToGround(
            IReadOnlyList<Vector3> points,
            float yOffset,
            NavMeshQueryFilter roadFilter)
        {
            if (points.Count == 0)
                return System.Array.Empty<Vector3>();

            var result = new Vector3[points.Count];
            for (var i = 0; i < points.Count; i++)
                result[i] = ProjectPointRoad(points[i], yOffset, roadFilter);
            return result;
        }

        private static Vector3 ProjectPointRoad(Vector3 point, float yOffset, NavMeshQueryFilter roadFilter)
        {
            var lift = Vector3.up * yOffset;
            if (NavMesh.SamplePosition(point, out var roadHit, 10f, roadFilter))
                return roadHit.position + lift;

            return ProjectPointPedestrian(point, yOffset);
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
}
