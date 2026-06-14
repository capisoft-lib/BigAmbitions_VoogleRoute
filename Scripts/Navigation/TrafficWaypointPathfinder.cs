using System;
using System.Collections.Generic;
using UnityEngine;
using VoogleRoute;

namespace VoogleRoute.Navigation
{
    internal static class TrafficWaypointPathfinder
    {
        private const float DefaultSearchRadius = 120f;
        private const int MaxStartCandidates = 4;
        private const int MaxEndCandidates = 6;
        private const int MaxAStarNodes = 8192;
        private const float RouteReuseMaxDistMeters = 55f;
        private const float RouteFollowMaxDistMeters = 20f;
        private const float RouteFollowMaxCrossTrackMeters = 14f;
        private const float RouteFollowMinHeadingDot = 0.45f;
        private const float CostTieEpsilon = 0.05f;
        private const int EnhancedTurnSegments = 8;
        private const float StrictStartAlignedSearchRadius = 220f;

        private static int _lockedStartWaypoint = -1;
        private static int _lockedEndWaypoint = -1;
        private static List<int> _lockedPathIndices;
        private static int _lockedPathProgressIndex;

        internal static void ResetDrivingLaneLock()
        {
            _lockedStartWaypoint = -1;
            _lockedEndWaypoint = -1;
            _lockedPathIndices = null;
            _lockedPathProgressIndex = 0;
        }

        /// <summary>True when the vehicle is still on the locked route (no A* / polyline rebuild needed).</summary>
        internal static bool IsFollowingLockedRoute(Vector3 origin, Vector3 forward)
        {
            if (_lockedPathIndices == null || _lockedPathIndices.Count < 2)
                return false;
            if (_lockedStartWaypoint < 0 || _lockedEndWaypoint < 0)
                return false;

            var graph = TrafficWaypointGraph.Instance;
            if (!graph.IsReady)
                return false;

            var progressIdx = FindBestProgressIndex(graph, _lockedPathIndices, origin, forward);
            if (progressIdx < 0)
                return false;

            if (!IsNearLockedPath(graph, _lockedPathIndices, progressIdx, origin))
                return false;

            if (_lockedPathIndices.Count - progressIdx < 2)
                return false;

            return IsAlignedWithRouteLeg(graph, _lockedPathIndices, progressIdx, origin, forward);
        }

        private static bool IsAlignedWithRouteLeg(
            TrafficWaypointGraph graph,
            IReadOnlyList<int> indices,
            int progressIdx,
            Vector3 origin,
            Vector3 forward)
        {
            var legEnd = Mathf.Min(progressIdx + 1, indices.Count - 1);
            if (legEnd <= progressIdx)
                return true;

            var legStart = graph.GetPosition(indices[progressIdx]);
            var legStop = graph.GetPosition(indices[legEnd]);
            var leg = legStop - legStart;
            leg.y = 0f;
            var legLenSq = leg.sqrMagnitude;
            if (legLenSq < 1f)
                return true;

            var legDir = leg / Mathf.Sqrt(legLenSq);
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                return true;
            forward.Normalize();

            if (Vector3.Dot(forward, legDir) < RouteFollowMinHeadingDot)
                return false;

            var crossTrack = HorizontalDistanceToSegment(origin, legStart, legStop);
            return crossTrack <= RouteFollowMaxCrossTrackMeters;
        }

        private static float HorizontalDistanceToSegment(Vector3 point, Vector3 segA, Vector3 segB)
        {
            var ab = segB - segA;
            ab.y = 0f;
            var ap = point - segA;
            ap.y = 0f;
            var abLenSq = ab.sqrMagnitude;
            if (abLenSq < 0.01f)
                return Mathf.Sqrt(ap.sqrMagnitude);

            var t = Mathf.Clamp01(Vector3.Dot(ap, ab) / abLenSq);
            var closest = segA + ab * t;
            var dx = point.x - closest.x;
            var dz = point.z - closest.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        internal static bool TryFindPath(
            Vector3 origin,
            Vector3 destination,
            out Vector3[] corners,
            bool allowRouteReuse = true)
        {
            corners = System.Array.Empty<Vector3>();

            var direct = VehiclePathArrival.FlatDistance(origin, destination);
            if (direct <= 22f)
            {
                if (ModConfig.ForceCorrectSideArrivalEnabled &&
                    VehiclePathHelper.TryGetArrivalRoadTarget(destination, out var snap))
                    corners = new[] { origin, snap };
                else if (VehiclePathHelper.TryGetRoadTarget(destination, out var roadTarget))
                    corners = new[] { origin, roadTarget };
                else
                    corners = new[] { origin, destination };
                return true;
            }

            var graph = TrafficWaypointGraph.Instance;
            if (!graph.TryEnsureLoaded())
                return false;

            if (!TryFindBestRoute(graph, origin, destination, allowRouteReuse,
                    out var pathIndices, out var endIdx, out var reusedLockedRoute))
                return false;

            List<int> displayIndices;
            if (reusedLockedRoute)
                displayIndices = pathIndices;
            else
                displayIndices = PathGeometry.SimplifyPathIndices(graph, pathIndices);
            corners = BuildPolyline(graph, displayIndices, endIdx, origin, destination);
            if (corners.Length < 2)
                return false;

            corners = VehiclePathArrival.Apply(origin, destination, corners);
            return true;
        }

        private static bool TryFindBestRoute(
            TrafficWaypointGraph graph,
            Vector3 origin,
            Vector3 destination,
            bool allowRouteReuse,
            out List<int> bestPath,
            out int bestEndIdx,
            out bool reusedLockedRoute)
        {
            bestPath = new List<int>();
            bestEndIdx = -1;
            reusedLockedRoute = false;

            var hasPose = MovementModeDetector.TryGetVehiclePose(out _, out var forward);
            if (allowRouteReuse && hasPose &&
                TryReuseLockedRoute(graph, origin, forward, out bestPath, out bestEndIdx))
            {
                reusedLockedRoute = true;
                return true;
            }

            var startBuf = new int[12];
            var endBuf = new int[12];
            var radius = VehiclePathArrival.FlatDistance(origin, destination) < 55f ? 55f : DefaultSearchRadius;
            var startCount = 0;
            var endCount = 0;

            if (allowRouteReuse &&
                hasPose &&
                _lockedStartWaypoint >= 0 &&
                graph.IsDrivingLaneAnchor(_lockedStartWaypoint, origin, forward))
            {
                startBuf[0] = _lockedStartWaypoint;
                startCount = 1;
            }
            else
            {
                startCount = hasPose
                    ? graph.CollectNearestAligned(origin, forward, radius, startBuf)
                    : graph.CollectNearest(origin, radius, startBuf);

                if (startCount == 0)
                {
                    if (!graph.TryFindNearest(origin, 200f, out var fallbackStart, out _))
                        return false;
                    startBuf[0] = fallbackStart;
                    startCount = 1;
                }

                if (hasPose)
                {
                    startCount = graph.FilterFlowAligned(startBuf, startCount, forward);
                    if (startCount == 0)
                        startCount = CollectStrictAlignedStarts(graph, origin, forward, startBuf);
                    if (startCount == 0)
                        return false;

                    startCount = graph.ExpandLaneCandidates(startBuf, startCount, startBuf.Length, forward);
                    startCount = graph.FilterFlowAligned(startBuf, startCount, forward);
                }
                else
                {
                    startCount = graph.ExpandLaneCandidates(startBuf, startCount, startBuf.Length);
                }

                if (startCount == 0)
                    return false;

                startCount = Mathf.Min(startCount, MaxStartCandidates);
            }

            if (allowRouteReuse &&
                _lockedEndWaypoint >= 0 &&
                IsLockedEndStillValid(graph, destination, _lockedEndWaypoint))
            {
                endBuf[0] = _lockedEndWaypoint;
                endCount = 1;
            }
            else
            {
                endCount = graph.CollectNearest(destination, radius, endBuf);
                if (endCount == 0)
                {
                    if (!graph.TryFindNearest(destination, 200f, out var fallbackEnd, out _))
                        return false;
                    endBuf[0] = fallbackEnd;
                    endCount = 1;
                }

                endCount = graph.ExpandLaneCandidates(endBuf, endCount, endBuf.Length);
                endCount = TrimEndCandidates(graph, endBuf, endCount, destination, MaxEndCandidates);
            }

            var bestCost = float.MaxValue;
            List<int> best = null;
            var bestEnd = -1;
            var bestStart = -1;

            for (var si = 0; si < startCount; si++)
            {
                var startIdx = startBuf[si];
                for (var ei = 0; ei < endCount; ei++)
                {
                    var endIdx = endBuf[ei];
                    if (!TryBuildPath(graph, startIdx, endIdx, out var path))
                        continue;

                    var cost = EstimateRouteCost(graph, origin, destination, startIdx, endIdx, path);
                    if (!ShouldPreferRoute(cost, path, bestCost, best))
                        continue;

                    bestCost = cost;
                    best = path;
                    bestEnd = endIdx;
                    bestStart = startIdx;
                }
            }

            if (best == null || best.Count == 0)
                return false;

            bestPath = best;
            bestEndIdx = bestEnd;
            CommitRouteLock(bestStart, bestEnd, best);
            return true;
        }

        private static bool TryReuseLockedRoute(
            TrafficWaypointGraph graph,
            Vector3 origin,
            Vector3 forward,
            out List<int> path,
            out int endIdx)
        {
            path = new List<int>();
            endIdx = -1;

            if (_lockedPathIndices == null || _lockedPathIndices.Count < 2)
                return false;
            if (_lockedStartWaypoint < 0 || _lockedEndWaypoint < 0)
                return false;

            var progressIdx = FindBestProgressIndex(graph, _lockedPathIndices, origin, forward);
            if (progressIdx < 0)
                return false;

            if (!IsNearLockedPath(graph, _lockedPathIndices, progressIdx, origin))
                return false;

            var distSq = PathGeometry.HorizontalDistSqToSegment(
                origin,
                graph.GetPosition(_lockedPathIndices[progressIdx]),
                graph.GetPosition(_lockedPathIndices[Mathf.Min(progressIdx + 1, _lockedPathIndices.Count - 1)]),
                out _);
            if (distSq > RouteReuseMaxDistMeters * RouteReuseMaxDistMeters)
                return false;

            var sliceStart = progressIdx;
            if (sliceStart > 0 &&
                graph.TryGetEnhancedTurnControl(_lockedPathIndices[sliceStart - 1], _lockedPathIndices[sliceStart], out _))
                sliceStart--;

            var remaining = _lockedPathIndices.Count - sliceStart;
            if (remaining < 2)
                return false;

            path = _lockedPathIndices.GetRange(sliceStart, remaining);
            endIdx = _lockedEndWaypoint;
            return true;
        }

        private static int CollectStrictAlignedStarts(
            TrafficWaypointGraph graph,
            Vector3 origin,
            Vector3 forward,
            int[] buffer)
        {
            var count = graph.CollectNearestAligned(origin, forward, StrictStartAlignedSearchRadius, buffer);
            if (count <= 0)
                return 0;

            count = graph.FilterFlowAligned(buffer, count, forward);
            if (count <= 1)
                return count;

            return graph.PrioritizeFlowAligned(buffer, count, forward);
        }

        private static int FindBestProgressIndex(
            TrafficWaypointGraph graph,
            List<int> indices,
            Vector3 origin,
            Vector3 forward)
        {
            if (indices == null || indices.Count == 0)
                return -1;

            if (indices.Count == 1)
                return 0;

            var positions = new Vector3[indices.Count];
            for (var i = 0; i < indices.Count; i++)
                positions[i] = graph.GetPosition(indices[i]);

            var seg = Mathf.Clamp(_lockedPathProgressIndex, 0, positions.Length - 2);
            PathGeometry.FindProgressSegmentIndex(
                positions,
                origin,
                forward,
                ref seg,
                maxSegmentJump: 1,
                lookAheadGateMeters: 22f);

            PathGeometry.HorizontalDistSqToSegment(origin, positions[seg], positions[seg + 1], out var t);
            var waypointIdx = t > 0.62f ? seg + 1 : seg;
            waypointIdx = Mathf.Clamp(waypointIdx, 0, indices.Count - 1);

            var prev = _lockedPathProgressIndex;
            waypointIdx = Mathf.Clamp(waypointIdx, prev - 1, prev + 2);
            _lockedPathProgressIndex = waypointIdx;
            return waypointIdx;
        }

        private static bool IsNearLockedPath(
            TrafficWaypointGraph graph,
            List<int> indices,
            int progressIdx,
            Vector3 origin)
        {
            if (progressIdx < 0 || progressIdx >= indices.Count)
                return false;

            if (progressIdx < indices.Count - 1)
            {
                var distSq = PathGeometry.HorizontalDistSqToSegment(
                    origin,
                    graph.GetPosition(indices[progressIdx]),
                    graph.GetPosition(indices[progressIdx + 1]),
                    out _);
                if (distSq <= RouteFollowMaxDistMeters * RouteFollowMaxDistMeters)
                    return true;
            }

            var toWaypoint = graph.GetPosition(indices[progressIdx]) - origin;
            toWaypoint.y = 0f;
            return toWaypoint.sqrMagnitude <= RouteFollowMaxDistMeters * RouteFollowMaxDistMeters;
        }

        private static bool IsLockedEndStillValid(
            TrafficWaypointGraph graph,
            Vector3 destination,
            int endIdx)
        {
            if (endIdx < 0)
                return false;

            if (!graph.TryFindNearest(destination, 90f, out var nearest, out _))
                return nearest == endIdx;

            if (nearest == endIdx)
                return true;

            var a = graph.GetPosition(nearest);
            var b = graph.GetPosition(endIdx);
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return dx * dx + dz * dz <= 30f * 30f;
        }

        private static bool ShouldPreferRoute(
            float cost,
            List<int> candidate,
            float bestCost,
            List<int> best)
        {
            if (best == null)
                return true;

            if (cost < bestCost - CostTieEpsilon)
                return true;

            if (Mathf.Abs(cost - bestCost) > CostTieEpsilon)
                return false;

            if (candidate.Count != best.Count)
                return candidate.Count < best.Count;

            return IsDeterministicallyPreferable(candidate, best);
        }

        private static bool IsDeterministicallyPreferable(List<int> candidate, List<int> currentBest)
        {
            if (candidate.Count != currentBest.Count)
                return candidate.Count < currentBest.Count;

            for (var i = 0; i < candidate.Count; i++)
            {
                if (candidate[i] == currentBest[i])
                    continue;
                return candidate[i] < currentBest[i];
            }

            return false;
        }

        private static void CommitRouteLock(int startIdx, int endIdx, List<int> path)
        {
            if (startIdx < 0 || endIdx < 0 || path == null || path.Count == 0)
                return;

            _lockedStartWaypoint = startIdx;
            _lockedEndWaypoint = endIdx;
            _lockedPathIndices = new List<int>(path);
            _lockedPathProgressIndex = 0;
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
            cost += graph.EstimatePathTravelCost(path);
            cost += EstimateArrivalCost(graph, endIdx, destination);
            return cost;
        }

        private static float EstimateArrivalCost(TrafficWaypointGraph graph, int endIdx, Vector3 destination) =>
            VehicleArrivalResolver.EstimateArrivalLegCost(graph, endIdx, destination);

        /// <summary>
        /// Garde les candidats d'arrivée les plus proches en ligne droite (même voie),
        /// sans favoriser la voie opposée — celle-ci est évaluée seulement sur le coût total.
        /// </summary>
        private static int TrimEndCandidates(
            TrafficWaypointGraph graph,
            int[] buffer,
            int count,
            Vector3 destination,
            int maxCount)
        {
            if (count <= maxCount)
                return count;

            var limit = Mathf.Min(count, maxCount, buffer.Length);
            for (var i = 0; i < limit; i++)
            {
                var best = i;
                var bestDist = VehiclePathArrival.FlatDistance(graph.GetPosition(buffer[i]), destination);
                for (var j = i + 1; j < count; j++)
                {
                    var dist = VehiclePathArrival.FlatDistance(graph.GetPosition(buffer[j]), destination);
                    if (dist >= bestDist)
                        continue;

                    bestDist = dist;
                    best = j;
                }

                if (best == i)
                    continue;

                var swap = buffer[i];
                buffer[i] = buffer[best];
                buffer[best] = swap;
            }

            return limit;
        }

        private static bool TryBuildPath(TrafficWaypointGraph graph, int startIdx, int endIdx, out List<int> path)
        {
            if (startIdx == endIdx)
            {
                path = new List<int> { startIdx };
                return true;
            }

            return TryAStar(graph, startIdx, endIdx, out path);
        }

        private static bool TryAStar(TrafficWaypointGraph graph, int start, int goal, out List<int> path)
        {
            path = new List<int>();

            var open = new List<int> { start };
            var openSet = new HashSet<int> { start };
            var cameFrom = new Dictionary<int, int>();
            var gScore = new Dictionary<int, float> { [start] = 0f };
            var fScore = new Dictionary<int, float> { [start] = Heuristic(graph, start, goal) };
            var closed = new HashSet<int>();

            var explored = 0;
            while (open.Count > 0)
            {
                if (++explored > MaxAStarNodes)
                    return false;

                var current = PopLowestF(open, openSet, fScore);
                if (current == goal)
                {
                    Reconstruct(cameFrom, current, path);
                    return path.Count >= 1;
                }

                closed.Add(current);
                var neighbors = graph.GetNeighbors(current);
                var gCurrent = gScore.TryGetValue(current, out var gc) ? gc : float.MaxValue;

                cameFrom.TryGetValue(current, out var incoming);
                RelaxEdges(graph, goal, current, incoming, gCurrent, neighbors, cameFrom, gScore, fScore, open, openSet, closed);
            }

            return false;
        }

        private static int PopLowestF(List<int> open, HashSet<int> openSet, Dictionary<int, float> fScore)
        {
            var best = 0;
            var bestF = float.MaxValue;
            for (var i = 0; i < open.Count; i++)
            {
                var idx = open[i];
                var f = fScore.TryGetValue(idx, out var fv) ? fv : float.MaxValue;
                var tie = Mathf.Abs(f - bestF) <= 0.001f;
                if (f < bestF - 0.001f || (tie && idx < open[best]))
                {
                    bestF = f;
                    best = i;
                }
            }

            var node = open[best];
            open.RemoveAt(best);
            openSet.Remove(node);
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

        private static void RelaxEdges(
            TrafficWaypointGraph graph,
            int goal,
            int current,
            int incoming,
            float gCurrent,
            ReadOnlySpan<int> targets,
            Dictionary<int, int> cameFrom,
            Dictionary<int, float> gScore,
            Dictionary<int, float> fScore,
            List<int> open,
            HashSet<int> openSet,
            HashSet<int> closed)
        {
            for (var i = 0; i < targets.Length; i++)
            {
                var next = targets[i];
                if (closed.Contains(next))
                    continue;
                if (!graph.IsForwardEdgeAllowed(incoming, current, next))
                    continue;

                var step = graph.GetForwardTravelCost(current, next, incoming);
                var tentative = gCurrent + step;
                if (gScore.TryGetValue(next, out var existing) && tentative >= existing)
                    continue;

                cameFrom[next] = current;
                gScore[next] = tentative;
                fScore[next] = tentative + Heuristic(graph, next, goal);

                if (openSet.Add(next))
                    open.Add(next);
            }
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
            int endWaypointIdx,
            Vector3 origin,
            Vector3 destination)
        {
            var points = new List<Vector3>(indices.Count + 3) { origin };

            for (var i = 0; i < indices.Count; i++)
            {
                var current = graph.GetPosition(indices[i]);
                if (i > 0 && graph.TryGetEnhancedTurnControl(indices[i - 1], indices[i], out var control))
                {
                    AppendQuadraticTurn(
                        points,
                        graph.GetPosition(indices[i - 1]),
                        control,
                        current,
                        EnhancedTurnSegments);
                    continue;
                }

                points.Add(current);
            }

            var arrival = VehicleArrivalResolver.Resolve(graph, endWaypointIdx, destination);
            if ((arrival.LanePoint - points[points.Count - 1]).sqrMagnitude >= 1f)
                points.Add(arrival.LanePoint);

            if ((arrival.FinalTarget - points[points.Count - 1]).sqrMagnitude >= 1f)
                points.Add(arrival.FinalTarget);

            return Deduplicate(points);
        }

        private static void AppendQuadraticTurn(List<Vector3> points, Vector3 from, Vector3 control, Vector3 to, int segments)
        {
            if (segments < 2)
                segments = 2;

            for (var s = 1; s <= segments; s++)
            {
                var t = s / (float)segments;
                var q = Vector3.Lerp(
                    Vector3.Lerp(from, control, t),
                    Vector3.Lerp(control, to, t),
                    t);
                points.Add(q);
            }
        }

        private static Vector3[] Deduplicate(List<Vector3> points)
        {
            if (points.Count == 0)
                return System.Array.Empty<Vector3>();

            var result = new List<Vector3>(points.Count);
            const float minSq = 2.25f;

            foreach (var p in points)
            {
                if (result.Count == 0 || (p - result[result.Count - 1]).sqrMagnitude >= minSq)
                    result.Add(p);
            }

            if (result.Count < 2 && points.Count >= 2)
                return new[] { points[0], points[points.Count - 1] };

            return result.ToArray();
        }
    }
}
