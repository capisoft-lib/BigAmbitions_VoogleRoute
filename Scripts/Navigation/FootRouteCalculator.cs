using Helpers;
using UnityEngine;
using UnityEngine.AI;

namespace VoogleRoute.Navigation
{
    
    internal static class FootRouteCalculator
    {
        internal static bool TryCalculate(
            Vector3 origin,
            Vector3 worldTarget,
            Vector3 sampleOrigin,
            NavMeshPath navPath,
            out NavMeshQueryFilter filterUsed)
        {
            filterUsed = NavMeshFilterProvider.GetPedestrianRouteFilter();
            return TryCalculate(origin, worldTarget, sampleOrigin, navPath, filterUsed, out _);
        }

        internal static bool TryCalculate(
            Vector3 origin,
            Vector3 worldTarget,
            Vector3 sampleOrigin,
            NavMeshPath navPath,
            out NavMeshQueryFilter filterUsed,
            out NavMeshPathStatus status)
        {
            filterUsed = NavMeshFilterProvider.GetPedestrianRouteFilter();
            return TryCalculate(origin, worldTarget, sampleOrigin, navPath, filterUsed, out status);
        }

        private static bool TryCalculate(
            Vector3 origin,
            Vector3 worldTarget,
            Vector3 sampleOrigin,
            NavMeshPath navPath,
            NavMeshQueryFilter filterUsed,
            out NavMeshPathStatus status)
        {
            status = NavMeshPathStatus.PathInvalid;

            if (!TrySampleOnNavMesh(sampleOrigin, 12f, filterUsed, out var navOrigin) &&
                !TrySampleOnNavMesh(origin, 12f, filterUsed, out navOrigin))
                return false;

            if (!TrySampleOnNavMesh(worldTarget, 64f, filterUsed, out var navTarget) &&
                !TrySampleOnNavMeshAnyArea(worldTarget, 64f, out navTarget))
                return false;

            if (TryCalcPath(navOrigin, navTarget, filterUsed, navPath))
            {
                status = navPath.status;
                return true;
            }

            if (NavMesh.CalculatePath(navOrigin, navTarget, NavMesh.AllAreas, navPath) &&
                navPath.status != NavMeshPathStatus.PathInvalid)
            {
                status = navPath.status;
                return true;
            }

            if (TrySampleOnNavMeshAnyArea(origin, 80f, out var altOrigin) &&
                TrySampleOnNavMeshAnyArea(worldTarget, 80f, out var altTarget) &&
                TryCalcPath(altOrigin, altTarget, filterUsed, navPath))
            {
                status = navPath.status;
                return true;
            }

            try
            {
                var agent = PlayerHelper.PlayerController?.Character?.navmeshAgent;
                if (agent != null && agent.CalculatePath(navTarget, navPath) &&
                    navPath.status != NavMeshPathStatus.PathInvalid)
                {
                    status = navPath.status;
                    return true;
                }
            }
            catch
            {
                // ignore
            }

            return false;
        }
    
        private static bool TrySampleOnNavMesh(
            Vector3 position,
            float maxDistance,
            NavMeshQueryFilter filter,
            out Vector3 sampled)
        {
            sampled = position;
            if (NavMesh.SamplePosition(position, out var hit, maxDistance, filter))
            {
                sampled = hit.position;
                return true;
            }
    
            return NavMesh.SamplePosition(position, out hit, maxDistance * 2f, NavMesh.AllAreas) &&
                   (sampled = hit.position).sqrMagnitude > 0.01f;
        }
    
        private static bool TrySampleOnNavMeshAnyArea(Vector3 position, float maxDistance, out Vector3 sampled)
        {
            sampled = position;
            return NavMesh.SamplePosition(position, out var hit, maxDistance, NavMesh.AllAreas) &&
                   (sampled = hit.position).sqrMagnitude > 0.01f;
        }
    
        private static bool TryCalcPath(
            Vector3 from,
            Vector3 to,
            NavMeshQueryFilter filter,
            NavMeshPath path) =>
            NavMesh.CalculatePath(from, to, filter, path) &&
            path.status != NavMeshPathStatus.PathInvalid;
    }
}
