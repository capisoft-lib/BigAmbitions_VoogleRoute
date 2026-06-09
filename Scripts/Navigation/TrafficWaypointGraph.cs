using System;

using System.Collections.Generic;

using GleyTrafficSystem;

using GleyUrbanAssets;

using UnityEngine;



namespace VoogleRoute.Navigation

{

    internal sealed class TrafficWaypointGraph

    {

        private const float JunctionZoneRadiusMeters = 32f;

        private const float JunctionClusterRadiusMeters = 34f;

        private const float ParallelLaneMaxLateralMeters = 18f;

        private const float OppositeBearingDegrees = 105f;

        private const int ApproachHops = 5;



        private static TrafficWaypointGraph _instance;



        private Waypoint[] _waypoints;

        private int[][] _edges;

        private int[][] _reverseEdges;

        private int[][] _laneChangeEdges;

        private int[][] _otherLanes;

        private Vector3[] _positions;

        private int[] _junctionGroupByIndex;

        private List<int>[] _junctionGroups;

        private TrafficRoutingIndex _routingIndex;

        private bool[] _junctionZone;

        private CurrentSceneData _sceneData;

        private Dictionary<long, Vector3> _enhancedTurnControls;

        private Dictionary<long, float> _enhancedEdgeLengths;

        private Dictionary<long, float> _enhancedTurnAngles;

        private HashSet<long> _authorizedUturnEdges;



        internal static TrafficWaypointGraph Instance => _instance ?? (_instance = new TrafficWaypointGraph());



        internal bool IsReady => _waypoints != null && _waypoints.Length > 0;



        internal static void InvalidateCache() => _instance = null;



        internal bool TryEnsureLoaded()

        {

            try

            {

                var scene = CurrentSceneData.GetSceneInstance();

                if (scene == null)

                    return false;



                if (IsReady && ReferenceEquals(scene, _sceneData))

                    return true;



                var array = scene.allWaypoints;

                if (array == null || array.Length == 0)

                    return false;



                Build(scene, array);

                return IsReady;

            }

            catch

            {

                return false;

            }

        }



        internal Vector3 GetPosition(int listIndex)

        {

            if (listIndex < 0 || listIndex >= _positions.Length)

                return default;

            return _positions[listIndex];

        }



        internal ReadOnlySpan<int> GetNeighbors(int listIndex)

        {

            if (listIndex < 0 || listIndex >= _edges.Length)

                return ReadOnlySpan<int>.Empty;

            return _edges[listIndex];

        }



        internal ReadOnlySpan<int> GetOtherLanes(int listIndex)

        {

            if (_otherLanes == null || listIndex < 0 || listIndex >= _otherLanes.Length)

                return ReadOnlySpan<int>.Empty;

            return _otherLanes[listIndex];

        }



        internal ReadOnlySpan<int> GetLaneChangeTargets(int listIndex)

        {

            if (_laneChangeEdges == null || listIndex < 0 || listIndex >= _laneChangeEdges.Length)

                return ReadOnlySpan<int>.Empty;

            return _laneChangeEdges[listIndex];

        }

        internal bool IsInJunctionZone(int listIndex) =>
            _junctionZone != null && listIndex >= 0 && listIndex < _junctionZone.Length && _junctionZone[listIndex];

        internal bool HasForwardEdge(int from, int to)
        {
            if (from < 0 || to < 0 || from >= _edges.Length)
                return false;

            var edges = _edges[from];
            if (edges == null)
                return false;

            for (var i = 0; i < edges.Length; i++)
            {
                if (edges[i] == to)
                    return true;
            }

            return false;
        }

        internal bool TryGetEnhancedTurnControl(int from, int to, out Vector3 control)
        {
            control = default;
            return _enhancedTurnControls != null &&
                   _enhancedTurnControls.TryGetValue(EdgeKey(from, to), out control);
        }

        internal bool TryGetEnhancedTurnAbsAngle(int from, int to, out float absDegrees)
        {
            absDegrees = 0f;
            return _enhancedTurnAngles != null &&
                   _enhancedTurnAngles.TryGetValue(EdgeKey(from, to), out absDegrees);
        }

        internal bool IsAuthorizedUturnEdge(int from, int to) =>
            _authorizedUturnEdges != null && _authorizedUturnEdges.Contains(EdgeKey(from, to));

        /// <summary>
        /// Forward edges only. ~180° turns are allowed solely on CSV synthetic uturn connectors.
        /// </summary>
        internal bool IsForwardEdgeAllowed(int incoming, int at, int next)
        {
            if (!HasForwardEdge(at, next))
                return false;
            if (incoming < 0)
                return true;

            var absTurn = Mathf.Abs(SignedTurnDegrees(incoming, at, next));
            if (absTurn < 150f)
                return true;

            return IsAuthorizedUturnEdge(at, next);
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

        internal bool IsLaneChangeEdge(int from, int to)
        {
            if (_laneChangeEdges == null || from < 0 || from >= _laneChangeEdges.Length)
                return false;

            var edges = _laneChangeEdges[from];
            if (edges == null)
                return false;

            for (var i = 0; i < edges.Length; i++)
            {
                if (edges[i] == to)
                    return true;
            }

            return false;
        }

        internal bool ShouldCollapseJunctionWaypoint(int prev, int mid, int next)
        {
            if (!IsInJunctionZone(mid))
                return false;

            if (prev < 0 || next < 0 || prev >= _positions.Length || mid >= _positions.Length || next >= _positions.Length)
                return false;

            var p0 = _positions[prev];
            var p1 = _positions[mid];
            var p2 = _positions[next];
            var leg1 = FlatLength(p0, p1);
            var leg2 = FlatLength(p1, p2);

            if ((IsLaneChangeEdge(prev, mid) || IsLaneChangeEdge(mid, next)) && leg1 < 24f && leg2 < 24f)
                return true;

            if (HasForwardEdge(prev, next) && leg1 < 26f && leg2 < 26f)
                return true;

            if (leg1 < 14f && leg2 < 14f)
            {
                var inDir = FlatDir(p0, p1);
                var outDir = FlatDir(p1, p2);
                if (inDir.sqrMagnitude > 0.01f && outDir.sqrMagnitude > 0.01f &&
                    Vector3.Angle(inDir, outDir) > 95f)
                    return true;
            }

            return false;
        }

        private static float FlatLength(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }



        internal int ExpandLaneCandidates(int[] buffer, int count, int capacity, Vector3 flatForward = default)

        {

            if (count <= 0 || capacity <= count)

                return count;

            var filterFlow = flatForward.sqrMagnitude > 0.01f;
            if (filterFlow)
            {
                flatForward.y = 0f;
                flatForward.Normalize();
            }



            var seen = new HashSet<int>(count + 8);

            for (var i = 0; i < count; i++)

                seen.Add(buffer[i]);



            var write = count;

            for (var i = 0; i < count && write < capacity; i++)

            {

                var idx = buffer[i];

                write = AddUniqueCandidates(idx, GetLaneChangeTargets(idx), buffer, write, capacity, seen, filterFlow, flatForward);

                write = AddUniqueCandidates(idx, GetOtherLanes(idx), buffer, write, capacity, seen, filterFlow, flatForward);

            }



            return write;

        }

        internal int FilterFlowAligned(int[] buffer, int count, Vector3 flatForward)
        {
            var kept = 0;
            for (var i = 0; i < count; i++)
            {
                if (!IsFlowAlignedWithHeading(buffer[i], flatForward))
                    continue;
                buffer[kept++] = buffer[i];
            }

            return kept;
        }

        /// <summary>Met les waypoints alignés au cap en tête sans en exclure d'autres.</summary>
        internal int PrioritizeFlowAligned(int[] buffer, int count, Vector3 flatForward)
        {
            if (count <= 1 || flatForward.sqrMagnitude < 0.01f)
                return count;

            Array.Sort(buffer, 0, count, Comparer<int>.Create((a, b) =>
            {
                var alignedA = IsFlowAlignedWithHeading(a, flatForward) ? 0 : 1;
                var alignedB = IsFlowAlignedWithHeading(b, flatForward) ? 0 : 1;
                if (alignedA != alignedB)
                    return alignedA.CompareTo(alignedB);
                return a.CompareTo(b);
            }));

            return count;
        }

        internal bool IsFlowAlignedWithHeading(int listIndex, Vector3 flatForward)
        {
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude < 0.01f)
                return true;
            if (!TryGetForwardBearing(listIndex, out var travelBearing))
                return true;

            var heading = Mathf.Atan2(flatForward.x, flatForward.z) * Mathf.Rad2Deg;
            return Mathf.Abs(Mathf.DeltaAngle(travelBearing, heading)) < OppositeBearingDegrees;
        }



        private int AddUniqueCandidates(

            int fromIdx,

            ReadOnlySpan<int> candidates,

            int[] buffer,

            int write,

            int capacity,

            HashSet<int> seen,

            bool filterFlow,

            Vector3 flatForward)

        {

            for (var j = 0; j < candidates.Length && write < capacity; j++)

            {

                var lane = candidates[j];

                if (lane == fromIdx || !seen.Add(lane))

                    continue;

                if (filterFlow && !IsFlowAlignedWithHeading(lane, flatForward))
                    continue;

                buffer[write++] = lane;

            }



            return write;

        }



        internal int CollectNearestAligned(Vector3 worldPos, Vector3 flatForward, float maxDistance, int[] buffer)

        {

            var count = CollectNearest(worldPos, maxDistance, buffer);

            if (count <= 1 || flatForward.sqrMagnitude < 0.01f)

                return count;



            flatForward.y = 0f;

            if (flatForward.sqrMagnitude < 0.01f)

                return count;

            flatForward.Normalize();

            var flowCount = FilterFlowAligned(buffer, count, flatForward);
            if (flowCount > 0)
                count = flowCount;

            Array.Sort(buffer, 0, count, Comparer<int>.Create((a, b) =>

            {

                var sa = ScoreAligned(a, worldPos, flatForward);

                var sb = ScoreAligned(b, worldPos, flatForward);

                return sa.CompareTo(sb);

            }));



            return count;

        }



        private float ScoreAligned(int listIndex, Vector3 worldPos, Vector3 flatForward)

        {

            if (listIndex < 0 || listIndex >= _positions.Length)

                return float.MaxValue;



            var pos = _positions[listIndex];

            var to = pos - worldPos;

            to.y = 0f;

            var distSq = to.sqrMagnitude;

            var align = 0f;

            if (to.sqrMagnitude > 0.25f)

                align = Vector3.Dot(flatForward, to.normalized);

            if (!IsFlowAlignedWithHeading(listIndex, flatForward))
                return float.MaxValue;

            if (TryGetForwardBearing(listIndex, out var travelBearing))
            {
                var heading = Mathf.Atan2(flatForward.x, flatForward.z) * Mathf.Rad2Deg;
                var travelAlign = Mathf.Cos(Mathf.DeltaAngle(travelBearing, heading) * Mathf.Deg2Rad);
                align = Mathf.Max(align, travelAlign);
            }

            var score = distSq - align * 160f;
            score += ScoreDrivingSideLateral(to, flatForward);
            return score;

        }

        /// <summary>Route à droite (circulation US) : favorise la voie à droite du cap véhicule.</summary>
        private static float ScoreDrivingSideLateral(Vector3 toWaypoint, Vector3 flatForward)
        {
            var right = Vector3.Cross(Vector3.up, flatForward);
            if (right.sqrMagnitude < 0.01f)
                return 0f;

            right.Normalize();
            var lateral = Vector3.Dot(toWaypoint, right);

            if (lateral < -4f)
                return 2500f;

            return -Mathf.Clamp(lateral, -1.5f, 12f) * 40f;
        }

        internal bool IsDrivingLaneAnchor(int listIndex, Vector3 worldPos, Vector3 flatForward)
        {
            if (!IsFlowAlignedWithHeading(listIndex, flatForward))
                return false;

            var to = _positions[listIndex] - worldPos;
            to.y = 0f;
            if (to.sqrMagnitude > 100f * 100f)
                return false;

            var right = Vector3.Cross(Vector3.up, flatForward);
            if (right.sqrMagnitude < 0.01f)
                return true;

            right.Normalize();
            return Vector3.Dot(to, right) > -5f;
        }



        internal int CollectNearest(Vector3 worldPos, float maxDistance, int[] buffer)

        {

            if (!IsReady || buffer.Length == 0)

                return 0;



            var maxSq = maxDistance * maxDistance;

            var candidates = new List<(int idx, float sq)>(16);

            var seen = new HashSet<int>();



            void Consider(int idx)

            {

                if (idx < 0 || idx >= _positions.Length || _edges[idx].Length == 0)

                    return;

                if (!seen.Add(idx))

                    return;



                var sq = FlatDistSq(_positions[idx], worldPos);

                if (sq <= maxSq)

                    candidates.Add((idx, sq));



                var lanes = GetOtherLanes(idx);

                for (var l = 0; l < lanes.Length; l++)

                {

                    var lane = lanes[l];

                    if (lane < 0 || lane >= _positions.Length || _edges[lane].Length == 0)

                        continue;

                    if (!seen.Add(lane))

                        continue;



                    var laneSq = FlatDistSq(_positions[lane], worldPos);

                    if (laneSq <= maxSq)

                        candidates.Add((lane, laneSq));

                }

            }



            try

            {

                var cell = _sceneData?.GetCell(worldPos);

                if (cell?.waypointsInCell != null)

                {

                    var list = cell.waypointsInCell;

                    for (var i = 0; i < list.Count; i++)

                        Consider(list[i]);

                }

            }

            catch

            {

                // ignore

            }



            if (candidates.Count == 0 && TryFindNearest(worldPos, maxDistance, out var nearest, out _))

                candidates.Add((nearest, FlatDistSq(_positions[nearest], worldPos)));



            candidates.Sort((a, b) => a.sq.CompareTo(b.sq));



            var count = Math.Min(candidates.Count, buffer.Length);

            for (var i = 0; i < count; i++)

                buffer[i] = candidates[i].idx;

            return count;

        }



        internal bool TryFindNearest(Vector3 worldPos, float maxDistance, out int listIndex, out float distanceSq)

        {

            listIndex = -1;

            distanceSq = float.MaxValue;



            if (!IsReady || _sceneData == null)

                return false;



            var maxSq = maxDistance * maxDistance;



            if (TryFindNearestInCell(worldPos, maxSq, ref listIndex, ref distanceSq))

                return listIndex >= 0;



            return TryFindNearestBrute(worldPos, maxSq, ref listIndex, ref distanceSq);

        }



        private bool TryFindNearestInCell(Vector3 worldPos, float maxSq, ref int bestIndex, ref float bestSq)

        {

            try

            {

                var cell = _sceneData.GetCell(worldPos);

                if (cell?.waypointsInCell == null)

                    return false;



                var candidates = cell.waypointsInCell;

                for (var i = 0; i < candidates.Count; i++)

                    ConsiderWaypoint(candidates[i], worldPos, maxSq, ref bestIndex, ref bestSq);



                return bestIndex >= 0;

            }

            catch

            {

                return false;

            }

        }



        private bool TryFindNearestBrute(Vector3 worldPos, float maxSq, ref int bestIndex, ref float bestSq)

        {

            for (var i = 0; i < _positions.Length; i++)

            {

                if (_edges[i].Length == 0)

                    continue;

                ConsiderWaypoint(i, worldPos, maxSq, ref bestIndex, ref bestSq);

            }



            return bestIndex >= 0;

        }



        private static Vector3 FlatDir(Vector3 from, Vector3 to)
        {
            var d = to - from;
            d.y = 0f;
            return d.sqrMagnitude > 0.01f ? d.normalized : Vector3.zero;
        }

        private static float FlatDistSq(Vector3 a, Vector3 b)

        {

            var dx = a.x - b.x;

            var dz = a.z - b.z;

            return dx * dx + dz * dz;

        }



        private void ConsiderWaypoint(int listIndex, Vector3 worldPos, float maxSq, ref int bestIndex, ref float bestSq)

        {

            if (listIndex < 0 || listIndex >= _positions.Length)

                return;



            var pos = _positions[listIndex];

            var dx = pos.x - worldPos.x;

            var dz = pos.z - worldPos.z;

            var sq = dx * dx + dz * dz;

            if (sq > maxSq || sq >= bestSq)

                return;



            bestSq = sq;

            bestIndex = listIndex;

        }



        private void Build(CurrentSceneData scene, Waypoint[] array)

        {

            _sceneData = scene;

            _waypoints = array;



            var maxIndex = 0;

            for (var i = 0; i < array.Length; i++)

            {

                var idx = array[i]?.listIndex ?? i;

                if (idx > maxIndex)

                    maxIndex = idx;

            }



            var size = Math.Max(maxIndex + 1, array.Length);

            _positions = new Vector3[size];

            _edges = new int[size][];

            _reverseEdges = new int[size][];

            _laneChangeEdges = new int[size][];

            _otherLanes = new int[size][];

            _junctionGroupByIndex = new int[size];



            for (var i = 0; i < size; i++)

                _junctionGroupByIndex[i] = -1;



            for (var i = 0; i < array.Length; i++)

            {

                var wp = array[i];

                if (wp == null)

                    continue;



                var idx = wp.listIndex;

                if (idx < 0 || idx >= size)

                    continue;



                if (wp.temporaryDisabled)

                {

                    _edges[idx] = Array.Empty<int>();

                    _reverseEdges[idx] = Array.Empty<int>();

                    _laneChangeEdges[idx] = Array.Empty<int>();

                    _otherLanes[idx] = Array.Empty<int>();

                    continue;

                }



                _positions[idx] = wp.position;

                _edges[idx] = CollectForwardEdges(wp);

                _otherLanes[idx] = CopyIndices(wp.otherLanes);

                _laneChangeEdges[idx] = Array.Empty<int>();

            }



            BuildReverseEdges(size);



            var junctionCore = new bool[size];

            var junctionZone = new bool[size];

            var approachZone = new bool[size];

            MarkJunctionCores(array, size, junctionCore);

            ExpandJunctionZones(size, junctionCore, junctionZone);

            BuildJunctionGroups(size, junctionZone);

            MarkApproachCorridors(size, junctionZone, approachZone);

            // Synthetic turns come from the enhanced CSV only (no runtime intersection augmentation).
            ApplyEnhancedRouteEdges(size);

            BuildReverseEdges(size);



            for (var i = 0; i < array.Length; i++)

            {

                var wp = array[i];

                if (wp == null || wp.temporaryDisabled)

                    continue;



                var idx = wp.listIndex;

                if (idx < 0 || idx >= size)

                    continue;



                _laneChangeEdges[idx] = BuildLaneChangeEdges(

                    wp, idx, size, junctionZone, approachZone, _junctionGroupByIndex[idx]);

            }

            _junctionZone = junctionZone;

            _routingIndex = TrafficRoutingIndex.Build(
                size,
                _positions,
                _edges,
                _laneChangeEdges,
                array,
                _enhancedTurnControls,
                _enhancedEdgeLengths);

        }

        internal float GetForwardTravelCost(int from, int to, int incomingFrom) =>
            _routingIndex != null
                ? _routingIndex.GetForwardTravelCost(from, to, incomingFrom)
                : FlatLengthBetween(from, to);

        internal float EstimatePathTravelCost(IReadOnlyList<int> path) =>
            _routingIndex != null
                ? _routingIndex.EstimatePathCost(path)
                : 0f;

        private float FlatLengthBetween(int from, int to)
        {
            if (from < 0 || to < 0 || from >= _positions.Length || to >= _positions.Length)
                return 0f;
            return Mathf.Sqrt(FlatDistSq(_positions[from], _positions[to]));
        }



        private void BuildReverseEdges(int size)

        {

            var builders = new List<int>[size];

            for (var i = 0; i < size; i++)

                builders[i] = new List<int>(2);



            for (var from = 0; from < size; from++)

            {

                var edges = _edges[from];

                if (edges == null)

                    continue;



                for (var i = 0; i < edges.Length; i++)

                {

                    var to = edges[i];

                    if (to < 0 || to >= size)

                        continue;

                    builders[to].Add(from);

                }

            }



            for (var i = 0; i < size; i++)

                _reverseEdges[i] = builders[i].ToArray();

        }

        private void ApplyEnhancedRouteEdges(int size)
        {
            _enhancedTurnControls = new Dictionary<long, Vector3>();
            _enhancedEdgeLengths = new Dictionary<long, float>();
            _enhancedTurnAngles = new Dictionary<long, float>();
            _authorizedUturnEdges = new HashSet<long>();
            var turns = EnhancedRouteEdges.LoadSyntheticTurns(size, _authorizedUturnEdges);
            if (turns == null || turns.Count == 0)
                return;

            var builders = new List<int>[size];
            var seen = new HashSet<int>[size];
            for (var i = 0; i < size; i++)
            {
                var existing = _edges[i];
                builders[i] = existing == null || existing.Length == 0
                    ? new List<int>(2)
                    : new List<int>(existing);
                seen[i] = new HashSet<int>(builders[i]);
            }

            var added = 0;
            for (var i = 0; i < turns.Count; i++)
            {
                var edge = turns[i];
                if (edge.From < 0 || edge.To < 0 || edge.From >= size || edge.To >= size)
                    continue;
                if (!seen[edge.From].Add(edge.To))
                    continue;

                builders[edge.From].Add(edge.To);
                var key = EdgeKey(edge.From, edge.To);
                _enhancedTurnControls[key] = edge.Control;
                if (edge.ArcLengthMeters > 0f)
                    _enhancedEdgeLengths[key] = edge.ArcLengthMeters;
                if (edge.AbsAngleDegrees > 0f)
                    _enhancedTurnAngles[key] = edge.AbsAngleDegrees;
                added++;
            }

            if (added == 0)
            {
                _enhancedTurnControls.Clear();
                _enhancedEdgeLengths.Clear();
                _enhancedTurnAngles.Clear();
                return;
            }

            for (var i = 0; i < size; i++)
                _edges[i] = builders[i].ToArray();
        }

        private static long EdgeKey(int from, int to) =>
            ((long)from << 32) ^ (uint)to;



        private void MarkJunctionCores(Waypoint[] array, int size, bool[] junctionCore)

        {

            for (var i = 0; i < array.Length; i++)

            {

                var wp = array[i];

                if (wp == null || wp.temporaryDisabled)

                    continue;



                var idx = wp.listIndex;

                if (idx < 0 || idx >= size)

                    continue;



                if (IsJunctionCore(wp, idx, size))

                    junctionCore[idx] = true;

            }

        }



        private bool IsJunctionCore(Waypoint wp, int idx, int size)

        {

            if (wp.IsInIntersection())

                return true;



            var edges = _edges[idx];

            if (edges != null && edges.Length >= 2)

                return true;



            if (edges == null)

                return false;



            for (var depth = 1; depth <= 2; depth++)

            {

                for (var i = 0; i < edges.Length; i++)

                {

                    if (HasForkWithinHops(edges[i], size, depth))

                        return true;

                }

            }



            return false;

        }



        private bool HasForkWithinHops(int start, int size, int maxHops)

        {

            if (start < 0 || start >= size)

                return false;



            var frontier = new List<int> { start };

            for (var hop = 0; hop < maxHops; hop++)

            {

                var nextFrontier = new List<int>();

                for (var f = 0; f < frontier.Count; f++)

                {

                    var node = frontier[f];

                    if (node < 0 || node >= size)

                        continue;



                    var edges = _edges[node];

                    if (edges == null)

                        continue;



                    if (edges.Length >= 2)

                        return true;



                    for (var e = 0; e < edges.Length; e++)

                        nextFrontier.Add(edges[e]);

                }



                frontier = nextFrontier;

            }



            return false;

        }



        private void ExpandJunctionZones(int size, bool[] junctionCore, bool[] junctionZone)
        {
            var radiusSq = JunctionZoneRadiusMeters * JunctionZoneRadiusMeters;

            for (var core = 0; core < size; core++)
            {
                if (!junctionCore[core])
                    continue;

                var corePos = _positions[core];
                var localSeen = new HashSet<int>();
                var queue = new Queue<int>();
                queue.Enqueue(core);
                localSeen.Add(core);
                junctionZone[core] = true;

                while (queue.Count > 0)
                {
                    var node = queue.Dequeue();
                    void Relax(int next)
                    {
                        if (next < 0 || next >= size || !localSeen.Add(next))
                            return;
                        if (_edges[next] == null || _edges[next].Length == 0)
                            return;
                        if (FlatDistSq(_positions[next], corePos) > radiusSq)
                            return;

                        junctionZone[next] = true;
                        queue.Enqueue(next);
                    }

                    var edges = _edges[node];
                    if (edges != null)
                    {
                        for (var i = 0; i < edges.Length; i++)
                            Relax(edges[i]);
                    }

                    var rev = _reverseEdges[node];
                    if (rev != null)
                    {
                        for (var i = 0; i < rev.Length; i++)
                            Relax(rev[i]);
                    }
                }
            }
        }



        private void BuildJunctionGroups(int size, bool[] junctionZone)
        {
            var groups = new List<List<int>>();
            var assigned = new bool[size];
            var clusterSq = JunctionClusterRadiusMeters * JunctionClusterRadiusMeters;

            for (var seed = 0; seed < size; seed++)
            {
                if (!junctionZone[seed] || assigned[seed])
                    continue;

                var group = new List<int>();
                var groupId = groups.Count;
                var seedPos = _positions[seed];
                var queue = new Queue<int>();
                queue.Enqueue(seed);
                assigned[seed] = true;

                while (queue.Count > 0)
                {
                    var node = queue.Dequeue();
                    group.Add(node);
                    _junctionGroupByIndex[node] = groupId;

                    void TryEnqueue(int next)
                    {
                        if (next < 0 || next >= size || !junctionZone[next] || assigned[next])
                            return;
                        if (FlatDistSq(_positions[next], seedPos) > clusterSq)
                            return;

                        assigned[next] = true;
                        queue.Enqueue(next);
                    }

                    var edges = _edges[node];
                    if (edges != null)
                    {
                        for (var e = 0; e < edges.Length; e++)
                            TryEnqueue(edges[e]);
                    }

                    var rev = _reverseEdges[node];
                    if (rev != null)
                    {
                        for (var r = 0; r < rev.Length; r++)
                            TryEnqueue(rev[r]);
                    }

                    var lanes = _otherLanes[node];
                    if (lanes != null)
                    {
                        for (var l = 0; l < lanes.Length; l++)
                            TryEnqueue(lanes[l]);
                    }
                }

                groups.Add(group);
            }

            _junctionGroups = groups.ToArray();
        }



        private void MarkApproachCorridors(int size, bool[] junctionZone, bool[] approachZone)

        {

            var queue = new Queue<(int node, int depth)>();

            var visited = new bool[size];



            for (var i = 0; i < size; i++)

            {

                if (!junctionZone[i])

                    continue;

                queue.Enqueue((i, 0));

                visited[i] = true;

            }



            while (queue.Count > 0)

            {

                var (node, depth) = queue.Dequeue();

                if (depth > 0)

                    approachZone[node] = true;



                if (depth >= ApproachHops)

                    continue;



                var reverse = _reverseEdges[node];

                if (reverse == null)

                    continue;



                for (var i = 0; i < reverse.Length; i++)

                {

                    var prev = reverse[i];

                    if (prev < 0 || prev >= size || visited[prev])

                        continue;



                    visited[prev] = true;

                    queue.Enqueue((prev, depth + 1));

                }

            }

        }



        private int[] BuildLaneChangeEdges(

            Waypoint wp,

            int idx,

            int size,

            bool[] junctionZone,

            bool[] approachZone,

            int groupId)

        {

            var list = new List<int>(8);

            var inZone = junctionZone[idx];

            var inApproach = approachZone[idx];

            var allowParallel = inZone || inApproach || wp.CanChange();



            if (wp.otherLanes != null)

            {

                for (var i = 0; i < wp.otherLanes.Count; i++)

                {

                    var lane = wp.otherLanes[i];

                    if (!IsValidLaneChangeTarget(idx, lane, size, allowParallel))

                        continue;

                    AddIndices(list, new List<int> { lane });

                }

            }



            if (inZone && groupId >= 0 && groupId < _junctionGroups.Length)

            {

                var group = _junctionGroups[groupId];

                for (var i = 0; i < group.Count; i++)

                {

                    var member = group[i];

                    if (member == idx)

                        continue;

                    if (!IsValidLaneChangeTarget(idx, member, size, allowParallel: true))

                        continue;

                    AddIndices(list, new List<int> { member });

                }

            }



            return list.ToArray();

        }



        private bool IsValidLaneChangeTarget(int fromIdx, int toIdx, int size, bool allowParallel)

        {

            if (toIdx < 0 || toIdx >= size || fromIdx == toIdx)

                return false;



            var edges = _edges[toIdx];

            if (edges == null || edges.Length == 0)

                return false;



            if (!SharesForwardBearing(fromIdx, toIdx))

                return false;



            if (allowParallel)

                return true;



            return FlatDistSq(_positions[fromIdx], _positions[toIdx]) <=

                   ParallelLaneMaxLateralMeters * ParallelLaneMaxLateralMeters;

        }



        private bool SharesForwardBearing(int fromIdx, int toIdx)

        {

            if (!TryGetForwardBearing(fromIdx, out var fromBearing) ||

                !TryGetForwardBearing(toIdx, out var toBearing))

                return true;



            var delta = Mathf.Abs(Mathf.DeltaAngle(fromBearing, toBearing));

            return delta < OppositeBearingDegrees;

        }



        private bool TryGetForwardBearing(int idx, out float bearingDegrees)

        {

            bearingDegrees = 0f;

            var edges = _edges[idx];

            if (edges == null || edges.Length == 0)

                return false;



            var from = _positions[idx];

            var bestSq = float.MaxValue;

            var found = false;



            for (var i = 0; i < edges.Length; i++)

            {

                var next = edges[i];

                if (next < 0 || next >= _positions.Length)

                    continue;



                var to = _positions[next];

                var dx = to.x - from.x;

                var dz = to.z - from.z;

                var sq = dx * dx + dz * dz;

                if (sq < 0.01f || sq >= bestSq)

                    continue;



                bestSq = sq;

                bearingDegrees = Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;

                found = true;

            }



            return found;

        }



        private static int[] CollectForwardEdges(Waypoint wp)

        {

            var list = new List<int>(4);

            AddIndices(list, wp.neighbors);

            return list.ToArray();

        }



        private static int[] CopyIndices(List<int> source)

        {

            if (source == null || source.Count == 0)

                return Array.Empty<int>();



            var list = new List<int>(source.Count);

            AddIndices(list, source);

            return list.ToArray();

        }



        private static void AddIndices(List<int> dest, List<int> source)

        {

            if (source == null)

                return;



            for (var i = 0; i < source.Count; i++)

            {

                var n = source[i];

                if (n < 0 || dest.Contains(n))

                    continue;

                dest.Add(n);

            }

        }

    }

}


