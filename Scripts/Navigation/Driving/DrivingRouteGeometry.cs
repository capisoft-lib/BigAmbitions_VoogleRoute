using System.Collections.Generic;
using UnityEngine;
using VoogleRoute.Pathfinding.Routing;

namespace VoogleRoute.Navigation.Driving
{
    internal static class DrivingRouteGeometry
    {
        internal static bool TryBuild(PhysicalRoutingGraph graph, RouteQuery query, float radius,
            out RouteResult route, out List<Vector3> samples, out string detail)
        {
            samples = null;
            route = null;
            detail = "graph_search_failed";
            for (var attempt = 0; attempt < 8; attempt++)
            {
                if (!WaypointPathfinder.TryFindBestRoute(graph, query, out route) || route.Path.Count < 2)
                {
                    detail = "graph_search_failed attempt=" + attempt + " previous=" + detail;
                    return false;
                }
                var anchors = BuildAnchors(graph, route.Path);
                if (DrivingPathGeometry.TryBuild(anchors, radius, out samples, out var curveDetail, out var failurePoint))
                {
                    detail = "attempt=" + attempt + " graph_nodes=" + route.Path.Count + " " + curveDetail;
                    return true;
                }
                detail = "attempt=" + attempt + " " + curveDetail;
                // A tight base connector is not necessarily the only way to the destination.
                // Exclude that directed connection and search for a wider route before refusing.
                var closest = -1;
                var error = float.PositiveInfinity;
                for (var i = 1; i < route.Path.Count - 1; i++)
                {
                    var distance = DrivingPath.Flat(PhysicalRoutingGraph.Unity(graph.GetPosition(route.Path[i])) - failurePoint).sqrMagnitude;
                    if (distance >= error) continue;
                    error = distance; closest = i;
                }
                if (closest < 0) return false;
                graph.ExcludeConnection(route.Path[closest], route.Path[closest + 1]);
            }
            return false;
        }

        private static List<Vector3> BuildAnchors(PhysicalRoutingGraph graph, IReadOnlyList<int> nodes)
        {
            var points = new List<Vector3> { PhysicalRoutingGraph.Unity(graph.GetPosition(nodes[0])) };
            for (var i = 1; i < nodes.Count; i++)
            {
                var a = PhysicalRoutingGraph.Unity(graph.GetPosition(nodes[i - 1]));
                var b = PhysicalRoutingGraph.Unity(graph.GetPosition(nodes[i]));
                if (graph.TryGetSyntheticTurnControl(nodes[i - 1], nodes[i], out var cp) &&
                    !graph.IsLaneChangeEdge(nodes[i - 1], nodes[i]))
                {
                    var c = PhysicalRoutingGraph.Unity(cp);
                    var count = Mathf.Max(8, Mathf.CeilToInt((Vector3.Distance(a, c) + Vector3.Distance(c, b)) / 1.5f));
                    for (var j = 1; j <= count; j++)
                    {
                        var t = j / (float)count;
                        Add(points, Vector3.Lerp(Vector3.Lerp(a, c, t), Vector3.Lerp(c, b, t), t));
                    }
                }
                else Add(points, b);
            }
            return points;
        }

        private static void Add(List<Vector3> points, Vector3 p)
        { if ((p - points[points.Count - 1]).sqrMagnitude > 0.01f) points.Add(p); }
    }
}
