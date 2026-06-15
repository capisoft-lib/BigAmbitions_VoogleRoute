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

        /// <summary>Route display points include VehicleGroundOffset (~0.4 m).</summary>
        private const float StreetLevelMaxY = 6f;

        private const float BridgeDeckMinY = 8f;

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

        internal static bool IsElevatedRoadPoint(Vector3 point) => point.y >= BridgeDeckMinY;

        internal static bool IsStreetLevelLane(float y) => y <= StreetLevelMaxY;

        private static bool TryFlattenDirection(Vector3 laneDirection, out Vector3 forward)
        {
            forward = laneDirection;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                return false;

            forward.Normalize();
            return true;
        }

        private static bool TryResolveDriveSurface(Vector3 laneAnchor, out Vector3 position)
        {
            position = laneAnchor;
            var roadMask = 1 << LayerHelper.RoadsLayerIndex;
            var ray = new Ray(
                new Vector3(laneAnchor.x, laneAnchor.y + RoadRaycastHeight, laneAnchor.z),
                Vector3.down);

            var hits = Physics.RaycastAll(ray, RoadRaycastDepth, roadMask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
                return false;

            var candidates = hits;
            if (IsStreetLevelLane(laneAnchor.y))
            {
                var streetCount = 0;
                for (var i = 0; i < hits.Length; i++)
                {
                    if (hits[i].point.y <= StreetLevelMaxY)
                        streetCount++;
                }

                if (streetCount > 0)
                {
                    var filtered = new RaycastHit[streetCount];
                    var write = 0;
                    for (var i = 0; i < hits.Length; i++)
                    {
                        if (hits[i].point.y > StreetLevelMaxY)
                            continue;
                        filtered[write++] = hits[i];
                    }

                    candidates = filtered;
                }
            }

            var best = candidates[0];
            var bestDy = Mathf.Abs(best.point.y - laneAnchor.y);
            for (var i = 1; i < candidates.Length; i++)
            {
                var dy = Mathf.Abs(candidates[i].point.y - laneAnchor.y);
                if (dy >= bestDy)
                    continue;

                bestDy = dy;
                best = candidates[i];
            }

            position = best.point;
            return true;
        }
    }
}
