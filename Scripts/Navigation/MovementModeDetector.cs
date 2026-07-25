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
        internal static MovementMode CurrentMode { get; private set; } = MovementMode.Unavailable;
        internal static MovementMode PreviousMode { get; private set; } = MovementMode.Unavailable;

        internal static void Reset()
        {
            CurrentMode = MovementMode.Unavailable;
            PreviousMode = MovementMode.Unavailable;
        }

        internal static void Apply(PlayerLocationSnapshot snapshot)
        {
            PreviousMode = CurrentMode;
            var reportedMode = snapshot.IsAvailable
                ? PlayerLocationSnapshotMapper.ToMovementMode(snapshot.MovementKind)
                : MovementMode.Unavailable;
            CurrentMode = reportedMode == MovementMode.Vehicle && IsPushingPlayerCargoVehicle()
                ? MovementMode.OnFoot
                : reportedMode;

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
            if (!snapshot.IsAvailable || snapshot.MovementKind != MovementKind.Car)
                return false;

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
    }
}
