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
        private static HybridRouteStroke _line;
        private static GameObject _secondaryRoot;
        private static HybridRouteStroke _secondaryLine;
        private static GameObject _subwayRoot;
        private static HybridRouteStroke _subwayLine;

        internal static void EnsureCreated()
        {
            EnsureMainLine();
            EnsureSecondaryLine();
            EnsureSubwayLine();
        }

        private static void EnsureMainLine()
        {
            if (_line != null && _line.IsReady && _root != null)
                return;

            _root = GameObject.Find(RootName);
            if (_root == null)
            {
                _root = new GameObject(RootName);
                Object.DontDestroyOnLoad(_root);
            }

            _line = HybridRouteStroke.Attach(_root);

            ApplyStyle();
        }

        private static void EnsureSecondaryLine()
        {
            if (_secondaryLine != null && _secondaryLine.IsReady && _secondaryRoot != null)
                return;

            _secondaryRoot = GameObject.Find(SecondaryRootName);
            if (_secondaryRoot == null)
            {
                _secondaryRoot = new GameObject(SecondaryRootName);
                Object.DontDestroyOnLoad(_secondaryRoot);
            }

            _secondaryLine = HybridRouteStroke.Attach(_secondaryRoot);

            ApplySecondaryStyle();
        }

        private static void EnsureSubwayLine()
        {
            if (_subwayLine != null && _subwayLine.IsReady && _subwayRoot != null)
                return;

            _subwayRoot = GameObject.Find(SubwayRootName);
            if (_subwayRoot == null)
            {
                _subwayRoot = new GameObject(SubwayRootName);
                Object.DontDestroyOnLoad(_subwayRoot);
            }

            _subwayLine = HybridRouteStroke.Attach(_subwayRoot);

            ApplySubwayStyle();
        }

        internal static void ApplyStyle()
        {
            if (_line == null)
                return;

            var width = ResolveLineWidth();
            _line.ApplyStyle(width, 4, 4, ResolveMapLineColor());
            ApplySecondaryStyle();
            ApplySubwayStyle();
        }

        private static void ApplySecondaryStyle()
        {
            if (_secondaryLine == null)
                return;

            var width = ResolveLineWidth();
            _secondaryLine.ApplyStyle(width, 4, 4, ResolveMapLineColor());
        }

        private static void ApplySubwayStyle()
        {
            if (_subwayLine == null)
                return;

            var width = ResolveLineWidth() * 0.85f;
            _subwayLine.ApplyStyle(width, 4, 4, RouteSegmentLineHelper.SubwayLineColor, dashed: true);
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
            _line.SetPositions(elevated);
            _line.SetColor(ResolveMapLineColor());
            MapOverlayDiagnostics.LogRouteShown(
                path,
                _root.layer,
                _line.Width);

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
            _secondaryLine.SetPositions(elevated);
            _secondaryLine.SetColor(ResolveMapLineColor());
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
            _subwayLine.SetPositions(elevated);
            _subwayLine.SetColor(RouteSegmentLineHelper.SubwayLineColor);
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
