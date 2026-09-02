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
        internal RoutePathSegment[] Segments;
        internal SubwayNavigationHint Subway;

        internal bool UsesSubway => Subway.Active;

        internal static PathResult None
        {
            get
            {
                var r = new PathResult();
                r.Points = System.Array.Empty<Vector3>();
                r.Segments = System.Array.Empty<RoutePathSegment>();
                r.Subway = SubwayNavigationHint.None;
                return r;
            }
        }
    }

    internal static class PathFinderService
    {
        // A mansion driveway can end on a tiny NavMesh seam. If Unity's partial
        // path stops inside the normal on-foot arrival radius, it is still a
        // usable route: walking to its final corner will complete navigation.
        private const float NearTargetPartialToleranceMeters = 7f;
        // Hamptons mansions expose a vehicle driveway target that can sit well
        // inside private grounds. For map-selected buildings, guide pedestrians
        // to the closest reachable gate instead of suppressing the whole route.
        private const float BuildingApproachPartialToleranceMeters = 50f;
        private const float FailedRouteRetryDelaySeconds = 5f;
        private const int MaxConsecutiveRouteFailures = 3;
        private const float FailedRouteResetMovementMeters = 25f;
        private const float FailedRouteTargetToleranceMeters = 2f;
        private const float FootRecalcMinMovementMeters = 2f;
        private static readonly NavMeshPath NavPath = new NavMeshPath();
        private static Vector3 _lastTarget;
        private static MovementMode _lastMode = MovementMode.Unavailable;
        private static PathResult _cached;
        private static bool _cacheValid;
        private static MovementMode _cachedMode = MovementMode.Unavailable;
        private static MovementMode _lastFinalPointsMode = MovementMode.Unavailable;
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
        private static CancellationTokenSource _asyncCancellation;
        private static MovementMode _asyncMode = MovementMode.Unavailable;
        private static bool _mapDestinationRecalcPending;
        private static float _lastFootRecalcTime = -999f;
        private static Vector3 _lastFootRecalcOrigin;
        private static bool _hasLastFootRecalcOrigin;
        private static bool _forceNextRecalc;
        private static bool _rejectLastGoodFallback;
        private static int _consecutiveRouteFailures;
        private static float _nextFailedRouteRetryTime;
        private static Vector3 _failedRouteOrigin;
        private static Vector3 _failedRouteTarget;
        private static MovementMode _failedRouteMode = MovementMode.Unavailable;
        private static bool _failedRouteLocked;
        private static bool _failedRouteNotificationShown;
        private static Vector3 _lastAttemptOrigin;
        private static Vector3 _lastAttemptTarget;

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
            var mode = MovementModeDetector.CurrentMode;

            if (_cacheValid &&
                _cached.Success &&
                _cachedMode == mode &&
                _cached.Points != null &&
                _cached.Points.Length >= 2)
            {
                path = _cached;
                return true;
            }

            if (mode == MovementMode.Vehicle &&
                _lastGoodVehiclePath.Success &&
                _lastGoodVehiclePath.Points != null &&
                _lastGoodVehiclePath.Points.Length >= 2)
            {
                path = _lastGoodVehiclePath;
                return true;
            }

            if (IsAsyncRecalcInProgress &&
                _lastFinalPointsMode == mode &&
                LastFinalPoints.Length >= 2)
            {
                path = new PathResult
                {
                    Success = true,
                    Points = LastFinalPoints,
                    Segments = _cached.Segments,
                    Subway = _cached.Subway
                };
                return true;
            }

            path = Empty();
            return false;
        }

        internal static bool TryGetEffectiveFootArrivalTarget(out Vector3 target)
        {
            target = default;
            if (MovementModeDetector.CurrentMode != MovementMode.OnFoot ||
                !TryGetCachedRouteForDisplay(out var path) ||
                !path.IsPartial ||
                path.Points == null ||
                path.Points.Length < 2)
                return false;

            target = path.Points[path.Points.Length - 1];
            return true;
        }

        internal static void EnsureCacheMatchesMovementMode()
        {
            var mode = MovementModeDetector.CurrentMode;
            if (!_cacheValid || _cachedMode == mode)
                return;

            InvalidateCache("movement_mode_mismatch");
        }

        internal static PathResult GetRoute(bool forceRecalc = false, string requestSource = "unknown")
        {
            var totalTimer = RouteRecalcDiagnostics.StartTimer();
            RouteWasRecalculated = false;

            if (_forceNextRecalc)
            {
                forceRecalc = true;
                _forceNextRecalc = false;
            }

            if (!GameState.ShouldRunPathfinding())
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

            if (TryUseFailedRouteCache(
                    origin,
                    target,
                    mode,
                    forceRecalc,
                    requestSource,
                    totalTimer,
                    out var failedRouteDisplay))
                return failedRouteDisplay;

            var cacheMiss = !_cacheValid || !_cached.Success;
            var corridorMargin = RouteLineDetection.GetCrossTrackMeters(mode == MovementMode.Vehicle);
            var withinCorridor = !cacheMiss &&
                PathGeometry.IsWithinRouteCorridor(origin, _cached.Points, corridorMargin);
            var targetChanged = _mapDestinationRecalcPending ||
                Time.unscaledTime - NavigationTargetTracker.LastChangeTime < 0.05f;
            var targetMoved = (target - _lastTarget).sqrMagnitude > 1f;
            var footMovedEnough = !_hasLastFootRecalcOrigin ||
                HorizontalDistanceSquared(origin, _lastFootRecalcOrigin) >=
                FootRecalcMinMovementMeters * FootRecalcMinMovementMeters;
            var footIntervalDue = mode == MovementMode.OnFoot &&
                footMovedEnough &&
                Time.unscaledTime - _lastFootRecalcTime >= ModConfig.RecalcIntervalSeconds;

            if (!forceRecalc &&
                !footIntervalDue &&
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
            if (footIntervalDue)
                forceRecalc = true;

            var recalcReason = RouteRecalcDiagnostics.BuildRecalcReason(
                forceRecalc,
                modeChanged,
                targetChanged,
                targetMoved,
                leftCorridor,
                cacheMiss);

            _lastAttemptOrigin = origin;
            _lastAttemptTarget = target;

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
                return ResolveDisplayCacheDuringRecalc(mode, target);

            var showBanner = mode == MovementMode.Vehicle &&
                (leftCorridor || forceRecalc || targetChanged || targetMoved);
            if (showBanner)
                RouteRecalcBanner.Show();

            if (targetChanged)
                _mapDestinationRecalcPending = false;

            _lastTarget = target;
            RouteWasRecalculated = true;
            if (mode == MovementMode.OnFoot)
            {
                _lastFootRecalcTime = Time.unscaledTime;
                _lastFootRecalcOrigin = origin;
                _hasLastFootRecalcOrigin = true;
            }

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
                !GameState.ShouldRunPathfinding() ||
                !NavigationTargetTracker.HasTarget)
                return false;

            RouteWasRecalculated = true;
            var completedMode = _asyncMode;
            LastFinalPoints = pending.Points ?? System.Array.Empty<Vector3>();
            _lastFinalPointsMode = completedMode;
            if (completedMode == MovementMode.OnFoot)
                _lastFootRecalcTime = Time.unscaledTime;
            if (completedMode == MovementMode.Vehicle && pending.Success)
                _lastGoodVehiclePath = pending;

            if (!pending.Success &&
                completedMode == MovementMode.Vehicle &&
                !_rejectLastGoodFallback &&
                TryReturnLastGoodVehiclePath(NavigationTargetTracker.ActiveTarget))
            {
                RecordRouteFailure(completedMode, _lastAttemptOrigin, _lastAttemptTarget);
                RouteRecalcDiagnostics.LogRecalc(
                    requestSource,
                    recalcReason + "|async|kept_last_good",
                    MovementModeDetector.CurrentMode,
                    0f,
                    RouteRecalcDiagnostics.LastPathfindMs,
                    0f,
                    _cached.Points?.Length ?? 0,
                    RoutePathfindKind.FullAStar,
                    true);
                _asyncRefreshPending = true;
                return true;
            }

            Cache(pending, completedMode);
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
            long totalTimer)
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
            var cancellation = new CancellationTokenSource();

            lock (AsyncGate)
            {
                _asyncInProgress = true;
                _asyncComplete = false;
                _asyncGeneration = generation;
                _asyncRequestSource = requestSource;
                _asyncRecalcReason = recalcReason;
                _asyncCancellation = cancellation;
                _asyncMode = mode;
            }

            _lastTarget = target;
            if (targetChanged)
                _mapDestinationRecalcPending = false;
            RouteRecalcBanner.Show();
            RouteRecalcDiagnostics.LogSkip(requestSource, "async_started",
                RouteRecalcDiagnostics.ElapsedMs(totalTimer));

            var pathOptions = VehicleRoutePathOptions.FromMainThread(target);
            ThreadPool.QueueUserWorkItem(_ =>
            {
                PathResult pending;
                var pathfindTimer = RouteRecalcDiagnostics.StartTimer();
                try
                {
                    pending = ComputeVehicleRouteSnapshot(
                        origin,
                        target,
                        forwardVec,
                        hasPose,
                        pathOptions,
                        cancellation.Token);
                }
                catch (System.Exception ex)
                {
                    if (!cancellation.IsCancellationRequested)
                        ModLog.Error("Async vehicle route failed", ex);
                    pending = Empty();
                }

                if (!cancellation.IsCancellationRequested)
                {
                    RouteRecalcDiagnostics.RecordPathfind(
                        pending.Success ? RoutePathfindKind.FullAStar : RoutePathfindKind.Failed,
                        RouteRecalcDiagnostics.ElapsedMs(pathfindTimer));
                }

                lock (AsyncGate)
                {
                    if (!cancellation.IsCancellationRequested &&
                        _asyncGeneration == generation &&
                        ReferenceEquals(_asyncCancellation, cancellation))
                    {
                        _asyncPendingResult = pending;
                        _asyncComplete = true;
                        _asyncCancellation = null;
                    }
                }

                cancellation.Dispose();
            });

            return true;
        }

        private static PathResult ComputeVehicleRouteSnapshot(
            Vector3 origin,
            Vector3 target,
            Vec3 forward,
            bool hasPose,
            VehicleRoutePathOptions pathOptions,
            CancellationToken cancellationToken)
        {
            if (!RoutePathfinder.TryFindPath(
                    origin,
                    target,
                    forward,
                    hasPose,
                    pathOptions,
                    cancellationToken,
                    out var navCorners) ||
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
            long totalTimer)
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
                if (FootSubwayRoutePlanner.TryBuildRoute(origin, target, sampleOrigin, out var footResult))
                {
                    RouteRecalcDiagnostics.RecordPathfind(
                        footResult.UsesSubway ? RoutePathfindKind.NavMeshFootSubway : RoutePathfindKind.NavMeshFoot,
                        RouteRecalcDiagnostics.ElapsedMs(pathfindTimer));

                    if (footResult.Success && footResult.Points != null && footResult.Points.Length >= 2)
                    {
                        LastFinalPoints = footResult.Points;
                        _lastFinalPointsMode = mode;
                        RouteRecalcDiagnostics.LogRecalc(
                            requestSource,
                            recalcReason + (footResult.UsesSubway ? "|subway" : string.Empty),
                            mode,
                            RouteRecalcDiagnostics.ElapsedMs(totalTimer),
                            RouteRecalcDiagnostics.ElapsedMs(pathfindTimer),
                            0f,
                            footResult.Points.Length,
                            footResult.UsesSubway ? RoutePathfindKind.NavMeshFootSubway : RoutePathfindKind.NavMeshFoot,
                            true);
                        return Cache(footResult, mode);
                    }
                }

                calculateOk = FootRouteCalculator.TryCalculate(
                    origin, target, sampleOrigin, NavPath, out pathFilterUsed, out status);
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
                    RecordRouteFailure(mode, _lastAttemptOrigin, _lastAttemptTarget);
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
                LastFinalPoints = System.Array.Empty<Vector3>();
                _lastFinalPointsMode = MovementMode.Unavailable;
                return Cache(Empty(), mode);
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
                LastFinalPoints = System.Array.Empty<Vector3>();
                _lastFinalPointsMode = MovementMode.Unavailable;
                return Cache(Empty(), mode);
            }

            var isPartial = status == NavMeshPathStatus.PathPartial;
            var terminalGap = HorizontalDistance(navCorners[navCorners.Length - 1], target);
            var acceptNearTargetPartial = isPartial &&
                                          mode == MovementMode.OnFoot &&
                                          terminalGap <= NearTargetPartialToleranceMeters;
            var acceptBuildingApproachPartial = isPartial &&
                                                mode == MovementMode.OnFoot &&
                                                terminalGap <= BuildingApproachPartialToleranceMeters &&
                                                NavigationTargetTracker.LastSource == NavigationTargetTracker.MapSource &&
                                                DestinationResolver.TryGetActiveMapAddress(out _);
            if (isPartial &&
                !ModConfig.ShowPartialPaths &&
                !acceptNearTargetPartial &&
                !acceptBuildingApproachPartial)
            {
                RouteRecalcDiagnostics.LogRecalcFailed(
                    requestSource,
                    recalcReason + "|partial_rejected",
                    mode,
                    RouteRecalcDiagnostics.ElapsedMs(totalTimer),
                    pathfindMs,
                    RouteRecalcDiagnostics.LastPathfindKind,
                    "partial=true terminal_gap_m=" +
                    terminalGap.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
                LastFinalPoints = System.Array.Empty<Vector3>();
                _lastFinalPointsMode = MovementMode.Unavailable;
                return Cache(Empty(), mode);
            }

            if (acceptNearTargetPartial)
                recalcReason += "|partial_near_target";
            else if (acceptBuildingApproachPartial)
                recalcReason += "|partial_building_approach_" +
                                terminalGap.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "m";

            var pipelineTimer = RouteRecalcDiagnostics.StartTimer();
            var linePoints = mode == MovementMode.Vehicle
                ? VehiclePathPipeline.BuildLinePoints(navCorners, origin, target, pathFilterUsed)
                : FootPathPipeline.BuildLinePoints(navCorners, origin);
            var pipelineMs = RouteRecalcDiagnostics.ElapsedMs(pipelineTimer);

            var success = linePoints.Length >= 2;
            LastFinalPoints = linePoints;
            _lastFinalPointsMode = mode;
            var result = new PathResult
            {
                Success = success,
                IsPartial = isPartial,
                Points = linePoints,
                Segments = new[]
                {
                    new RoutePathSegment
                    {
                        Kind = RoutePathSegmentKind.Foot,
                        Points = linePoints
                    }
                },
                Subway = SubwayNavigationHint.None
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

            return Cache(result, mode);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        internal static void NotifyMapDestinationChanged()
        {
            CancelAsyncRecalc();
            _cacheGeneration++;
            _cacheValid = false;
            _lastGoodVehiclePath = PathResult.None;
            LastFinalPoints = System.Array.Empty<Vector3>();
            _lastFinalPointsMode = MovementMode.Unavailable;
            _mapDestinationRecalcPending = true;
            ResetRouteFailureState();
            RouteRecalcDiagnostics.LogCacheInvalidated("map_destination_changed");
        }

        internal static void InvalidateCache(string reason = "unspecified")
        {
            CancelAsyncRecalc();
            _cacheGeneration++;
            _cached = Empty();
            _cacheValid = false;
            _lastMode = MovementMode.Unavailable;
            _lastFootRecalcTime = -999f;
            _lastFootRecalcOrigin = default;
            _hasLastFootRecalcOrigin = false;
            _lastGoodVehiclePath = PathResult.None;
            LastFinalPoints = System.Array.Empty<Vector3>();
            _cachedMode = MovementMode.Unavailable;
            _lastFinalPointsMode = MovementMode.Unavailable;
            PathGeometry.ResetVehicleLineTrimState();
            VehiclePathPipeline.InvalidateRouteLineCache();
            if (!SubwayLegTracker.IsRideCompleted)
                AutoWalkService.ResetSubwayState();
            ResetRouteFailureState();
            RouteRecalcDiagnostics.LogCacheInvalidated(reason);
        }

        /// <summary>Invalidate route cache and force the next GetRoute to recompute (e.g. mod option toggled).</summary>
        internal static void InvalidateCacheAndForceRecalc(string reason = "unspecified")
        {
            InvalidateCache(reason);
            _forceNextRecalc = true;
            _rejectLastGoodFallback = true;
            ModLog.Info("Route force-recalc requested | reason=" + reason);
        }

        private static void CancelAsyncRecalc()
        {
            CancellationTokenSource cancellation;
            lock (AsyncGate)
            {
                cancellation = _asyncCancellation;
                _asyncCancellation = null;
                _asyncInProgress = false;
                _asyncComplete = false;
                _asyncPendingResult = PathResult.None;
                _asyncMode = MovementMode.Unavailable;
            }

            try
            {
                cancellation?.Cancel();
            }
            catch (System.ObjectDisposedException)
            {
                // Worker completed between the state snapshot and cancellation.
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
            _cachedMode = MovementMode.Vehicle;
            RouteWasRecalculated = false;
            return true;
        }

        private static PathResult ResolveDisplayCacheDuringRecalc(MovementMode mode, Vector3 target)
        {
            if (_cacheValid && _cached.Success && _cachedMode == mode)
                return _cached;

            if (mode == MovementMode.Vehicle && TryReturnLastGoodVehiclePath(target))
                return _cached;

            return Empty();
        }

        private static bool TryUseFailedRouteCache(
            Vector3 origin,
            Vector3 target,
            MovementMode mode,
            bool forceRecalc,
            string requestSource,
            long totalTimer,
            out PathResult display)
        {
            display = Empty();
            if (_consecutiveRouteFailures <= 0)
                return false;

            var targetChanged = HorizontalDistanceSquared(target, _failedRouteTarget) >
                                FailedRouteTargetToleranceMeters * FailedRouteTargetToleranceMeters;
            var movedEnough = HorizontalDistanceSquared(origin, _failedRouteOrigin) >=
                              FailedRouteResetMovementMeters * FailedRouteResetMovementMeters;
            if (forceRecalc || mode != _failedRouteMode || targetChanged || movedEnough)
            {
                ResetRouteFailureState();
                return false;
            }

            if (!_failedRouteLocked && Time.unscaledTime >= _nextFailedRouteRetryTime)
                return false;

            RouteRecalcDiagnostics.LogSkip(
                requestSource,
                _failedRouteLocked ? "failed_route_locked" : "failed_route_backoff",
                RouteRecalcDiagnostics.ElapsedMs(totalTimer));
            display = ResolveDisplayCacheDuringRecalc(mode, target);
            return true;
        }

        private static void RecordRouteFailure(MovementMode mode, Vector3 origin, Vector3 target)
        {
            var sameRequest = _consecutiveRouteFailures > 0 &&
                              mode == _failedRouteMode &&
                              HorizontalDistanceSquared(target, _failedRouteTarget) <=
                              FailedRouteTargetToleranceMeters * FailedRouteTargetToleranceMeters;
            if (!sameRequest)
            {
                _consecutiveRouteFailures = 0;
                _failedRouteOrigin = origin;
                _failedRouteTarget = target;
                _failedRouteMode = mode;
                _failedRouteNotificationShown = false;
            }

            _consecutiveRouteFailures++;
            _nextFailedRouteRetryTime = Time.unscaledTime +
                                        FailedRouteRetryDelaySeconds *
                                        Mathf.Min(_consecutiveRouteFailures, 2);
            _failedRouteLocked = _consecutiveRouteFailures >= MaxConsecutiveRouteFailures;

            if (!_failedRouteLocked || _failedRouteNotificationShown)
                return;

            _failedRouteNotificationShown = true;
            RouteRecalcBanner.ShowUnavailable();
            ModLog.Info(() =>
                "Route retries stopped after " + _consecutiveRouteFailures +
                " failures; waiting for destination, mode, or significant position change.");
        }

        private static void ResetRouteFailureState()
        {
            _consecutiveRouteFailures = 0;
            _nextFailedRouteRetryTime = 0f;
            _failedRouteOrigin = default;
            _failedRouteTarget = default;
            _failedRouteMode = MovementMode.Unavailable;
            _failedRouteLocked = false;
            _failedRouteNotificationShown = false;
        }

        private static PathResult FinishSilent(long timer, string requestSource, PathResult result)
        {
            _ = timer;
            _ = requestSource;
            return result;
        }

        private static PathResult Cache(PathResult result, MovementMode mode)
        {
            _cached = result;
            _cacheValid = true;
            _cachedMode = mode;
            if (result.Success && result.Points != null && result.Points.Length >= 2)
            {
                LastFinalPoints = result.Points;
                _lastFinalPointsMode = mode;
                _rejectLastGoodFallback = false;
                ResetRouteFailureState();
            }
            else
                RecordRouteFailure(mode, _lastAttemptOrigin, _lastAttemptTarget);

            return result;
        }

        private static float HorizontalDistanceSquared(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        private static PathResult Empty()
        {
            var r = new PathResult();
            r.Points = System.Array.Empty<Vector3>();
            r.Segments = System.Array.Empty<RoutePathSegment>();
            r.Subway = SubwayNavigationHint.None;
            return r;
        }

        private static PathResult SilentEmpty() => Empty();
    }
}
