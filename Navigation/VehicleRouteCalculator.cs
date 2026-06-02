using Il2Cpp;
using Il2CppHelpers;
using UnityEngine;
using UnityEngine.AI;

namespace VoogleRoute.Navigation;

/// <summary>
/// Route en voiture : graphe Gley Traffic waypoints (A*), puis secours NavMesh route / topo piéton.
/// </summary>
internal static class VehicleRouteCalculator
{
    internal static bool LastPathFromGley { get; private set; }

    internal static bool TryCalculate(
        Vector3 vehicleOrigin,
        Vector3 worldTarget,
        Vector3 sampleOrigin,
        NavMeshPath navPath,
        out NavMeshQueryFilter displayFilter,
        out Vector3[] corners,
        out NavMeshPathStatus status)
    {
        displayFilter = NavMeshFilterProvider.GetVehicleRouteFilter();
        corners = System.Array.Empty<Vector3>();
        status = NavMeshPathStatus.PathInvalid;
        LastPathFromGley = false;

        if (TrafficWaypointPathfinder.TryFindPath(vehicleOrigin, worldTarget, out var gleyCorners) &&
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

        if (!FootRouteCalculator.TryCalculate(vehicleOrigin, worldTarget, sampleOrigin, navPath, out _))
            return false;

        corners = navPath.corners != null ? CopyCorners(navPath.corners) : System.Array.Empty<Vector3>();
        status = navPath.status;

        return corners.Length >= 2;
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
