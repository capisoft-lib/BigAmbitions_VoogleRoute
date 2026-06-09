using UnityEngine;
using VoogleRoute.Navigation;

namespace VoogleRoute.Rendering
{
    /// <summary>Affiche uniquement le segment de route visible à l'écran (+ marge).</summary>
    internal static class RouteLineViewportCuller
    {
        private const float ExtraMarginMeters = 70f;
        private const float MinVisibleAngleDegrees = 12f;
        private const float VehicleProximityVisibleMeters = 90f;
        private const float CullRefreshSeconds = 1.25f;
        private const float CameraMoveResampleSq = 36f;

        private static Vector3[] _cachedSlice;
        private static Vector3[] _cachedSource;
        private static Vector3 _lastCameraPos;
        private static float _lastCullTime = -999f;

        internal static void Reset()
        {
            _cachedSlice = null;
            _cachedSource = null;
            _lastCullTime = -999f;
        }

        internal static Vector3[] CullForDisplay(Vector3[] points)
        {
            if (points == null || points.Length < 2)
                return points ?? System.Array.Empty<Vector3>();

            var camera = Camera.main;
            if (camera == null)
                return points;

            var now = Time.unscaledTime;
            var camPos = camera.transform.position;
            if (_cachedSlice != null &&
                ReferenceEquals(points, _cachedSource) &&
                now - _lastCullTime < CullRefreshSeconds &&
                (camPos - _lastCameraPos).sqrMagnitude < CameraMoveResampleSq)
                return _cachedSlice;

            _cachedSource = points;
            _lastCameraPos = camPos;
            _lastCullTime = now;
            _cachedSlice = CullInternal(points, camera);
            return _cachedSlice;
        }

        private static Vector3[] CullInternal(Vector3[] points, Camera camera)
        {
            var first = -1;
            var last = -1;
            for (var i = 0; i < points.Length; i++)
            {
                if (!IsRoughlyVisible(points[i], camera))
                    continue;

                if (first < 0)
                    first = i;
                last = i;
            }

            if (first < 0)
                return SliceAroundNearest(points, GetViewAnchor(camera));

            first = Mathf.Max(0, first - 2);
            last = Mathf.Min(points.Length - 1, last + 2);
            return CopySlice(points, first, last);
        }

        private static Vector3 GetViewAnchor(Camera camera)
        {
            if (MovementModeDetector.TryGetVehiclePose(out var vehiclePos, out _))
                return vehiclePos;
            return camera != null ? camera.transform.position : Vector3.zero;
        }

        private static Vector3[] SliceAroundNearest(Vector3[] points, Vector3 viewPosition)
        {
            var nearest = 0;
            var bestSq = float.MaxValue;
            for (var i = 0; i < points.Length; i++)
            {
                var sq = HorizontalDistanceSq(points[i], viewPosition);
                if (sq < bestSq)
                {
                    bestSq = sq;
                    nearest = i;
                }
            }

            var first = nearest;
            var last = nearest;
            var acc = 0f;

            for (var i = nearest - 1; i >= 0; i--)
            {
                acc += Vector3.Distance(points[i], points[i + 1]);
                first = i;
                if (acc > ExtraMarginMeters)
                    break;
            }

            acc = 0f;
            for (var i = nearest + 1; i < points.Length; i++)
            {
                acc += Vector3.Distance(points[i - 1], points[i]);
                last = i;
                if (acc > ExtraMarginMeters * 1.35f)
                    break;
            }

            return CopySlice(points, first, last);
        }

        private static bool IsRoughlyVisible(Vector3 world, Camera camera)
        {
            var camPos = camera.transform.position;
            var anchor = GetViewAnchor(camera);
            if (HorizontalDistanceSq(world, anchor) <= VehicleProximityVisibleMeters * VehicleProximityVisibleMeters)
                return true;
            var toPoint = world - camPos;
            var horizontal = new Vector3(toPoint.x, 0f, toPoint.z);
            var dist = horizontal.magnitude;
            if (dist < 0.5f)
                return true;

            var reach = GetReachDistance(camera);
            if (dist > reach)
                return false;

            var forward = camera.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                return true;
            forward.Normalize();

            var dir = horizontal / dist;
            var angle = Vector3.Angle(forward, dir);
            var halfFov = camera.fieldOfView * 0.5f;
            return angle <= halfFov + MinVisibleAngleDegrees;
        }

        private static float GetReachDistance(Camera camera)
        {
            var far = camera.farClipPlane > 1f ? camera.farClipPlane : 250f;
            return Mathf.Min(far * 0.55f, 220f) + ExtraMarginMeters;
        }

        private static float HorizontalDistanceSq(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        private static Vector3[] CopySlice(Vector3[] points, int first, int last)
        {
            if (first <= 0 && last >= points.Length - 1)
                return points;

            var length = last - first + 1;
            if (length < 2)
                return new[] { points[first], points[Mathf.Min(first + 1, points.Length - 1)] };

            var slice = new Vector3[length];
            for (var i = 0; i < length; i++)
                slice[i] = points[first + i];
            return slice;
        }
    }
}
