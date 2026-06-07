using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoogleRoute.Navigation
{
    internal static class PathGeometry
    {
        private static int _vehicleLineTrimSegment;

        internal static void ResetVehicleLineTrimState() => _vehicleLineTrimSegment = 0;

        internal static Vector3[] DecimateColinear(IReadOnlyList<Vector3> points, float epsilonDegrees, int maxPoints)
        {
            if (points.Count <= 2)
                return points.Count == 0 ? Array.Empty<Vector3>() : new[] { points[0], points[points.Count - 1] };

            var list = new List<Vector3> { points[0] };
            for (var i = 1; i < points.Count - 1; i++)
            {
                if (!IsColinearMiddle(points[i - 1], points[i], points[i + 1], epsilonDegrees))
                    list.Add(points[i]);
            }

            list.Add(points[points.Count - 1]);

            if (list.Count <= maxPoints)
                return list.ToArray();

            return Decimate(list, 8f, maxPoints);
        }

        internal static List<int> SimplifyPathIndices(
            TrafficWaypointGraph graph,
            IReadOnlyList<int> path)
        {
            if (path.Count < 3)
                return new List<int>(path);

            var work = new List<int>(path);
            var changed = true;
            var passes = 0;

            while (changed && passes < 6 && work.Count >= 3)
            {
                changed = false;
                passes++;

                for (var i = 1; i < work.Count - 1; i++)
                {
                    if (!graph.ShouldCollapseJunctionWaypoint(work[i - 1], work[i], work[i + 1]))
                        continue;

                    work.RemoveAt(i);
                    changed = true;
                    break;
                }
            }

            return work;
        }

        internal static Vector3[] TrimBehindOrigin(Vector3[] points, Vector3 origin, float keepBehindMeters) =>
            TrimBehindOrigin(points, origin, Vector3.zero, keepBehindMeters, useForward: false);

        internal static Vector3[] TrimBehindOrigin(
            Vector3[] points,
            Vector3 origin,
            Vector3 forward,
            float keepBehindMeters) =>
            TrimBehindOrigin(points, origin, forward, keepBehindMeters, useForward: true);

        internal static List<Vector3> SmoothCorners(IReadOnlyList<Vector3> corners, float maxSegment)
        {
            var list = new List<Vector3>(corners.Count * 2);
            for (var i = 0; i < corners.Count; i++)
            {
                var a = corners[i];
                if (i == 0)
                {
                    list.Add(a);
                    continue;
                }

                var b = corners[i];
                var dist = Vector3.Distance(a, b);
                if (dist <= maxSegment)
                {
                    list.Add(b);
                    continue;
                }

                var steps = Mathf.CeilToInt(dist / maxSegment);
                for (var s = 1; s <= steps; s++)
                    list.Add(Vector3.Lerp(a, b, s / (float)steps));
            }

            return list;
        }

        private static Vector3[] Decimate(IReadOnlyList<Vector3> points, float minSpacing, int maxPoints = 48)
        {
            if (points.Count == 0)
                return Array.Empty<Vector3>();

            var minSq = minSpacing * minSpacing;
            var list = new List<Vector3>(Mathf.Min(points.Count, maxPoints)) { points[0] };

            for (var i = 1; i < points.Count; i++)
            {
                var p = points[i];
                if ((p - list[list.Count - 1]).sqrMagnitude < minSq)
                    continue;
                list.Add(p);
                if (list.Count >= maxPoints - 1)
                    break;
            }

            var last = points[points.Count - 1];
            if ((last - list[list.Count - 1]).sqrMagnitude >= minSq * 0.25f)
                list.Add(last);
            else if (list.Count > 0)
                list[list.Count - 1] = last;

            return list.Count >= 2 ? list.ToArray() : new[] { points[0], last };
        }

        private static Vector3[] TrimBehindOrigin(
            Vector3[] points,
            Vector3 origin,
            Vector3 forward,
            float keepBehindMeters,
            bool useForward)
        {
            if (points.Length < 2)
                return points;

            if (!useForward)
                return TrimBehindNearestVertex(points, origin, keepBehindMeters);

            return TrimBehindPolylineProgress(points, origin, forward, keepBehindMeters);
        }

        private static Vector3[] TrimBehindNearestVertex(Vector3[] points, Vector3 origin, float keepBehindMeters)
        {
            var bestIndex = 0;
            var bestDist = float.MaxValue;
            for (var i = 0; i < points.Length; i++)
            {
                var d = HorizontalDistSq(origin, points[i]);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestIndex = i;
                }
            }

            return SliceFromVertex(points, bestIndex, keepBehindMeters);
        }

        private static Vector3[] TrimBehindPolylineProgress(
            Vector3[] points,
            Vector3 origin,
            Vector3 forward,
            float keepBehindMeters)
        {
            var seg = Mathf.Clamp(_vehicleLineTrimSegment, 0, points.Length - 2);
            FindProgressSegmentIndex(points, origin, forward, ref seg, maxSegmentJump: 1, lookAheadGateMeters: 20f);
            _vehicleLineTrimSegment = seg;

            HorizontalDistSqToSegment(origin, points[seg], points[seg + 1], out var cutT);
            var backRemaining = keepBehindMeters;
            var startSeg = seg;
            var startT = cutT;

            while (backRemaining > 0.001f)
            {
                var segLen = HorizontalDistance(points[startSeg], points[startSeg + 1]);
                if (segLen < 0.001f)
                {
                    if (startSeg == 0)
                        break;
                    startSeg--;
                    startT = 1f;
                    continue;
                }

                var backOnSeg = startT * segLen;
                if (backOnSeg >= backRemaining)
                {
                    startT -= backRemaining / segLen;
                    break;
                }

                backRemaining -= backOnSeg;
                if (startSeg == 0)
                {
                    startT = 0f;
                    break;
                }

                startSeg--;
                startT = 1f;
            }

            var list = new List<Vector3>(points.Length - startSeg + 1);
            list.Add(Vector3.Lerp(points[startSeg], points[startSeg + 1], startT));
            for (var i = startSeg + 1; i < points.Length; i++)
                list.Add(points[i]);

            return list.Count >= 2 ? list.ToArray() : points;
        }

        private static Vector3[] SliceFromVertex(Vector3[] points, int bestIndex, float keepBehindMeters)
        {
            var start = Mathf.Max(0, bestIndex - 1);
            if (start > 0 && keepBehindMeters > 0f)
            {
                var segLen = HorizontalDistance(points[start - 1], points[start]);
                if (segLen > 0.001f)
                {
                    var t = Mathf.Clamp01(keepBehindMeters / segLen);
                    var trimmed = new Vector3[points.Length - start + 1];
                    trimmed[0] = Vector3.Lerp(points[start - 1], points[start], 1f - t);
                    for (var i = start; i < points.Length; i++)
                        trimmed[i - start + 1] = points[i];
                    return trimmed;
                }
            }

            if (start == 0)
                return points;

            var slice = new Vector3[points.Length - start];
            Array.Copy(points, start, slice, 0, slice.Length);
            return slice.Length >= 2 ? slice : points;
        }

        /// <summary>
        /// Closest segment on the polyline with monotonic progress — avoids jumping to a later leg at intersections.
        /// </summary>
        internal static int FindProgressSegmentIndex(
            IReadOnlyList<Vector3> points,
            Vector3 origin,
            Vector3 forward,
            ref int progressSegmentIndex,
            int maxSegmentJump = 1,
            float lookAheadGateMeters = 20f)
        {
            if (points.Count < 2)
                return 0;

            forward.y = 0f;
            var hasForward = forward.sqrMagnitude > 0.01f;
            if (hasForward)
                forward.Normalize();

            progressSegmentIndex = Mathf.Clamp(progressSegmentIndex, 0, points.Count - 2);
            var searchStart = Mathf.Max(0, progressSegmentIndex - 1);
            var bestSeg = progressSegmentIndex;
            var bestScore = float.MaxValue;

            for (var seg = searchStart; seg < points.Count - 1; seg++)
            {
                if (seg > progressSegmentIndex + maxSegmentJump)
                {
                    var gate = points[Mathf.Min(progressSegmentIndex + 1, points.Count - 1)];
                    if (HorizontalDistance(origin, gate) > lookAheadGateMeters)
                        break;
                }

                var distSq = HorizontalDistSqToSegment(origin, points[seg], points[seg + 1], out _);
                var score = distSq;
                var ahead = Mathf.Max(0, seg - progressSegmentIndex);
                score += ahead * ahead * 144f;

                if (hasForward)
                {
                    var segDir = points[seg + 1] - points[seg];
                    segDir.y = 0f;
                    if (segDir.sqrMagnitude > 0.01f)
                    {
                        segDir.Normalize();
                        if (Vector3.Dot(forward, segDir) < 0.35f)
                            score += 3600f;
                    }
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    bestSeg = seg;
                }
            }

            progressSegmentIndex = Mathf.Clamp(
                Mathf.Max(progressSegmentIndex - 1, bestSeg),
                0,
                points.Count - 2);
            return progressSegmentIndex;
        }

        internal static float HorizontalDistSqToSegment(
            Vector3 point,
            Vector3 segA,
            Vector3 segB,
            out float t)
        {
            var ab = segB - segA;
            ab.y = 0f;
            var ap = point - segA;
            ap.y = 0f;
            var abLenSq = ab.sqrMagnitude;
            if (abLenSq < 0.01f)
            {
                t = 0f;
                return ap.sqrMagnitude;
            }

            t = Mathf.Clamp01(Vector3.Dot(ap, ab) / abLenSq);
            var closest = segA + ab * t;
            var dx = point.x - closest.x;
            var dz = point.z - closest.z;
            return dx * dx + dz * dz;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static float HorizontalDistSq(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        private static bool IsColinearMiddle(Vector3 a, Vector3 b, Vector3 c, float epsilonDegrees)
        {
            var ab = b - a;
            var bc = c - b;
            ab.y = 0f;
            bc.y = 0f;
            if (ab.sqrMagnitude < 0.01f || bc.sqrMagnitude < 0.01f)
                return true;

            var angle = Vector3.Angle(ab, bc);
            return angle < epsilonDegrees;
        }
    }
}
