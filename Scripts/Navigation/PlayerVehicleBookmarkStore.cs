using System;
using System.Collections.Generic;
using Buildings;
using Entities;
using Helpers;
using Localizor;
using Streets;
using UnityEngine;
using Vehicles.VehicleTypes;

namespace VoogleRoute.Navigation
{
    /// <summary>Auto-listed owned motor vehicles parked outside (not in a warehouse).</summary>
    internal static class PlayerVehicleBookmarkStore
    {
        private static readonly List<(string VehicleId, BookmarkEntry Entry)> Vehicles =
            new List<(string, BookmarkEntry)>();

        internal static int Count => Vehicles.Count;

        internal static void Refresh()
        {
            Vehicles.Clear();

            try
            {
                var save = SaveGameManager.Current;
                if (save?.VehicleInstances == null)
                    return;

                var instances = save.VehicleInstances;
                for (var i = 0; i < instances.Count; i++)
                {
                    var instance = instances[i];
                    if (instance == null || !TryCreateEntry(instance, out var entry))
                        continue;

                    Vehicles.Add((instance.id, entry));
                }

                Vehicles.Sort((a, b) =>
                    string.Compare(a.Entry.DisplayName, b.Entry.DisplayName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                ModLog.Error("Failed to refresh owned vehicle bookmarks", ex);
            }
        }

        internal static bool TryGetAt(int index, out BookmarkEntry entry)
        {
            entry = null;
            if (index < 0 || index >= Vehicles.Count)
                return false;

            entry = Vehicles[index].Entry;
            return entry != null;
        }

        internal static bool TryGetVehicleIdAt(int index, out string vehicleId)
        {
            vehicleId = null;
            if (index < 0 || index >= Vehicles.Count)
                return false;

            vehicleId = Vehicles[index].VehicleId;
            return !string.IsNullOrEmpty(vehicleId);
        }

        private static bool TryCreateEntry(VehicleInstance vehicle, out BookmarkEntry entry)
        {
            entry = null;
            if (vehicle == null || string.IsNullOrWhiteSpace(vehicle.vehicleTypeName))
                return false;

            VehicleType vehicleType;
            try
            {
                vehicleType = vehicle.VehicleType;
            }
            catch
            {
                return false;
            }

            if (vehicleType == null || !vehicleType.IsMotorVehicle)
                return false;

            if (IsStoredInWarehouse(vehicle))
                return false;

            if (!TryResolvePosition(vehicle, out var worldPos, out var worldOnly, out var hasAddress))
                return false;

            entry = new BookmarkEntry
            {
                Name = BuildDisplayName(vehicle),
                WorldX = worldPos.x,
                WorldY = worldPos.y,
                WorldZ = worldPos.z,
                WorldOnly = worldOnly,
                LocationLabel = BuildDisplayName(vehicle)
            };

            if (hasAddress)
            {
                entry.StreetName = vehicle.streetName;
                entry.StreetNumber = vehicle.streetNumber;
            }

            return entry.TryGetNavigationTarget(out _);
        }

        private static bool IsStoredInWarehouse(VehicleInstance vehicle)
        {
            if (vehicle == null)
                return false;

            // Spawned outdoors — show even if warehouse slot data is stale.
            if (TryGetSpawnedPosition(vehicle.id, out _))
                return false;

            if (IsAssignedToWarehouseSlot(vehicle.id))
                return true;

            return IsAtWarehouseAddress(vehicle);
        }

        private static bool IsAssignedToWarehouseSlot(string vehicleId)
        {
            if (string.IsNullOrEmpty(vehicleId))
                return false;

            try
            {
                var registrations = SaveGameManager.Current?.BuildingRegistrations;
                if (registrations == null)
                    return false;

                for (var i = 0; i < registrations.Count; i++)
                {
                    if (registrations[i] is not Warehouse warehouse)
                        continue;

                    var slots = warehouse.vehicleSlots;
                    if (slots == null)
                        continue;

                    for (var j = 0; j < slots.Count; j++)
                    {
                        if (slots[j].vehicleInstanceId == vehicleId)
                            return true;
                    }
                }
            }
            catch
            {
                // ignore
            }

            return false;
        }

        private static bool IsAtWarehouseAddress(VehicleInstance vehicle)
        {
            if (vehicle == null || string.IsNullOrWhiteSpace(vehicle.streetName))
                return false;

            if (IsStreetParking(vehicle))
                return false;

            try
            {
                var registration = BuildingHelper.GetBuildingRegistration(vehicle.Address);
                if (registration == null)
                    return false;

                return registration is Warehouse ||
                       registration.GetBuildingType() == "ba:buildingtype_warehouse";
            }
            catch
            {
                return false;
            }
        }

        private static bool TryResolvePosition(
            VehicleInstance vehicle,
            out Vector3 worldPos,
            out bool worldOnly,
            out bool hasAddress)
        {
            worldPos = default;
            worldOnly = false;
            hasAddress = HasOutdoorAddress(vehicle);

            if (TryGetSpawnedPosition(vehicle.id, out worldPos))
            {
                worldOnly = !hasAddress || IsStreetParking(vehicle);
                return true;
            }

            var saved = (Vector3)vehicle.position;
            if (saved.sqrMagnitude > 0.01f)
            {
                worldPos = saved;
                worldOnly = !hasAddress || IsStreetParking(vehicle);
                return true;
            }

            if (!hasAddress)
                return false;

            worldOnly = false;
            return DestinationResolver.TryResolveWorldPosition(
                new Address(vehicle.streetName, vehicle.streetNumber),
                out worldPos);
        }

        private static bool TryGetSpawnedPosition(string vehicleId, out Vector3 position)
        {
            position = default;
            if (string.IsNullOrEmpty(vehicleId))
                return false;

            try
            {
                foreach (var controller in VehicleHelper.AllPlayerVehicles)
                {
                    if (controller?.vehicleInstance?.id != vehicleId)
                        continue;

                    if (VehicleEntranceHelper.TryGetDriverEntrancePosition(controller, out position))
                        return true;
                }
            }
            catch
            {
                // ignore
            }

            return false;
        }

        private static bool HasOutdoorAddress(VehicleInstance vehicle) =>
            !string.IsNullOrWhiteSpace(vehicle.streetName) &&
            vehicle.streetNumber > 0 &&
            !IsStreetParking(vehicle);

        private static bool IsStreetParking(VehicleInstance vehicle) =>
            string.Equals(vehicle.streetName, "ba:street_parking", StringComparison.Ordinal);

        private static string BuildDisplayName(VehicleInstance vehicle)
        {
            var label = SafeLocalize(vehicle.vehicleTypeName);
            if (!string.IsNullOrWhiteSpace(vehicle.vehicleColorName))
            {
                var color = SafeLocalize(vehicle.vehicleColorName);
                if (!string.IsNullOrWhiteSpace(color) && !string.Equals(color, vehicle.vehicleColorName, StringComparison.Ordinal))
                    label += " (" + color + ")";
            }

            return label;
        }

        private static string SafeLocalize(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return "";

            return key.GetLocalization();
        }
    }
}
