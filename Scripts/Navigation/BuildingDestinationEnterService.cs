using System;
using Buildings;
using Helpers;
using Parking.UndergroundParking;
using Streets;
using UnityEngine;
using VoogleRoute;

namespace VoogleRoute.Navigation
{
    /// <summary>Enters a building after foot navigation reaches a map or job destination.</summary>
    internal static class BuildingDestinationEnterService
    {
        private const float MaxDoorDistanceMeters = 9f;
        private const float CargoDeliveryDoorDistanceMeters = 13f;

        private static string _lastDeliveryStopInteractKey = "";

        internal static bool IsBuildingNavigationSource(string source) =>
            source == NavigationTargetTracker.MapSource ||
            source == NavigationTargetTracker.JobSource;

        internal static bool TryEnterAfterNavigation(Vector3 target, string source)
        {
            if (!ModConfig.AutoEnterDestinationEnabled)
                return false;

            if (!IsBuildingNavigationSource(source))
                return false;

            return TryInteractBuildingForSource(target, source, releaseNavigation: true);
        }

        private static bool TryInteractBuildingForSource(Vector3 target, string source, bool releaseNavigation)
        {
            if (!IsBuildingNavigationSource(source))
                return false;

            if (!TryResolveAddress(source, out var address))
                return false;

            return TryInteractBuilding(target, address, releaseNavigation);
        }

        /// <summary>
        /// Mimics clicking the retail door during delivery: vanilla Interact delivers from flatbed/handtruck or enters.
        /// </summary>
        internal static bool TryDeliveryJobStopInteract(Vector3 target)
        {
            if (!JobDestinationSync.IsInDeliveryMissionContext())
                return false;

            if (!JobDestinationSync.TryGetDeliveryStopAddress(out var address))
                return false;

            var key = FormatAddressKey(address);
            if (_lastDeliveryStopInteractKey == key)
                return false;

            _lastDeliveryStopInteractKey = key;
            return TryInteractBuilding(target, address, releaseNavigation: true);
        }

        internal static void ResetDeliveryStopInteract()
        {
            _lastDeliveryStopInteractKey = "";
        }

        private static bool TryInteractBuilding(Vector3 target, Address address, bool releaseNavigation)
        {
            if (address == null)
                return false;

            if (BuildingManager.IsInsideBuilding || UndergroundParkingManager.IsInsideParking)
                return false;

            if (!MovementModeDetector.IsEffectivelyOnFootForNavigation())
                return false;

            if (!MovementModeDetector.TryGetPlayerOrigin(out var playerPos) &&
                !MovementModeDetector.TryGetPathOrigin(out playerPos))
                return false;

            if (!DestinationResolver.TryResolveWorldPosition(address, out var doorPos))
                doorPos = target;

            var maxDoorDistance = MovementModeDetector.IsPushingPlayerCargoVehicle()
                ? CargoDeliveryDoorDistanceMeters
                : MaxDoorDistanceMeters;

            if (HorizontalDistance(playerPos, doorPos) > maxDoorDistance)
                return false;

            try
            {
                if (!CityManager.IsInitialized)
                    return false;

                var cbc = CityManager.Instance?.FindCityBuildingController(address);
                if (cbc == null)
                    return false;

                if (releaseNavigation)
                    PlayerNavigationRelease.Release();

                if (cbc.Interact())
                {
                    ModLog.Info("Building interact after navigation: " + address);
                    return true;
                }
            }
            catch (Exception ex)
            {
                ModLog.Error("Failed building interact after navigation", ex);
            }

            return false;
        }

        private static string FormatAddressKey(Address address) =>
            address == null ? "" : address.streetName + "|" + address.streetNumber;

        private static bool TryResolveAddress(string source, out Address address)
        {
            address = null;

            if (source == NavigationTargetTracker.MapSource)
            {
                if (DestinationResolver.TryGetActiveMapAddress(out address))
                    return true;

                try
                {
                    address = SaveGameManager.Current?.customDestination;
                    return address != null && !address.IsUndefined();
                }
                catch
                {
                    return false;
                }
            }

            if (source == NavigationTargetTracker.JobSource)
                return JobDestinationSync.TryGetActiveJobAddress(out address);

            return false;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
