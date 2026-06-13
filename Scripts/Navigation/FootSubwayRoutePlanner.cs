using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using VoogleRoute;
using VoogleRoute.Rendering;

namespace VoogleRoute.Navigation
{
    /// <summary>Foot routing with subway fallback when NavMesh cannot reach the destination.</summary>
    internal static class FootSubwayRoutePlanner
    {
        private const int MaxStationCandidates = 5;
        private const float MaxStationPickMeters = 900f;
        private const float SubwayRidePenaltyMeters = 40f;

        private static readonly NavMeshPath NavPath = new NavMeshPath();

        internal static bool TryBuildRoute(
            Vector3 origin,
            Vector3 target,
            Vector3 sampleOrigin,
            out PathResult result)
        {
            result = PathResult.None;

            if (TryBuildDirect(origin, target, sampleOrigin, out result))
                return true;

            if (!SubwayStationStore.TryEnsureLoaded())
                return false;

            SubwayGraph.RefreshBridgePaths();
            return TryBuildViaSubway(origin, target, sampleOrigin, out result);
        }

        internal static bool TryEstimateMeters(Vector3 origin, Vector3 target, out float meters)
        {
            meters = -1f;
            if (!TryBuildRoute(origin, target, origin, out var result) || !result.Success)
                return false;

            meters = VehiclePathArrival.PolylineLength(result.Points);
            return meters > 0f;
        }

        private static bool TryBuildDirect(
            Vector3 origin,
            Vector3 target,
            Vector3 sampleOrigin,
            out PathResult result)
        {
            result = PathResult.None;
            NavMeshPathStatus status;

            if (!FootRouteCalculator.TryCalculate(origin, target, sampleOrigin, NavPath, out _, out status))
                return false;

            if (status == NavMeshPathStatus.PathInvalid)
                return false;

            var isPartial = status == NavMeshPathStatus.PathPartial;
            if (isPartial && !ModConfig.ShowPartialPaths)
                return false;

            var corners = NavPath.corners;
            if (corners == null || corners.Length == 0)
                return false;

            var linePoints = FootPathPipeline.BuildLinePoints(corners, origin);
            if (linePoints.Length < 2)
                return false;

            result = new PathResult
            {
                Success = true,
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
            return true;
        }

        private static bool TryBuildViaSubway(
            Vector3 origin,
            Vector3 target,
            Vector3 sampleOrigin,
            out PathResult result)
        {
            result = PathResult.None;
            var stations = SubwayStationStore.All;
            if (stations.Count == 0)
                return false;

            var boardCandidates = CollectNearestCandidates(origin, stations);
            var exitCandidates = CollectNearestCandidates(target, stations);
            if (boardCandidates.Count == 0 || exitCandidates.Count == 0)
                return false;

            var bestCost = float.PositiveInfinity;
            SubwayStationRecord bestBoard = null;
            SubwayStationRecord bestExit = null;
            Vector3[] bestWalkToBoard = null;
            Vector3[] bestWalkFromExit = null;
            Vector3[] bestSubwayDisplay = null;
            var bestPartial = false;

            for (var bi = 0; bi < boardCandidates.Count; bi++)
            {
                var board = boardCandidates[bi];
                if (!TryBuildFootLeg(origin, board.NavPosition, sampleOrigin, out var walkToBoard, out var walkToBoardPartial))
                    continue;

                var walkToBoardLen = VehiclePathArrival.PolylineLength(walkToBoard);

                for (var ei = 0; ei < exitCandidates.Count; ei++)
                {
                    var exit = exitCandidates[ei];
                    if (!TryBuildFootLeg(exit.NavPosition, target, exit.NavPosition, out var walkFromExit, out var walkFromExitPartial))
                        continue;

                    var subwayDisplay = SubwayGraph.BuildDisplayPath(board, exit);
                    if (subwayDisplay.Length < 2)
                        continue;

                    var subwayLen = VehiclePathArrival.PolylineLength(subwayDisplay);
                    var walkFromExitLen = VehiclePathArrival.PolylineLength(walkFromExit);
                    var total = walkToBoardLen + subwayLen + walkFromExitLen + SubwayRidePenaltyMeters;

                    if (total >= bestCost)
                        continue;

                    bestCost = total;
                    bestBoard = board;
                    bestExit = exit;
                    bestWalkToBoard = walkToBoard;
                    bestWalkFromExit = walkFromExit;
                    bestSubwayDisplay = subwayDisplay;
                    bestPartial = walkToBoardPartial || walkFromExitPartial;
                }
            }

            if (bestBoard == null || bestExit == null || bestWalkToBoard == null || bestWalkFromExit == null)
                return false;

            var segments = new[]
            {
                new RoutePathSegment { Kind = RoutePathSegmentKind.Foot, Points = bestWalkToBoard },
                new RoutePathSegment { Kind = RoutePathSegmentKind.Subway, Points = bestSubwayDisplay },
                new RoutePathSegment { Kind = RoutePathSegmentKind.Foot, Points = bestWalkFromExit }
            };

            result = new PathResult
            {
                Success = true,
                IsPartial = bestPartial,
                Points = ConcatenateSegments(segments),
                Segments = segments,
                Subway = new SubwayNavigationHint
                {
                    Active = true,
                    BoardStationName = bestBoard.StationName,
                    ExitStationName = bestExit.StationName,
                    BoardNavPosition = bestBoard.NavPosition,
                    ExitNavPosition = bestExit.NavPosition
                }
            };

            return true;
        }

        private static List<SubwayStationRecord> CollectNearestCandidates(
            Vector3 worldPos,
            IReadOnlyList<SubwayStationRecord> stations)
        {
            var ranked = new List<(SubwayStationRecord Station, float Distance)>(stations.Count);
            for (var i = 0; i < stations.Count; i++)
            {
                var station = stations[i];
                var distance = station.HorizontalDistanceTo(worldPos);
                if (distance > MaxStationPickMeters)
                    continue;

                ranked.Add((station, distance));
            }

            ranked.Sort((a, b) => a.Distance.CompareTo(b.Distance));

            var result = new List<SubwayStationRecord>(MaxStationCandidates);
            for (var i = 0; i < ranked.Count && result.Count < MaxStationCandidates; i++)
                result.Add(ranked[i].Station);

            return result;
        }

        private static bool TryBuildFootLeg(
            Vector3 origin,
            Vector3 target,
            Vector3 sampleOrigin,
            out Vector3[] linePoints,
            out bool isPartial)
        {
            linePoints = Array.Empty<Vector3>();
            isPartial = false;
            NavMeshPathStatus status;

            if (!FootRouteCalculator.TryCalculate(origin, target, sampleOrigin, NavPath, out _, out status))
                return false;

            if (status == NavMeshPathStatus.PathInvalid)
                return false;

            isPartial = status == NavMeshPathStatus.PathPartial;
            if (isPartial && !ModConfig.ShowPartialPaths)
                return false;

            var corners = NavPath.corners;
            if (corners == null || corners.Length == 0)
                return false;

            linePoints = FootPathPipeline.BuildLinePoints(corners, origin);
            return linePoints.Length >= 2;
        }

        private static Vector3[] ConcatenateSegments(RoutePathSegment[] segments)
        {
            var total = 0;
            for (var i = 0; i < segments.Length; i++)
                total += segments[i].Points?.Length ?? 0;

            if (total < 2)
                return Array.Empty<Vector3>();

            var merged = new List<Vector3>(total);
            for (var i = 0; i < segments.Length; i++)
            {
                var points = segments[i].Points;
                if (points == null || points.Length == 0)
                    continue;

                for (var p = 0; p < points.Length; p++)
                {
                    if (merged.Count > 0 && (points[p] - merged[^1]).sqrMagnitude < 0.04f)
                        continue;

                    merged.Add(points[p]);
                }
            }

            return merged.Count >= 2 ? merged.ToArray() : Array.Empty<Vector3>();
        }
    }
}
