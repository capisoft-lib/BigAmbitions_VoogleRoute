using BaPlayerLocation.Subscriber;
using Helpers;
using UnityEngine;
using VoogleRoute;

namespace VoogleRoute.Navigation
{
    /// <summary>Remembers the last vehicle position when the player exits on foot.</summary>
    internal static class ParkedVehicleStore
    {
        internal const string NavigationSource = NavigationTargetTracker.ParkedVehicleSource;

        private static Vector3 _lastVehiclePosition;
        private static Vector3 _parkedPosition;
        private static bool _hasParkedPosition;

        internal static bool HasParkedPosition => _hasParkedPosition;

        internal static Vector3 ParkedPosition => _parkedPosition;

        internal static void OnMovementModeApplied(
            MovementMode previous,
            MovementMode current,
            in PlayerLocationSnapshot snapshot)
        {
            if (current == MovementMode.Vehicle &&
                snapshot.IsAvailable &&
                snapshot.MovementKind == MovementKind.Car)
            {
                if (TryGetDriverEntranceFromSelectedVehicle(out var driverPos))
                    _lastVehiclePosition = driverPos;
                else
                    _lastVehiclePosition = snapshot.Position;
            }

            if (previous != MovementMode.Vehicle || current != MovementMode.OnFoot)
                return;

            if (TryGetVehiclePositionOnExit(out var exitPosition))
                RecordParkedPosition(exitPosition);
        }

        internal static void LoadFromConfig(BookmarkConfigEntry entry)
        {
            _hasParkedPosition = false;
            _parkedPosition = default;
            _lastVehiclePosition = default;

            if (entry == null)
                return;

            var position = new Vector3(entry.WorldX, entry.WorldY, entry.WorldZ);
            if (position.sqrMagnitude < 0.01f)
                return;

            _parkedPosition = position;
            _hasParkedPosition = true;
        }

        internal static void Clear()
        {
            _hasParkedPosition = false;
            _parkedPosition = default;
            _lastVehiclePosition = default;
        }

        private static void RecordParkedPosition(Vector3 position)
        {
            if (position.sqrMagnitude < 0.01f)
                return;

            _parkedPosition = position;
            _hasParkedPosition = true;
            ModLog.Info("Parked vehicle position saved: " + position);
            QuickBookmarkStore.OnVehicleParked();
        }

        private static bool TryGetVehiclePositionOnExit(out Vector3 position)
        {
            if (_lastVehiclePosition.sqrMagnitude > 0.01f)
            {
                position = _lastVehiclePosition;
                return true;
            }

            try
            {
                var vehicle = GameManager.Instance?.selectedVehicle;
                if (vehicle != null && VehicleEntranceHelper.TryGetDriverEntrancePosition(vehicle, out position))
                    return true;
            }
            catch
            {
                // ignore
            }

            position = default;
            return false;
        }

        private static bool TryGetDriverEntranceFromSelectedVehicle(out Vector3 position)
        {
            position = default;
            try
            {
                var vehicle = GameManager.Instance?.selectedVehicle;
                return vehicle != null && VehicleEntranceHelper.TryGetDriverEntrancePosition(vehicle, out position);
            }
            catch
            {
                return false;
            }
        }
    }
}
