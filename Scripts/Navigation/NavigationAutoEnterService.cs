using UnityEngine;
using VoogleRoute;

namespace VoogleRoute.Navigation
{
    /// <summary>Auto-enter vehicles or buildings once when navigation reaches a destination.</summary>
    internal static class NavigationAutoEnterService
    {
        private static string _lastAttemptKey = "";

        internal static void Reset() => _lastAttemptKey = "";

        internal static void NotifyTargetChanged() => _lastAttemptKey = "";

        internal static void TryOnArrival(Vector3 target, string source)
        {
            if (!ModConfig.AutoEnterDestinationEnabled)
                return;

            if (!IsValidNavigationSource(source))
                return;

            if (target.sqrMagnitude < 0.01f)
                return;

            var key = source + "|" + target.x.ToString("F1") + "|" + target.z.ToString("F1");
            if (_lastAttemptKey == key)
                return;

            _lastAttemptKey = key;

            if (VehicleDestinationEnterService.IsVehicleNavigationSource(source))
            {
                VehicleDestinationEnterService.TryEnterAfterNavigation(target, source);
                return;
            }

            if (BuildingDestinationEnterService.IsBuildingNavigationSource(source))
                BuildingDestinationEnterService.TryEnterAfterNavigation(target, source);
        }

        private static bool IsValidNavigationSource(string source) =>
            source == NavigationTargetTracker.MapSource ||
            source == NavigationTargetTracker.JobSource ||
            source == NavigationTargetTracker.ParkedVehicleSource ||
            source == NavigationTargetTracker.WorldPositionSource;
    }
}
