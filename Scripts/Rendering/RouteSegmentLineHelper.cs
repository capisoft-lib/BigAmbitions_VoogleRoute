using System.Collections.Generic;
using UnityEngine;
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
            footPoints = System.Array.Empty<Vector3>();
            subwayPoints = System.Array.Empty<Vector3>();

            if (path.Segments != null && path.Segments.Length > 0)
            {
                var foot = new List<Vector3>();
                var subway = new List<Vector3>();
                for (var i = 0; i < path.Segments.Length; i++)
                {
                    var segment = path.Segments[i];
                    if (segment.Points == null || segment.Points.Length == 0)
                        continue;

                    var target = segment.Kind == RoutePathSegmentKind.Subway ? subway : foot;
                    AppendDistinct(target, segment.Points);
                }

                footPoints = foot.Count >= 2 ? foot.ToArray() : System.Array.Empty<Vector3>();
                subwayPoints = subway.Count >= 2 ? subway.ToArray() : System.Array.Empty<Vector3>();
                return;
            }

            if (path.Points != null && path.Points.Length >= 2)
                footPoints = path.Points;
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
