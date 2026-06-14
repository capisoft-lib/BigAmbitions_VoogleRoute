using System;
using UnityEngine;
using VoogleRoute.Pathfinding.Geometry;
using VoogleRoute.Pathfinding.Routing.Foot;
using DllSubwayStation = VoogleRoute.Pathfinding.Routing.Foot.SubwayStation;

namespace VoogleRoute.Navigation
{
    /// <summary>Subway travel costs and display paths (complete graph + Manhattan bridge rule).</summary>
    internal static class SubwayGraph
    {
        internal static SubwayNetwork Network { get; } = new();

        internal static void RefreshBridgePaths()
        {
            try
            {
                if (!CityManager.IsInitialized)
                {
                    Network.SetBridgePaths(Array.Empty<Vec3>(), Array.Empty<Vec3>());
                    return;
                }

                var subwaySystem = CityManager.Instance?.subwaySystem;
                if (subwaySystem == null)
                {
                    Network.SetBridgePaths(Array.Empty<Vec3>(), Array.Empty<Vec3>());
                    return;
                }

                Network.SetBridgePaths(
                    ToVec3Array(subwaySystem.manhattanBridgeLmToIc),
                    ToVec3Array(subwaySystem.manhattanBridgeIcToLm));
            }
            catch (Exception ex)
            {
                ModLog.Error("Failed to read subway bridge paths", ex);
                Network.SetBridgePaths(Array.Empty<Vec3>(), Array.Empty<Vec3>());
            }
        }

        internal static float EstimateTravelMeters(SubwayStationRecord from, SubwayStationRecord to)
        {
            if (from == null || to == null)
                return float.PositiveInfinity;

            if (from.Index == to.Index)
                return 0f;

            return VehiclePathArrival.PolylineLength(
                ToUnity(Network.BuildTravelPoints(ToDll(from), ToDll(to))));
        }

        internal static Vector3[] BuildTravelPoints(SubwayStationRecord from, SubwayStationRecord to) =>
            ToUnity(Network.BuildTravelPoints(ToDll(from), ToDll(to)));

        internal static Vector3[] BuildDisplayPath(SubwayStationRecord from, SubwayStationRecord to) =>
            ToUnity(Network.BuildDisplayPath(ToDll(from), ToDll(to)));

        internal static bool CrossesManhattanBridge(string fromNeighborhood, string toNeighborhood) =>
            SubwayNetwork.CrossesManhattanBridge(fromNeighborhood, toNeighborhood);

        private static DllSubwayStation ToDll(SubwayStationRecord record) =>
            new()
            {
                Index = record.Index,
                StationName = record.StationName,
                Neighborhood = record.Neighborhood ?? string.Empty,
                WorldPosition = new Vec3(record.WorldPosition.x, record.WorldPosition.y, record.WorldPosition.z),
                NavPosition = new Vec3(record.NavPosition.x, record.NavPosition.y, record.NavPosition.z)
            };

        private static Vec3[] ToVec3Array(Vector3[] source)
        {
            if (source == null || source.Length == 0)
                return Array.Empty<Vec3>();

            var copy = new Vec3[source.Length];
            for (var i = 0; i < source.Length; i++)
                copy[i] = new Vec3(source[i].x, source[i].y, source[i].z);
            return copy;
        }

        private static Vector3[] ToUnity(System.Collections.Generic.IReadOnlyList<Vec3> points)
        {
            if (points.Count == 0)
                return Array.Empty<Vector3>();

            var array = new Vector3[points.Count];
            for (var i = 0; i < points.Count; i++)
            {
                var p = points[i];
                array[i] = new Vector3(p.X, p.Y, p.Z);
            }

            return array;
        }
    }
}
