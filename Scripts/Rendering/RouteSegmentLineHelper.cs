using System.Collections.Generic;
using UnityEngine;
using VoogleRoute;
using VoogleRoute.Navigation;

namespace VoogleRoute.Rendering
{
    internal static class RouteSegmentLineHelper
    {
        internal static readonly Color SubwayLineColor = new Color(1f, 0.82f, 0.2f, 0.95f);

        internal static void ExtractSegments(
            PathResult path,
            out Vector3[] footPoints,
            out Vector3[] subwayPoints)
        {
            ExtractFootLegs(path, out var legs, out subwayPoints);
            footPoints = MergeLegs(legs);
        }

        internal static void ExtractFootLegs(
            PathResult path,
            out Vector3[][] footLegs,
            out Vector3[] subwayPoints)
        {
            footLegs = System.Array.Empty<Vector3[]>();
            subwayPoints = System.Array.Empty<Vector3>();

            if (path.Segments != null && path.Segments.Length > 0)
            {
                var legs = new List<Vector3[]>();
                var subway = new List<Vector3>();
                for (var i = 0; i < path.Segments.Length; i++)
                {
                    var segment = path.Segments[i];
                    if (segment.Points == null || segment.Points.Length < 2)
                        continue;

                    if (segment.Kind == RoutePathSegmentKind.Subway)
                        AppendDistinct(subway, segment.Points);
                    else
                        legs.Add(CopyPoints(segment.Points));
                }

                footLegs = legs.Count > 0 ? legs.ToArray() : System.Array.Empty<Vector3[]>();
                subwayPoints = subway.Count >= 2
                    ? ProjectSubwayDisplayPoints(subway.ToArray())
                    : System.Array.Empty<Vector3>();
                return;
            }

            if (path.Points != null && path.Points.Length >= 2)
                footLegs = new[] { CopyPoints(path.Points) };
        }

        internal static Vector3[] ProjectSubwayDisplayPoints(Vector3[] points)
        {
            if (points == null || points.Length == 0)
                return System.Array.Empty<Vector3>();

            return GroundProjector.ProjectToGround(points, ModConfig.FootGroundOffset);
        }

        private static Vector3[] MergeLegs(Vector3[][] legs)
        {
            if (legs == null || legs.Length == 0)
                return System.Array.Empty<Vector3>();

            if (legs.Length == 1)
                return legs[0];

            var merged = new List<Vector3>();
            for (var i = 0; i < legs.Length; i++)
                AppendDistinct(merged, legs[i]);

            return merged.Count >= 2 ? merged.ToArray() : System.Array.Empty<Vector3>();
        }

        private static Vector3[] CopyPoints(Vector3[] points)
        {
            var copy = new Vector3[points.Length];
            for (var i = 0; i < points.Length; i++)
                copy[i] = points[i];
            return copy;
        }

        private static void AppendDistinct(List<Vector3> target, Vector3[] points)
        {
            for (var i = 0; i < points.Length; i++)
            {
                if (target.Count > 0 && (points[i] - target[^1]).sqrMagnitude < 0.04f)
                    continue;

                target.Add(points[i]);
            }
        }
    }
}
