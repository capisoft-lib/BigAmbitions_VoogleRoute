using System;
using UI.Notification;
using UnityEngine;
using VoogleRoute.Live;

namespace VoogleRoute.Navigation
{
    /// <summary>Success notification + sound when the player reaches the active map destination.</summary>
    internal static class NavigationArrivalService
    {
        private const float FootArrivalRadiusMeters = 7f;
        private const float VehicleArrivalRadiusMeters = 25f;
        private const float MinTravelBeforeAnnounceMeters = 35f;

        private static float _trackedTargetChangeTime = -1f;
        private static bool _armed;
        private static bool _announced;

        internal static void Reset()
        {
            _trackedTargetChangeTime = -1f;
            _armed = false;
            _announced = false;
        }

        internal static void Tick()
        {
            if (!GameState.ShouldRunNavigationSystems())
                return;

            if (!NavigationTargetTracker.HasMapGpsTarget)
            {
                Reset();
                return;
            }

            if (MovementModeDetector.CurrentMode is not (MovementMode.OnFoot or MovementMode.Vehicle))
                return;

            var targetChange = NavigationTargetTracker.LastChangeTime;
            if (!Mathf.Approximately(targetChange, _trackedTargetChangeTime))
            {
                _trackedTargetChangeTime = targetChange;
                _armed = false;
                _announced = false;
            }

            if (_announced)
                return;

            if (!TryGetHorizontalPosition(out var position))
                return;

            var destination = NavigationTargetTracker.ActiveTarget;
            var distance = HorizontalDistance(position, destination);
            var radius = MovementModeDetector.CurrentMode == MovementMode.Vehicle
                ? VehicleArrivalRadiusMeters
                : FootArrivalRadiusMeters;

            if (distance > Mathf.Max(MinTravelBeforeAnnounceMeters, radius * 2f))
                _armed = true;

            if (!_armed || distance > radius)
                return;

            AnnounceArrival();
            _announced = true;
        }

        private static void AnnounceArrival()
        {
            try
            {
                Notifications.Show(
                    NotificationType.Success,
                    "voogle_route_arrived_at_destination",
                    null,
                    4f,
                    null,
                    null,
                    notificationSound: true,
                    trackOnSaveGame: false);
            }
            catch (Exception ex)
            {
                ModLog.Error("Navigation arrival notification failed", ex);
            }

            ModLog.Info("Navigation destination reached.");
        }

        private static bool TryGetHorizontalPosition(out Vector3 position)
        {
            if (MovementModeDetector.TryGetPathOrigin(out position))
                return true;

            if (!PlayerLocationSession.IsAvailable)
                return false;

            position = PlayerLocationSession.Snapshot.Position;
            return position.sqrMagnitude > 0.01f;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
