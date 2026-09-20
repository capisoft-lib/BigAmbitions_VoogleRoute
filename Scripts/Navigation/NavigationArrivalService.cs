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

        private static float _trackedTargetChangeTime = -1f;
        private static bool _armed;
        private static bool _announced;
        private static bool _vehicleArrivalAnnounced;

        internal static void Reset()
        {
            _trackedTargetChangeTime = -1f;
            _armed = false;
            _announced = false;
            _vehicleArrivalAnnounced = false;
            NavigationAutoEnterService.Reset();
        }

        internal static void RememberCompletedTarget()
        {
            if (!NavigationTargetTracker.HasTarget)
                return;

            CompletedNavigationTarget.Remember(
                NavigationTargetTracker.ActiveTarget,
                ResolveArrivalTarget(),
                MovementModeDetector.CurrentMode == MovementMode.Vehicle
                    ? VehicleArrivalRadiusMeters : FootArrivalRadiusMeters);
        }

        internal static void TryCompleteNearbyDestination()
        {
            if (JobDestinationSync.ShouldDeferDestinationArrivalHandling())
                return;

            if (!NavigationTargetTracker.HasMapGpsTarget)
                return;

            SyncTrackedTarget();
            if (_announced)
                return;

            if (MovementModeDetector.CurrentMode is not (MovementMode.OnFoot or MovementMode.Vehicle))
                return;

            if (!TryGetHorizontalPosition(out var position))
                return;

            var destination = ResolveArrivalTarget();
            var distance = HorizontalDistance(position, destination);
            var radius = MovementModeDetector.CurrentMode == MovementMode.Vehicle
                ? VehicleArrivalRadiusMeters
                : FootArrivalRadiusMeters;

            if (distance > radius)
                return;

            // Only ordinary building GPS routes continue after parking. Mission ownership
            // (including a job guider without a mission object) keeps the existing behavior.
            var keepDestination = MovementModeDetector.CurrentMode == MovementMode.Vehicle &&
                NavigationTargetTracker.LastSource == NavigationTargetTracker.MapSource &&
                !JobDestinationSync.ShouldPreserveDestinationOnArrival();
            if (keepDestination && _vehicleArrivalAnnounced)
                return;

            AnnounceArrival(keepDestination);
            if (keepDestination)
                _vehicleArrivalAnnounced = true;
            else
                _announced = true;
        }

        internal static void Tick()
        {
            if (!GameState.ShouldRunNavigationSystems())
                return;

            if (JobDestinationSync.IsInDeliveryMissionContext())
                return;

            if (!NavigationTargetTracker.HasMapGpsTarget)
            {
                Reset();
                return;
            }

            if (MovementModeDetector.CurrentMode is not (MovementMode.OnFoot or MovementMode.Vehicle))
                return;

            var targetChange = NavigationTargetTracker.LastChangeTime;
            SyncTrackedTarget();

            if (_announced)
                return;

            if (!TryGetHorizontalPosition(out var position))
                return;

            var destination = ResolveArrivalTarget();
            var distance = HorizontalDistance(position, destination);
            var radius = MovementModeDetector.CurrentMode == MovementMode.Vehicle
                ? VehicleArrivalRadiusMeters
                : FootArrivalRadiusMeters;

            if (distance > radius)
            {
                _armed = true;
                return;
            }

            if (!_armed && Time.unscaledTime - targetChange < 0.75f)
                return;

            TryCompleteNearbyDestination();
        }

        private static void SyncTrackedTarget()
        {
            var targetChange = NavigationTargetTracker.LastChangeTime;
            if (Mathf.Approximately(targetChange, _trackedTargetChangeTime))
                return;

            _trackedTargetChangeTime = targetChange;
            _armed = false;
            _announced = false;
            _vehicleArrivalAnnounced = false;
        }

        private static void AnnounceArrival(bool keepDestination)
        {
            if (!keepDestination)
                RememberCompletedTarget();
            AutoWalkService.PrepareForDestinationArrival();

            var target = NavigationTargetTracker.ActiveTarget;
            var source = NavigationTargetTracker.LastSource;
            if (!keepDestination)
                NavigationAutoEnterService.TryOnArrival(target, source);

            if (!_vehicleArrivalAnnounced || JobDestinationSync.ShouldPreserveDestinationOnArrival())
                ShowArrivalNotification();

            ModLog.Info(keepDestination
                ? "Vehicle destination reached; keeping GPS for walking."
                : "Navigation destination reached.");
            if (!keepDestination)
                NavigationDestinationClear.ClearActiveDestination("navigation_arrival");
        }

        private static void ShowArrivalNotification()
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

        private static Vector3 ResolveArrivalTarget()
        {
            return PathFinderService.TryGetEffectiveFootArrivalTarget(out var routeEnd)
                ? routeEnd
                : NavigationTargetTracker.ActiveTarget;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
