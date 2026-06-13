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

        internal static Vector3 ActiveExitTarget { get; private set; }

        internal static PathResult GetRoute(bool forceRecalc = false)
        {
            if (!GameState.IsIndoorNavigationContext())
                return ResetAndReturnEmpty();

            if (!MovementModeDetector.TryGetPathOrigin(out var origin))
                return ResetAndReturnEmpty();

            if (!IndoorExitResolver.TryGetNearestExit(origin, out var exit))
                return ResetAndReturnEmpty();

            ActiveExitTarget = exit;

            var originMoved = (_lastOrigin - origin).sqrMagnitude > 1f;
            var exitChanged = (_lastExit - exit).sqrMagnitude > 0.25f;

            if (!forceRecalc && _cacheValid && !originMoved && !exitChanged)
                return _cached;

            _lastOrigin = origin;
            _lastExit = exit;

            var sampleOrigin = origin;
            if (MovementModeDetector.TryGetPlayerOrigin(out var feet))
                sampleOrigin = feet;

            if (!FootRouteCalculator.TryCalculate(origin, exit, sampleOrigin, NavPath, out _))
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
            ActiveExitTarget = default;
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
