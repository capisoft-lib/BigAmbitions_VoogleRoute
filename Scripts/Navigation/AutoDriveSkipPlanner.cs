using UnityEngine;

namespace VoogleRoute.Navigation
{
    /// <summary>Auto-drive skip travel uses the on-screen route line (no path recompute).</summary>
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

            var laneAnchor = points[points.Length - 1];
            if (laneAnchor.sqrMagnitude < 0.01f)
                return false;

            laneHint = laneAnchor;

            var prev = points[points.Length - 2];
            var laneDirection = laneAnchor - prev;
            laneDirection.y = 0f;

            return AutoDriveRoadTeleport.TryResolveRoadPose(
                laneAnchor,
                laneDirection,
                out position,
                out rotation);
        }
    }
}
