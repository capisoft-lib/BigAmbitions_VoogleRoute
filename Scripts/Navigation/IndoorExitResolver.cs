using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace VoogleRoute.Navigation
{
    internal static class IndoorExitResolver
    {
        private static readonly NavMeshPath NavPath = new NavMeshPath();

        internal static bool TryGetNearestExit(Vector3 origin, out Vector3 exitPosition)
        {
            exitPosition = default;

            List<ExitZone> zones;
            try
            {
                if (!BuildingManager.IsInitialized)
                    return false;

                zones = BuildingManager.Instance?.exitZones;
            }
            catch
            {
                return false;
            }

            if (zones == null || zones.Count == 0)
                return false;

            var bestFound = false;
            var bestScore = float.MaxValue;
            var bestPosition = default(Vector3);

            foreach (var zone in zones)
            {
                if (zone == null)
                    continue;

                if (zone.isDriveInBay)
                    continue;

                var candidate = GetWalkTarget(zone);
                if (candidate.sqrMagnitude < 0.01f)
                    continue;

                var score = TryGetPathLength(origin, candidate, out var pathLength)
                    ? pathLength
                    : HorizontalDistance(origin, candidate);

                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestPosition = candidate;
                bestFound = true;
            }

            if (!bestFound)
                return false;

            exitPosition = bestPosition;
            return true;
        }

        private static Vector3 GetWalkTarget(ExitZone zone)
        {
            // Match vanilla NPC exit behaviour: walk to the despawner trigger inside the building.
            // playerSpawnPoint is outside and is not reachable from indoor navmesh.
            if (zone.despawner != null)
                return zone.despawner.transform.position;

            if (zone.door != null)
                return zone.door.position;

            return zone.transform.position;
        }

        private static bool TryGetPathLength(Vector3 origin, Vector3 target, out float length)
        {
            length = 0f;

            if (!MovementModeDetector.TryGetPlayerOrigin(out var sampleOrigin))
                sampleOrigin = origin;

            if (!FootRouteCalculator.TryCalculate(origin, target, sampleOrigin, NavPath, out _))
                return false;

            var corners = NavPath.corners;
            if (corners == null || corners.Length < 2)
                return false;

            length = PolylineLength(corners);
            return length > 0.1f;
        }

        private static float PolylineLength(Vector3[] points)
        {
            var total = 0f;
            for (var i = 1; i < points.Length; i++)
                total += Vector3.Distance(points[i - 1], points[i]);

            return total;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
