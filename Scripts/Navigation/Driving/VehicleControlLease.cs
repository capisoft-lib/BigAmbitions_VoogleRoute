using System;
using Helpers;
using UnityEngine;
using NWH.VehiclePhysics2.Powertrain;
using NWH.VehiclePhysics2.Modules.SpeedLimiter;

namespace VoogleRoute.Navigation.Driving
{
    /// <summary>Owns only the occupied car's input. No transform, fuel or clock manipulation.</summary>
    internal sealed class VehicleControlLease : IDisposable
    {
        internal readonly CarController Car;
        internal readonly NWH.VehiclePhysics2.VehicleController Vehicle;
        internal readonly Rigidbody Body;
        internal readonly float Wheelbase, RearAxleZ, FrontZ, HalfWidth, MaxSteer, MaxSpeedKmh;
        private readonly bool _autoInput, _swapReverse, _rawSteering;
        private readonly float _externalAngle;
        private readonly bool _ownsInput;
        private readonly TransmissionComponent.Shift _originalShift;
        private readonly bool _controlsGears;
        private static readonly TransmissionComponent.Shift DriverShift = IgnoreNativeShift;
        private float _nextShiftAt;
        private int _lastGear;
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
            MaxSpeedKmh = car.vehicleType != null && car.vehicleType.maxSpeed > 0 ? car.vehicleType.maxSpeed : DrivingSpeedLimits.MaximumKmh;
            var limiter = car.GetComponent<SpeedLimiterModuleWrapper>();
            if (limiter != null && limiter.module.speedLimit > 0f) MaxSpeedKmh = Mathf.Min(MaxSpeedKmh, limiter.module.speedLimit);
            if (!acquire) return;
            _autoInput = Vehicle.input.autoSetInput;
            _swapReverse = Vehicle.input.swapInputInReverse;
            _rawSteering = Vehicle.steering.useRawInput;
            _externalAngle = Vehicle.steering.externallyAddedAngle;
            var transmission = Vehicle.powertrain.transmission;
            _originalShift = transmission.shiftDelegate;
            _controlsGears = transmission.forwardGearCount > 1 &&
                transmission.transmissionType != TransmissionComponent.TransmissionShiftType.CVT;
            _lastGear = transmission.Gear;
            _ownsInput = true;
            try
            {
                Vehicle.input.autoSetInput = false;
                Vehicle.input.swapInputInReverse = false;
                // Our controller supplies a wheel angle and limits its rate itself.
                Vehicle.steering.useRawInput = true;
                if (_controlsGears) transmission.shiftDelegate = DriverShift;
                Hold();
            }
            catch { Dispose(); throw; }
        }

        internal bool IsCurrent => !_disposed && Car != null && Vehicle != null && Body != null &&
            VehicleHelper.GetCurrentVehicleBase() == Car && MovementModeDetector.CanUseAutoDrive();
        internal Vector3 RearPosition => Vehicle.transform.TransformPoint(new Vector3(0f, 0f, RearAxleZ));
        internal float Speed => DrivingPath.Flat(Body.velocity).magnitude;
        internal float MinRadius => Wheelbase / Mathf.Tan(MaxSteer * Mathf.Deg2Rad);
        internal int Gear => Vehicle.powertrain.transmission.Gear;
        internal float Rpm => Mathf.Abs(Vehicle.powertrain.engine.OutputRPM);

        internal void Write(float steering, float throttle, float brake)
        {
            if (_disposed || !_ownsInput || Vehicle == null) return;
            ClearAssist();
            Vehicle.input.Steering = Mathf.Clamp(steering, -1f, 1f);
            Vehicle.input.Throttle = Mathf.Clamp01(throttle);
            Vehicle.input.Brakes = Mathf.Clamp01(brake);
            // The handbrake prevents NWH's brake-to-reverse logic at a red light.
            Vehicle.input.Handbrake = brake > 0.5f && Speed < 0.3f ? 1f : 0f;
            if (throttle > 0f && !Vehicle.powertrain.engine.IsRunning)
                Vehicle.powertrain.engine.StartEngine();
            UpdateGears(throttle, brake);
        }

        private static void IgnoreNativeShift(NWH.VehiclePhysics2.VehicleController vehicle) { }

        private void UpdateGears(float throttle, float brake)
        {
            var transmission = Vehicle.powertrain.transmission;
            var gear = transmission.Gear;
            if (gear > 0 && gear != _lastGear && !transmission.isShifting)
            {
                _lastGear = gear;
                DrivingPerformance.Event("gear_engaged gear=" + gear + " rpm=" + Mathf.RoundToInt(Rpm));
            }
            if (transmission.isShifting || transmission.isPostShiftBanActive || Time.time < _nextShiftAt) return;
            if (throttle > 0f && gear <= 0) { ShiftTo(1, instant: true); return; }
            if (!_controlsGears || gear <= 0 || !Vehicle.powertrain.engine.IsRunning) return;
            if (Speed < 0.5f && gear > 1) { ShiftTo(1); return; }
            var engine = Vehicle.powertrain.engine;
            var idle = Mathf.Max(400f, engine.idleRPM);
            var redline = Mathf.Max(idle * 3f, engine.revLimiterRPM);
            var configuredUp = transmission.UpshiftRPM > idle ? transmission.UpshiftRPM : redline * 0.65f;
            var up = Mathf.Clamp(configuredUp, idle * 2f, redline * 0.85f);
            up = Mathf.Lerp(Mathf.Max(idle * 1.8f, up * 0.7f), up, Mathf.Clamp01(throttle));
            var down = Mathf.Clamp(transmission.DownshiftRPM, idle * 1.15f, up * 0.6f);
            var currentRatio = Mathf.Abs(transmission.GetGearRatio(gear));
            if (currentRatio < 0.001f) return;
            if (gear < transmission.forwardGearCount && brake < 0.1f && Rpm >= up)
            {
                var nextRpm = Rpm * Mathf.Abs(transmission.GetGearRatio(gear + 1)) / currentRatio;
                if (nextRpm > down * 1.15f) { ShiftTo(gear + 1); return; }
            }
            if (gear > 1 && (Rpm < down || (throttle > 0.85f && Rpm < up * 0.55f)))
            {
                var lowerRpm = Rpm * Mathf.Abs(transmission.GetGearRatio(gear - 1)) / currentRatio;
                if (lowerRpm < Mathf.Min(redline * 0.85f, up * 0.9f)) ShiftTo(gear - 1);
            }
        }

        private void ShiftTo(int gear, bool instant = false)
        {
            var transmission = Vehicle.powertrain.transmission;
            DrivingPerformance.Event("gear_request from=" + transmission.Gear + " to=" + gear + " rpm=" + Mathf.RoundToInt(Rpm));
            transmission.ShiftInto(gear, instant);
            _nextShiftAt = Time.time + Mathf.Max(0.8f, transmission.shiftDuration + transmission.postShiftBan);
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
            if (_controlsGears) Vehicle.powertrain.transmission.shiftDelegate = _originalShift;
            Vehicle.input.autoSetInput = _autoInput;
        }
    }
}
