using Il2CppGleyTrafficSystem;
using Il2CppGleyUrbanAssets;
using UnityEngine;

namespace VoogleRoute.Navigation;

/// <summary>Graphe routier Gley (waypoints trafic) pour la ville courante.</summary>
internal sealed class TrafficWaypointGraph
{
    private static TrafficWaypointGraph? _instance;

    private Waypoint[] _waypoints = null!;
    private int[][] _forwardEdges = null!;
    private Vector3[] _positions = null!;
    private CurrentSceneData? _sceneData;

    public static TrafficWaypointGraph Instance => _instance ??= new TrafficWaypointGraph();

    public bool IsReady => _waypoints != null && _waypoints.Length > 0;

    public int WaypointCount => _waypoints?.Length ?? 0;

    public static void InvalidateCache() => _instance = null;

    /// <summary>Charge ou recharge le graphe depuis <see cref="CurrentSceneData"/>.</summary>
    public bool TryEnsureLoaded()
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

    public Vector3 GetPosition(int listIndex)
    {
        if (listIndex < 0 || listIndex >= _positions.Length)
            return default;
        return _positions[listIndex];
    }

    public ReadOnlySpan<int> GetNeighbors(int listIndex)
    {
        if (listIndex < 0 || listIndex >= _forwardEdges.Length)
            return ReadOnlySpan<int>.Empty;
        return _forwardEdges[listIndex];
    }

    /// <summary>Plusieurs waypoints proches (pour choisir une meilleure extrémité de route).</summary>
    public int CollectNearest(Vector3 worldPos, float maxDistance, int[] buffer)
    {
        if (!IsReady || buffer.Length == 0)
            return 0;

        var maxSq = maxDistance * maxDistance;
        var candidates = new System.Collections.Generic.List<(int idx, float sq)>(12);

        void Consider(int idx)
        {
            if (idx < 0 || idx >= _positions.Length || _forwardEdges[idx].Length == 0)
                return;
            var sq = FlatDistSq(_positions[idx], worldPos);
            if (sq <= maxSq)
                candidates.Add((idx, sq));
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

        if (candidates.Count < buffer.Length)
        {
            for (var i = 0; i < _positions.Length; i++)
                Consider(i);
        }

        candidates.Sort((a, b) => a.sq.CompareTo(b.sq));

        var count = System.Math.Min(candidates.Count, buffer.Length);
        for (var i = 0; i < count; i++)
            buffer[i] = candidates[i].idx;
        return count;
    }

    /// <summary>Waypoint routier le plus proche (XZ), via grille Gley puis élargissement.</summary>
    public bool TryFindNearest(Vector3 worldPos, float maxDistance, out int listIndex, out float distanceSq)
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
            var cell = _sceneData!.GetCell(worldPos);
            if (cell?.waypointsInCell == null)
                return false;

            var candidates = cell.waypointsInCell;
            var count = candidates.Count;
            for (var i = 0; i < count; i++)
            {
                ConsiderWaypoint(candidates[i], worldPos, maxSq, ref bestIndex, ref bestSq);
            }

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
            if (_forwardEdges[i].Length == 0)
                continue;
            ConsiderWaypoint(i, worldPos, maxSq, ref bestIndex, ref bestSq);
        }

        return bestIndex >= 0;
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

        var size = System.Math.Max(maxIndex + 1, array.Length);
        _positions = new Vector3[size];
        _forwardEdges = new int[size][];

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
                _forwardEdges[idx] = System.Array.Empty<int>();
                continue;
            }

            _positions[idx] = wp.position;
            _forwardEdges[idx] = CollectEdges(wp);
        }

    }

    private static int[] CollectEdges(Waypoint wp)
    {
        var list = new System.Collections.Generic.List<int>(8);
        AddIndices(list, wp.neighbors);
        AddIndices(list, wp.otherLanes);
        return list.ToArray();
    }

    private static void AddIndices(System.Collections.Generic.List<int> dest, Il2CppSystem.Collections.Generic.List<int>? source)
    {
        if (source == null)
            return;

        var count = source.Count;
        for (var i = 0; i < count; i++)
        {
            var n = source[i];
            if (n < 0 || dest.Contains(n))
                continue;
            dest.Add(n);
        }
    }
}
