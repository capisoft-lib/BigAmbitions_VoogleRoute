using UnityEngine;

namespace VoogleRoute.Navigation
{
    internal static class VehicleInputApplicator
    {
        internal readonly struct VehicleHandle
        {
            internal global::NWH.VehiclePhysics2.VehicleController Physics { get; }
            internal string VehicleKind { get; }

            internal VehicleHandle(global::NWH.VehiclePhysics2.VehicleController physics, string vehicleKind)
            {
                Physics = physics;
                VehicleKind = vehicleKind;
            }

            internal float Speed => Physics != null ? Physics.Speed : 0f;
        }

        private static global::NWH.VehiclePhysics2.VehicleController _activePhysics;
        private static bool _inputLocked;
        private static bool _loggedBinding;

        internal static bool TryGetPlayerVehicle(out VehicleHandle handle)
        {
            handle = default;

            try
            {
                if (!GameManager.IsInitialized)
                {
                    AutoDriveDiagnostics.LogBlockedOnce("GameManager not initialized");
                    return false;
                }

                var vehicle = GameManager.Instance?.selectedVehicle;
                if (vehicle == null)
                {
                    AutoDriveDiagnostics.LogBlockedOnce("no selectedVehicle");
                    return false;
                }

                if (!vehicle.controlledByPlayer)
                {
                    AutoDriveDiagnostics.LogBlockedOnce("vehicle not controlledByPlayer");
                    return false;
                }

                if (vehicle is CarController car && car.vehicleController?.input != null)
                {
                    handle = new VehicleHandle(car.vehicleController, "CarController");
                    return true;
                }

                var physics = ((Component)vehicle).GetComponent<global::NWH.VehiclePhysics2.VehicleController>();
                if (physics?.input != null)
                {
                    handle = new VehicleHandle(physics, vehicle.GetType().Name);
                    return true;
                }

                AutoDriveDiagnostics.LogBlockedOnce(
                    "no NWH VehicleController on " + vehicle.GetType().Name);
                return false;
            }
            catch (System.Exception ex)
            {
                AutoDriveDiagnostics.LogBlockedOnce("TryGetPlayerVehicle exception: " + ex.Message);
                return false;
            }
        }

        internal static void Apply(VehicleHandle handle, VehicleDriveCommand command)
        {
            var physics = handle.Physics;
            var input = physics?.input;
            if (input == null)
            {
                AutoDriveDiagnostics.LogBlockedOnce("vehicle input is null");
                return;
            }

            if (!_inputLocked || _activePhysics != physics)
            {
                input.autoSetInput = false;
                _inputLocked = true;
                _activePhysics = physics;
                _loggedBinding = false;
                AutoDriveDiagnostics.LogStatusThrottled(
                    "input takeover | vehicle=" + handle.VehicleKind);
            }

            if (!_loggedBinding)
            {
                _loggedBinding = true;
                AutoDriveDiagnostics.LogInputBinding(
                    input.autoSetInput,
                    true,
                    true,
                    true);
            }

            input.Throttle = command.Throttle;
            input.Brakes = command.Brakes;
            input.Steering = command.Steering;

            if (physics.steering != null)
                physics.steering.externallyAddedAngle = 0f;

            ManualVehicleInputDetector.SuppressBriefly();
        }

        internal static void Release()
        {
            if (!_inputLocked)
                return;

            try
            {
                if (_activePhysics?.input != null)
                    _activePhysics.input.autoSetInput = true;

                if (_activePhysics?.steering != null)
                    _activePhysics.steering.externallyAddedAngle = 0f;
            }
            catch
            {
                // ignore
            }

            _inputLocked = false;
            _activePhysics = null;
            _loggedBinding = false;
        }
    }
}
