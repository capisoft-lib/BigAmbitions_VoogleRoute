using Il2Cpp;
using Il2CppHelpers;
using UnityEngine;

namespace VoogleRoute.Navigation;

public enum MovementMode
{
    OnFoot,
    Vehicle,
    Subway,
    Unavailable
}

public static class MovementModeDetector
{
    public static MovementMode CurrentMode { get; private set; } = MovementMode.Unavailable;
    public static MovementMode PreviousMode { get; private set; } = MovementMode.Unavailable;

    public static void Tick()
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

            // IsUsingVehicle = conducteur actif (pas seulement selectedVehicle sur un parking).
            CurrentMode = PlayerHelper.IsUsingVehicle ? MovementMode.Vehicle : MovementMode.OnFoot;
        }
        catch
        {
            CurrentMode = MovementMode.Unavailable;
        }
    }

    public static bool ModeChangedSinceLastTick() =>
        CurrentMode != PreviousMode;

    public static bool IsDriving => CurrentMode == MovementMode.Vehicle;

    public static bool ShouldShowHudButton()
    {
        return CurrentMode is MovementMode.OnFoot or MovementMode.Vehicle;
    }

    public static bool TryGetPathOrigin(out Vector3 origin)
    {
        origin = default;
        if (CurrentMode == MovementMode.Subway || CurrentMode == MovementMode.Unavailable)
            return false;

        try
        {
            if (CurrentMode == MovementMode.Vehicle)
            {
                if (TryGetVehiclePose(out origin, out _))
                    return true;
            }

            return TryGetPlayerOrigin(out origin);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Position du joueur (secours si la voiture n'est pas sur le NavMesh piéton).</summary>
    public static bool TryGetPlayerOrigin(out Vector3 origin)
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

    public static bool TryGetVehiclePose(out Vector3 position, out Vector3 forward)
    {
        position = default;
        forward = Vector3.forward;
        try
        {
            var vehicle = GetCurrentVehicle();
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

    private static VehicleController? GetCurrentVehicle()
    {
        if (!GameManager.IsInitialized)
            return null;
        return GameManager.Instance?.selectedVehicle;
    }
}
