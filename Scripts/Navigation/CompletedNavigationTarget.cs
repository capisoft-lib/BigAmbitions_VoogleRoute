using UnityEngine;

namespace VoogleRoute.Navigation
{
    /// <summary>An arrival survives clearing the active route and transient gaps in external GPS updates.</summary>
    internal static class CompletedNavigationTarget
    {
        private const float ExitMarginMeters = 5f;
        private static bool _hasCompletedTarget;
        private static Vector3 _target;
        private static Vector3 _arrivalPosition;
        private static float _exitRadius;
        private static object _mission;

        internal static void Reset() => _hasCompletedTarget = false;

        internal static void Remember(Vector3 target, Vector3 arrivalPosition, float arrivalRadius)
        {
            _target = target;
            _arrivalPosition = arrivalPosition;
            _exitRadius = arrivalRadius + ExitMarginMeters;
            _mission = SaveGameManager.Current?.currentPlayerMission;
            _hasCompletedTarget = true;
        }

        // Also called with no active GPS, so leaving and returning during a gap rearms arrival.
        internal static void ObservePlayer()
        {
            if (!_hasCompletedTarget)
                return;

            if (!ReferenceEquals(_mission, SaveGameManager.Current?.currentPlayerMission))
            {
                Reset();
                return;
            }

            if (MovementModeDetector.TryGetPathOrigin(out var position) &&
                HorizontalDistanceSquared(position, _arrivalPosition) > _exitRadius * _exitRadius)
                Reset();
        }

        internal static bool ShouldSuppress(Vector3 target)
        {
            ObservePlayer();
            if (!_hasCompletedTarget)
                return false;

            // Map GPS and job guider can expose the same stop under different source names.
            if ((target - _target).sqrMagnitude < 0.25f)
                return true;

            Reset();
            return false;
        }

        private static float HorizontalDistanceSquared(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0f;
            return (a - b).sqrMagnitude;
        }
    }
}
