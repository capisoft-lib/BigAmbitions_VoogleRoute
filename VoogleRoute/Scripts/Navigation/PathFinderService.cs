using UnityEngine;
using UnityEngine.AI;
using VoogleRoute;

namespace VoogleRoute.Navigation
{
    internal struct PathResult
    {
        internal bool Success;
        internal bool IsPartial;
        internal Vector3[] Points;

        internal static PathResult None
        {
            get
            {
                var r = new PathResult();
                r.Points = System.Array.Empty<Vector3>();
                return r;
            }
        }
    }

    internal static class PathFinderService
    {
        private static readonly NavMeshPath NavPath = new NavMeshPath();
        private static Vector3 _lastOrigin;
        private static Vector3 _lastTarget;
        private static float _lastCalcTime;
        private static MovementMode _lastMode = MovementMode.Unavailable;
        private static PathResult _cached;
        private static bool _cacheValid;
        private static PathResult _lastGoodVehiclePath;

        internal static Vector3[] LastFinalPoints { get; private set; } = System.Array.Empty<Vector3>();
        internal static bool RouteWasRecalculated { get; private set; }

        internal static bool TryGetCachedRouteForDisplay(out PathResult path)
        {
            if (_cacheValid && _cached.Success && _cached.Points != null && _cached.Points.Length >= 2)
            {
                path = _cached;
                return true;
            }

            if (LastFinalPoints.Length >= 2)
            {
                path = new PathResult
                {
                    Success = true,
                    Points = LastFinalPoints
                };
                return true;
            }

            path = Empty();
            return false;
        }

        internal static PathResult GetRoute(bool forceRecalc = false)
        {
            RouteWasRecalculated = false;

            if (!GameState.ShouldRunNavigationSystems())
                return SilentEmpty();

            if (!NavigationTargetTracker.HasTarget)
            {
                LastFinalPoints = System.Array.Empty<Vector3>();
                return Empty();
            }

            if (!MovementModeDetector.TryGetPathOrigin(out var origin))
            {
                LastFinalPoints = System.Array.Empty<Vector3>();
                return Empty();
            }

            var target = NavigationTargetTracker.ActiveTarget;
            var mode = MovementModeDetector.CurrentMode;
            var modeChanged = mode != _lastMode;
            _lastMode = mode;

            var interval = mode == MovementMode.Vehicle
                ? Mathf.Max(5f, ModConfig.VehicleRecalcIntervalSeconds)
                : Mathf.Max(0.5f, ModConfig.RecalcIntervalSeconds);

            var movedThreshold = mode == MovementMode.Vehicle ? 14400f : 225f;
            var moved = (origin - _lastOrigin).sqrMagnitude > movedThreshold;
            var targetChanged = Time.unscaledTime - NavigationTargetTracker.LastChangeTime < 0.05f;
            var targetMoved = (target - _lastTarget).sqrMagnitude > 1f;

            if (!forceRecalc &&
                _cacheValid &&
                !modeChanged &&
                !moved &&
                !targetChanged &&
                !targetMoved &&
                Time.unscaledTime - _lastCalcTime < interval)
            {
                return _cached;
            }

            _lastOrigin = origin;
            _lastTarget = target;
            _lastCalcTime = Time.unscaledTime;
            RouteWasRecalculated = true;

            var sampleOrigin = origin;
            if (MovementModeDetector.TryGetPlayerOrigin(out var feet))
                sampleOrigin = feet;

            NavMeshQueryFilter pathFilterUsed;
            bool calculateOk;
            Vector3[] navCorners;
            NavMeshPathStatus status;

            if (mode == MovementMode.Vehicle)
            {
                calculateOk = VehicleRouteCalculator.TryCalculate(
                    origin, target, sampleOrigin, NavPath, allowRouteReuse: false,
                    out pathFilterUsed, out navCorners, out status);
            }
            else
            {
                calculateOk = FootRouteCalculator.TryCalculate(origin, target, sampleOrigin, NavPath, out pathFilterUsed);
                status = calculateOk ? NavPath.status : NavMeshPathStatus.PathInvalid;
                navCorners = calculateOk && NavPath.corners != null
                    ? NavPath.corners
                    : System.Array.Empty<Vector3>();
            }

            if (!calculateOk || status == NavMeshPathStatus.PathInvalid)
            {
                ModLog.Debug("Route calculation failed (mode=" + mode + ", status=" + status + ").");
                if (mode == MovementMode.Vehicle && TryReturnLastGoodVehiclePath(target))
                    return _cached;
                return Cache(Empty());
            }

            var isPartial = status == NavMeshPathStatus.PathPartial;
            if (isPartial && !ModConfig.ShowPartialPaths)
                return Cache(Empty());

            if (navCorners.Length == 0)
                return Cache(Empty());

            var linePoints = mode == MovementMode.Vehicle
                ? VehiclePathPipeline.BuildLinePoints(navCorners, origin, target, pathFilterUsed)
                : FootPathPipeline.BuildLinePoints(navCorners, origin);

            var success = linePoints.Length >= 2;
            LastFinalPoints = linePoints;
            var result = new PathResult
            {
                Success = success,
                IsPartial = isPartial,
                Points = linePoints
            };

            if (mode == MovementMode.Vehicle && success)
                _lastGoodVehiclePath = result;

            ModLog.Debug("Route recalculated (mode=" + mode + ", points=" + linePoints.Length +
                         ", partial=" + isPartial + ", csv=" + VehicleRouteCalculator.LastPathFromCsv + ").");
            return Cache(result);
        }

        internal static void NotifyMapDestinationChanged()
        {
            _cacheValid = false;
            _lastCalcTime = 0f;
            _lastGoodVehiclePath = PathResult.None;
            LastFinalPoints = System.Array.Empty<Vector3>();
        }

        internal static void InvalidateCache()
        {
            _cached = Empty();
            _cacheValid = false;
            _lastMode = MovementMode.Unavailable;
            _lastGoodVehiclePath = PathResult.None;
            _lastCalcTime = 0f;
            PathGeometry.ResetVehicleLineTrimState();
            RouteGraphStore.Invalidate();
            VehiclePathPipeline.InvalidateRouteLineCache();
        }

        private static bool TryReturnLastGoodVehiclePath(Vector3 target)
        {
            if (!_lastGoodVehiclePath.Success || _lastGoodVehiclePath.Points == null ||
                _lastGoodVehiclePath.Points.Length < 2)
                return false;

            if ((target - _lastTarget).sqrMagnitude > 4f)
                return false;

            _cached = _lastGoodVehiclePath;
            _cacheValid = true;
            RouteWasRecalculated = false;
            return true;
        }

        private static PathResult Cache(PathResult result)
        {
            _cached = result;
            _cacheValid = true;
            return result;
        }

        private static PathResult Empty()
        {
            var r = new PathResult();
            r.Points = System.Array.Empty<Vector3>();
            return r;
        }

        private static PathResult SilentEmpty() => Empty();
    }
}
