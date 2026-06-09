using UnityEngine;
using UnityEngine.AI;

namespace VoogleRoute.Navigation
{
    internal static class VehicleRouteCalculator
    {
        internal static bool LastPathFromCsv { get; private set; }

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
            _ = sampleOrigin;
            _ = navPath;
            _ = allowRouteReuse;

            displayFilter = NavMeshFilterProvider.GetVehicleRouteFilter();
            corners = System.Array.Empty<Vector3>();
            status = NavMeshPathStatus.PathInvalid;
            LastPathFromCsv = false;

            if (!RoutePathfinder.TryFindPath(vehicleOrigin, worldTarget, out var routeCorners) ||
                routeCorners.Length < 2)
                return false;

            VehiclePathPipeline.InvalidateRouteLineCache();
            corners = routeCorners;
            status = NavMeshPathStatus.PathComplete;
            LastPathFromCsv = true;
            return true;
        }
    }
}
