using BaPlayerLocation.Subscriber;
using UnityEngine;

namespace VoogleRoute.Navigation
{
    /// <summary>Releases the previous navigation goal independently of route/UI visibility.</summary>
    internal static class VehicleExitNavigationRelease
    {
        private const float RetryWindowSeconds = 2f;
        private const float RetryIntervalSeconds = 0.1f;
        private static bool _wasInVehicle;
        private static bool _pending;
        private static float _deadline;
        private static float _nextAttempt;

        internal static void Reset()
        {
            _wasInVehicle = false;
            _pending = false;
        }

        internal static void OnLocationApplied(PlayerLocationSnapshot snapshot)
        {
            // Keep the last available mode across a short missing-pose transition.
            if (!snapshot.IsAvailable)
                return;

            var mode = MovementModeDetector.CurrentMode;
            if (mode != MovementMode.OnFoot)
            {
                _wasInVehicle = mode == MovementMode.Vehicle;
                _pending = false;
                return;
            }

            if (_wasInVehicle)
            {
                _wasInVehicle = false;
                _pending = true;
                _deadline = Time.unscaledTime + RetryWindowSeconds;
                _nextAttempt = Time.unscaledTime;
            }

            Tick();
        }

        internal static void Tick()
        {
            if (!_pending)
                return;

            var now = Time.unscaledTime;
            if (now >= _deadline)
            {
                _pending = false;
                ModLog.Error("Could not release previous navigation after vehicle exit.");
                return;
            }

            if (now < _nextAttempt || !PlayerLocationSession.IsAvailable)
                return;

            if (MovementModeDetector.CurrentMode != MovementMode.OnFoot)
            {
                _pending = false;
                return;
            }

            _nextAttempt = now + RetryIntervalSeconds;
            if (!PlayerNavigationRelease.TryRelease())
                return;

            _pending = false;
            ModLog.Info("Released mod navigation after exiting vehicle.");
        }
    }
}
