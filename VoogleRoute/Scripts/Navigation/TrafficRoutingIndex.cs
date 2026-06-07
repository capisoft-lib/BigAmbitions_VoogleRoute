using System.Collections.Generic;
using GleyTrafficSystem;
using UnityEngine;

namespace VoogleRoute.Navigation
{
    /// <summary>
    /// Graphe routier prétraité : segments (longueur + vitesse), intersections, pénalités de virage.
    /// Les coûts sont en mètres effectifs pour l'A*.
    /// </summary>
    internal sealed class TrafficRoutingIndex
    {
        private const float DefaultSpeedKmh = 45f;
        private const float ReferenceSpeedKmh = 50f;
        private const float LaneChangeBaseMeters = 22f;

        private const float JunctionLaneChangePenaltyMeters = 14f;

        private readonly Vector3[] _positions;
        private readonly int[][] _forwardNeighbors;
        private readonly int[][] _laneChangeNeighbors;
        private readonly float[][] _forwardCosts;
        private readonly float[][] _laneChangeCosts;
        private readonly bool[] _intersectionNode;

        private TrafficRoutingIndex(
            Vector3[] positions,
            int[][] forwardNeighbors,
            int[][] laneChangeNeighbors,
            float[][] forwardCosts,
            float[][] laneChangeCosts,
            bool[] intersectionNode)
        {
            _positions = positions;
            _forwardNeighbors = forwardNeighbors;
            _laneChangeNeighbors = laneChangeNeighbors;
            _forwardCosts = forwardCosts;
            _laneChangeCosts = laneChangeCosts;
            _intersectionNode = intersectionNode;
        }

        internal static TrafficRoutingIndex Build(
            int size,
            Vector3[] positions,
            int[][] forwardEdges,
            int[][] laneChangeEdges,
            int[][] reverseEdges,
            Waypoint[] waypoints,
            bool[] junctionZone)
        {
            var maxSpeed = BuildMaxSpeed(size, waypoints);
            var intersectionNode = BuildIntersectionNodes(size, forwardEdges, reverseEdges, junctionZone);
            var forwardCosts = BuildEdgeCosts(size, positions, forwardEdges, maxSpeed, isLaneChange: false);
            var laneChangeCosts = BuildEdgeCosts(size, positions, laneChangeEdges, maxSpeed, isLaneChange: true);

            return new TrafficRoutingIndex(
                positions,
                forwardEdges,
                laneChangeEdges,
                forwardCosts,
                laneChangeCosts,
                intersectionNode);
        }

        internal float GetForwardTravelCost(int from, int to, int incomingFrom)
        {
            var cost = LookupCost(_forwardNeighbors, _forwardCosts, from, to);
            if (incomingFrom >= 0)
                cost += GetTurnPenalty(incomingFrom, from, to);
            return cost;
        }

        internal float EstimatePathCost(IReadOnlyList<int> path)
        {
            if (path == null || path.Count < 2)
                return 0f;

            var cost = 0f;
            var incoming = -1;
            for (var i = 0; i < path.Count - 1; i++)
            {
                cost += GetForwardTravelCost(path[i], path[i + 1], incoming);
                incoming = path[i];
            }

            return cost;
        }

        private static float[] BuildMaxSpeed(int size, Waypoint[] waypoints)
        {
            var maxSpeed = new float[size];
            for (var i = 0; i < size; i++)
                maxSpeed[i] = DefaultSpeedKmh;

            for (var i = 0; i < waypoints.Length; i++)
            {
                var wp = waypoints[i];
                if (wp == null || wp.temporaryDisabled)
                    continue;

                var idx = wp.listIndex;
                if (idx < 0 || idx >= size)
                    continue;

                maxSpeed[idx] = wp.maxSpeed > 5 ? wp.maxSpeed : DefaultSpeedKmh;
            }

            return maxSpeed;
        }

        private static bool[] BuildIntersectionNodes(
            int size,
            int[][] forwardEdges,
            int[][] reverseEdges,
            bool[] junctionZone)
        {
            var nodes = new bool[size];
            for (var i = 0; i < size; i++)
            {
                if (junctionZone != null && junctionZone[i])
                {
                    nodes[i] = true;
                    continue;
                }

                var fwd = forwardEdges[i];
                var rev = reverseEdges[i];
                if (fwd != null && fwd.Length >= 2)
                {
                    nodes[i] = true;
                    continue;
                }

                if (rev != null && rev.Length >= 2)
                    nodes[i] = true;
            }

            return nodes;
        }

        private static float[][] BuildEdgeCosts(
            int size,
            Vector3[] positions,
            int[][] neighbors,
            float[] maxSpeed,
            bool isLaneChange)
        {
            var costs = new float[size][];
            for (var from = 0; from < size; from++)
            {
                var next = neighbors[from];
                if (next == null || next.Length == 0)
                {
                    costs[from] = System.Array.Empty<float>();
                    continue;
                }

                var row = new float[next.Length];
                for (var i = 0; i < next.Length; i++)
                {
                    var to = next[i];
                    row[i] = isLaneChange
                        ? ComputeLaneChangeCost(positions, from, to, maxSpeed)
                        : ComputeSegmentCost(positions, from, to, maxSpeed);
                }

                costs[from] = row;
            }

            return costs;
        }

        private static float ComputeSegmentCost(Vector3[] positions, int from, int to, float[] maxSpeed)
        {
            var length = FlatLength(positions[from], positions[to]);
            var speed = from >= 0 && from < maxSpeed.Length
                ? Mathf.Max(maxSpeed[from], 12f)
                : DefaultSpeedKmh;
            return length * (ReferenceSpeedKmh / speed);
        }

        private static float ComputeLaneChangeCost(Vector3[] positions, int from, int to, float[] maxSpeed)
        {
            var lateral = FlatLength(positions[from], positions[to]);
            var speed = from >= 0 && from < maxSpeed.Length
                ? Mathf.Max(maxSpeed[from], 12f)
                : DefaultSpeedKmh;
            var timeCost = Mathf.Max(lateral, 4f) * (ReferenceSpeedKmh / speed);
            return timeCost + LaneChangeBaseMeters;
        }

        private float LookupCost(int[][] neighbors, float[][] costs, int from, int to)
        {
            if (from < 0 || from >= neighbors.Length)
                return FlatLength(_positions[from], _positions[to]);

            var next = neighbors[from];
            var row = costs[from];
            if (next == null || row == null)
                return FlatLength(_positions[from], _positions[to]);

            for (var i = 0; i < next.Length; i++)
            {
                if (next[i] == to)
                    return row[i];
            }

            return FlatLength(_positions[from], _positions[to]);
        }

        private float GetTurnPenalty(int incoming, int at, int to)
        {
            if (!_intersectionNode[at])
                return 0f;

            var signed = SignedTurnDegrees(incoming, at, to);
            var abs = Mathf.Abs(signed);

            if (abs < 22f)
                return 0f;
            if (abs < 50f)
                return 5f;
            if (abs < 100f)
                return 13f;
            if (abs < 150f)
                return 30f;

            return 55f;
        }

        private float SignedTurnDegrees(int incoming, int at, int to)
        {
            if (incoming < 0 || at < 0 || to < 0 ||
                incoming >= _positions.Length || at >= _positions.Length || to >= _positions.Length)
                return 0f;

            var inDir = FlatDir(_positions[incoming], _positions[at]);
            var outDir = FlatDir(_positions[at], _positions[to]);
            if (inDir.sqrMagnitude < 0.01f || outDir.sqrMagnitude < 0.01f)
                return 0f;

            return Vector3.SignedAngle(inDir, outDir, Vector3.up);
        }

        private static Vector3 FlatDir(Vector3 from, Vector3 to)
        {
            var d = to - from;
            d.y = 0f;
            return d.sqrMagnitude > 0.01f ? d.normalized : Vector3.zero;
        }

        private static float FlatLength(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
