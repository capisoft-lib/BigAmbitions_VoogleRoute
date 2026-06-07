using VoogleRoute.Navigation;
using UnityEngine;

namespace VoogleRoute.Rendering
{
    internal static class VehicleRouteLineRenderer
    {
        private const string RootName = "VoogleRoute_RouteVehicle";
        private const float TrimBehindVehicleMeters = 6f;
        private const float LineRefreshSeconds = 0.15f;
        private const float LineMoveResampleSq = 9f;

        private static GameObject _root;
        private static LineRenderer _line;
        private static Vector3[] _lastGoodDisplayPoints;
        private static float _lastLineRefreshTime = -999f;
        private static Vector3 _lastLinePose;

        internal static void EnsureCreated()
        {
            if (_line != null && _root != null)
                return;

            _root = GameObject.Find(RootName);
            if (_root == null)
            {
                _root = new GameObject(RootName);
                Object.DontDestroyOnLoad(_root);
            }

            _line = _root.GetComponent<LineRenderer>();
            if (_line == null)
                _line = _root.AddComponent<LineRenderer>();

            ApplyStyle();
        }

        internal static void ApplyStyle()
        {
            if (_line == null)
                return;

            _line.useWorldSpace = true;
            _line.alignment = LineAlignment.View;
            _line.textureMode = LineTextureMode.Stretch;
            _line.numCapVertices = 2;
            _line.numCornerVertices = 2;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.loop = false;

            var width = Mathf.Max(0.12f, ModConfig.VehicleLineWidth);
            _line.startWidth = width;
            _line.endWidth = width;

            LineRendererMaterial.Apply(_line);
        }

        internal static void ShowPath(PathResult path)
        {
            EnsureCreated();
            if (_root == null || _line == null)
                return;

            var points = path.Points ?? System.Array.Empty<Vector3>();
            if (!path.Success || points.Length < 2)
            {
                if (_lastGoodDisplayPoints != null && _lastGoodDisplayPoints.Length >= 2)
                {
                    _root.SetActive(true);
                    return;
                }

                Hide();
                return;
            }

            MovementModeDetector.TryGetVehiclePose(out var pose, out _);
            var now = Time.unscaledTime;
            var routeDirty = PathFinderService.RouteWasRecalculated;
            if (routeDirty)
            {
                PathGeometry.ResetVehicleLineTrimState();
                RouteLineViewportCuller.Reset();
            }

            if (!routeDirty &&
                _lastGoodDisplayPoints != null &&
                _lastGoodDisplayPoints.Length >= 2 &&
                now - _lastLineRefreshTime < LineRefreshSeconds &&
                (pose - _lastLinePose).sqrMagnitude < LineMoveResampleSq)
            {
                _root.SetActive(true);
                return;
            }

            _lastLineRefreshTime = now;
            _lastLinePose = pose;

            var displayPoints = BuildDisplayPoints(points);
            if (displayPoints.Length < 2)
            {
                if (_lastGoodDisplayPoints != null && _lastGoodDisplayPoints.Length >= 2)
                    displayPoints = _lastGoodDisplayPoints;
                else
                    displayPoints = points;
            }

            _lastGoodDisplayPoints = displayPoints;
            _root.SetActive(true);

            if (!LineRendererPathCache.IsSame(displayPoints))
            {
                _line.positionCount = displayPoints.Length;
                _line.SetPositions(displayPoints);
                LineRendererMaterial.Apply(_line);
            }
        }

        private static Vector3[] BuildDisplayPoints(Vector3[] points)
        {
            var displayPoints = points;
            if (!MovementModeDetector.TryGetVehiclePose(out var pose, out var forward))
                return displayPoints;

            var trimmed = PathGeometry.TrimBehindOrigin(points, pose, forward, TrimBehindVehicleMeters);
            if (trimmed.Length >= 2)
                displayPoints = trimmed;

            return displayPoints.Length >= 2 ? displayPoints : points;
        }

        internal static void Hide()
        {
            _lastGoodDisplayPoints = null;
            _lastLineRefreshTime = -999f;
            PathGeometry.ResetVehicleLineTrimState();
            LineRendererPathCache.Reset();
            RouteLineViewportCuller.Reset();

            if (_root != null)
                _root.SetActive(false);
        }

        internal static void Destroy()
        {
            _lastGoodDisplayPoints = null;

            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
                _line = null;
            }
        }
    }
}
