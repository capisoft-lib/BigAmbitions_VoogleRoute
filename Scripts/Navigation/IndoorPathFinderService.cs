using UnityEngine;
using UnityEngine.AI;

namespace VoogleRoute.Navigation
{
    internal static class IndoorPathFinderService
    {
        private static readonly NavMeshPath NavPath = new NavMeshPath();
        private static Vector3 _lastOrigin;
        private static Vector3 _lastExit;
        private static PathResult _cached;
        private static bool _cacheValid;
        private static int _lastHamptonsHouseId;

        internal static IndoorExitTarget ActiveExit { get; private set; }

        internal static PathResult GetRoute(bool forceRecalc = false)
        {
            if (!GameState.IsIndoorNavigationContext())
                return ResetAndReturnEmpty();

            if (!MovementModeDetector.TryGetPathOrigin(out var origin))
                return ResetAndReturnEmpty();

            if (HamptonsCompatibility.TryGetCurrentHouseId(out var hamptonsHouseId))
                return GetHamptonsRoute(hamptonsHouseId, origin, forceRecalc);

            if (!IndoorExitResolver.TryGetNearestExit(origin, out var exit))
                return ResetAndReturnEmpty();

            ActiveExit = exit;

            var originMoved = (_lastOrigin - origin).sqrMagnitude > 1f;
            var exitChanged = (_lastExit - exit.WalkPosition).sqrMagnitude > 0.25f;

            if (!forceRecalc && _cacheValid && !originMoved && !exitChanged)
                return _cached;

            _lastOrigin = origin;
            _lastExit = exit.WalkPosition;

            var sampleOrigin = origin;
            if (MovementModeDetector.TryGetPlayerOrigin(out var feet))
                sampleOrigin = feet;

            if (!FootRouteCalculator.TryCalculate(origin, exit.WalkPosition, sampleOrigin, NavPath, out _))
                return Cache(PathResult.None);

            var corners = NavPath.corners;
            if (corners == null || corners.Length < 2)
                return Cache(PathResult.None);

            var linePoints = FootPathPipeline.BuildLinePoints(corners, origin);
            if (linePoints.Length < 2)
                return Cache(PathResult.None);

            return Cache(new PathResult
            {
                Success = true,
                IsPartial = NavPath.status == NavMeshPathStatus.PathPartial,
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
            });
        }

        internal static void InvalidateCache()
        {
            _cacheValid = false;
            _cached = PathResult.None;
            _lastOrigin = default;
            _lastExit = default;
            _lastHamptonsHouseId = 0;
            ActiveExit = IndoorExitTarget.None;
            HamptonsCompatibility.InvalidateCache();
        }

        private static PathResult GetHamptonsRoute(int houseId, Vector3 origin, bool forceRecalc)
        {
            var originMoved = (_lastOrigin - origin).sqrMagnitude > 1f;
            var sameHouseCache = _cacheValid && ActiveExit.IsHamptonsPlotExit &&
                                 _lastHamptonsHouseId == houseId;
            // The exterior transition strip is a separate NavMesh island in
            // some plots. Once auto-walk has accepted a complete path, keep
            // that route stable until the native Hamptons exit clears it.
            if (!forceRecalc && sameHouseCache &&
                (!originMoved || ModConfig.IndoorAutoWalkEnabled))
                return _cached;

            _lastOrigin = origin;
            ActiveExit = IndoorExitTarget.None;
            if (!HamptonsCompatibility.TryCalculateCurrentRoute(origin, NavPath, out var exit))
                return Cache(PathResult.None);

            ActiveExit = exit;
            _lastHamptonsHouseId = houseId;
            _lastExit = exit.WalkPosition;
            var corners = NavPath.corners;
            if (corners == null || corners.Length < 2)
                return Cache(PathResult.None);

            // Preserve the stairs and floor elevations exactly. Ground projection
            // can otherwise collapse an upper-floor route onto the ground floor.
            var linePoints = FootPathPipeline.BuildVanillaLinePoints(corners, ModConfig.FootGroundOffset);
            if (linePoints.Length < 2)
                return Cache(PathResult.None);

            return Cache(new PathResult
            {
                Success = true,
                IsPartial = NavPath.status == UnityEngine.AI.NavMeshPathStatus.PathPartial,
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
            });
        }

        private static PathResult ResetAndReturnEmpty()
        {
            InvalidateCache();
            return PathResult.None;
        }

        private static PathResult Cache(PathResult result)
        {
            _cached = result;
            _cacheValid = result.Success;
            return result;
        }
    }
}
