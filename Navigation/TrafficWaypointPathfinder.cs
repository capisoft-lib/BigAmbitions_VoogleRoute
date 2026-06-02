using System.Collections.Generic;
using UnityEngine;

namespace VoogleRoute.Navigation;

/// <summary>A* sur le graphe Gley Traffic waypoints.</summary>
internal static class TrafficWaypointPathfinder
{
    private const float DefaultSearchRadius = 120f;
    private const float MinSearchRadius = 40f;

    public static bool TryFindPath(Vector3 origin, Vector3 destination, out Vector3[] corners)
    {
        corners = System.Array.Empty<Vector3>();

        var direct = VehiclePathArrival.FlatDistance(origin, destination);
        if (direct <= 22f)
        {
            corners = new[] { origin, destination };
            return true;
        }

        var graph = TrafficWaypointGraph.Instance;
        if (!graph.TryEnsureLoaded())
            return false;

        if (!TryFindBestRoute(graph, origin, destination, out var pathIndices))
            return false;

        corners = BuildPolyline(graph, pathIndices, origin, destination);
        if (corners.Length < 2)
            return false;

        corners = VehiclePathArrival.Apply(origin, destination, corners);

        return true;
    }

    private static bool TryFindBestRoute(
        TrafficWaypointGraph graph,
        Vector3 origin,
        Vector3 destination,
        out List<int> bestPath)
    {
        bestPath = new List<int>();
        var startBuf = new int[6];
        var endBuf = new int[6];

        var radius = VehiclePathArrival.FlatDistance(origin, destination) < 55f ? 55f : DefaultSearchRadius;
        var startCount = graph.CollectNearest(origin, radius, startBuf);
        var endCount = graph.CollectNearest(destination, radius, endBuf);

        if (startCount == 0)
        {
            if (!graph.TryFindNearest(origin, 200f, out var fallbackStart, out _))
                return false;
            startBuf[0] = fallbackStart;
            startCount = 1;
        }

        if (endCount == 0)
        {
            if (!graph.TryFindNearest(destination, 200f, out var fallbackEnd, out _))
                return false;
            endBuf[0] = fallbackEnd;
            endCount = 1;
        }

        var bestCost = float.MaxValue;
        List<int>? best = null;

        for (var si = 0; si < startCount; si++)
        {
            var startIdx = startBuf[si];
            for (var ei = 0; ei < endCount; ei++)
            {
                var endIdx = endBuf[ei];
                List<int> path;
                if (startIdx == endIdx)
                {
                    path = new List<int> { startIdx };
                }
                else if (!TryAStar(graph, startIdx, endIdx, out path))
                {
                    continue;
                }

                var cost = EstimateRouteCost(graph, origin, destination, startIdx, endIdx, path);
                if (cost >= bestCost)
                    continue;

                bestCost = cost;
                best = path;
            }
        }

        if (best == null || best.Count == 0)
            return false;

        bestPath = best;
        return true;
    }

    private static float EstimateRouteCost(
        TrafficWaypointGraph graph,
        Vector3 origin,
        Vector3 destination,
        int startIdx,
        int endIdx,
        List<int> path)
    {
        var cost = VehiclePathArrival.FlatDistance(origin, graph.GetPosition(startIdx));
        for (var i = 1; i < path.Count; i++)
            cost += VehiclePathArrival.FlatDistance(
                graph.GetPosition(path[i - 1]),
                graph.GetPosition(path[i]));
        cost += VehiclePathArrival.FlatDistance(graph.GetPosition(endIdx), destination);
        return cost;
    }

    private static bool TryAStar(TrafficWaypointGraph graph, int start, int goal, out List<int> path)
    {
        path = new List<int>();

        var open = new List<int> { start };
        var cameFrom = new Dictionary<int, int>();
        var gScore = new Dictionary<int, float> { [start] = 0f };
        var fScore = new Dictionary<int, float> { [start] = Heuristic(graph, start, goal) };
        var closed = new HashSet<int>();

        while (open.Count > 0)
        {
            var current = PopLowestF(open, fScore);
            if (current == goal)
            {
                Reconstruct(cameFrom, current, path);
                return path.Count >= 1;
            }

            closed.Add(current);
            var neighbors = graph.GetNeighbors(current);
            var gCurrent = gScore.GetValueOrDefault(current, float.MaxValue);

            for (var i = 0; i < neighbors.Length; i++)
            {
                var next = neighbors[i];
                if (closed.Contains(next))
                    continue;

                var tentative = gCurrent + EdgeCost(graph, current, next);
                if (tentative >= gScore.GetValueOrDefault(next, float.MaxValue))
                    continue;

                cameFrom[next] = current;
                gScore[next] = tentative;
                fScore[next] = tentative + Heuristic(graph, next, goal);

                if (!open.Contains(next))
                    open.Add(next);
            }
        }

        return false;
    }

    private static int PopLowestF(List<int> open, Dictionary<int, float> fScore)
    {
        var best = 0;
        var bestF = float.MaxValue;
        for (var i = 0; i < open.Count; i++)
        {
            var idx = open[i];
            var f = fScore.GetValueOrDefault(idx, float.MaxValue);
            if (f < bestF)
            {
                bestF = f;
                best = i;
            }
        }

        var node = open[best];
        open.RemoveAt(best);
        return node;
    }

    private static float Heuristic(TrafficWaypointGraph graph, int from, int to)
    {
        var a = graph.GetPosition(from);
        var b = graph.GetPosition(to);
        var dx = a.x - b.x;
        var dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private static float EdgeCost(TrafficWaypointGraph graph, int from, int to)
    {
        var a = graph.GetPosition(from);
        var b = graph.GetPosition(to);
        var dx = a.x - b.x;
        var dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private static void Reconstruct(Dictionary<int, int> cameFrom, int current, List<int> path)
    {
        path.Add(current);
        while (cameFrom.TryGetValue(current, out var prev))
        {
            current = prev;
            path.Add(current);
        }

        path.Reverse();
    }

    private static Vector3[] BuildPolyline(
        TrafficWaypointGraph graph,
        List<int> indices,
        Vector3 origin,
        Vector3 destination)
    {
        var points = new List<Vector3>(indices.Count + 2);
        points.Add(origin);

        for (var i = 0; i < indices.Count; i++)
            points.Add(graph.GetPosition(indices[i]));

        points.Add(destination);
        return Deduplicate(points);
    }

    private static Vector3[] Deduplicate(List<Vector3> points)
    {
        if (points.Count == 0)
            return System.Array.Empty<Vector3>();

        var result = new List<Vector3>(points.Count);
        const float minSq = 2.25f;

        foreach (var p in points)
        {
            if (result.Count == 0 || (p - result[^1]).sqrMagnitude >= minSq)
                result.Add(p);
        }

        if (result.Count < 2 && points.Count >= 2)
            return new[] { points[0], points[^1] };

        return result.ToArray();
    }
}
