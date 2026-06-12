using UnityEngine;

namespace VoogleRoute.Navigation
{
    internal static class VehicleDriveController
    {
        private const float MaxCruiseMps = 7.5f;
        private const float ArrivalSlowZoneMeters = 35f;
        private const float ArrivalStopMeters = 14f;
        private const float StanleyCrossGain = 1.35f;
        private const float StanleySoftSpeed = 2.2f;
        private const float MaxSteerRadians = 0.55f;

        private static float _lastSteering;

        internal static void Reset() => _lastSteering = 0f;

        internal static VehicleDriveCommand Compute(
            VehiclePathFollower.FollowState state,
            float speedMps,
            float obstacleBrake)
        {
            var targetSpeed = ComputeTargetSpeed(state, obstacleBrake);
            if (state.OffRoute)
                targetSpeed = Mathf.Min(targetSpeed, 3.5f);

            var throttle = 0f;
            var brakes = obstacleBrake;

            if (state.DistanceToDestination <= ArrivalStopMeters)
            {
                targetSpeed = Mathf.Min(targetSpeed, 2.5f);
                if (speedMps > 0.8f)
                    brakes = Mathf.Max(brakes, Mathf.Clamp01((speedMps - 0.5f) / 3.5f));
            }
            else if (speedMps < targetSpeed - 0.8f)
            {
                throttle = Mathf.Clamp01((targetSpeed - speedMps) / 4.5f);
            }
            else if (speedMps > targetSpeed + 0.5f)
            {
                brakes = Mathf.Max(brakes, Mathf.Clamp01((speedMps - targetSpeed) / 4f));
            }

            var headingRad = state.HeadingErrorDegrees * Mathf.Deg2Rad;
            var crossRad = Mathf.Atan(
                (StanleyCrossGain * state.SignedCrossTrackMeters) /
                (speedMps + StanleySoftSpeed));
            var steerRaw = (headingRad + crossRad) / MaxSteerRadians;
            steerRaw = Mathf.Clamp(steerRaw, -1f, 1f);

            if (Mathf.Abs(state.HeadingErrorDegrees) < 4f && state.CrossTrackMeters < 1.5f)
                steerRaw *= 0.35f;

            const float maxSteerDelta = 0.22f;
            var steering = Mathf.Clamp(
                Mathf.MoveTowards(_lastSteering, steerRaw, maxSteerDelta),
                -1f,
                1f);
            _lastSteering = steering;

            if (Mathf.Abs(state.HeadingErrorDegrees) > 55f && speedMps > 2.5f)
            {
                throttle = 0f;
                brakes = Mathf.Max(brakes, 0.55f);
            }

            if (obstacleBrake > 0.55f)
                throttle = 0f;

            return new VehicleDriveCommand(throttle, Mathf.Clamp01(brakes), steering);
        }

        private static float ComputeTargetSpeed(VehiclePathFollower.FollowState state, float obstacleBrake)
        {
            var turn = state.UpcomingTurnDegrees;
            var turnFactor = turn switch
            {
                <= 12f => 1f,
                <= 28f => 0.78f,
                <= 45f => 0.58f,
                <= 65f => 0.42f,
                _ => 0.28f
            };

            var target = MaxCruiseMps * turnFactor;

            if (state.DistanceToDestination < ArrivalSlowZoneMeters)
            {
                var arrivalFactor = Mathf.Clamp01(state.DistanceToDestination / ArrivalSlowZoneMeters);
                target = Mathf.Min(target, Mathf.Lerp(2.5f, MaxCruiseMps, arrivalFactor));
            }

            if (state.CrossTrackMeters > 2.5f)
                target = Mathf.Min(target, 5f);

            if (obstacleBrake > 0.05f)
                target = Mathf.Min(target, Mathf.Lerp(MaxCruiseMps, 2f, obstacleBrake));

            return target;
        }
    }
}
