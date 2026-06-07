using Helpers;
using UnityEngine;
using VoogleRoute;

namespace VoogleRoute.Navigation
{
    
    internal enum MovementMode
    {
        OnFoot,
        Vehicle,
        Subway,
        Unavailable
    }
    
    internal static class MovementModeDetector
    {
        internal static MovementMode CurrentMode { get; private set; } = MovementMode.Unavailable;
        internal static MovementMode PreviousMode { get; private set; } = MovementMode.Unavailable;
    
        internal static void Tick()
        {
            PreviousMode = CurrentMode;
    
            if (SubwaySystem.IsRiding)
            {
                CurrentMode = MovementMode.Subway;
                return;
            }
    
            try
            {
                if (!GameManager.IsInitialized || GameManager.Instance?.playerController == null)
                {
                    CurrentMode = MovementMode.Unavailable;
                    return;
                }
    
                CurrentMode = PlayerHelper.IsUsingVehicle ? MovementMode.Vehicle : MovementMode.OnFoot;
            }
            catch
            {
                CurrentMode = MovementMode.Unavailable;
            }
        }
    
        internal static bool ModeChangedSinceLastTick() => CurrentMode != PreviousMode;
    
        internal static bool ShouldShowHudButton() =>
            CurrentMode == MovementMode.OnFoot || CurrentMode == MovementMode.Vehicle;
    
        internal static bool TryGetPathOrigin(out Vector3 origin)
        {
            origin = default;
            if (CurrentMode == MovementMode.Subway || CurrentMode == MovementMode.Unavailable)
                return false;

            if (CurrentMode == MovementMode.Vehicle)
                return TryGetVehiclePose(out origin, out _);

            return TryGetPlayerOrigin(out origin);
        }

        internal static bool TryGetVehiclePose(out Vector3 position, out Vector3 forward)
        {
            position = default;
            forward = Vector3.forward;
            try
            {
                var vehicle = GameManager.Instance?.selectedVehicle;
                if (vehicle == null)
                    return false;

                position = vehicle.FrontPoint;
                forward = vehicle.transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.01f)
                    forward = vehicle.transform.forward;
                forward.Normalize();
                return true;
            }
            catch
            {
                return false;
            }
        }
    
        internal static bool TryGetPlayerOrigin(out Vector3 origin)
        {
            origin = default;
            try
            {
                var player = PlayerHelper.PlayerController;
                if (player == null)
                    return false;
    
                origin = player.transform.position;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
