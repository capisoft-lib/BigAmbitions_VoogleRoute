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

            // Do not fall back to pedestrian/sidewalk navmesh — graph anchors are lane-specific.
            return false;
        }
    }
}
