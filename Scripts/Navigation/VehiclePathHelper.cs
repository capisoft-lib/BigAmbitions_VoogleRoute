using System.Collections.Generic;
using Helpers;
using UnityEngine;
using UnityEngine.AI;

namespace VoogleRoute.Navigation
{
    internal static class VehiclePathHelper
    {
        internal static bool TryGetRoadOrigin(out Vector3 roadOrigin)
        {
            roadOrigin = default;
            if (!MovementModeDetector.TryGetVehiclePose(out var pose, out _))
                return false;

            roadOrigin = SnapToVehicleNavMesh(pose);
            return roadOrigin.sqrMagnitude > 0.01f;
        }

        internal static bool TryGetRoadTarget(Vector3 worldTarget, out Vector3 roadTarget)
        {
            roadTarget = SnapToVehicleNavMesh(worldTarget);
            return roadTarget.sqrMagnitude > 0.01f;
        }

        /// <summary>Snap arrivée : voie côté bâtiment via la navmesh jeu quand possible.</summary>
        internal static bool TryGetArrivalRoadTarget(Vector3 worldTarget, out Vector3 roadTarget)
        {
            roadTarget = default;
            try
            {
                var vehicle = GameManager.Instance?.selectedVehicle;
                if (vehicle != null)
                {
                    var gameSnap = vehicle.GetClosestNavMeshTargetPosition(worldTarget);
                    if (gameSnap.sqrMagnitude > 0.01f)
                    {
                        roadTarget = gameSnap;
                        return true;
                    }
                }
            }
            catch
            {
                // ignore
            }

            roadTarget = SnapToVehicleNavMesh(worldTarget);
            return roadTarget.sqrMagnitude > 0.01f;
        }

        internal static Vector3 SnapToVehicleNavMesh(Vector3 worldPosition) =>
            SnapToVehicleNavMeshInternal(worldPosition);

        internal static Vector3[] ProjectOntoRoadNetwork(Vector3[] points, NavMeshQueryFilter filter)
        {
            if (points.Length == 0)
                return points;

            var result = new List<Vector3>(points.Length) { SnapToVehicleNavMesh(points[0]) };

            for (var i = 1; i < points.Length; i++)
            {
                var snapped = SnapToVehicleNavMesh(points[i]);
                var prev = result[result.Count - 1];
                var step = snapped - prev;
                step.y = 0f;
                var dist = step.magnitude;

                if (dist > 12f)
                {
                    var segments = Mathf.CeilToInt(dist / 8f);
                    for (var s = 1; s <= segments; s++)
                    {
                        var interp = Vector3.Lerp(prev, snapped, s / (float)segments);
                        result.Add(SnapToVehicleNavMesh(interp));
                    }
                }
                else if ((snapped - prev).sqrMagnitude >= 0.5f)
                {
                    result.Add(snapped);
                }
            }

            return DeduplicateClose(result, 0.6f);
        }

        private static Vector3 SnapToVehicleNavMeshInternal(Vector3 worldPosition)
        {
            var filter = NavMeshFilterProvider.GetVehicleRouteFilter();
            Vector3 roadPos = worldPosition;
            var hasRoad = NavMesh.SamplePosition(worldPosition, out var roadHit, 16f, filter);
            if (hasRoad)
                roadPos = roadHit.position;

            try
            {
                var vehicle = GameManager.Instance?.selectedVehicle;
                if (vehicle != null)
                {
                    var gameSnap = vehicle.GetClosestNavMeshTargetPosition(worldPosition);
                    if (gameSnap.sqrMagnitude > 0.01f)
                    {
                        if (!hasRoad)
                            return gameSnap;

                        var gameFlat = gameSnap - worldPosition;
                        gameFlat.y = 0f;
                        var roadFlat = roadPos - worldPosition;
                        roadFlat.y = 0f;
                        if (gameFlat.sqrMagnitude <= roadFlat.sqrMagnitude + 4f)
                            return gameSnap;
                    }
                }
            }
            catch
            {
                // ignore
            }

            return hasRoad ? roadPos : worldPosition;
        }

        private static Vector3[] DeduplicateClose(List<Vector3> points, float minDist)
        {
            if (points.Count == 0)
                return System.Array.Empty<Vector3>();

            var minSq = minDist * minDist;
            var result = new List<Vector3> { points[0] };
            for (var i = 1; i < points.Count; i++)
            {
                if ((points[i] - result[result.Count - 1]).sqrMagnitude >= minSq)
                    result.Add(points[i]);
            }

            return result.Count >= 2 ? result.ToArray() : new[] { points[0], points[points.Count - 1] };
        }
    }
}
