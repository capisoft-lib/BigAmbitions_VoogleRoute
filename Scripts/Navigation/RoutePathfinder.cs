using System.Collections.Generic;
using UnityEngine;
using VoogleRoute.Pathfinding.Geometry;
using VoogleRoute.Pathfinding.Routing;

namespace VoogleRoute.Navigation
{
    /// <summary>
    /// Itinéraire véhicule : CSV + WaypointPathfinder + polyligne partagée (parité Blazor).
    /// </summary>
    internal static class RoutePathfinder
    {
        internal static bool TryFindPath(Vector3 origin, Vector3 destination, out Vector3[] corners)
        {
            corners = System.Array.Empty<Vector3>();

            if (!RouteGraphStore.TryEnsureLoaded())
                return false;

            var graph = RouteGraphStore.Graph;
            var hasPose = MovementModeDetector.TryGetVehiclePose(out _, out var forward);
            var query = new RouteQuery
            {
                Origin = ToVec3(origin),
                Destination = ToVec3(destination),
                Forward = hasPose ? ToVec3(forward) : default,
                HasPose = hasPose,
                ForcedStartWaypoint = -1,
                ForcedEndWaypoint = -1
            };

            if (!WaypointPathfinder.TryFindBestRoute(graph, query, out var result))
                return false;

            var points = RoutePolylineBuilder.BuildPoints(
                graph,
                result.Path,
                prependOrigin: ToVec3(origin),
                appendDestination: ToVec3(destination));

            corners = ToUnity(points);
            return corners.Length >= 2;
        }

        private static Vec3 ToVec3(Vector3 v) => new Vec3(v.x, v.y, v.z);

        private static Vector3[] ToUnity(List<Vec3> points)
        {
            var array = new Vector3[points.Count];
            for (var i = 0; i < points.Count; i++)
            {
                var p = points[i];
                array[i] = new Vector3(p.X, p.Y, p.Z);
            }

            return array;
        }
    }
}
