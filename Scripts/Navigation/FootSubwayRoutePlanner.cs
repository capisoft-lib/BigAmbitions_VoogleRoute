using System;
using System.Collections.Generic;
using UnityEngine;
using VoogleRoute;
using VoogleRoute.Pathfinding.Geometry;
using VoogleRoute.Pathfinding.Routing.Foot;
using DllSubwayStation = VoogleRoute.Pathfinding.Routing.Foot.SubwayStation;
using PathFootPlanner = VoogleRoute.Pathfinding.Routing.Foot.FootSubwayRoutePlanner;

namespace VoogleRoute.Navigation
{
    /// <summary>
    /// Thin mod wrapper: NavMesh foot legs + PathFinding subway planner → Unity PathResult for display.
    /// </summary>
    internal static class FootSubwayRoutePlanner
    {
        internal static bool TryBuildRoute(
            Vector3 origin,
            Vector3 target,
            Vector3 sampleOrigin,
            out PathResult result)
        {
            result = PathResult.None;

            SubwayGraph.RefreshBridgePaths();

            var stations = CollectDllStations();
            var options = new FootRouteOptions
            {
                UseSubwayEnabled = ModConfig.UseSubwayEnabled,
                AllowSubwayPlanning = SubwayLegTracker.ShouldPlanSubway(),
                ShowPartialPaths = ModConfig.ShowPartialPaths
            };

            if (!PathFootPlanner.TryBuildRoute(
                    ToVec3(origin),
                    ToVec3(target),
                    ToVec3(sampleOrigin),
                    NavMeshFootPathProvider.Instance,
                    stations,
                    SubwayGraph.Network,
                    options,
                    out var built))
                return false;

            result = ToPathResult(built);

            if (built.UsesSubway)
                SubwayLegTracker.Bind(built.Subway.BoardStationName, built.Subway.ExitStationName);
            else
                SubwayLegTracker.Clear();

            return result.Success;
        }

        internal static bool TryEstimateMeters(Vector3 origin, Vector3 target, out float meters)
        {
            meters = -1f;
            if (!TryBuildRoute(origin, target, origin, out var result) || !result.Success)
                return false;

            meters = VehiclePathArrival.PolylineLength(result.Points);
            return meters > 0f;
        }

        private static IReadOnlyList<DllSubwayStation> CollectDllStations()
        {
            if (!SubwayStationStore.TryEnsureLoaded())
                return Array.Empty<DllSubwayStation>();

            var source = SubwayStationStore.All;
            var list = new List<DllSubwayStation>(source.Count);
            for (var i = 0; i < source.Count; i++)
            {
                var s = source[i];
                list.Add(new DllSubwayStation
                {
                    Index = s.Index,
                    StationName = s.StationName,
                    Neighborhood = s.Neighborhood ?? string.Empty,
                    WorldPosition = ToVec3(s.WorldPosition),
                    NavPosition = ToVec3(s.NavPosition)
                });
            }

            return list;
        }

        private static PathResult ToPathResult(FootRouteResult built)
        {
            var segments = new RoutePathSegment[built.Segments.Count];
            for (var i = 0; i < built.Segments.Count; i++)
            {
                var seg = built.Segments[i];
                segments[i] = new RoutePathSegment
                {
                    Kind = seg.Kind == FootRouteSegmentKind.Foot
                        ? RoutePathSegmentKind.Foot
                        : RoutePathSegmentKind.Subway,
                    Points = ToUnityArray(seg.Points)
                };
            }

            var subway = built.Subway.Active
                ? new SubwayNavigationHint
                {
                    Active = true,
                    BoardStationName = built.Subway.BoardStationName,
                    ExitStationName = built.Subway.ExitStationName,
                    BoardNavPosition = ToUnity(built.Subway.BoardNavPosition),
                    ExitNavPosition = ToUnity(built.Subway.ExitNavPosition),
                    BoardWorldPosition = ToUnity(built.Subway.BoardWorldPosition),
                    ExitWorldPosition = ToUnity(built.Subway.ExitWorldPosition)
                }
                : SubwayNavigationHint.None;

            return new PathResult
            {
                Success = built.Success,
                IsPartial = built.IsPartial,
                Points = ToUnityArray(built.Points),
                Segments = segments,
                Subway = subway
            };
        }

        private static Vec3 ToVec3(Vector3 v) => new Vec3(v.x, v.y, v.z);
        private static Vector3 ToUnity(Vec3 v) => new Vector3(v.X, v.Y, v.Z);

        private static Vector3[] ToUnityArray(IReadOnlyList<Vec3> points)
        {
            var array = new Vector3[points.Count];
            for (var i = 0; i < points.Count; i++)
                array[i] = ToUnity(points[i]);
            return array;
        }
    }
}
