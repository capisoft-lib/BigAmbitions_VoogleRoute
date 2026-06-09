using System.Diagnostics;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;
using VoogleRoute;
using VoogleRoute.Pathfinding.Geometry;
using VoogleRoute.UI;

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
        private static Vector3 _lastTarget;
        private static MovementMode _lastMode = MovementMode.Unavailable;
        private static PathResult _cached;
        private static bool _cacheValid;
        private static PathResult _lastGoodVehiclePath;
        private static readonly object AsyncGate = new object();
        private static int _cacheGeneration;
        private static bool _asyncInProgress;
        private static bool _asyncComplete;
        private static int _asyncGeneration;
        private static PathResult _asyncPendingResult;
        private static bool _asyncRefreshPending;
        private static string _asyncRequestSource = "unknown";
        private static string _asyncRecalcReason = "unknown";
        private static bool _mapDestinationRecalcPending;

        internal static Vector3[] LastFinalPoints { get; private set; } = System.Array.Empty<Vector3>();
        internal static bool RouteWasRecalculated { get; private set; }
        internal static bool IsAsyncRecalcInProgress
        {
            get
            {
                lock (AsyncGate)
                    return _asyncInProgress;
            }
        }

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

        internal static PathResult GetRoute(bool forceRecalc = false, string requestSource = "unknown")
        {
            var totalTimer = RouteRecalcDiagnostics.StartTimer();
            RouteWasRecalculated = false;

            if (!GameState.ShouldRunNavigationSystems())
                return FinishSilent(totalTimer, requestSource, SilentEmpty());

            if (!NavigationTargetTracker.HasTarget)
            {
                LastFinalPoints = System.Array.Empty<Vector3>();
                return FinishSilent(totalTimer, requestSource, Empty());
            }

            if (!MovementModeDetector.TryGetPathOrigin(out var origin))
            {
                LastFinalPoints = System.Array.Empty<Vector3>();
                return FinishSilent(totalTimer, requestSource, Empty());
            }

            var target = NavigationTargetTracker.ActiveTarget;
            var mode = MovementModeDetector.CurrentMode;
            var modeChanged = mode != _lastMode;
            _lastMode = mode;

            var cacheMiss = !_cacheValid || !_cached.Success;
            var corridorMargin = RouteLineDetection.GetCrossTrackMeters(mode == MovementMode.Vehicle);
            var withinCorridor = !cacheMiss &&
                PathGeometry.IsWithinRouteCorridor(origin, _cached.Points, corridorMargin);
            var targetChanged = _mapDestinationRecalcPending ||
                Time.unscaledTime - NavigationTargetTracker.LastChangeTime < 0.05f;
            var targetMoved = (target - _lastTarget).sqrMagnitude > 1f;

            if (!forceRecalc &&
                !cacheMiss &&
                !modeChanged &&
                withinCorridor &&
                !targetChanged &&
                !targetMoved)
            {
                RouteRecalcDiagnostics.LogSkip(requestSource, "corridor_ok",
                    RouteRecalcDiagnostics.ElapsedMs(totalTimer));
                return _cached;
            }

            var leftCorridor = !cacheMiss && !withinCorridor;
            var recalcReason = RouteRecalcDiagnostics.BuildRecalcReason(
                forceRecalc,
                modeChanged,
                targetChanged,
                targetMoved,
                leftCorridor,
                cacheMiss);

            if (TryQueueAsyncRecalc(
                    cacheMiss,
                    leftCorridor,
                    targetChanged,
                    targetMoved,
                    forceRecalc,
                    mode,
                    origin,
                    target,
                    requestSource,
                    recalcReason,
                    totalTimer))
                return _cached;

            var showBanner = leftCorridor || forceRecalc || targetChanged || targetMoved;
            if (showBanner)
                RouteRecalcBanner.Show();

            if (targetChanged)
                _mapDestinationRecalcPending = false;

            _lastTarget = target;
            RouteWasRecalculated = true;

            var sampleOrigin = origin;
            if (MovementModeDetector.TryGetPlayerOrigin(out var feet))
                sampleOrigin = feet;

            var result = CalculateRouteSync(
                mode,
                origin,
                target,
                sampleOrigin,
                requestSource,
                recalcReason,
                totalTimer);

            if (showBanner)
                RouteRecalcBanner.RequestHide();

            return result;
        }

        internal static bool TickAsyncRecalc()
        {
            PathResult pending;
            string requestSource;
            string recalcReason;
            int generation;

            lock (AsyncGate)
            {
                if (!_asyncComplete)
                    return false;

                _asyncComplete = false;
                _asyncInProgress = false;
                pending = _asyncPendingResult;
                requestSource = _asyncRequestSource;
                recalcReason = _asyncRecalcReason;
                generation = _asyncGeneration;
            }

            RouteRecalcBanner.RequestHide();

            if (generation != _cacheGeneration ||
                !GameState.ShouldRunNavigationSystems() ||
                !NavigationTargetTracker.HasTarget)
                return false;

            RouteWasRecalculated = true;
            LastFinalPoints = pending.Points ?? System.Array.Empty<Vector3>();
            if (MovementModeDetector.CurrentMode == MovementMode.Vehicle && pending.Success)
                _lastGoodVehiclePath = pending;

            Cache(pending);
            RouteRecalcDiagnostics.LogRecalc(
                requestSource,
                recalcReason + "|async",
                MovementModeDetector.CurrentMode,
                0f,
                RouteRecalcDiagnostics.LastPathfindMs,
                0f,
                pending.Points?.Length ?? 0,
                RouteRecalcDiagnostics.LastPathfindKind,
                pending.Success);

            _asyncRefreshPending = true;
            return true;
        }

        internal static bool ConsumeAsyncRefreshRequest()
        {
            if (!_asyncRefreshPending)
                return false;

            _asyncRefreshPending = false;
            return true;
        }

        private static bool TryQueueAsyncRecalc(
            bool cacheMiss,
            bool leftCorridor,
            bool targetChanged,
            bool targetMoved,
            bool forceRecalc,
            MovementMode mode,
            Vector3 origin,
            Vector3 target,
            string requestSource,
            string recalcReason,
            Stopwatch totalTimer)
        {
            var preferAsync = mode == MovementMode.Vehicle &&
                (cacheMiss || leftCorridor || targetChanged || targetMoved || forceRecalc);
            if (!preferAsync)
                return false;

            lock (AsyncGate)
            {
                if (_asyncInProgress)
                {
                    RouteRecalcDiagnostics.LogSkip(requestSource, "async_pending",
                        RouteRecalcDiagnostics.ElapsedMs(totalTimer));
                    RouteRecalcBanner.Show();
                    return true;
                }
            }

            MovementModeDetector.TryGetVehiclePose(out _, out var forward);
            var hasPose = forward.sqrMagnitude > 0.01f;
            var forwardVec = hasPose
                ? new Vec3(forward.x, forward.y, forward.z)
                : default;
            var generation = _cacheGeneration;

            lock (AsyncGate)
            {
                _asyncInProgress = true;
                _asyncComplete = false;
                _asyncGeneration = generation;
                _asyncRequestSource = requestSource;
                _asyncRecalcReason = recalcReason;
            }

            _lastTarget = target;
            if (targetChanged)
                _mapDestinationRecalcPending = false;
            RouteRecalcBanner.Show();
            RouteRecalcDiagnostics.LogSkip(requestSource, "async_started",
                RouteRecalcDiagnostics.ElapsedMs(totalTimer));

            ThreadPool.QueueUserWorkItem(_ =>
            {
                var pathfindTimer = RouteRecalcDiagnostics.StartTimer();
                var pending = ComputeVehicleRouteSnapshot(origin, target, forwardVec, hasPose);
                RouteRecalcDiagnostics.RecordPathfind(
                    pending.Success ? RoutePathfindKind.FullAStar : RoutePathfindKind.Failed,
                    RouteRecalcDiagnostics.ElapsedMs(pathfindTimer));

                lock (AsyncGate)
                {
                    if (_asyncGeneration != generation)
                    {
                        _asyncInProgress = false;
                        _asyncPendingResult = Empty();
                        _asyncComplete = true;
                        return;
                    }

                    _asyncPendingResult = pending;
                    _asyncComplete = true;
                }
            });

            return true;
        }

        private static PathResult ComputeVehicleRouteSnapshot(
            Vector3 origin,
            Vector3 target,
            Vec3 forward,
            bool hasPose)
        {
            if (!RoutePathfinder.TryFindPath(origin, target, forward, hasPose, out var navCorners) ||
                navCorners == null ||
                navCorners.Length < 2)
                return Empty();

            var yOff = ModConfig.VehicleGroundOffset;
            var linePoints = new Vector3[navCorners.Length];
            for (var i = 0; i < navCorners.Length; i++)
            {
                var p = navCorners[i];
                p.y += yOff;
                linePoints[i] = p;
            }

            if (linePoints.Length < 2)
                return Empty();

            return new PathResult
            {
                Success = true,
                IsPartial = false,
                Points = linePoints
            };
        }

        private static PathResult CalculateRouteSync(
            MovementMode mode,
            Vector3 origin,
            Vector3 target,
            Vector3 sampleOrigin,
            string requestSource,
            string recalcReason,
            Stopwatch totalTimer)
        {
            var pathfindTimer = RouteRecalcDiagnostics.StartTimer();
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
                RouteRecalcDiagnostics.RecordPathfind(
                    calculateOk ? RoutePathfindKind.NavMeshFoot : RoutePathfindKind.Failed,
                    RouteRecalcDiagnostics.ElapsedMs(pathfindTimer));
            }

            var pathfindMs = RouteRecalcDiagnostics.ElapsedMs(pathfindTimer);

            if (!calculateOk || status == NavMeshPathStatus.PathInvalid)
            {
                if (mode == MovementMode.Vehicle && TryReturnLastGoodVehiclePath(target))
                {
                    RouteRecalcDiagnostics.LogRecalcFailed(
                        requestSource,
                        recalcReason + "|fallback_last_good",
                        mode,
                        RouteRecalcDiagnostics.ElapsedMs(totalTimer),
                        pathfindMs,
                        RouteRecalcDiagnostics.LastPathfindKind,
                        "status=" + status);
                    RouteWasRecalculated = false;
                    return _cached;
                }

                RouteRecalcDiagnostics.LogRecalcFailed(
                    requestSource,
                    recalcReason,
                    mode,
                    RouteRecalcDiagnostics.ElapsedMs(totalTimer),
                    pathfindMs,
                    RouteRecalcDiagnostics.LastPathfindKind,
                    "status=" + status);
                return Cache(Empty());
            }

            var isPartial = status == NavMeshPathStatus.PathPartial;
            if (isPartial && !ModConfig.ShowPartialPaths)
            {
                RouteRecalcDiagnostics.LogRecalcFailed(
                    requestSource,
                    recalcReason + "|partial_rejected",
                    mode,
                    RouteRecalcDiagnostics.ElapsedMs(totalTimer),
                    pathfindMs,
                    RouteRecalcDiagnostics.LastPathfindKind,
                    "partial=true");
                return Cache(Empty());
            }

            if (navCorners.Length == 0)
            {
                RouteRecalcDiagnostics.LogRecalcFailed(
                    requestSource,
                    recalcReason + "|empty_corners",
                    mode,
                    RouteRecalcDiagnostics.ElapsedMs(totalTimer),
                    pathfindMs,
                    RouteRecalcDiagnostics.LastPathfindKind,
                    "corners=0");
                return Cache(Empty());
            }

            var pipelineTimer = RouteRecalcDiagnostics.StartTimer();
            var linePoints = mode == MovementMode.Vehicle
                ? VehiclePathPipeline.BuildLinePoints(navCorners, origin, target, pathFilterUsed)
                : FootPathPipeline.BuildLinePoints(navCorners, origin);
            var pipelineMs = RouteRecalcDiagnostics.ElapsedMs(pipelineTimer);

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

            RouteRecalcDiagnostics.LogRecalc(
                requestSource,
                recalcReason,
                mode,
                RouteRecalcDiagnostics.ElapsedMs(totalTimer),
                pathfindMs,
                pipelineMs,
                linePoints.Length,
                RouteRecalcDiagnostics.LastPathfindKind,
                success);

            return Cache(result);
        }

        internal static void NotifyMapDestinationChanged()
        {
            CancelAsyncRecalc();
            _cacheGeneration++;
            _cacheValid = false;
            _lastGoodVehiclePath = PathResult.None;
            LastFinalPoints = System.Array.Empty<Vector3>();
            _mapDestinationRecalcPending = true;
            RouteRecalcDiagnostics.LogCacheInvalidated("map_destination_changed");
        }

        internal static void InvalidateCache(string reason = "unspecified")
        {
            CancelAsyncRecalc();
            _cacheGeneration++;
            _cached = Empty();
            _cacheValid = false;
            _lastMode = MovementMode.Unavailable;
            _lastGoodVehiclePath = PathResult.None;
            PathGeometry.ResetVehicleLineTrimState();
            VehiclePathPipeline.InvalidateRouteLineCache();
            RouteRecalcDiagnostics.LogCacheInvalidated(reason);
        }

        private static void CancelAsyncRecalc()
        {
            lock (AsyncGate)
            {
                _asyncInProgress = false;
                _asyncComplete = false;
                _asyncPendingResult = PathResult.None;
            }

            RouteRecalcBanner.ForceHide();
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

        private static PathResult FinishSilent(Stopwatch timer, string requestSource, PathResult result)
        {
            _ = timer;
            _ = requestSource;
            return result;
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
