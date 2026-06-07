using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using VoogleRoute.Rendering;

namespace VoogleRoute.Navigation
{
    internal static class VehiclePathPipeline
    {
        private const int GleyMaxSkeletonPoints = 48;

        private static Vector3[] _cachedSourceCorners;
        private static Vector3[] _cachedGleyLine;

        internal static void InvalidateGleyLineCache()
        {
            _cachedSourceCorners = null;
            _cachedGleyLine = null;
        }

        internal static Vector3[] BuildLinePoints(
            Vector3[] navCorners,
            Vector3 vehicleOrigin,
            Vector3 worldTarget,
            NavMeshQueryFilter filter)
        {
            if (VehicleRouteCalculator.LastPathFromGley)
            {
                var gleyLine = BuildGleyPolyline(navCorners);
                return gleyLine.Length >= 2 ? gleyLine : System.Array.Empty<Vector3>();
            }

            var cornerPoints = CopyCorners(navCorners);
            var smoothed = PathGeometry.SmoothCorners(cornerPoints, 8f);
            var onRoad = VehiclePathHelper.ProjectOntoRoadNetwork(smoothed.ToArray(), filter);
            var projected = GroundProjector.ProjectToGround(onRoad, ModConfig.VehicleGroundOffset, filter);
            var fallbackLine = VehiclePathArrival.ApplyDisplayLine(vehicleOrigin, worldTarget, projected);
            return fallbackLine.Length >= 2 ? fallbackLine : projected;
        }

        /// <summary>
        /// Pathfinder polyline already includes CSV quadratic beziers for synthetic turns.
        /// </summary>
        private static Vector3[] BuildGleyPolyline(Vector3[] pathCorners)
        {
            if (pathCorners == null || pathCorners.Length < 2)
                return System.Array.Empty<Vector3>();

            if (ReferenceEquals(pathCorners, _cachedSourceCorners) && _cachedGleyLine != null)
                return _cachedGleyLine;

            var yOff = ModConfig.VehicleGroundOffset;
            var list = new List<Vector3>(pathCorners.Length);
            for (var i = 0; i < pathCorners.Length; i++)
            {
                var p = pathCorners[i];
                p.y += yOff;
                list.Add(p);
            }

            var result = PathGeometry.DecimateColinear(list, 6f, GleyMaxSkeletonPoints);
            if (result.Length < 2)
                result = list.ToArray();

            _cachedSourceCorners = pathCorners;
            _cachedGleyLine = result;
            return result;
        }

        private static Vector3[] CopyCorners(Vector3[] corners)
        {
            var copy = new Vector3[corners.Length];
            for (var i = 0; i < corners.Length; i++)
                copy[i] = corners[i];
            return copy;
        }
    }
}
