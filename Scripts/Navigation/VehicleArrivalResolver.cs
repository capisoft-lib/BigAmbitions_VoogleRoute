using UnityEngine;
using VoogleRoute;

namespace VoogleRoute.Navigation
{
    /// <summary>Vehicle arrival leg: snap to the building-side lane when enabled in mod options.</summary>
    internal static class VehicleArrivalResolver
    {
        internal const float MaxArrivalZoneMeters = 65f;

        internal struct ArrivalSegment
        {
            internal Vector3 LanePoint;
            internal Vector3 FinalTarget;
        }

        internal static float EstimateArrivalLegCost(
            TrafficWaypointGraph graph,
            int endWaypointIdx,
            Vector3 buildingTarget)
        {
            if (!ModConfig.ForceCorrectSideArrivalEnabled)
                return EstimateDirectLegCost(graph, endWaypointIdx, buildingTarget);

            if (endWaypointIdx < 0)
                return VehiclePathArrival.FlatDistance(ResolveRoadSnap(buildingTarget), buildingTarget);

            var forwardPos = graph.GetPosition(endWaypointIdx);
            var sameLaneCost = VehiclePathArrival.FlatDistance(ResolveRoadSnap(buildingTarget), buildingTarget);
            var forwardDist = VehiclePathArrival.FlatDistance(forwardPos, buildingTarget);

            if (forwardDist > MaxArrivalZoneMeters)
                return Mathf.Min(sameLaneCost, forwardDist);

            return Mathf.Min(sameLaneCost, forwardDist);
        }

        internal static ArrivalSegment Resolve(
            TrafficWaypointGraph graph,
            int endWaypointIdx,
            Vector3 buildingTarget)
        {
            if (!ModConfig.ForceCorrectSideArrivalEnabled)
                return ResolveDirect(graph, endWaypointIdx);

            if (endWaypointIdx < 0)
            {
                return new ArrivalSegment
                {
                    LanePoint = ResolveRoadSnap(buildingTarget),
                    FinalTarget = buildingTarget
                };
            }

            var forwardPos = graph.GetPosition(endWaypointIdx);
            var forwardDist = VehiclePathArrival.FlatDistance(forwardPos, buildingTarget);
            var sameLanePoint = ResolveRoadSnap(buildingTarget);
            var sameLaneCost = VehiclePathArrival.FlatDistance(sameLanePoint, buildingTarget);

            if (forwardDist > MaxArrivalZoneMeters)
            {
                return new ArrivalSegment
                {
                    LanePoint = forwardPos,
                    FinalTarget = sameLanePoint
                };
            }

            sameLaneCost = Mathf.Min(sameLaneCost, forwardDist);
            return new ArrivalSegment
            {
                LanePoint = sameLanePoint,
                FinalTarget = buildingTarget
            };
        }

        private static ArrivalSegment ResolveDirect(TrafficWaypointGraph graph, int endWaypointIdx)
        {
            var lanePoint = endWaypointIdx >= 0 ? graph.GetPosition(endWaypointIdx) : Vector3.zero;
            return new ArrivalSegment
            {
                LanePoint = lanePoint,
                FinalTarget = lanePoint
            };
        }

        private static float EstimateDirectLegCost(
            TrafficWaypointGraph graph,
            int endWaypointIdx,
            Vector3 buildingTarget)
        {
            if (endWaypointIdx < 0)
                return 0f;

            return VehiclePathArrival.FlatDistance(graph.GetPosition(endWaypointIdx), buildingTarget);
        }

        private static Vector3 ResolveRoadSnap(Vector3 buildingTarget)
        {
            if (VehiclePathHelper.TryGetArrivalRoadTarget(buildingTarget, out var snap))
                return snap;

            return buildingTarget;
        }
    }
}
