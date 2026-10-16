using System.Collections.Generic;
using UnityEngine;

namespace VoogleRoute.Navigation.Driving
{
    /// <summary>Fits the whole road curve, independent of arbitrary spacing between Gley anchors.</summary>
    internal static class DrivingPathGeometry
    {
        internal const float CorridorMeters = 1.8f;
        internal static bool TryBuild(List<Vector3> anchors, float minimumRadius,
            out List<Vector3> result, out string detail)
            => TryBuild(anchors, minimumRadius, out result, out detail, out _);

        internal static bool TryBuild(List<Vector3> anchors, float minimumRadius,
            out List<Vector3> result, out string detail, out Vector3 failurePoint)
        {
            result = new List<Vector3>();
            detail = "insufficient_anchors";
            failurePoint = anchors != null && anchors.Count > 0 ? anchors[0] : default;
            if (anchors == null || anchors.Count < 2) return false;
            var original = Resample(anchors, 0.75f);
            var current = original.ToArray();
            var next = new Vector3[current.Length];
            var allowedCurvature = 1f / Mathf.Max(1f, minimumRadius);
            var bestCurvature = float.PositiveInfinity;
            for (var iteration = 0; iteration <= 240; iteration++)
            {
                var maximum = 0f;
                var peak = 0;
                for (var i = 1; i < current.Length - 1; i++)
                {
                    var curvature = DrivingPath.Curvature(current[i - 1], current[i], current[i + 1]);
                    if (curvature > maximum) { maximum = curvature; peak = i; }
                }
                if (maximum <= allowedCurvature * 1.03f)
                {
                    result.AddRange(current);
                    detail = "iterations=" + iteration + " max_curvature=" + maximum.ToString("F3") +
                        " min_radius=" + minimumRadius.ToString("F2") + " corridor=" + CorridorMeters;
                    return true;
                }
                if (maximum < bestCurvature)
                {
                    bestCurvature = maximum;
                    failurePoint = original[peak];
                }
                next[0] = original[0];
                next[next.Length - 1] = original[original.Count - 1];
                for (var i = 1; i < current.Length - 1; i++)
                {
                    var relaxed = current[i] * 0.5f + (current[i - 1] + current[i + 1]) * 0.25f;
                    var offset = DrivingPath.Flat(relaxed - original[i]);
                    if (offset.sqrMagnitude > CorridorMeters * CorridorMeters)
                        relaxed = original[i] + offset.normalized * CorridorMeters;
                    relaxed.y = original[i].y;
                    next[i] = relaxed;
                }
                var swap = current; current = next; next = swap;
                detail = "curve_too_tight max_curvature=" + maximum.ToString("F3") +
                    " allowed=" + allowedCurvature.ToString("F3") + " corridor=" + CorridorMeters +
                    " sample=" + peak + " position=" + current[peak].ToString("F2");
                detail += " best_curvature=" + bestCurvature.ToString("F3");
            }
            return false;
        }

        private static List<Vector3> Resample(List<Vector3> points, float spacing)
        {
            var distances = new float[points.Count];
            for (var i = 1; i < points.Count; i++)
                distances[i] = distances[i - 1] + DrivingPath.FlatDistance(points[i - 1], points[i]);
            var length = distances[distances.Length - 1];
            var result = new List<Vector3> { points[0] };
            var segment = 0;
            for (var d = spacing; d < length - 0.25f; d += spacing)
            {
                while (segment < points.Count - 2 && distances[segment + 1] < d) segment++;
                result.Add(Vector3.Lerp(points[segment], points[segment + 1],
                    (d - distances[segment]) / Mathf.Max(0.001f, distances[segment + 1] - distances[segment])));
            }
            result.Add(points[points.Count - 1]);
            return result;
        }
    }
}
