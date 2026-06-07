using UnityEngine;
using UnityEngine.AI;

namespace VoogleRoute.Navigation
{
    internal static class VehicleRouteCalculator
    {
        internal static bool LastPathFromGley { get; private set; }

        internal static bool TryCalculate(
            Vector3 vehicleOrigin,
            Vector3 worldTarget,
            Vector3 sampleOrigin,
            NavMeshPath navPath,
            bool allowRouteReuse,
            out NavMeshQueryFilter displayFilter,
            out Vector3[] corners,
            out NavMeshPathStatus status)
        {
            displayFilter = NavMeshFilterProvider.GetVehicleRouteFilter();
            corners = System.Array.Empty<Vector3>();
            status = NavMeshPathStatus.PathInvalid;
            LastPathFromGley = false;
            VehiclePathPipeline.InvalidateGleyLineCache();

            if (TrafficWaypointPathfinder.TryFindPath(
                    vehicleOrigin, worldTarget, out var gleyCorners, allowRouteReuse) &&
                gleyCorners.Length >= 2)
            {
                corners = gleyCorners;
                status = NavMeshPathStatus.PathComplete;
                LastPathFromGley = true;
                return true;
            }

            var roadPathOk = TryCalculateOnRoadMeshOnly(vehicleOrigin, worldTarget, navPath, displayFilter);
            if (roadPathOk && navPath.corners != null && navPath.corners.Length >= 2)
            {
                corners = CopyCorners(navPath.corners);
                status = navPath.status;
                return true;
            }

            return false;
        }

        private static Vector3[] CopyCorners(Vector3[] source)
        {
            var copy = new Vector3[source.Length];
            for (var i = 0; i < source.Length; i++)
                copy[i] = source[i];
            return copy;
        }

        private static bool TryCalculateOnRoadMeshOnly(
            Vector3 vehicleOrigin,
            Vector3 worldTarget,
            NavMeshPath navPath,
            NavMeshQueryFilter filter)
        {
            if (!TryResolveRoadEndpoints(vehicleOrigin, worldTarget, filter, out var navOrigin, out var navTarget))
                return false;

            return NavMesh.CalculatePath(navOrigin, navTarget, filter, navPath) &&
                   navPath.status != NavMeshPathStatus.PathInvalid;
        }

        private static bool TryResolveRoadEndpoints(
            Vector3 vehicleOrigin,
            Vector3 worldTarget,
            NavMeshQueryFilter filter,
            out Vector3 navOrigin,
            out Vector3 navTarget)
        {
            navOrigin = default;
            navTarget = default;

            if (!VehiclePathHelper.TryGetRoadOrigin(out navOrigin) &&
                (!NavMesh.SamplePosition(vehicleOrigin, out var oHit, 48f, filter) ||
                 (navOrigin = oHit.position).sqrMagnitude < 0.01f))
                return false;

            if (VehiclePathHelper.TryGetRoadTarget(worldTarget, out navTarget))
                return true;

            if (NavMesh.SamplePosition(worldTarget, out var tHit, 64f, filter))
            {
                navTarget = tHit.position;
                return true;
            }

            if (!NavMesh.SamplePosition(worldTarget, out var pedHit, 80f, NavMesh.AllAreas))
                return false;

            navTarget = VehiclePathHelper.SnapToVehicleNavMesh(pedHit.position);
            return navTarget.sqrMagnitude > 0.01f;
        }
    }
}
