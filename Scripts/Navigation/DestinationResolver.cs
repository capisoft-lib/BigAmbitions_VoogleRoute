using System;
using Buildings;
using Helpers;
using UnityEngine;
using Vehicles.DeliveryDriverJob;
using VoogleRoute;

namespace VoogleRoute.Navigation
{
    
    /// <summary>Lit uniquement <see cref="GameInstance.customDestination"/> (GPS carte).</summary>
    internal static class DestinationResolver
    {
        private static Address _lastResolvedAddress;
        private static bool _hasLastAddress;
        private static float _lastPollTime;
    
        internal static void Poll()
        {
            if (!GameState.IsWorldReady())
                return;
    
            if (Time.unscaledTime - _lastPollTime < 0.25f)
                return;
    
            _lastPollTime = Time.unscaledTime;
            SyncFromMapDestination();
        }
    
        private static void SyncFromMapDestination()
        {
            try
            {
                var game = SaveGameManager.Current;
                if (game == null)
                    return;
    
                var address = game.customDestination;
                if (IsInvalidAddress(address))
                {
                    if (NavigationTargetTracker.HasMapGpsTarget &&
                        NavigationTargetTracker.LastSource != NavigationTargetTracker.MapSource)
                        return;

                    if (NavigationTargetTracker.HasMapGpsTarget)
                    {
                        ModLog.Info("Map destination cleared (customDestination empty).");
                        NavigationTargetTracker.ClearMapGpsTarget("map destination cleared");
                    }

                    _hasLastAddress = false;
                    _lastResolvedAddress = null;
                    return;
                }
    
                if (_hasLastAddress && AddressesEqual(_lastResolvedAddress, address) &&
                    NavigationTargetTracker.HasMapGpsTarget)
                    return;
    
                _lastResolvedAddress = address;
                _hasLastAddress = true;
    
                if (TryResolveWorldPosition(address, out var worldPos))
                {
                    ModLog.Info("Map destination synced: " + address + " -> " + worldPos);
                    NavigationTargetTracker.SetMapGpsTarget(worldPos);
                }
                else
                    ModLog.Info("Map destination unresolved: " + address);
            }
            catch (Exception ex)
            {
                ModLog.Error("Map destination sync failed", ex);
            }
        }
    
        internal static bool TryResolveWorldPosition(Address address, out Vector3 worldPosition)
        {
            worldPosition = default;
            if (!GameState.IsWorldReady())
                return false;
    
            try
            {
                if (CityManager.IsInitialized)
                {
                    var cbc = CityManager.Instance?.FindCityBuildingController(address);
                    if (cbc != null && TryGetEntranceDoorPosition(cbc, out worldPosition))
                        return true;
                }
            }
            catch
            {
                // ignore
            }
    
            try
            {
                var entrance = DeliveryJobHelper.GetAddressEntranceTransform(address);
                if (entrance != null)
                {
                    worldPosition = entrance.position;
                    return true;
                }
            }
            catch
            {
                // ignore
            }
    
            try
            {
                var reg = BuildingHelper.GetBuildingRegistration(address);
                if (reg != null && CityManager.IsInitialized)
                {
                    var cbc = CityManager.Instance?.FindCityBuildingController(reg.Address);
                    if (cbc != null && TryGetEntranceDoorPosition(cbc, out worldPosition))
                        return true;
                }
            }
            catch
            {
                // ignore
            }
    
            try
            {
                if (CityManager.IsInitialized)
                {
                    var cbc = CityManager.Instance?.FindCityBuildingController(address);
                    if (cbc != null)
                    {
                        var poi = cbc.GetPoiPosition();
                        if (poi != null)
                        {
                            worldPosition = poi.position;
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }
    
            return false;
        }
    
        private static bool TryGetEntranceDoorPosition(CityBuildingController cbc, out Vector3 worldPosition)
        {
            worldPosition = default;
            var doors = cbc.entranceDoors;
            if (doors == null || doors.Length == 0)
                return false;
    
            Transform best = null;
            var bestDist = float.MaxValue;
            var hasReference = false;
            Vector3 reference = default;
    
            if (PlayerLocationSession.IsAvailable)
            {
                reference = PlayerLocationSession.Snapshot.Position;
                hasReference = reference.sqrMagnitude > 0.01f;
            }
    
            for (var i = 0; i < doors.Length; i++)
            {
                var door = doors[i];
                var t = door?.doorTransform;
                if (t == null)
                    continue;
    
                if (!hasReference)
                {
                    worldPosition = t.position;
                    return true;
                }

                var d = (t.position - reference).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = t;
                }
            }
    
            if (best == null)
                return false;
    
            worldPosition = best.position;
            return true;
        }
    
        internal static void Clear()
        {
            _hasLastAddress = false;
            _lastResolvedAddress = null;
        }
    
        private static bool IsInvalidAddress(Address address)
        {
            if (address == null)
                return true;

            return address.streetNumber <= 0 && string.IsNullOrEmpty(address.streetName);
        }
    
        private static bool AddressesEqual(Address a, Address b) =>
            a != null && b != null && a.streetName == b.streetName && a.streetNumber == b.streetNumber;
    }
}
