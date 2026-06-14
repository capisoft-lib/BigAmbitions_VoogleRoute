using BaPlayerLocation.Subscriber;
using UnityEngine;
using VoogleRoute.Pathfinding.Geometry;
using VoogleRoute.Pathfinding.Graph;
using VoogleRoute.Pathfinding.Routing;

namespace VoogleRoute.Navigation
{
    /// <summary>Bookmark distances use the CSV road graph (same pathfinding as Voogle route lines).</summary>
    internal static class BookmarkRouteDistance
    {
        internal static bool TryGetRouteMeters(BookmarkEntry bookmark, out float meters)
        {
            meters = -1f;
            if (bookmark == null || !bookmark.TryGetNavigationTarget(out var target))
                return false;

            if (!PlayerLocationSession.IsAvailable || !GameState.IsWorldReady())
                return false;

            if (!TryGetOrigin(out var origin))
                return false;

            if (MovementModeDetector.CurrentMode == MovementMode.OnFoot)
                return FootSubwayRoutePlanner.TryEstimateMeters(origin, target, out meters);

            if (!RouteGraphStore.TryEnsureLoaded())
                return false;

            var hasPose = MovementModeDetector.TryGetVehiclePose(out _, out var forward);
            return TryComputeRouteMeters(origin, target, forward, hasPose, RouteGraphStore.Graph, out meters);
        }

        internal static bool TryComputeRouteMeters(
            Vector3 origin,
            Vector3 target,
            Vector3 forward,
            bool hasPose,
            RouteGraph graph,
            out float meters) =>
            TryComputeRouteMeters(
                origin,
                target,
                forward,
                hasPose,
                graph,
                VehicleRoutePathOptions.FromMainThread(target),
                out meters);

        internal static bool TryComputeRouteMeters(
            Vector3 origin,
            Vector3 target,
            Vector3 forward,
            bool hasPose,
            RouteGraph graph,
            VehicleRoutePathOptions pathOptions,
            out float meters)
        {
            meters = -1f;
            if (graph == null)
                return false;

            var query = pathOptions.ToRouteQuery(ToVec3(origin), ToVec3(target), ToVec3(forward), hasPose);

            if (!VehicleRoutePolyline.TryBuild(graph, query, out var built))
                return false;

            meters = Mathf.Max(built.GraphCostMeters, built.PolylineLengthMeters);
            return meters > 0f;
        }

        private static bool TryGetOrigin(out Vector3 origin)
        {
            if (MovementModeDetector.TryGetPathOrigin(out origin))
                return true;

            origin = PlayerLocationSession.Snapshot.Position;
            return origin.sqrMagnitude > 0.01f;
        }

        private static Vec3 ToVec3(Vector3 v) => new Vec3(v.x, v.y, v.z);

        internal static string FormatDistance(float meters)
        {
            if (meters < 0f)
                return "—";

            return Mathf.Max(0, Mathf.RoundToInt(meters)) + " m";
        }
    }
}
