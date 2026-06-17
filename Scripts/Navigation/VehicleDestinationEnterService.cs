using System;
using Helpers;
using UnityEngine;
using Vehicles.VehicleTypes;
using VoogleRoute;

namespace VoogleRoute.Navigation
{
    /// <summary>Enters a parked player vehicle after foot navigation reaches a car destination.</summary>
    internal static class VehicleDestinationEnterService
    {
        private const float MatchRadiusMeters = 8f;

        internal static bool IsVehicleNavigationSource(string source) =>
            source == NavigationTargetTracker.ParkedVehicleSource ||
            source == NavigationTargetTracker.WorldPositionSource;

        internal static bool TryEnterAfterNavigation(Vector3 target, string source)
        {
            if (!ModConfig.AutoEnterDestinationEnabled)
                return false;

            if (!IsVehicleNavigationSource(source))
                return false;

            if (!TryFindMotorVehicleNear(target, out var controller))
                return false;

            return TryEnterVehicle(controller);
        }

        private static bool TryEnterVehicle(VehicleController controller)
        {
            if (controller == null)
                return false;

            try
            {
                var player = PlayerHelper.PlayerController;
                if (player == null)
                    return false;

                if (GameManager.Instance?.selectedVehicle != null)
                    return false;

                PlayerNavigationRelease.Release();
                controller.DriveVehicle();
                ModLog.Info(
                    "Entering vehicle after navigation: " +
                    (controller.vehicleInstance?.id ?? controller.name));
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Error("Failed to enter vehicle after navigation", ex);
                return false;
            }
        }

        private static bool TryFindMotorVehicleNear(Vector3 target, out VehicleController controller)
        {
            controller = null;
            if (target.sqrMagnitude < 0.01f)
                return false;

            var bestDistSq = MatchRadiusMeters * MatchRadiusMeters;

            try
            {
                foreach (var vehicle in VehicleHelper.AllPlayerVehicles)
                {
                    if (!IsEnterableMotorVehicle(vehicle))
                        continue;

                    if (!VehicleEntranceHelper.TryGetDriverEntrancePosition(vehicle, out var entrance))
                        continue;

                    var distSq = HorizontalDistanceSq(entrance, target);
                    if (distSq > bestDistSq)
                        continue;

                    bestDistSq = distSq;
                    controller = vehicle;
                }
            }
            catch
            {
                return false;
            }

            return controller != null;
        }

        private static bool IsEnterableMotorVehicle(VehicleController vehicle)
        {
            if (vehicle?.vehicleInstance == null)
                return false;

            try
            {
                var vehicleType = vehicle.vehicleInstance.VehicleType;
                return vehicleType != null && vehicleType.IsMotorVehicle;
            }
            catch
            {
                return false;
            }
        }

        private static float HorizontalDistanceSq(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return (a - b).sqrMagnitude;
        }
    }
}
