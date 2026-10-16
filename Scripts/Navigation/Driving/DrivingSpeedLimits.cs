using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace VoogleRoute.Navigation.Driving
{
    /// <summary>Same 50/80 km/h road policy and baked zones as BetterFines; no runtime dependency on that mod.</summary>
    internal static class DrivingSpeedLimits
    {
        internal const float CityKmh = 50f;
        internal const float MaximumKmh = 80f;
        private const float MarginMps = 0.1f;
        private static Zone[] _zones;
        private struct Zone { internal Vector3 Center, Forward; internal float HalfLength, HalfWidth, Kmh; }

        internal static float At(Vector3 position)
        {
            EnsureLoaded();
            foreach (var zone in _zones)
            {
                var delta = position - zone.Center;
                if (Mathf.Abs(delta.y) > 2f) continue;
                var along = delta.x * zone.Forward.x + delta.z * zone.Forward.z;
                var across = -delta.x * zone.Forward.z + delta.z * zone.Forward.x;
                if (Mathf.Abs(along) <= zone.HalfLength && Mathf.Abs(across) <= zone.HalfWidth) return zone.Kmh;
            }
            return CityKmh;
        }

        internal static float TargetMps(Vector3 position) => Mathf.Max(0f, At(position) / 3.6f - MarginMps);
        internal static float CruiseMps(VehicleControlLease car) => Mathf.Min(car.MaxSpeedKmh,
            ModConfig.PhysicalDriveCruiseCeilingKmh) / 3.6f;
        internal static void Invalidate() => _zones = null;

        private static void EnsureLoaded()
        {
            if (_zones != null) return;
            _zones = Array.Empty<Zone>();
            var path = ModStoragePaths.PathInModRoot("Data/road_speed_zones_80.csv");
            try
            {
                var zones = new List<Zone>();
                using (var reader = new StreamReader(path))
                {
                    reader.ReadLine();
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var c = line.Split(',');
                        if (c.Length < 18) continue;
                        float Read(int i) => float.Parse(c[i], CultureInfo.InvariantCulture);
                        var forward = new Vector3(Read(10), 0f, Read(11)).normalized;
                        if (forward.sqrMagnitude < 0.5f) continue;
                        zones.Add(new Zone { Center = new Vector3(Read(7), Read(8), Read(9)), Forward = forward,
                            HalfLength = Read(12), HalfWidth = Read(13), Kmh = Mathf.Clamp(Read(6), CityKmh, MaximumKmh) });
                    }
                }
                _zones = zones.ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[VoogleRoute][PhysicalDrive] speed_zones_unavailable; city_limit=50; " + ex.Message);
            }
        }
    }
}
