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
            var hasPose = MovementModeDetector.TryGetVehiclePose(out _, out var forward);
            return TryFindPath(
                origin,
                destination,
                hasPose ? ToVec3(forward) : default,
                hasPose,
                out corners);
        }

        internal static bool TryFindPath(
            Vector3 origin,
            Vector3 destination,
            Vec3 forward,
            bool hasPose,
            out Vector3[] corners)
        {
            corners = System.Array.Empty<Vector3>();
            var timer = RouteRecalcDiagnostics.StartTimer();

            if (!RouteGraphStore.TryEnsureLoaded())
            {
                RouteRecalcDiagnostics.RecordPathfind(RoutePathfindKind.Failed, RouteRecalcDiagnostics.ElapsedMs(timer));
                return false;
            }

            var graph = RouteGraphStore.Graph;
            var query = new RouteQuery
            {
                Origin = ToVec3(origin),
                Destination = ToVec3(destination),
                Forward = forward,
                HasPose = hasPose,
                ForcedStartWaypoint = -1,
                ForcedEndWaypoint = -1
            };

            if (!WaypointPathfinder.TryFindBestRoute(graph, query, out var result))
            {
                RouteRecalcDiagnostics.RecordPathfind(RoutePathfindKind.Failed, RouteRecalcDiagnostics.ElapsedMs(timer));
                return false;
            }

            var points = RoutePolylineBuilder.BuildPoints(
                graph,
                result.Path,
                prependOrigin: ToVec3(origin),
                appendDestination: ToVec3(destination));

            corners = ToUnity(points);
            var success = corners.Length >= 2;
            RouteRecalcDiagnostics.RecordPathfind(
                success ? RoutePathfindKind.FullAStar : RoutePathfindKind.Failed,
                RouteRecalcDiagnostics.ElapsedMs(timer));
            return success;
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
