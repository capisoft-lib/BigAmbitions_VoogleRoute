using UnityEngine;
using VoogleRoute;
using VoogleRoute.Navigation;

namespace VoogleRoute.Rendering
{
    /// <summary>Route line visible on the 3D city map (M), at player/vehicle ground elevation.</summary>
    internal static class CityMapRouteLineRenderer
    {
        private const string RootName = "VoogleRoute_RouteCityMap";
        private const string SecondaryRootName = "VoogleRoute_RouteCityMapSecondary";
        private const string SubwayRootName = "VoogleRoute_RouteCityMapSubway";
        private const float MapLineClearance = 0.5f;
        private const float LineWidth = 0.9f;
        private const float MinMapLineWidth = 0.7f;
        private const float MaxMapLineWidth = 2.5f;
        private const float MapLineWidthDistanceScale = 0.006f;

        private static GameObject _root;
        private static LineRenderer _line;
        private static GameObject _secondaryRoot;
        private static LineRenderer _secondaryLine;
        private static GameObject _subwayRoot;
        private static LineRenderer _subwayLine;

        internal static void EnsureCreated()
        {
            EnsureMainLine();
            EnsureSecondaryLine();
            EnsureSubwayLine();
        }

        private static void EnsureMainLine()
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

        private static void EnsureSecondaryLine()
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

            var width = ResolveLineWidth();
            _line.startWidth = width;
            _line.endWidth = width;

            LineRendererMaterial.Apply(_line, ResolveMapLineColor());
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

            var width = ResolveLineWidth();
            _secondaryLine.startWidth = width;
            _secondaryLine.endWidth = width;
            LineRendererMaterial.Apply(_secondaryLine, ResolveMapLineColor());
        }

        private static void ApplySubwayStyle()
        {
            if (_subwayLine == null)
                return;

            _subwayLine.useWorldSpace = true;
            _subwayLine.alignment = LineAlignment.View;
            _subwayLine.numCapVertices = 4;
            _subwayLine.numCornerVertices = 4;
            _subwayLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _subwayLine.receiveShadows = false;
            _subwayLine.loop = false;

            var width = ResolveLineWidth() * 0.85f;
            _subwayLine.startWidth = width;
            _subwayLine.endWidth = width;
            LineRendererMaterial.ApplyDashed(_subwayLine, RouteSegmentLineHelper.SubwayLineColor);
        }

        internal static void ShowPath(PathResult path)
        {
            EnsureCreated();
            if (_root == null || _line == null)
            {
                MapOverlayDiagnostics.LogRouteHidden("renderer_missing");
                return;
            }

            Vector3[][] footLegs;
            Vector3[] subwayPoints;
            if (MovementModeDetector.CurrentMode == MovementMode.OnFoot && path.UsesSubway)
            {
                RouteSegmentLineHelper.ExtractFootLegs(path, out footLegs, out subwayPoints);
            }
            else
            {
                footLegs = new[] { path.Points ?? System.Array.Empty<Vector3>() };
                subwayPoints = System.Array.Empty<Vector3>();
            }

            var primaryFootPoints = footLegs.Length > 0 ? footLegs[0] : System.Array.Empty<Vector3>();
            var secondaryFootPoints = footLegs.Length > 1 ? footLegs[1] : System.Array.Empty<Vector3>();

            if (!path.Success || primaryFootPoints.Length < 2)
            {
                MapOverlayDiagnostics.LogRouteHidden(
                    !path.Success ? "path_failed" : "path_too_short(points=" + primaryFootPoints.Length + ")");
                Hide();
                return;
            }

            CityMapLayerHelper.ApplyToMapRoute(_root);
            if (_secondaryRoot != null)
                CityMapLayerHelper.ApplyToMapRoute(_secondaryRoot);
            if (_subwayRoot != null)
                CityMapLayerHelper.ApplyToMapRoute(_subwayRoot);
            ApplyStyle();

            _root.SetActive(true);

            var elevated = BuildMapElevatedPoints(primaryFootPoints);
            _line.positionCount = elevated.Length;
            _line.SetPositions(elevated);
            LineRendererMaterial.Apply(_line, ResolveMapLineColor());
            MapOverlayDiagnostics.LogRouteShown(path, _root.layer, _line.startWidth);

            ShowSecondaryFootOnMap(secondaryFootPoints);
            ShowSubwayOnMap(subwayPoints);
        }

        private static void ShowSecondaryFootOnMap(Vector3[] footPoints)
        {
            if (_secondaryRoot == null || _secondaryLine == null)
                return;

            if (footPoints == null || footPoints.Length < 2)
            {
                CityMapLayerHelper.Restore(_secondaryRoot);
                _secondaryRoot.SetActive(false);
                return;
            }

            _secondaryRoot.SetActive(true);
            var elevated = BuildMapElevatedPoints(footPoints);
            _secondaryLine.positionCount = elevated.Length;
            _secondaryLine.SetPositions(elevated);
            LineRendererMaterial.Apply(_secondaryLine, ResolveMapLineColor());
        }

        private static void ShowSubwayOnMap(Vector3[] subwayPoints)
        {
            if (_subwayRoot == null || _subwayLine == null)
                return;

            if (subwayPoints == null || subwayPoints.Length < 2)
            {
                CityMapLayerHelper.Restore(_subwayRoot);
                _subwayRoot.SetActive(false);
                return;
            }

            _subwayRoot.SetActive(true);
            var elevated = BuildMapElevatedPoints(subwayPoints);
            _subwayLine.positionCount = elevated.Length;
            _subwayLine.SetPositions(elevated);
            LineRendererMaterial.ApplyDashed(_subwayLine, RouteSegmentLineHelper.SubwayLineColor);
        }

        internal static void Hide()
        {
            if (_root != null)
            {
                CityMapLayerHelper.Restore(_root);
                _root.SetActive(false);
            }

            if (_secondaryRoot != null)
            {
                CityMapLayerHelper.Restore(_secondaryRoot);
                _secondaryRoot.SetActive(false);
            }

            if (_subwayRoot != null)
            {
                CityMapLayerHelper.Restore(_subwayRoot);
                _subwayRoot.SetActive(false);
            }
        }

        private static Color ResolveMapLineColor() =>
            MovementModeDetector.CurrentMode == MovementMode.Vehicle
                ? ModConfig.VehicleLineColor
                : ModConfig.FootLineColor;

        private static float ResolveLineWidth()
        {
            try
            {
                var mapCam = CityManager.Instance?.cityMap?.cityMapCam;
                if (mapCam != null)
                    return Mathf.Clamp(mapCam.distance * MapLineWidthDistanceScale, MinMapLineWidth, MaxMapLineWidth);
            }
            catch
            {
                // ignore
            }

            return Mathf.Max(0.5f, LineWidth);
        }

        private static Vector3[] BuildMapElevatedPoints(Vector3[] points)
        {
            if (points.Length == 0)
                return System.Array.Empty<Vector3>();

            var mode = MovementModeDetector.CurrentMode;
            if (mode == MovementMode.Vehicle &&
                VehicleRouteLineRenderer.TryGetDisplayPointsForMap(points, out var vehiclePoints))
            {
                return ApplyMapClearance(vehiclePoints);
            }

            if (mode == MovementMode.OnFoot &&
                FootRouteLineRenderer.TryGetDisplayPointsForMap(points, out var footPoints))
            {
                return ApplyMapClearance(footPoints);
            }

            var lift = ModConfig.FootGroundOffset + MapLineClearance;
            return GroundProjector.ProjectToGround(points, lift);
        }

        private static Vector3[] ApplyMapClearance(Vector3[] source)
        {
            var elevated = new Vector3[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                var p = source[i];
                elevated[i] = new Vector3(p.x, p.y + MapLineClearance, p.z);
            }

            return elevated;
        }

        internal static void Destroy()
        {
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
    }
}
