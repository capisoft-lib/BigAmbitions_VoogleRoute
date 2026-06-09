using BaPlayerLocation.Subscriber;
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
            CurrentMode = snapshot.IsAvailable
                ? PlayerLocationSnapshotMapper.ToMovementMode(snapshot.MovementKind)
                : MovementMode.Unavailable;
        }

        internal static bool ModeChangedSinceLastApply => CurrentMode != PreviousMode;

        internal static bool ShouldShowHudButton() =>
            CurrentMode == MovementMode.OnFoot || CurrentMode == MovementMode.Vehicle;

        internal static bool TryGetPathOrigin(out Vector3 origin)
        {
            origin = default;
            if (!PlayerLocationSession.IsAvailable)
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

            if (snapshot.MovementKind is not (MovementKind.Walk or MovementKind.Indoor))
                return false;

            origin = snapshot.Position;
            return origin.sqrMagnitude > 0.01f;
        }
    }
}
