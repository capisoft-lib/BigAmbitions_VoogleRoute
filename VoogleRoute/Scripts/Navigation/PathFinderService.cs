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
        private const float VehicleGleyRetrySeconds = 5f;
        private const float VehicleGleyRetryIntervalSeconds = 1.25f;

        private static readonly NavMeshPath NavPath = new NavMeshPath();
        private static Vector3 _lastOrigin;
        private static Vector3 _lastTarget;
        private static float _lastCalcTime;
        private static float _forceVehicleGleyUntil;
        private static MovementMode _lastMode = MovementMode.Unavailable;
        private static PathResult _cached;
        private static PathResult _lastGoodVehiclePath;
        private static float _lastOffRouteRecalcTime = -999f;
        private const float OffRouteRecalcSeconds = 0.75f;

        internal static Vector3[] LastFinalPoints { get; private set; } = System.Array.Empty<Vector3>();
        internal static bool RouteWasRecalculated { get; private set; }

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
            if (mode == MovementMode.Vehicle && Time.unscaledTime < _forceVehicleGleyUntil)
                interval = VehicleGleyRetryIntervalSeconds;

            var movedThreshold = mode == MovementMode.Vehicle ? 14400f : 225f;
            var moved = (origin - _lastOrigin).sqrMagnitude > movedThreshold;
            var targetChanged = Time.unscaledTime - NavigationTargetTracker.LastChangeTime < 0.05f;
            var targetMoved = (target - _lastTarget).sqrMagnitude > 1f;

            if (targetChanged || targetMoved || modeChanged)
                TrafficWaypointPathfinder.ResetDrivingLaneLock();

            if (!forceRecalc &&
                mode == MovementMode.Vehicle &&
                !targetChanged &&
                !targetMoved &&
                !modeChanged &&
                _cached.Success &&
                MovementModeDetector.TryGetVehiclePose(out var vehiclePos, out var vehicleForward))
            {
                if (TrafficWaypointPathfinder.IsFollowingLockedRoute(vehiclePos, vehicleForward))
                {
                    _lastOrigin = origin;
                    _lastCalcTime = Time.unscaledTime;
                    RouteWasRecalculated = false;
                    return _cached;
                }

                TrafficWaypointPathfinder.ResetDrivingLaneLock();
                var now = Time.unscaledTime;
                if (now - _lastOffRouteRecalcTime < OffRouteRecalcSeconds)
                {
                    RouteWasRecalculated = false;
                    return _cached;
                }

                _lastOffRouteRecalcTime = now;
            }
            else if (!forceRecalc && !modeChanged && !moved && !targetChanged && !targetMoved &&
                     Time.unscaledTime - _lastCalcTime < interval)
            {
                return _cached;
            }

            _lastOrigin = origin;
            _lastTarget = target;
            _lastCalcTime = Time.unscaledTime;
            RouteWasRecalculated = true;
            var allowRouteReuse = true;

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
                    origin, target, sampleOrigin, NavPath, allowRouteReuse,
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
                if (mode == MovementMode.Vehicle && TryReturnLastGoodVehiclePath(target))
                    return _cached;
                return Cache(Empty());
            }

            if (mode == MovementMode.Vehicle && !VehicleRouteCalculator.LastPathFromGley)
            {
                if (Time.unscaledTime < _forceVehicleGleyUntil)
                {
                    if (TryReturnLastGoodVehiclePath(target))
                        return _cached;
                    return Cache(Empty());
                }
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

            if (mode == MovementMode.Vehicle && success && VehicleRouteCalculator.LastPathFromGley)
                _lastGoodVehiclePath = result;

            return Cache(result);
        }

        internal static void NotifyMapDestinationChanged()
        {
            _forceVehicleGleyUntil = Time.unscaledTime + VehicleGleyRetrySeconds;
            _lastCalcTime = 0f;
            _lastGoodVehiclePath = PathResult.None;
            TrafficWaypointPathfinder.ResetDrivingLaneLock();
        }

        internal static void InvalidateCache()
        {
            _cached = Empty();
            _lastMode = MovementMode.Unavailable;
            _lastGoodVehiclePath = PathResult.None;
            LastFinalPoints = System.Array.Empty<Vector3>();
            TrafficWaypointPathfinder.ResetDrivingLaneLock();
            PathGeometry.ResetVehicleLineTrimState();
            TrafficWaypointGraph.InvalidateCache();
        }

        private static bool TryReturnLastGoodVehiclePath(Vector3 target)
        {
            if (!_lastGoodVehiclePath.Success || _lastGoodVehiclePath.Points == null ||
                _lastGoodVehiclePath.Points.Length < 2)
                return false;

            if ((target - _lastTarget).sqrMagnitude > 4f)
                return false;

            _cached = _lastGoodVehiclePath;
            RouteWasRecalculated = false;
            return true;
        }

        private static PathResult Cache(PathResult result)
        {
            _cached = result;
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
