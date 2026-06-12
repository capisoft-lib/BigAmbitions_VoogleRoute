using UnityEngine;

namespace VoogleRoute.Navigation
{
    internal static class VehicleObstacleProbe
    {
        private static readonly Vector3 BoxHalfExtents = new Vector3(0.95f, 0.55f, 1.1f);
        private const float MinProbeMeters = 5f;
        private const float MaxProbeMeters = 22f;

        internal static float ComputeBrakeRequest(Transform vehicleTransform, float speedMps)
        {
            if (vehicleTransform == null)
                return 0f;

            var forward = vehicleTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                return 0f;
            forward.Normalize();

            var origin = vehicleTransform.position + Vector3.up * 0.65f + forward * 1.4f;
            var lookDist = Mathf.Clamp(3.5f + speedMps * 1.4f, MinProbeMeters, MaxProbeMeters);
            var rotation = Quaternion.LookRotation(forward, Vector3.up);

            if (!Physics.BoxCast(
                    origin,
                    BoxHalfExtents,
                    forward,
                    out var hit,
                    rotation,
                    lookDist,
                    ~0,
                    QueryTriggerInteraction.Ignore))
                return 0f;

            if (hit.collider != null && hit.collider.transform.IsChildOf(vehicleTransform))
                return 0f;

            if (hit.distance <= 2.2f)
                return 1f;

            var urgency = 1f - hit.distance / lookDist;
            return Mathf.Clamp01(urgency * urgency * 1.15f);
        }
    }
}
