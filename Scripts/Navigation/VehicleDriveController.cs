using UnityEngine;

namespace VoogleRoute.Navigation
{
    internal static class VehicleDriveController
    {
        private const float MaxCruiseMps = 12f;
        private const float ArrivalSlowZoneMeters = 35f;
        private const float ArrivalStopMeters = 14f;

        private static float _lastSteering;

        internal static void Reset() => _lastSteering = 0f;

        internal static VehicleDriveCommand Compute(VehiclePathFollower.FollowState state, float speedMps)
        {
            if (state.OffRoute)
                return VehicleDriveCommand.Stop;

            var targetSpeed = ComputeTargetSpeed(state);
            var throttle = 0f;
            var brakes = 0f;

            if (state.DistanceToDestination <= ArrivalStopMeters)
            {
                if (speedMps > 0.8f)
                    brakes = Mathf.Clamp01((speedMps - 0.5f) / 4f);
                else
                    brakes = speedMps > 0.15f ? 0.35f : 0f;
            }
            else if (speedMps < targetSpeed - 1.2f)
            {
                throttle = Mathf.Clamp01((targetSpeed - speedMps) / 7f);
            }
            else if (speedMps > targetSpeed + 0.8f)
            {
                brakes = Mathf.Clamp01((speedMps - targetSpeed) / 5f);
            }

            var steerRaw = state.HeadingErrorDegrees * 0.028f +
                           Mathf.Clamp(state.CrossTrackMeters * 0.04f, 0f, 8f) *
                           Mathf.Sign(state.HeadingErrorDegrees);
            steerRaw = Mathf.Clamp(steerRaw, -1f, 1f);

            const float maxSteerDelta = 0.12f;
            var steering = Mathf.Clamp(
                Mathf.MoveTowards(_lastSteering, steerRaw, maxSteerDelta),
                -1f,
                1f);
            _lastSteering = steering;

            if (Mathf.Abs(state.HeadingErrorDegrees) > 75f && speedMps > 3f)
            {
                throttle = 0f;
                brakes = Mathf.Max(brakes, 0.45f);
            }

            return new VehicleDriveCommand(throttle, brakes, steering);
        }

        private static float ComputeTargetSpeed(VehiclePathFollower.FollowState state)
        {
            var turn = state.UpcomingTurnDegrees;
            var turnFactor = turn switch
            {
                <= 15f => 1f,
                <= 35f => 0.82f,
                <= 55f => 0.62f,
                <= 75f => 0.45f,
                _ => 0.3f
            };

            var target = MaxCruiseMps * turnFactor;

            if (state.DistanceToDestination < ArrivalSlowZoneMeters)
            {
                var arrivalFactor = Mathf.Clamp01(state.DistanceToDestination / ArrivalSlowZoneMeters);
                target = Mathf.Min(target, Mathf.Lerp(3f, MaxCruiseMps, arrivalFactor));
            }

            if (state.CrossTrackMeters > 4f)
                target = Mathf.Min(target, 7f);

            return target;
        }
    }
}
