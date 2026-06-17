using UnityEngine;
using VoogleRoute.Live;

namespace VoogleRoute.Navigation
{
    /// <summary>Hides outdoor route display when the player is already near the active destination.</summary>
    internal static class NavigationProximityService
    {
        internal const float FootNearDestinationMeters = 12f;
        internal const float VehicleNearDestinationMeters = 25f;

        internal static bool IsNearActiveDestination()
        {
            if (!NavigationTargetTracker.HasMapGpsTarget)
                return false;

            var destination = NavigationTargetTracker.ActiveTarget;
            if (destination.sqrMagnitude < 0.01f)
                return false;

            if (!TryGetHorizontalPosition(out var position))
                return false;

            return HorizontalDistance(position, destination) <= GetNearRadius();
        }

        internal static float GetNearRadius() =>
            MovementModeDetector.CurrentMode == MovementMode.Vehicle
                ? VehicleNearDestinationMeters
                : FootNearDestinationMeters;

        private static bool TryGetHorizontalPosition(out Vector3 position)
        {
            if (MovementModeDetector.TryGetPathOrigin(out position))
                return true;

            if (!PlayerLocationSession.IsAvailable)
                return false;

            position = PlayerLocationSession.Snapshot.Position;
            return position.sqrMagnitude > 0.01f;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
