using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoogleRoute.Navigation.Driving
{
    /// <summary>Arc-length path; progress is local and monotonic, including at overlapping junctions.</summary>
    internal sealed class DrivingPath
    {
        internal readonly Vector3[] Points;
        internal readonly float[] Distances;
        internal readonly float[] SpeedLimits;
        internal float Length => Distances[Distances.Length - 1];

        internal DrivingPath(List<Vector3> points, float cruise)
        {
            Points = points.ToArray();
            Distances = new float[Points.Length];
            SpeedLimits = new float[Points.Length];
            for (var i = 1; i < Points.Length; i++)
                Distances[i] = Distances[i - 1] + FlatDistance(Points[i - 1], Points[i]);
            for (var i = 0; i < Points.Length; i++)
            {
                var curvature = i == 0 || i == Points.Length - 1 ? 0f :
                    Curvature(Points[i - 1], Points[i], Points[i + 1]);
                SpeedLimits[i] = Mathf.Min(cruise, Mathf.Sqrt(1.6f / Mathf.Max(curvature, 0.001f)));
            }
            SpeedLimits[SpeedLimits.Length - 1] = 0f;
            ApplyBrakingEnvelope();
        }

        internal void ApplyBrakingEnvelope()
        {
            // Propagate future braking constraints backwards, before entering a bend.
            for (var i = SpeedLimits.Length - 2; i >= 0; i--)
                SpeedLimits[i] = Mathf.Min(SpeedLimits[i], Mathf.Sqrt(SpeedLimits[i + 1] * SpeedLimits[i + 1] +
                    2f * 2f * (Distances[i + 1] - Distances[i])));
        }

        internal float Project(Vector3 position, float progress, out float lateralError, float searchAhead = 18f)
        {
            var best = float.PositiveInfinity;
            var result = progress;
            for (var i = 0; i < Points.Length - 1; i++)
            {
                if (Distances[i + 1] < progress - 2f) continue;
                if (Distances[i] > progress + searchAhead) break;
                var segment = Flat(Points[i + 1] - Points[i]);
                var t = Mathf.Clamp01(Vector3.Dot(Flat(position - Points[i]), segment) /
                    Mathf.Max(0.001f, segment.sqrMagnitude));
                var projected = Vector3.Lerp(Points[i], Points[i + 1], t);
                var error = Flat(position - projected).sqrMagnitude;
                if (Mathf.Abs(position.y - projected.y) > 3f) continue;
                if (error >= best) continue;
                best = error;
                result = Mathf.Lerp(Distances[i], Distances[i + 1], t);
            }
            lateralError = Mathf.Sqrt(best);
            return Mathf.Max(progress, result);
        }

        internal Vector3 At(float distance)
        {
            distance = Mathf.Clamp(distance, 0f, Length);
            var i = SegmentAt(distance);
            return Vector3.Lerp(Points[i], Points[i + 1],
                (distance - Distances[i]) / Mathf.Max(0.001f, Distances[i + 1] - Distances[i]));
        }

        internal float TargetSpeed(float distance)
        {
            var i = SegmentAt(distance);
            return Mathf.Lerp(SpeedLimits[i], SpeedLimits[i + 1],
                Mathf.InverseLerp(Distances[i], Distances[i + 1], distance));
        }

        private int SegmentAt(float distance)
        {
            var i = Array.BinarySearch(Distances, distance);
            if (i < 0) i = ~i - 1;
            return Mathf.Clamp(i, 0, Points.Length - 2);
        }

        internal static float Curvature(Vector3 a, Vector3 b, Vector3 c)
        {
            var ab = Flat(b - a);
            var ac = Flat(c - a);
            return 2f * Mathf.Abs(Vector3.Cross(ab, ac).y) /
                Mathf.Max(0.001f, ab.magnitude * ac.magnitude * Flat(c - b).magnitude);
        }

        internal static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
        internal static float FlatDistance(Vector3 a, Vector3 b) => Flat(a - b).magnitude;
    }
}
