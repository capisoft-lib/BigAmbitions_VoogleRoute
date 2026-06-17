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

        internal static bool IsBuildingNavigationSource(string source) =>
            source == NavigationTargetTracker.MapSource ||
            source == NavigationTargetTracker.JobSource;

        internal static bool TryEnterAfterNavigation(Vector3 target, string source)
        {
            if (!ModConfig.AutoEnterDestinationEnabled)
                return false;

            if (!IsBuildingNavigationSource(source))
                return false;

            if (BuildingManager.IsInsideBuilding || UndergroundParkingManager.IsInsideParking)
                return false;

            if (MovementModeDetector.CurrentMode != MovementMode.OnFoot)
                return false;

            if (!TryResolveAddress(source, out var address))
                return false;

            if (!MovementModeDetector.TryGetPlayerOrigin(out var playerPos))
                return false;

            if (!DestinationResolver.TryResolveWorldPosition(address, out var doorPos))
                doorPos = target;

            if (HorizontalDistance(playerPos, doorPos) > MaxDoorDistanceMeters)
                return false;

            try
            {
                if (!CityManager.IsInitialized)
                    return false;

                var cbc = CityManager.Instance?.FindCityBuildingController(address);
                if (cbc == null)
                    return false;

                PlayerNavigationRelease.Release();
                if (cbc.Interact())
                {
                    ModLog.Info("Entering building after navigation: " + address);
                    return true;
                }
            }
            catch (Exception ex)
            {
                ModLog.Error("Failed to enter building after navigation", ex);
            }

            return false;
        }

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
