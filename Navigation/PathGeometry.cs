using UnityEngine;

namespace VoogleRoute.Navigation;

internal static class PathGeometry
{
    /// <summary>Réduit le nombre de points (distance min. entre sommets, plafond).</summary>
    internal static Vector3[] Decimate(IReadOnlyList<Vector3> points, float minSpacing, int maxPoints = 48)
    {
        if (points.Count == 0)
            return Array.Empty<Vector3>();

        var minSq = minSpacing * minSpacing;
        var list = new List<Vector3>(Mathf.Min(points.Count, maxPoints)) { points[0] };

        for (var i = 1; i < points.Count; i++)
        {
            var p = points[i];
            if ((p - list[^1]).sqrMagnitude < minSq)
                continue;
            list.Add(p);
            if (list.Count >= maxPoints - 1)
                break;
        }

        var last = points[^1];
        if ((last - list[^1]).sqrMagnitude >= minSq * 0.25f)
            list.Add(last);
        else if (list.Count > 0)
            list[^1] = last;

        return list.Count >= 2 ? list.ToArray() : new[] { points[0], last };
    }

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

    internal static Vector3[] TrimBehindOrigin(Vector3[] points, Vector3 origin, float keepBehindMeters)
    {
        if (points.Length < 2)
            return points;

        var bestIndex = 0;
        var bestDist = float.MaxValue;
        for (var i = 0; i < points.Length; i++)
        {
            var d = (points[i] - origin).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                bestIndex = i;
            }
        }

        var start = Mathf.Max(0, bestIndex - 1);
        if (start > 0 && keepBehindMeters > 0f)
        {
            var seg = points[start] - points[start - 1];
            var len = seg.magnitude;
            if (len > 0.001f)
            {
                var t = Mathf.Clamp01(keepBehindMeters / len);
                var trimmed = new List<Vector3> { Vector3.Lerp(points[start - 1], points[start], 1f - t) };
                for (var i = start; i < points.Length; i++)
                    trimmed.Add(points[i]);
                return trimmed.ToArray();
            }
        }

        if (start == 0)
            return points;

        var slice = new Vector3[points.Length - start];
        Array.Copy(points, start, slice, 0, slice.Length);
        return slice.Length >= 2 ? slice : points;
    }
}
