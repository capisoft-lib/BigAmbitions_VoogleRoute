using System.Collections.Generic;
using UnityEngine;
using VoogleRoute;

namespace VoogleRoute.Navigation
{
 
    internal static class VehiclePathArrival
    {
        private const float ImmediateArrivalMeters = 22f;
        private const float ArrivalZoneMeters = 96f;
        private const float DetourRatioThreshold = 1.85f;

        internal static Vector3[] Apply(Vector3 origin, Vector3 destination, Vector3[] path)
        {
            return Apply(origin, destination, path, forceCorrectSideArrival: false);
        }

        internal static Vector3[] Apply(
            Vector3 origin,
            Vector3 destination,
            Vector3[] path,
            bool forceCorrectSideArrival)
        {
            if (!forceCorrectSideArrival && !ModConfig.ForceCorrectSideArrivalEnabled)
                return path;

            if (path.Length < 2)
                return path;

            var direct = FlatDistance(origin, destination);
            if (direct > ArrivalZoneMeters)
                return path;

            if (direct <= ImmediateArrivalMeters)
                return FinishArrival(origin, destination);

            var pathLen = PolylineLength(path);
            if (pathLen <= direct * DetourRatioThreshold)
                return path;

            if (TryTrimToDestination(origin, destination, path, out var trimmed))
                return trimmed;

            return path;
        }

        internal static Vector3[] ApplyDisplayLine(Vector3 origin, Vector3 destination, Vector3[] line)
        {
            if (!ModConfig.ForceCorrectSideArrivalEnabled)
                return line;

            if (line.Length < 2)
                return line;

            var direct = FlatDistance(origin, destination);
            if (direct > ArrivalZoneMeters)
                return line;

            if (direct <= ImmediateArrivalMeters)
                return new[] { origin, destination };

            var lineLen = PolylineLength(line);
            if (lineLen > direct * DetourRatioThreshold &&
                TryTrimToDestination(origin, destination, line, out var trimmed))
                return trimmed;

            return line;
        }

        private static Vector3[] FinishArrival(Vector3 origin, Vector3 destination) =>
            new[] { origin, destination };

        private static bool TryTrimToDestination(
            Vector3 origin,
            Vector3 destination,
            Vector3[] path,
            out Vector3[] trimmed)
        {
            trimmed = path;

            var destIdx = IndexNearest(path, destination);
            var originIdx = IndexNearest(path, origin);
            var distAtBest = FlatDistance(path[destIdx], destination);

            if (distAtBest > 28f)
                return false;

            if (destIdx <= originIdx && FlatDistance(origin, destination) < ArrivalZoneMeters)
            {
                trimmed = new[] { origin, destination };
                return true;
            }

            var list = new List<Vector3> { origin };
            var start = Mathf.Max(0, originIdx - 1);
            for (var i = start; i <= destIdx; i++)
                list.Add(path[i]);

            list.Add(destination);
            trimmed = Deduplicate(list);
            return trimmed.Length >= 2 && PolylineLength(trimmed) < PolylineLength(path) * 0.92f;
        }

        private static int IndexNearest(Vector3[] path, Vector3 point)
        {
            var best = 0;
            var bestSq = float.MaxValue;
            for (var i = 0; i < path.Length; i++)
            {
                var sq = FlatDistanceSq(path[i], point);
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = i;
                }
            }

            return best;
        }

        private static Vector3[] Deduplicate(List<Vector3> points)
        {
            if (points.Count == 0)
                return System.Array.Empty<Vector3>();

            var result = new List<Vector3> { points[0] };
            for (var i = 1; i < points.Count; i++)
            {
                if ((points[i] - result[result.Count - 1]).sqrMagnitude >= 1f)
                    result.Add(points[i]);
            }

            if (result.Count < 2)
                return new[] { points[0], points[points.Count - 1] };
            return result.ToArray();
        }

        internal static float FlatDistance(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static float FlatDistanceSq(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        internal static float PolylineLength(Vector3[] points)
        {
            var len = 0f;
            for (var i = 1; i < points.Length; i++)
                len += FlatDistance(points[i - 1], points[i]);
            return len;
        }
    }
}
