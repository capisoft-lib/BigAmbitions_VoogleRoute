using System;
using UnityEngine;

namespace VoogleRoute.Navigation
{
    /// <summary>Subway travel costs and display paths (complete graph + Manhattan bridge rule).</summary>
    internal static class SubwayGraph
    {
        internal const string IndustryCityNeighborhood = "ba:neighborhood_industriacity";

        private static Vector3[] _bridgeLmToIc = Array.Empty<Vector3>();
        private static Vector3[] _bridgeIcToLm = Array.Empty<Vector3>();
        private static bool _bridgeLoaded;

        internal static void RefreshBridgePaths()
        {
            _bridgeLoaded = false;
            _bridgeLmToIc = Array.Empty<Vector3>();
            _bridgeIcToLm = Array.Empty<Vector3>();

            try
            {
                if (!CityManager.IsInitialized)
                    return;

                var subwaySystem = CityManager.Instance?.subwaySystem;
                if (subwaySystem == null)
                    return;

                _bridgeLmToIc = CloneOrEmpty(subwaySystem.manhattanBridgeLmToIc);
                _bridgeIcToLm = CloneOrEmpty(subwaySystem.manhattanBridgeIcToLm);
                _bridgeLoaded = _bridgeLmToIc.Length > 0 && _bridgeIcToLm.Length > 0;
            }
            catch (Exception ex)
            {
                ModLog.Error("Failed to read subway bridge paths", ex);
            }
        }

        internal static float EstimateTravelMeters(SubwayStationRecord from, SubwayStationRecord to)
        {
            if (from == null || to == null)
                return float.PositiveInfinity;

            if (from.Index == to.Index)
                return 0f;

            return VehiclePathArrival.PolylineLength(BuildTravelPoints(from, to));
        }

        internal static Vector3[] BuildTravelPoints(SubwayStationRecord from, SubwayStationRecord to)
        {
            if (from == null || to == null)
                return Array.Empty<Vector3>();

            if (from.Index == to.Index)
                return new[] { from.NavPosition };

            EnsureBridgePaths();
            var destination = to.NavPosition;

            if (CrossesManhattanBridge(from.Neighborhood, to.Neighborhood))
            {
                if (from.Neighborhood == IndustryCityNeighborhood && _bridgeIcToLm.Length >= 2)
                {
                    return new[]
                    {
                        _bridgeIcToLm[0],
                        _bridgeIcToLm[1],
                        destination
                    };
                }

                if (_bridgeLmToIc.Length >= 2)
                {
                    return new[]
                    {
                        _bridgeLmToIc[0],
                        _bridgeLmToIc[1],
                        destination
                    };
                }
            }

            return new[] { destination };
        }

        internal static Vector3[] BuildDisplayPath(SubwayStationRecord from, SubwayStationRecord to)
        {
            if (from == null || to == null)
                return Array.Empty<Vector3>();

            var travel = BuildTravelPoints(from, to);
            if (travel.Length == 0)
                return Array.Empty<Vector3>();

            var points = new Vector3[travel.Length + 1];
            points[0] = from.NavPosition;
            for (var i = 0; i < travel.Length; i++)
                points[i + 1] = travel[i];

            return points;
        }

        internal static bool CrossesManhattanBridge(string fromNeighborhood, string toNeighborhood)
        {
            if (string.IsNullOrEmpty(fromNeighborhood) || string.IsNullOrEmpty(toNeighborhood))
                return false;

            if (toNeighborhood == IndustryCityNeighborhood && fromNeighborhood != IndustryCityNeighborhood)
                return true;

            return toNeighborhood != IndustryCityNeighborhood &&
                   fromNeighborhood == IndustryCityNeighborhood;
        }

        private static void EnsureBridgePaths()
        {
            if (_bridgeLoaded)
                return;

            RefreshBridgePaths();
        }

        private static Vector3[] CloneOrEmpty(Vector3[] source)
        {
            if (source == null || source.Length == 0)
                return Array.Empty<Vector3>();

            var copy = new Vector3[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }
    }
}
