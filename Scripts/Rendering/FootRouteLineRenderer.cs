using VoogleRoute;
using VoogleRoute.Navigation;
using UnityEngine;

namespace VoogleRoute.Rendering
{
    
    internal static class FootRouteLineRenderer
    {
        private const string RootName = "VoogleRoute_RouteFoot";
        private const float TrimBehindFootMeters = 2f;
        private const float LineRefreshSeconds = 0.12f;
        private const float LineMoveResampleSq = 1f;

        private static GameObject _root;
        private static LineRenderer _line;
        private static bool _lastIndoorStyle;
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
            _line.numCapVertices = 4;
            _line.numCornerVertices = 4;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.loop = false;
    
            var indoor = GameState.IsIndoorNavigationContext();
            _lastIndoorStyle = indoor;
            var configuredWidth = indoor ? ModConfig.IndoorFootLineWidth : ModConfig.FootLineWidth;
            var minWidth = indoor ? 0.06f : 0.15f;
            var width = Mathf.Max(minWidth, configuredWidth);
            _line.startWidth = width;
            _line.endWidth = width;
    
            LineRendererMaterial.Apply(_line);
        }
    
        internal static void ShowPath(PathResult path)
        {
            EnsureCreated();
            if (_root == null || _line == null)
                return;

            var indoor = GameState.IsIndoorNavigationContext();
            if (indoor != _lastIndoorStyle)
                ApplyStyle();

            var points = path.Points ?? System.Array.Empty<Vector3>();
            if (!path.Success || points.Length < 2)
            {
                if (!PathFinderService.RouteWasRecalculated &&
                    _lastGoodDisplayPoints != null && _lastGoodDisplayPoints.Length >= 2)
                {
                    _root.SetActive(true);
                    return;
                }

                Hide();
                return;
            }

            MovementModeDetector.TryGetPlayerOrigin(out var pose);
            var now = Time.unscaledTime;
            var routeDirty = PathFinderService.RouteWasRecalculated;

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
            if (!MovementModeDetector.TryGetPlayerOrigin(out var pose))
                return points;

            var trimmed = PathGeometry.TrimBehindOrigin(points, pose, TrimBehindFootMeters);
            return trimmed.Length >= 2 ? trimmed : points;
        }

        internal static bool TryGetDisplayPointsForMap(Vector3[] pathPoints, out Vector3[] displayPoints)
        {
            Vector3[] source = null;
            if (pathPoints != null && pathPoints.Length >= 2)
                source = pathPoints;
            else if (_lastGoodDisplayPoints != null && _lastGoodDisplayPoints.Length >= 2)
                source = _lastGoodDisplayPoints;

            if (source == null)
            {
                displayPoints = null;
                return false;
            }

            displayPoints = new Vector3[source.Length];
            for (var i = 0; i < source.Length; i++)
                displayPoints[i] = source[i];

            return true;
        }

        internal static void Hide()
        {
            _lastGoodDisplayPoints = null;
            _lastLineRefreshTime = -999f;
            LineRendererPathCache.Reset();
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
