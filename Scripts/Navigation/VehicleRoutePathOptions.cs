using UnityEngine;
using VoogleRoute;
using VoogleRoute.Pathfinding.Geometry;
using VoogleRoute.Pathfinding.Routing;

namespace VoogleRoute.Navigation
{
    /// <summary>Main-thread snapshot of route options and arrival hints for background pathfinding.</summary>
    internal readonly struct VehicleRoutePathOptions
    {
        internal bool PreferBuildingSideArrival { get; }
        internal bool AllowUturnAtStart { get; }
        internal bool HasArrivalRoadHint { get; }
        internal Vec3 ArrivalRoadHint { get; }

        internal VehicleRoutePathOptions(
            bool preferBuildingSideArrival,
            bool allowUturnAtStart,
            bool hasArrivalRoadHint,
            Vec3 arrivalRoadHint)
        {
            PreferBuildingSideArrival = preferBuildingSideArrival;
            AllowUturnAtStart = allowUturnAtStart;
            HasArrivalRoadHint = hasArrivalRoadHint;
            ArrivalRoadHint = arrivalRoadHint;
        }

        internal static VehicleRoutePathOptions FromMainThread(Vector3 target, bool? forceCorrectSideArrivalOverride = null)
        {
            var useCorrectSide = forceCorrectSideArrivalOverride ?? ModConfig.ForceCorrectSideArrivalEnabled;
            var hasHint = false;
            Vector3 snap = default;
            if (useCorrectSide && VehiclePathHelper.TryGetArrivalRoadTarget(target, out snap))
                hasHint = true;

            return new VehicleRoutePathOptions(
                useCorrectSide,
                ModConfig.AllowUturnAtStartEnabled,
                hasHint,
                hasHint ? new Vec3(snap.x, snap.y, snap.z) : default);
        }

        internal RouteQuery ToRouteQuery(Vec3 origin, Vec3 destination, Vec3 forward, bool hasPose) =>
            new RouteQuery
            {
                Origin = origin,
                Destination = destination,
                Forward = forward,
                HasPose = hasPose,
                ForcedStartWaypoint = -1,
                ForcedEndWaypoint = -1,
                AllowUturnAtStart = AllowUturnAtStart,
                PreferBuildingSideArrival = PreferBuildingSideArrival,
                HasArrivalRoadHint = HasArrivalRoadHint,
                ArrivalRoadHint = ArrivalRoadHint
            };
    }
}
