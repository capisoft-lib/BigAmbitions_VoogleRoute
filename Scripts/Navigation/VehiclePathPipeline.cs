using UnityEngine;
using UnityEngine.AI;

namespace VoogleRoute.Navigation
{
    internal static class VehiclePathPipeline
    {
        private static Vector3[] _cachedSourceCorners;
        private static Vector3[] _cachedLine;

        internal static void InvalidateRouteLineCache()
        {
            _cachedSourceCorners = null;
            _cachedLine = null;
        }

        internal static Vector3[] BuildLinePoints(
            Vector3[] routeCorners,
            Vector3 vehicleOrigin,
            Vector3 worldTarget,
            NavMeshQueryFilter filter)
        {
            _ = vehicleOrigin;
            _ = worldTarget;
            _ = filter;

            if (!VehicleRouteCalculator.LastPathFromCsv)
                return System.Array.Empty<Vector3>();

            if (routeCorners == null || routeCorners.Length < 2)
                return System.Array.Empty<Vector3>();

            if (ReferenceEquals(routeCorners, _cachedSourceCorners) && _cachedLine != null)
                return _cachedLine;

            var yOff = ModConfig.VehicleGroundOffset;
            var line = new Vector3[routeCorners.Length];
            for (var i = 0; i < routeCorners.Length; i++)
            {
                var p = routeCorners[i];
                p.y += yOff;
                line[i] = p;
            }

            _cachedSourceCorners = routeCorners;
            _cachedLine = line;
            return line;
        }
    }
}
