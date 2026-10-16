using System;
using System.Collections.Generic;
using UnityEngine;
using VoogleRoute.Pathfinding.Geometry;
using VoogleRoute.Pathfinding.Graph;

namespace VoogleRoute.Navigation.Driving
{
    /// <summary>Excludes U-turns and synthetic edges tighter than this vehicle can steer.</summary>
    internal sealed class PhysicalRoutingGraph : IRoutingGraph
    {
        private readonly IRoutingGraph _graph;
        private readonly float _radius;
        private readonly Dictionary<int, int[]> _forward = new Dictionary<int, int[]>();
        private readonly Dictionary<int, int[]> _lanes = new Dictionary<int, int[]>();
        internal PhysicalRoutingGraph(IRoutingGraph graph, float radius) { _graph = graph; _radius = radius; }
        private bool Allowed(int a, int b)
        {
            if (_graph.IsAuthorizedUturnEdge(a, b)) return false;
            if (_graph.TryGetSyntheticTurnAbsAngle(a, b, out var angle) && angle > 125f) return false;
            var start = GetPosition(a);
            var end = GetPosition(b);
            // This first driver supports city streets, not bridge/underground transitions.
            if (start.Y > 6f || end.Y > 6f || start.Y < -3f || end.Y < -3f) return false;
            if (Mathf.Abs(start.Y - end.Y) > Vec3.FlatLength(start, end) * 0.25f + 0.5f) return false;
            if (!_graph.TryGetSyntheticTurnControl(a, b, out var control)) return true;
            var p = Unity(start); var c = Unity(control); var q = Unity(end);
            for (var i = 0; i <= 16; i++)
            {
                var t = i / 16f;
                var d = DrivingPath.Flat(2f * ((1f - t) * (c - p) + t * (q - c)));
                var dd = DrivingPath.Flat(2f * (q - 2f * c + p));
                var curvature = Mathf.Abs(Vector3.Cross(d, dd).y) / Mathf.Max(0.001f, d.magnitude * d.sqrMagnitude);
                if (curvature > 1f / Mathf.Max(1f, _radius)) return false;
            }
            return true;
        }
        private ReadOnlySpan<int> Neighbors(int index, bool lane)
        {
            var cache = lane ? _lanes : _forward;
            if (!cache.TryGetValue(index, out var result))
            {
                var source = lane ? _graph.GetLaneChangeNeighbors(index) : _graph.GetForwardNeighbors(index);
                var list = new List<int>(source.Length);
                foreach (var next in source) if (Allowed(index, next)) list.Add(next);
                result = list.ToArray(); cache[index] = result;
            }
            return result;
        }
        internal static Vector3 Unity(Vec3 p) => new Vector3(p.X, p.Y, p.Z);
        internal static Vec3 Native(Vector3 p) => new Vec3(p.x, p.y, p.z);
        public Vec3 GetPosition(int i) => _graph.GetPosition(i);
        public ReadOnlySpan<int> GetForwardNeighbors(int i) => Neighbors(i, false);
        public ReadOnlySpan<int> GetLaneChangeNeighbors(int i) => Neighbors(i, true);
        public bool IsLaneChangeEdge(int a, int b) => _graph.IsLaneChangeEdge(a, b);
        public bool IsSyntheticTurnEdge(int a, int b) => _graph.IsSyntheticTurnEdge(a, b);
        public bool TryGetSyntheticTurnControl(int a, int b, out Vec3 c) => _graph.TryGetSyntheticTurnControl(a, b, out c);
        public bool TryGetSyntheticTurnAbsAngle(int a, int b, out float d) => _graph.TryGetSyntheticTurnAbsAngle(a, b, out d);
        public bool IsForwardEdgeAllowed(int incoming, int at, int next)
        {
            if (!_graph.IsForwardEdgeAllowed(incoming, at, next) || !Allowed(at, next)) return false;
            if (incoming < 0) return true;
            var a = DrivingPath.Flat(Unity(GetPosition(at) - GetPosition(incoming)));
            var b = DrivingPath.Flat(Unity(GetPosition(next) - GetPosition(at)));
            return Vector3.Angle(a, b) < 125f;
        }
        public float GetForwardTravelCost(int a, int b, int c) => _graph.GetForwardTravelCost(a, b, c);
        public float EstimatePathTravelCost(IReadOnlyList<int> p) => _graph.EstimatePathTravelCost(p);
        public bool IsAuthorizedUturnEdge(int a, int b) => false;
        public bool IsIntersectionNode(int i) => _graph.IsIntersectionNode(i);
        public int CollectNearest(Vec3 p, float d, int[] b) => _graph.CollectNearest(p, d, b);
        public int CollectNearestAligned(Vec3 p, Vec3 f, float d, int[] b) => _graph.CollectNearestAligned(p, f, d, b);
        public int FilterFlowAligned(int[] b, int c, Vec3 f) => _graph.FilterFlowAligned(b, c, f);
        public int ExpandLaneCandidates(int[] b, int c, int n, Vec3 f) => _graph.ExpandLaneCandidates(b, c, n, f);
        public bool TryFindNearest(Vec3 p, float d, out int i) => _graph.TryFindNearest(p, d, out i);
        public float EstimateArrivalLegCost(int i, Vec3 p) => _graph.EstimateArrivalLegCost(i, p);
        public float FlatDistance(Vec3 a, Vec3 b) => _graph.FlatDistance(a, b);
        public float DistanceToDestination(Vec3 a, Vec3 b) => _graph.DistanceToDestination(a, b);
    }
}
