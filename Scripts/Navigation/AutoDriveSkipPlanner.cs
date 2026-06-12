using UnityEngine;
using VoogleRoute.Pathfinding.Geometry;
using VoogleRoute.Pathfinding.Routing;

namespace VoogleRoute.Navigation
{
    /// <summary>Estimates auto-drive skip travel using the CSV route graph.</summary>
    internal static class AutoDriveSkipPlanner
    {
        private const float MinTravelMeters = 25f;

        internal readonly struct Plan
        {
            internal readonly bool Success;
            internal readonly float DistanceMeters;
            internal readonly float TravelMinutes;
            internal readonly Vector3 RouteLaneHint;
            internal readonly Vector3 TeleportPosition;
            internal readonly Quaternion TeleportRotation;
            internal readonly string FailureKey;

            internal Plan(
                bool success,
                float distanceMeters,
                float travelMinutes,
                Vector3 routeLaneHint,
                Vector3 teleportPosition,
                Quaternion teleportRotation,
                string failureKey)
            {
                Success = success;
                DistanceMeters = Mathf.Max(0f, distanceMeters);
                TravelMinutes = success ? Mathf.Max(1f, travelMinutes) : 0f;
                RouteLaneHint = routeLaneHint;
                TeleportPosition = teleportPosition;
                TeleportRotation = teleportRotation;
                FailureKey = failureKey;
            }

            internal static Plan Failed(string failureKey) =>
                new Plan(false, 0f, 0f, default, default, Quaternion.identity, failureKey);
        }

        internal static bool TryBuildPlan(out Plan plan)
        {
            plan = Plan.Failed("voogle_route_autodrive_no_route");

            if (!NavigationTargetTracker.HasMapGpsTarget)
                return false;

            if (!MovementModeDetector.TryGetVehiclePose(out var origin, out var forward))
                return false;

            var destination = NavigationTargetTracker.ActiveTarget;

            if (!RouteGraphStore.TryEnsureLoaded())
                return false;

            var graph = RouteGraphStore.Graph;
            var query = new RouteQuery
            {
                Origin = ToVec3(origin),
                Destination = ToVec3(destination),
                Forward = ToVec3(forward),
                HasPose = true,
                ForcedStartWaypoint = -1,
                ForcedEndWaypoint = -1,
                ForceBuildingSide = ModConfig.ForceCarSideEnabled
            };

            if (!WaypointPathfinder.TryFindBestRoute(graph, query, out var routeResult))
                return false;

            if (!RoutePathfinder.TryFindPath(origin, destination, ToVec3(forward), true, out var pathCorners) ||
                pathCorners.Length < 2)
                return false;

            var polylineDistance = VehiclePathArrival.PolylineLength(pathCorners);
            var distance = Mathf.Max(routeResult.TotalCostMeters, polylineDistance);
            if (distance < MinTravelMeters)
            {
                plan = Plan.Failed("voogle_route_autodrive_too_close");
                return false;
            }

            var travelMinutes = AutoDriveTravelTimeEstimator.EstimateMinutes(distance);

            if (!TryResolveTeleportPose(
                    graph,
                    routeResult,
                    out var laneHint,
                    out var position,
                    out var rotation))
                return false;

            plan = new Plan(true, distance, travelMinutes, laneHint, position, rotation, null);
            return true;
        }

        private static bool TryResolveTeleportPose(
            VoogleRoute.Pathfinding.Graph.RouteGraph graph,
            RouteResult routeResult,
            out Vector3 laneHint,
            out Vector3 position,
            out Quaternion rotation)
        {
            laneHint = default;
            position = default;
            rotation = Quaternion.identity;

            var path = routeResult.Path;
            if (path == null || path.Count < 1)
                return false;

            if (!TryGetArrivalSegment(graph, path, out var laneAnchor, out var laneDirection))
                return false;

            laneHint = laneAnchor;

            return AutoDriveRoadTeleport.TryResolveRoadPose(
                laneAnchor,
                laneDirection,
                out position,
                out rotation);
        }

        private static bool TryGetArrivalSegment(
            VoogleRoute.Pathfinding.Graph.RouteGraph graph,
            System.Collections.Generic.IReadOnlyList<int> path,
            out Vector3 laneAnchor,
            out Vector3 laneDirection)
        {
            laneAnchor = default;
            laneDirection = Vector3.forward;

            if (path.Count < 1)
                return false;

            var endIdx = path[path.Count - 1];
            var end = ToVector3(graph.GetPosition(endIdx));
            if (end.sqrMagnitude < 0.01f)
                return false;

            laneAnchor = end;

            if (path.Count >= 2)
            {
                var prev = ToVector3(graph.GetPosition(path[path.Count - 2]));
                var seg = end - prev;
                seg.y = 0f;
                if (seg.sqrMagnitude >= 0.01f)
                {
                    laneDirection = seg;
                    return true;
                }
            }

            var forward = graph.GetForwardNeighbors(endIdx);
            if (forward.Length > 0)
            {
                var next = ToVector3(graph.GetPosition(forward[0]));
                laneDirection = next - end;
                laneDirection.y = 0f;
                if (laneDirection.sqrMagnitude >= 0.01f)
                    return true;
            }

            return true;
        }

        private static Vector3 ToVector3(Vec3 v) => new Vector3(v.X, v.Y, v.Z);

        private static Vec3 ToVec3(Vector3 v) => new Vec3(v.x, v.y, v.z);
    }
}
