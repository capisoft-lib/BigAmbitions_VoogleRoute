using System.Collections.Generic;
using GleyTrafficSystem;
using UnityEngine;
using VoogleRoute.Pathfinding.Geometry;
using VoogleRoute.Pathfinding.Routing;

namespace VoogleRoute.Navigation
{
    /// <summary>
    /// Graphe routier prétraité : coûts de segment (longueur / arc CSV, facteur vitesse).
    /// Les coûts sont en mètres effectifs pour l'A*.
    /// </summary>
    internal sealed class TrafficRoutingIndex
    {
        private const float DefaultSpeedKmh = 45f;
        private const float ReferenceSpeedKmh = 50f;
        private const float LaneChangeBaseMeters = 22f;

        private readonly Vector3[] _positions;
        private readonly int[][] _forwardNeighbors;
        private readonly int[][] _laneChangeNeighbors;
        private readonly float[][] _forwardCosts;
        private readonly float[][] _laneChangeCosts;
        private readonly Dictionary<long, float> _edgeLengths;

        private TrafficRoutingIndex(
            Vector3[] positions,
            int[][] forwardNeighbors,
            int[][] laneChangeNeighbors,
            float[][] forwardCosts,
            float[][] laneChangeCosts,
            Dictionary<long, float> edgeLengths)
        {
            _positions = positions;
            _forwardNeighbors = forwardNeighbors;
            _laneChangeNeighbors = laneChangeNeighbors;
            _forwardCosts = forwardCosts;
            _laneChangeCosts = laneChangeCosts;
            _edgeLengths = edgeLengths ?? new Dictionary<long, float>();
        }

        internal static TrafficRoutingIndex Build(
            int size,
            Vector3[] positions,
            int[][] forwardEdges,
            int[][] laneChangeEdges,
            Waypoint[] waypoints,
            Dictionary<long, Vector3> turnControls = null,
            Dictionary<long, float> edgeLengths = null)
        {
            var maxSpeed = BuildMaxSpeed(size, waypoints);
            var forwardCosts = BuildEdgeCosts(
                size, positions, forwardEdges, maxSpeed, isLaneChange: false, turnControls, edgeLengths);
            var laneChangeCosts = BuildEdgeCosts(
                size, positions, laneChangeEdges, maxSpeed, isLaneChange: true, null, null);

            return new TrafficRoutingIndex(
                positions,
                forwardEdges,
                laneChangeEdges,
                forwardCosts,
                laneChangeCosts,
                edgeLengths);
        }

        internal float GetForwardTravelCost(int from, int to, int incomingFrom)
        {
            var cost = LookupCost(_forwardNeighbors, _forwardCosts, from, to);
            if (cost < 0f)
                cost = LookupCost(_laneChangeNeighbors, _laneChangeCosts, from, to);
            if (cost < 0f)
                cost = FlatLength(_positions[from], _positions[to]);

            return cost;
        }

        internal bool IsLaneChange(int from, int to) =>
            ContainsEdge(_laneChangeNeighbors, from, to);

        private static bool ContainsEdge(int[][] neighbors, int from, int to)
        {
            if (from < 0 || from >= neighbors.Length)
                return false;

            var next = neighbors[from];
            if (next == null)
                return false;

            for (var i = 0; i < next.Length; i++)
            {
                if (next[i] == to)
                    return true;
            }

            return false;
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

        private static float[][] BuildEdgeCosts(
            int size,
            Vector3[] positions,
            int[][] neighbors,
            float[] maxSpeed,
            bool isLaneChange,
            Dictionary<long, Vector3> turnControls,
            Dictionary<long, float> edgeLengths)
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
                        : ComputeSegmentCost(positions, from, to, maxSpeed, turnControls, edgeLengths);
                }

                costs[from] = row;
            }

            return costs;
        }

        private static float ComputeSegmentCost(
            Vector3[] positions,
            int from,
            int to,
            float[] maxSpeed,
            Dictionary<long, Vector3> turnControls,
            Dictionary<long, float> edgeLengths)
        {
            var key = EdgeKey(from, to);
            float length;
            if (edgeLengths != null && edgeLengths.TryGetValue(key, out var csvLength) && csvLength > 0f)
            {
                length = csvLength;
            }
            else if (turnControls != null && turnControls.TryGetValue(key, out var control))
            {
                var fromV = new Vec3(positions[from].x, positions[from].y, positions[from].z);
                var toV = new Vec3(positions[to].x, positions[to].y, positions[to].z);
                var ctrlV = new Vec3(control.x, control.y, control.z);
                length = ManeuverGeometry.SyntheticTurnTravelMeters(fromV, toV, ctrlV);
            }
            else
            {
                length = FlatLength(positions[from], positions[to]);
            }

            var speed = from >= 0 && from < maxSpeed.Length
                ? Mathf.Max(maxSpeed[from], 12f)
                : DefaultSpeedKmh;
            return length * (ReferenceSpeedKmh / speed);
        }

        private static long EdgeKey(int from, int to) => ((long)from << 32) ^ (uint)to;

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
                return -1f;

            var next = neighbors[from];
            var row = costs[from];
            if (next == null || row == null)
                return -1f;

            for (var i = 0; i < next.Length; i++)
            {
                if (next[i] == to)
                    return row[i];
            }

            return -1f;
        }

        private static float FlatLength(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
