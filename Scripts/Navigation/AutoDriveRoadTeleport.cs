using Helpers;
using UnityEngine;
using Vehicles;

namespace VoogleRoute.Navigation
{
    /// <summary>Places the player vehicle on a drivable road surface with lane-aligned heading.</summary>
    internal static class AutoDriveRoadTeleport
    {
        private const float RoadRaycastHeight = 50f;
        private const float RoadRaycastDepth = 120f;
        private const float MaxGameSnapDriftMeters = 6f;

        internal static bool TryResolveRoadPose(
            Vector3 laneAnchor,
            Vector3 laneDirection,
            out Vector3 position,
            out Quaternion rotation)
        {
            position = laneAnchor;
            rotation = Quaternion.identity;

            if (!TryFlattenDirection(laneDirection, out var forward))
                forward = Vector3.forward;

            rotation = Quaternion.LookRotation(forward, Vector3.up);

            // CSV graph nodes are already lane-specific (e.g. Road_5-Lane_1 vs Lane_0 ~4 m apart).
            // A fixed lateral offset lands on the opposite-direction parallel lane.
            if (!TryResolveDriveSurface(laneAnchor, out position))
                position = laneAnchor;

            return position.sqrMagnitude > 0.01f;
        }

        internal static void Apply(
            VehicleController vehicle,
            Vector3 routeLaneHint,
            Vector3 position,
            Quaternion rotation)
        {
            _ = routeLaneHint;
            VehicleHelper.TeleportVehicle(vehicle, position, rotation);
            vehicle.SavePosition();
        }

        private static bool TryFlattenDirection(Vector3 laneDirection, out Vector3 forward)
        {
            forward = laneDirection;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                return false;

            forward.Normalize();
            return true;
        }

        private static bool TryResolveDriveSurface(Vector3 flatTarget, out Vector3 position)
        {
            position = flatTarget;
            var roadMask = 1 << LayerHelper.RoadsLayerIndex;
            var ray = new Ray(
                new Vector3(flatTarget.x, flatTarget.y + RoadRaycastHeight, flatTarget.z),
                Vector3.down);

            if (Physics.Raycast(ray, out var hit, RoadRaycastDepth, roadMask, QueryTriggerInteraction.Ignore))
            {
                position = hit.point;
                return true;
            }

            try
            {
                var vehicle = GameManager.Instance?.selectedVehicle;
                if (vehicle != null)
                {
                    var gameSnap = vehicle.GetClosestNavMeshTargetPosition(flatTarget);
                    if (gameSnap.sqrMagnitude > 0.01f &&
                        HorizontalDistance(gameSnap, flatTarget) <= MaxGameSnapDriftMeters)
                    {
                        position = new Vector3(gameSnap.x, flatTarget.y, gameSnap.z);
                        ray = new Ray(position + Vector3.up * RoadRaycastHeight, Vector3.down);
                        if (Physics.Raycast(ray, out hit, RoadRaycastDepth, roadMask, QueryTriggerInteraction.Ignore))
                            position = hit.point;
                        return true;
                    }
                }
            }
            catch
            {
                // ignore
            }

            return false;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
