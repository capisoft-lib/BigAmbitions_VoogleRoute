using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace VoogleRoute.Navigation
{
    /// <summary>Subway stations from live city data, with a packaged CSV fallback.</summary>
    internal static class SubwayStationStore
    {
        private static readonly List<SubwayStationRecord> Stations = new List<SubwayStationRecord>();
        private static bool _loaded;

        internal static IReadOnlyList<SubwayStationRecord> All => Stations;

        internal static int Count => Stations.Count;

        internal static void WarmUp() => TryEnsureLoaded();

        internal static void Invalidate()
        {
            Stations.Clear();
            _loaded = false;
        }

        internal static bool TryEnsureLoaded()
        {
            if (_loaded && Stations.Count > 0)
                return true;

            if (TryLoadFromRuntime())
            {
                _loaded = true;
                return true;
            }

            if (TryLoadFromCsv())
            {
                _loaded = true;
                return true;
            }

            return false;
        }

        internal static bool TryGetAt(int index, out SubwayStationRecord station)
        {
            station = null;
            if (!TryEnsureLoaded() || index < 0 || index >= Stations.Count)
                return false;

            station = Stations[index];
            return station != null;
        }

        internal static bool TryFindByName(string stationName, out SubwayStationRecord station)
        {
            station = null;
            if (!TryEnsureLoaded() || string.IsNullOrEmpty(stationName))
                return false;

            for (var i = 0; i < Stations.Count; i++)
            {
                if (Stations[i].StationName == stationName)
                {
                    station = Stations[i];
                    return true;
                }
            }

            return false;
        }

        private static bool TryLoadFromRuntime()
        {
            try
            {
                if (!CityManager.IsInitialized)
                    return false;

                var runtimeStations = CityManager.Instance?.subwayStations;
                if (runtimeStations == null || runtimeStations.Count == 0)
                    return false;

                Stations.Clear();
                for (var i = 0; i < runtimeStations.Count; i++)
                {
                    var source = runtimeStations[i];
                    if (source == null)
                        continue;

                    var world = source.transform.position;
                    Vector3 nav;
                    try
                    {
                        nav = source.GetNavMeshTargetPosition();
                    }
                    catch
                    {
                        nav = world;
                    }

                    Stations.Add(new SubwayStationRecord
                    {
                        Index = Stations.Count,
                        StationName = source.stationName.ToStringFast(),
                        Neighborhood = source.neighbourhood ?? string.Empty,
                        WorldPosition = world,
                        NavPosition = nav
                    });
                }

                if (Stations.Count == 0)
                    return false;

                ModLog.Info("Subway stations loaded from city (" + Stations.Count + ").");
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Error("Failed to load subway stations from city", ex);
                return false;
            }
        }

        private static bool TryLoadFromCsv()
        {
            var path = ModStoragePaths.PathInModRoot(ModStoragePaths.SubwayStationsCsv);
            if (!File.Exists(path))
                return false;

            try
            {
                Stations.Clear();
                using var reader = new StreamReader(path);
                var header = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(header))
                    return false;

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                        continue;

                    var parts = line.Split(',');
                    if (parts.Length < 8)
                        continue;

                    Stations.Add(new SubwayStationRecord
                    {
                        Index = Stations.Count,
                        StationName = parts[0].Trim(),
                        Neighborhood = parts[1].Trim(),
                        WorldPosition = ReadVector(parts, 2),
                        NavPosition = ReadVector(parts, 5)
                    });
                }

                if (Stations.Count == 0)
                    return false;

                ModLog.Info("Subway stations loaded from CSV (" + Stations.Count + ") from " + path);
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Error("Failed to load subway stations CSV", ex);
                Stations.Clear();
                return false;
            }
        }

        private static Vector3 ReadVector(string[] parts, int start)
        {
            var x = float.Parse(parts[start], CultureInfo.InvariantCulture);
            var y = float.Parse(parts[start + 1], CultureInfo.InvariantCulture);
            var z = float.Parse(parts[start + 2], CultureInfo.InvariantCulture);
            return new Vector3(x, y, z);
        }
    }
}
