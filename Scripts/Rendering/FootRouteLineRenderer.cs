using VoogleRoute;
using VoogleRoute.Navigation;
using UnityEngine;

namespace VoogleRoute.Rendering
{
    
    internal static class FootRouteLineRenderer
    {
        private const string RootName = "VoogleRoute_RouteFoot";
        private const string SubwayRootName = "VoogleRoute_RouteFootSubway";
        private const float TrimBehindFootMeters = 2f;
        private const float LineRefreshSeconds = 0.12f;
        private const float LineMoveResampleSq = 1f;

        private static GameObject _root;
        private static LineRenderer _line;
        private static GameObject _subwayRoot;
        private static LineRenderer _subwayLine;
        private static bool _lastIndoorStyle;
        private static Vector3[] _lastGoodDisplayPoints;
        private static Vector3[] _lastGoodSubwayPoints;
        private static float _lastLineRefreshTime = -999f;
        private static Vector3 _lastLinePose;
    
        internal static void EnsureCreated()
        {
            EnsureFootLine();
            EnsureSubwayLine();
        }

        private static void EnsureFootLine()
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

        private static void EnsureSubwayLine()
        {
            if (_subwayLine != null && _subwayRoot != null)
                return;

            _subwayRoot = GameObject.Find(SubwayRootName);
            if (_subwayRoot == null)
            {
                _subwayRoot = new GameObject(SubwayRootName);
                Object.DontDestroyOnLoad(_subwayRoot);
            }

            _subwayLine = _subwayRoot.GetComponent<LineRenderer>();
            if (_subwayLine == null)
                _subwayLine = _subwayRoot.AddComponent<LineRenderer>();

            ApplySubwayStyle();
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
    
            LineRendererMaterial.Apply(_line, ModConfig.FootLineColor);
            ApplySubwayStyle();
        }

        private static void ApplySubwayStyle()
        {
            if (_subwayLine == null)
                return;

            _subwayLine.useWorldSpace = true;
            _subwayLine.alignment = LineAlignment.View;
            _subwayLine.textureMode = LineTextureMode.Stretch;
            _subwayLine.numCapVertices = 4;
            _subwayLine.numCornerVertices = 4;
            _subwayLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _subwayLine.receiveShadows = false;
            _subwayLine.loop = false;

            var indoor = GameState.IsIndoorNavigationContext();
            var configuredWidth = indoor ? ModConfig.IndoorFootLineWidth : ModConfig.FootLineWidth;
            var minWidth = indoor ? 0.06f : 0.15f;
            var width = Mathf.Max(minWidth, configuredWidth) * 0.85f;
            _subwayLine.startWidth = width;
            _subwayLine.endWidth = width;
            LineRendererMaterial.Apply(_subwayLine, RouteSegmentLineHelper.SubwayLineColor);
        }
    
        internal static void ShowPath(PathResult path)
        {
            EnsureCreated();
            if (_root == null || _line == null)
                return;

            var indoor = GameState.IsIndoorNavigationContext();
            if (indoor != _lastIndoorStyle)
                ApplyStyle();

            RouteSegmentLineHelper.ExtractSegments(path, out var footPoints, out var subwayPoints);
            if (!path.Success || footPoints.Length < 2)
            {
                if (!PathFinderService.RouteWasRecalculated &&
                    _lastGoodDisplayPoints != null && _lastGoodDisplayPoints.Length >= 2)
                {
                    _root.SetActive(true);
                    if (_lastGoodSubwayPoints != null && _lastGoodSubwayPoints.Length >= 2 && _subwayRoot != null)
                        _subwayRoot.SetActive(true);
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
                if (_subwayRoot != null)
                    _subwayRoot.SetActive(_lastGoodSubwayPoints != null && _lastGoodSubwayPoints.Length >= 2);
                return;
            }

            _lastLineRefreshTime = now;
            _lastLinePose = pose;

            var displayPoints = BuildDisplayPoints(footPoints);
            if (displayPoints.Length < 2)
            {
                if (_lastGoodDisplayPoints != null && _lastGoodDisplayPoints.Length >= 2)
                    displayPoints = _lastGoodDisplayPoints;
                else
                    displayPoints = footPoints;
            }

            _lastGoodDisplayPoints = displayPoints;
            _lastGoodSubwayPoints = subwayPoints.Length >= 2 ? subwayPoints : null;
            _root.SetActive(true);

            if (!LineRendererPathCache.IsSame(displayPoints))
            {
                _line.positionCount = displayPoints.Length;
                _line.SetPositions(displayPoints);
                LineRendererMaterial.Apply(_line, ModConfig.FootLineColor);
            }

            ShowSubwayLine(subwayPoints);
        }

        private static void ShowSubwayLine(Vector3[] subwayPoints)
        {
            if (_subwayRoot == null || _subwayLine == null)
                return;

            if (subwayPoints == null || subwayPoints.Length < 2)
            {
                _subwayRoot.SetActive(false);
                return;
            }

            _subwayRoot.SetActive(true);
            _subwayLine.positionCount = subwayPoints.Length;
            _subwayLine.SetPositions(subwayPoints);
            LineRendererMaterial.Apply(_subwayLine, RouteSegmentLineHelper.SubwayLineColor);
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
            _lastGoodSubwayPoints = null;
            _lastLineRefreshTime = -999f;
            LineRendererPathCache.Reset();
            if (_root != null)
                _root.SetActive(false);
            if (_subwayRoot != null)
                _subwayRoot.SetActive(false);
        }
    
        internal static void Destroy()
        {
            _lastGoodDisplayPoints = null;
            _lastGoodSubwayPoints = null;

            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
                _line = null;
            }

            if (_subwayRoot != null)
            {
                Object.Destroy(_subwayRoot);
                _subwayRoot = null;
                _subwayLine = null;
            }
        }
    }
}
