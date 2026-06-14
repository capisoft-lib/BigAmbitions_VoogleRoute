using VoogleRoute;
using VoogleRoute.Navigation;
using UnityEngine;

namespace VoogleRoute.Rendering
{
    
    internal static class FootRouteLineRenderer
    {
        private const string RootName = "VoogleRoute_RouteFoot";
        private const string SecondaryRootName = "VoogleRoute_RouteFootSecondary";
        private const string SubwayRootName = "VoogleRoute_RouteFootSubway";
        private const float TrimBehindFootMeters = 2f;
        private const float LineRefreshSeconds = 0.12f;
        private const float LineMoveResampleSq = 1f;

        private static GameObject _root;
        private static LineRenderer _line;
        private static GameObject _secondaryRoot;
        private static LineRenderer _secondaryLine;
        private static GameObject _subwayRoot;
        private static LineRenderer _subwayLine;
        private static bool _lastIndoorStyle;
        private static Vector3[] _lastGoodDisplayPoints;
        private static Vector3[] _lastGoodSecondaryDisplayPoints;
        private static Vector3[] _lastGoodSubwayPoints;
        private static float _lastLineRefreshTime = -999f;
        private static Vector3 _lastLinePose;
    
        internal static void EnsureCreated()
        {
            EnsureFootLine();
            EnsureSecondaryFootLine();
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

        private static void EnsureSecondaryFootLine()
        {
            if (_secondaryLine != null && _secondaryRoot != null)
                return;

            _secondaryRoot = GameObject.Find(SecondaryRootName);
            if (_secondaryRoot == null)
            {
                _secondaryRoot = new GameObject(SecondaryRootName);
                Object.DontDestroyOnLoad(_secondaryRoot);
            }

            _secondaryLine = _secondaryRoot.GetComponent<LineRenderer>();
            if (_secondaryLine == null)
                _secondaryLine = _secondaryRoot.AddComponent<LineRenderer>();

            ApplySecondaryStyle();
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
    
            LineRendererMaterial.Apply(_line, CurrentFootLineColor());
            ApplySecondaryStyle();
            ApplySubwayStyle();
        }

        private static void ApplySecondaryStyle()
        {
            if (_secondaryLine == null)
                return;

            _secondaryLine.useWorldSpace = true;
            _secondaryLine.alignment = LineAlignment.View;
            _secondaryLine.textureMode = LineTextureMode.Stretch;
            _secondaryLine.numCapVertices = 4;
            _secondaryLine.numCornerVertices = 4;
            _secondaryLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _secondaryLine.receiveShadows = false;
            _secondaryLine.loop = false;

            var indoor = GameState.IsIndoorNavigationContext();
            var configuredWidth = indoor ? ModConfig.IndoorFootLineWidth : ModConfig.FootLineWidth;
            var minWidth = indoor ? 0.06f : 0.15f;
            var width = Mathf.Max(minWidth, configuredWidth);
            _secondaryLine.startWidth = width;
            _secondaryLine.endWidth = width;
            LineRendererMaterial.Apply(_secondaryLine, CurrentFootLineColor());
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
            LineRendererMaterial.ApplyDashed(_subwayLine, RouteSegmentLineHelper.SubwayLineColor);
        }
    
        internal static void ShowPath(PathResult path)
        {
            EnsureCreated();
            if (_root == null || _line == null)
                return;

            var indoor = GameState.IsIndoorNavigationContext();
            if (indoor != _lastIndoorStyle)
                ApplyStyle();

            RouteSegmentLineHelper.ExtractFootLegs(path, out var footLegs, out var subwayPoints);
            var primaryFootPoints = footLegs.Length > 0 ? footLegs[0] : System.Array.Empty<Vector3>();
            var secondaryFootPoints = footLegs.Length > 1 ? footLegs[1] : System.Array.Empty<Vector3>();

            if (!path.Success || primaryFootPoints.Length < 2)
            {
                if (!PathFinderService.RouteWasRecalculated &&
                    _lastGoodDisplayPoints != null && _lastGoodDisplayPoints.Length >= 2)
                {
                    _root.SetActive(true);
                    if (_secondaryRoot != null)
                        _secondaryRoot.SetActive(_lastGoodSecondaryDisplayPoints != null &&
                                                 _lastGoodSecondaryDisplayPoints.Length >= 2);
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
                if (_secondaryRoot != null)
                    _secondaryRoot.SetActive(_lastGoodSecondaryDisplayPoints != null &&
                                             _lastGoodSecondaryDisplayPoints.Length >= 2);
                if (_subwayRoot != null)
                    _subwayRoot.SetActive(_lastGoodSubwayPoints != null && _lastGoodSubwayPoints.Length >= 2);
                return;
            }

            _lastLineRefreshTime = now;
            _lastLinePose = pose;

            var displayPoints = BuildDisplayPoints(primaryFootPoints);
            if (displayPoints.Length < 2)
            {
                if (_lastGoodDisplayPoints != null && _lastGoodDisplayPoints.Length >= 2)
                    displayPoints = _lastGoodDisplayPoints;
                else
                    displayPoints = primaryFootPoints;
            }

            Vector3[] secondaryDisplayPoints = null;
            if (secondaryFootPoints.Length >= 2)
            {
                secondaryDisplayPoints = BuildDisplayPoints(secondaryFootPoints);
                if (secondaryDisplayPoints.Length < 2)
                    secondaryDisplayPoints = secondaryFootPoints;
            }

            _lastGoodDisplayPoints = displayPoints;
            _lastGoodSecondaryDisplayPoints = secondaryDisplayPoints;
            _lastGoodSubwayPoints = subwayPoints.Length >= 2 ? subwayPoints : null;
            _root.SetActive(true);

            if (!LineRendererPathCache.IsSame(displayPoints))
            {
                _line.positionCount = displayPoints.Length;
                _line.SetPositions(displayPoints);
                LineRendererMaterial.Apply(_line, CurrentFootLineColor());
            }

            ShowSecondaryFootLine(secondaryDisplayPoints);
            ShowSubwayLine(subwayPoints);
        }

        private static void ShowSecondaryFootLine(Vector3[] displayPoints)
        {
            if (_secondaryRoot == null || _secondaryLine == null)
                return;

            if (displayPoints == null || displayPoints.Length < 2)
            {
                _secondaryRoot.SetActive(false);
                return;
            }

            _secondaryRoot.SetActive(true);
            _secondaryLine.positionCount = displayPoints.Length;
            _secondaryLine.SetPositions(displayPoints);
            LineRendererMaterial.Apply(_secondaryLine, CurrentFootLineColor());
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
            LineRendererMaterial.ApplyDashed(_subwayLine, RouteSegmentLineHelper.SubwayLineColor);
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
            _lastGoodSecondaryDisplayPoints = null;
            _lastGoodSubwayPoints = null;
            _lastLineRefreshTime = -999f;
            LineRendererPathCache.Reset();
            if (_root != null)
                _root.SetActive(false);
            if (_secondaryRoot != null)
                _secondaryRoot.SetActive(false);
            if (_subwayRoot != null)
                _subwayRoot.SetActive(false);
        }
    
        internal static void Destroy()
        {
            _lastGoodDisplayPoints = null;
            _lastGoodSecondaryDisplayPoints = null;
            _lastGoodSubwayPoints = null;

            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
                _line = null;
            }

            if (_secondaryRoot != null)
            {
                Object.Destroy(_secondaryRoot);
                _secondaryRoot = null;
                _secondaryLine = null;
            }

            if (_subwayRoot != null)
            {
                Object.Destroy(_subwayRoot);
                _subwayRoot = null;
                _subwayLine = null;
            }
        }

        private static Color CurrentFootLineColor() =>
            GameState.IsIndoorNavigationContext() ? ModConfig.IndoorFootLineColor : ModConfig.FootLineColor;
    }
}

