using Helpers;
using UnityEngine;
using UnityEngine.AI;

namespace VoogleRoute.Navigation
{
    /// <summary>
    /// Matches vanilla PlayerController.ExistsRoute: agent path must be PathComplete.
    /// No AllAreas fallback — used for direct destination legs only.
    /// </summary>
    internal static class VanillaFootRouteCalculator
    {
        internal static bool TryCalculateComplete(
            Vector3 origin,
            Vector3 worldTarget,
            Vector3 sampleOrigin,
            NavMeshPath navPath,
            out NavMeshPathStatus status)
        {
            status = NavMeshPathStatus.PathInvalid;
            var filter = NavMeshFilterProvider.GetPedestrianRouteFilter();

            if (!TrySampleOnNavMesh(sampleOrigin, 12f, filter, out var navOrigin) &&
                !TrySampleOnNavMesh(origin, 12f, filter, out navOrigin))
                return false;

            if (!TrySampleOnNavMesh(worldTarget, 64f, filter, out var navTarget))
                return false;

            if (TryAgentCalculatePath(navTarget, navPath, out status))
                return true;

            if (NavMesh.CalculatePath(navOrigin, navTarget, filter, navPath))
            {
                status = navPath.status;
                return status == NavMeshPathStatus.PathComplete;
            }

            return false;
        }

        private static bool TryAgentCalculatePath(
            Vector3 navTarget,
            NavMeshPath navPath,
            out NavMeshPathStatus status)
        {
            status = NavMeshPathStatus.PathInvalid;
            try
            {
                var agent = PlayerHelper.PlayerController?.Character?.navmeshAgent;
                if (agent == null || !agent.CalculatePath(navTarget, navPath))
                    return false;

                status = navPath.status;
                return status == NavMeshPathStatus.PathComplete;
            }
            catch
            {
                return false;
            }
        }

        private static bool TrySampleOnNavMesh(
            Vector3 position,
            float maxDistance,
            NavMeshQueryFilter filter,
            out Vector3 sampled)
        {
            sampled = position;
            return NavMesh.SamplePosition(position, out var hit, maxDistance, filter) &&
                   (sampled = hit.position).sqrMagnitude > 0.01f;
        }
    }
}
