using UnityEngine;

namespace VoogleRoute.Navigation
{
    internal static class VehicleInputApplicator
    {
        internal readonly struct VehicleHandle
        {
            internal global::NWH.VehiclePhysics2.VehicleController Physics { get; }
            internal Transform Transform { get; }
            internal string VehicleKind { get; }

            internal VehicleHandle(
                global::NWH.VehiclePhysics2.VehicleController physics,
                Transform transform,
                string vehicleKind)
            {
                Physics = physics;
                Transform = transform;
                VehicleKind = vehicleKind;
            }

            internal float Speed => Physics != null ? Physics.Speed : 0f;

            internal bool TryGetKinematics(out Vector3 position, out Vector3 forward)
            {
                position = default;
                forward = Vector3.forward;

                var t = Transform;
                if (t == null)
                    return false;

                position = t.position;
                forward = t.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.01f)
                    return false;

                forward.Normalize();
                return true;
            }
        }

        private static global::NWH.VehiclePhysics2.VehicleController _activePhysics;
        private static bool _inputLocked;

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
                    var t = car.vehicleController.transform;
                    handle = new VehicleHandle(car.vehicleController, t, "CarController");
                    return true;
                }

                var physics = ((Component)vehicle).GetComponent<global::NWH.VehiclePhysics2.VehicleController>();
                if (physics?.input != null)
                {
                    handle = new VehicleHandle(physics, physics.transform, vehicle.GetType().Name);
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
                AutoDriveDiagnostics.LogStatusThrottled(
                    "input takeover | vehicle=" + handle.VehicleKind);
            }

            input.Throttle = command.Throttle;
            input.Brakes = command.Brakes;
            input.Steering = command.Steering;

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
            }
            catch
            {
                // ignore
            }

            _inputLocked = false;
            _activePhysics = null;
        }
    }
}
