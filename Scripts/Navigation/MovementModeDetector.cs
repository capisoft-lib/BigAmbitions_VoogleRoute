using BaPlayerLocation.Subscriber;
using Helpers;
using UnityEngine;
using VoogleRoute.Live;

namespace VoogleRoute.Navigation
{
    internal enum MovementMode
    {
        OnFoot,
        Vehicle,
        Subway,
        Unavailable
    }

    /// <summary>Movement mode and path origin derived from <see cref="PlayerLocationSession"/> only.</summary>
    internal static class MovementModeDetector
    {
        private static MovementMode _reportedMode = MovementMode.Unavailable;
        private static int _hamptonsVehicleCacheFrame = -1;
        private static bool _hamptonsVehicleCacheValue;

        internal static MovementMode CurrentMode =>
            IsHamptonsVehicleNavigationContext() ? MovementMode.Vehicle : _reportedMode;
        internal static MovementMode PreviousMode { get; private set; } = MovementMode.Unavailable;

        internal static void Reset()
        {
            _reportedMode = MovementMode.Unavailable;
            PreviousMode = MovementMode.Unavailable;
            InvalidateHamptonsVehicleCache();
        }

        internal static void Apply(PlayerLocationSnapshot snapshot)
        {
            var previousMode = CurrentMode;
            var reportedMode = snapshot.IsAvailable
                ? PlayerLocationSnapshotMapper.ToMovementMode(snapshot.MovementKind)
                : MovementMode.Unavailable;
            _reportedMode = reportedMode == MovementMode.Vehicle && IsPushingPlayerCargoVehicle()
                ? MovementMode.OnFoot
                : reportedMode;
            InvalidateHamptonsVehicleCache();
            PreviousMode = previousMode;

            ParkedVehicleStore.OnMovementModeApplied(PreviousMode, CurrentMode, in snapshot);
        }

        internal static bool ModeChangedSinceLastApply => CurrentMode != PreviousMode;

        internal static bool ShouldShowActionPanel() =>
            CurrentMode == MovementMode.OnFoot || CurrentMode == MovementMode.Vehicle;

        internal static bool CanUseAutoDrive() =>
            CurrentMode == MovementMode.Vehicle && !IsPushingPlayerCargoVehicle();

        /// <summary>On foot or pushing hand truck / flatbed (spawnInPlayerObject cargo).</summary>
        internal static bool IsEffectivelyOnFootForNavigation()
        {
            if (CurrentMode == MovementMode.OnFoot)
                return true;

            return IsPushingPlayerCargoVehicle();
        }

        internal static bool IsPushingPlayerCargoVehicle()
        {
            try
            {
                var controller = VehicleHelper.GetCurrentVehicleBase();
                if (controller?.vehicleType != null && controller.vehicleType.spawnInPlayerObject)
                    return true;
            }
            catch
            {
                // ignore
            }

            return false;
        }

        /// <summary>
        /// Hamptons plots are represented by the game as an indoor building even
        /// while the player is driving through the open-world parcel. Keep this
        /// override local to real motor vehicles in those plots.
        /// </summary>
        internal static bool IsHamptonsVehicleNavigationContext()
        {
            var frame = Time.frameCount;
            if (_hamptonsVehicleCacheFrame == frame)
                return _hamptonsVehicleCacheValue;

            _hamptonsVehicleCacheFrame = frame;
            _hamptonsVehicleCacheValue = false;

            try
            {
                if (!HamptonsCompatibility.TryGetCurrentHouseId(out _))
                    return false;

                var controller = VehicleHelper.GetCurrentVehicleBase();
                _hamptonsVehicleCacheValue =
                    controller?.vehicleType != null && !controller.vehicleType.spawnInPlayerObject;
            }
            catch
            {
                _hamptonsVehicleCacheValue = false;
            }

            return _hamptonsVehicleCacheValue;
        }

        [System.Obsolete("Use ShouldShowActionPanel.")]
        internal static bool ShouldShowHudButton() => ShouldShowActionPanel();

        internal static bool TryGetPathOrigin(out Vector3 origin)
        {
            origin = default;
            if (!PlayerLocationSession.IsLibraryActive || !PlayerLocationSession.IsAvailable)
                return false;

            return CurrentMode switch
            {
                MovementMode.Vehicle => TryGetVehiclePose(out origin, out _),
                MovementMode.OnFoot => TryGetPlayerOrigin(out origin),
                _ => false
            };
        }

        internal static bool TryGetVehiclePose(out Vector3 position, out Vector3 forward)
        {
            position = default;
            forward = Vector3.forward;

            var snapshot = PlayerLocationSession.Snapshot;
            if (!snapshot.IsAvailable)
                return false;

            if (snapshot.MovementKind != MovementKind.Car)
                return TryGetHamptonsVehiclePose(out position, out forward);

            position = snapshot.Position;
            return PlayerLocationSnapshotMapper.TryGetForward(
                snapshot.MovementKind,
                snapshot.HeadingDeg,
                out forward);
        }

        internal static bool TryGetPlayerOrigin(out Vector3 origin)
        {
            origin = default;

            var snapshot = PlayerLocationSession.Snapshot;
            if (!snapshot.IsAvailable)
                return false;

            var cargoToolReportedAsCar =
                snapshot.MovementKind == MovementKind.Car && IsPushingPlayerCargoVehicle();
            if (snapshot.MovementKind is not (MovementKind.Walk or MovementKind.Indoor) &&
                !cargoToolReportedAsCar)
                return false;

            if (cargoToolReportedAsCar)
            {
                var player = PlayerHelper.PlayerController;
                if (player != null)
                {
                    origin = player.transform.position;
                    return origin.sqrMagnitude > 0.01f;
                }
            }

            origin = snapshot.Position;
            return origin.sqrMagnitude > 0.01f;
        }

        private static bool TryGetHamptonsVehiclePose(out Vector3 position, out Vector3 forward)
        {
            position = default;
            forward = Vector3.forward;
            if (!IsHamptonsVehicleNavigationContext())
                return false;

            try
            {
                var controller = VehicleHelper.GetCurrentVehicleBase();
                if (controller == null)
                    return false;

                position = controller.FrontPoint;
                forward = controller.transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude > 0.01f)
                    forward.Normalize();
                else
                    forward = Vector3.forward;

                return position.sqrMagnitude > 0.01f;
            }
            catch
            {
                position = default;
                forward = Vector3.forward;
                return false;
            }
        }

        private static void InvalidateHamptonsVehicleCache()
        {
            _hamptonsVehicleCacheFrame = -1;
            _hamptonsVehicleCacheValue = false;
        }
    }
}
