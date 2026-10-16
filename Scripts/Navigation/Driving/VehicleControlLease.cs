using System;
using Helpers;
using UnityEngine;

namespace VoogleRoute.Navigation.Driving
{
    /// <summary>Owns only the occupied car's input. No transform, fuel or clock manipulation.</summary>
    internal sealed class VehicleControlLease : IDisposable
    {
        internal readonly CarController Car;
        internal readonly NWH.VehiclePhysics2.VehicleController Vehicle;
        internal readonly Rigidbody Body;
        internal readonly float Wheelbase, RearAxleZ, FrontZ, HalfWidth, MaxSteer;
        private readonly bool _autoInput, _swapReverse, _rawSteering;
        private readonly float _externalAngle;
        private readonly bool _ownsInput;
        private bool _disposed;

        internal VehicleControlLease(CarController car, bool acquire = true)
        {
            Car = car;
            Vehicle = car.vehicleController;
            Body = Vehicle.GetComponent<Rigidbody>();
            if (Body == null || Vehicle.input == null || !Vehicle.input.autoSetInput)
                throw new InvalidOperationException("Car input is unavailable or already owned by another driver.");

            var rear = float.PositiveInfinity;
            var front = float.NegativeInfinity;
            var width = 0.65f;
            foreach (var wheel in Vehicle.powertrain.wheels)
            {
                if (wheel?.wheelUAPI == null) continue;
                var local = Vehicle.transform.InverseTransformPoint(wheel.wheelUAPI.transform.position);
                rear = Mathf.Min(rear, local.z);
                front = Mathf.Max(front, local.z);
                width = Mathf.Max(width, Mathf.Abs(local.x));
            }
            Wheelbase = front - rear;
            if (Wheelbase < 1.2f || Wheelbase > 12f)
                throw new InvalidOperationException("Cannot resolve this car's wheelbase.");
            RearAxleZ = rear;
            FrontZ = front + Mathf.Clamp(Wheelbase * 0.32f, 0.7f, 1.7f);
            HalfWidth = width + 0.25f;
            MaxSteer = Mathf.Clamp(Vehicle.steering.maximumSteerAngle, 10f, 60f);
            if (!acquire) return;
            _autoInput = Vehicle.input.autoSetInput;
            _swapReverse = Vehicle.input.swapInputInReverse;
            _rawSteering = Vehicle.steering.useRawInput;
            _externalAngle = Vehicle.steering.externallyAddedAngle;
            _ownsInput = true;
            try
            {
                Vehicle.input.autoSetInput = false;
                Vehicle.input.swapInputInReverse = false;
                // Our controller supplies a wheel angle and limits its rate itself.
                Vehicle.steering.useRawInput = true;
                Hold();
            }
            catch { Dispose(); throw; }
        }

        internal bool IsCurrent => !_disposed && Car != null && Vehicle != null && Body != null &&
            VehicleHelper.GetCurrentVehicleBase() == Car && MovementModeDetector.CanUseAutoDrive();
        internal Vector3 RearPosition => Vehicle.transform.TransformPoint(new Vector3(0f, 0f, RearAxleZ));
        internal float Speed => DrivingPath.Flat(Body.velocity).magnitude;
        internal float MinRadius => Wheelbase / Mathf.Tan(MaxSteer * Mathf.Deg2Rad);

        internal void Write(float steering, float throttle, float brake)
        {
            if (_disposed || !_ownsInput || Vehicle == null) return;
            ClearAssist();
            Vehicle.input.Steering = Mathf.Clamp(steering, -1f, 1f);
            Vehicle.input.Throttle = Mathf.Clamp01(throttle);
            Vehicle.input.Brakes = Mathf.Clamp01(brake);
            // The handbrake prevents NWH's brake-to-reverse logic at a red light.
            Vehicle.input.Handbrake = brake > 0.5f && Speed < 0.3f ? 1f : 0f;
            if (throttle > 0f && Vehicle.powertrain.transmission.Gear <= 0)
                Vehicle.powertrain.transmission.ShiftInto(1, true);
            if (throttle > 0f && !Vehicle.powertrain.engine.IsRunning)
                Vehicle.powertrain.engine.StartEngine();
        }

        internal void Hold() => Write(0f, 0f, 1f);
        internal void ClearAssist()
        {
            if (!_disposed && _ownsInput && Vehicle != null) Vehicle.steering.externallyAddedAngle = 0f;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (!_ownsInput || Vehicle == null) return;
            Vehicle.input.Throttle = 0f;
            Vehicle.input.Brakes = 0f;
            Vehicle.input.Steering = 0f;
            Vehicle.input.Handbrake = 0f;
            Vehicle.input.swapInputInReverse = _swapReverse;
            Vehicle.steering.useRawInput = _rawSteering;
            Vehicle.steering.externallyAddedAngle = _externalAngle;
            Vehicle.input.autoSetInput = _autoInput;
        }
    }
}
