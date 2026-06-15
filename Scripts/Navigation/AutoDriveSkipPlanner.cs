using UnityEngine;

namespace VoogleRoute.Navigation
{
    /// <summary>Auto-drive skip travel uses the on-screen route line (no path recompute).</summary>
    internal static class AutoDriveSkipPlanner
    {
        private const float MinTravelMeters = 25f;
        private const float BuildingTailMaxFlatMeters = 20f;

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

            if (MovementModeDetector.CurrentMode != MovementMode.Vehicle)
                return false;

            if (!MovementModeDetector.TryGetVehiclePose(out _, out _))
                return false;

            if (!PathFinderService.TryGetCachedRouteForDisplay(out var route) ||
                route.Points == null ||
                route.Points.Length < 2)
                return false;

            var distance = VehiclePathArrival.PolylineLength(route.Points);
            if (distance < MinTravelMeters)
            {
                plan = Plan.Failed("voogle_route_autodrive_too_close");
                return false;
            }

            var travelMinutes = AutoDriveTravelTimeEstimator.EstimateMinutes(distance);

            if (!TryResolveTeleportPose(route.Points, out var laneHint, out var position, out var rotation))
                return false;

            plan = new Plan(true, distance, travelMinutes, laneHint, position, rotation, null);
            return true;
        }

        private static bool TryResolveTeleportPose(
            Vector3[] points,
            out Vector3 laneHint,
            out Vector3 position,
            out Quaternion rotation)
        {
            laneHint = default;
            position = default;
            rotation = Quaternion.identity;

            if (points.Length < 2)
                return false;

            if (!TryResolveLaneAnchor(points, out var anchorIndex, out var laneAnchor))
                return false;

            if (laneAnchor.sqrMagnitude < 0.01f)
                return false;

            laneHint = laneAnchor;

            var prevIndex = Mathf.Max(0, anchorIndex - 1);
            var prev = points[prevIndex];
            var laneDirection = laneAnchor - prev;
            laneDirection.y = 0f;

            return AutoDriveRoadTeleport.TryResolveRoadPose(
                laneAnchor,
                laneDirection,
                out position,
                out rotation);
        }

        /// <summary>
        /// Last on-road graph node: skip building GPS chord and stacked bridge deck tails.
        /// </summary>
        internal static bool TryResolveLaneAnchor(
            Vector3[] points,
            out int anchorIndex,
            out Vector3 laneAnchor)
        {
            anchorIndex = points.Length - 1;
            laneAnchor = points[anchorIndex];

            if (NavigationTargetTracker.HasMapGpsTarget)
            {
                var destination = NavigationTargetTracker.ActiveTarget;
                while (anchorIndex > 0 &&
                       IsBuildingTailPoint(points[anchorIndex], points[anchorIndex - 1], destination))
                    anchorIndex--;
            }

            laneAnchor = points[anchorIndex];

            if (AutoDriveRoadTeleport.IsElevatedRoadPoint(laneAnchor))
            {
                for (var i = anchorIndex - 1; i >= 0; i--)
                {
                    if (AutoDriveRoadTeleport.IsElevatedRoadPoint(points[i]))
                        continue;

                    anchorIndex = i;
                    break;
                }
            }

            laneAnchor = points[anchorIndex];
            return laneAnchor.sqrMagnitude > 0.01f;
        }

        private static bool IsBuildingTailPoint(Vector3 point, Vector3 prev, Vector3 destination)
        {
            var toDest = point - destination;
            toDest.y = 0f;
            if (toDest.sqrMagnitude > BuildingTailMaxFlatMeters * BuildingTailMaxFlatMeters)
                return false;

            var seg = point - prev;
            seg.y = 0f;
            if (seg.sqrMagnitude < 1f)
                return true;

            var prevToDest = destination - prev;
            prevToDest.y = 0f;
            return toDest.sqrMagnitude < prevToDest.sqrMagnitude * 0.25f;
        }
    }
}
