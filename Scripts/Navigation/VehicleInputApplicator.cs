using System.Reflection;
using UnityEngine;

namespace VoogleRoute.Navigation
{
    internal static class VehicleInputApplicator
    {
        private static readonly BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.Instance;

        private static CarController _activeCar;
        private static bool _inputLocked;
        private static PropertyInfo _autoSetInputProp;
        private static PropertyInfo _throttleProp;
        private static PropertyInfo _brakesProp;
        private static PropertyInfo _steeringProp;
        private static PropertyInfo _externallyAddedAngleProp;

        internal static bool TryGetPlayerCar(out CarController car)
        {
            car = null;

            try
            {
                if (!GameManager.IsInitialized)
                    return false;

                var vehicle = GameManager.Instance?.selectedVehicle;
                if (vehicle == null || !vehicle.controlledByPlayer)
                    return false;

                car = vehicle as CarController;
                return car != null && car.vehicleController?.input != null;
            }
            catch
            {
                return false;
            }
        }

        internal static void Apply(CarController car, VehicleDriveCommand command)
        {
            var physics = car.vehicleController;
            if (physics == null)
                return;

            var input = physics.input;
            if (input == null)
                return;

            if (!_inputLocked || _activeCar != car)
            {
                CacheInputReflection(input.GetType(), physics.steering?.GetType());
                SetBool(input, _autoSetInputProp, false);
                _inputLocked = true;
                _activeCar = car;
            }

            SetFloat(input, _throttleProp, command.Throttle);
            SetFloat(input, _brakesProp, command.Brakes);
            SetFloat(input, _steeringProp, command.Steering);

            if (physics.steering != null)
                SetFloat(physics.steering, _externallyAddedAngleProp, 0f);

            ManualVehicleInputDetector.SuppressBriefly();
        }

        internal static void Release()
        {
            if (!_inputLocked)
                return;

            try
            {
                if (_activeCar?.vehicleController?.input != null)
                    SetBool(_activeCar.vehicleController.input, _autoSetInputProp, true);

                if (_activeCar?.vehicleController?.steering != null)
                    SetFloat(_activeCar.vehicleController.steering, _externallyAddedAngleProp, 0f);
            }
            catch
            {
                // ignore
            }

            _inputLocked = false;
            _activeCar = null;
        }

        private static void CacheInputReflection(System.Type inputType, System.Type steeringType)
        {
            _autoSetInputProp ??= inputType.GetProperty("autoSetInput", InstanceFlags);
            _throttleProp ??= inputType.GetProperty("Throttle", InstanceFlags);
            _brakesProp ??= inputType.GetProperty("Brakes", InstanceFlags);
            _steeringProp ??= inputType.GetProperty("Steering", InstanceFlags);

            if (steeringType != null)
                _externallyAddedAngleProp ??= steeringType.GetProperty("externallyAddedAngle", InstanceFlags);
        }

        private static void SetFloat(object target, PropertyInfo prop, float value)
        {
            if (target == null || prop == null)
                return;

            prop.SetValue(target, value);
        }

        private static void SetBool(object target, PropertyInfo prop, bool value)
        {
            if (target == null || prop == null)
                return;

            prop.SetValue(target, value);
        }
    }
}
