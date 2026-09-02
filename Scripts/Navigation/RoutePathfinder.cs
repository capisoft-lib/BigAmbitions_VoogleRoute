using System.Collections.Generic;
using System.Threading;

using UnityEngine;

using VoogleRoute.Pathfinding.Geometry;

using VoogleRoute.Pathfinding.Routing;



namespace VoogleRoute.Navigation

{

    /// <summary>

    /// Thin mod wrapper: main-thread query snapshot → PathFinding polyline → Unity vectors for display.

    /// </summary>

    internal static class RoutePathfinder

    {

        internal static bool TryFindPath(Vector3 origin, Vector3 destination, out Vector3[] corners)

        {

            var hasPose = MovementModeDetector.TryGetVehiclePose(out _, out var forward);

            return TryFindPath(

                origin,

                destination,

                hasPose ? ToVec3(forward) : default,

                hasPose,

                out corners);

        }



        internal static bool TryFindPath(

            Vector3 origin,

            Vector3 destination,

            Vec3 forward,

            bool hasPose,

            out Vector3[] corners)

        {

            return TryFindPath(origin, destination, forward, hasPose, out corners, forceCorrectSideArrivalOverride: null);

        }



        internal static bool TryFindPath(

            Vector3 origin,

            Vector3 destination,

            Vec3 forward,

            bool hasPose,

            out Vector3[] corners,

            bool? forceCorrectSideArrivalOverride)

        {

            var options = VehicleRoutePathOptions.FromMainThread(destination, forceCorrectSideArrivalOverride);

            return TryFindPath(origin, destination, forward, hasPose, options, out corners);

        }



        internal static bool TryFindPath(

            Vector3 origin,

            Vector3 destination,

            Vec3 forward,

            bool hasPose,

            VehicleRoutePathOptions options,

            out Vector3[] corners)

        {

            return TryFindPath(
                origin,
                destination,
                forward,
                hasPose,
                options,
                CancellationToken.None,
                out corners);

        }



        internal static bool TryFindPath(

            Vector3 origin,

            Vector3 destination,

            Vec3 forward,

            bool hasPose,

            VehicleRoutePathOptions options,

            CancellationToken cancellationToken,

            out Vector3[] corners)

        {

            corners = System.Array.Empty<Vector3>();

            var timer = RouteRecalcDiagnostics.StartTimer();



            if (!RouteGraphStore.TryEnsureLoaded())

            {

                RouteRecalcDiagnostics.RecordPathfind(RoutePathfindKind.Failed, RouteRecalcDiagnostics.ElapsedMs(timer));

                return false;

            }



            var graph = RouteGraphStore.Graph;

            var query = options.ToRouteQuery(
                ToVec3(origin),
                ToVec3(destination),
                forward,
                hasPose,
                cancellationToken);



            if (!VehicleRoutePolyline.TryBuild(graph, query, out var built))

            {

                RouteRecalcDiagnostics.RecordPathfind(RoutePathfindKind.Failed, RouteRecalcDiagnostics.ElapsedMs(timer));

                if (ModLog.IsEnabled(ModLogLevel.Debug))
                    ModLog.Debug(

                    "Vehicle route build failed | preferSide=" + options.PreferBuildingSideArrival +

                    " allowUturn=" + options.AllowUturnAtStart +

                    " arrivalHint=" + options.HasArrivalRoadHint +

                    (options.HasArrivalRoadHint

                        ? " hint=(" + options.ArrivalRoadHint.X.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "," +

                          options.ArrivalRoadHint.Z.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + ")"

                        : ""));

                return false;

            }



            corners = ToUnity(built.Points);

            if (corners.Length < 2)

                corners = new[] { origin, destination };



            var last = corners[corners.Length - 1];

            if (ModLog.IsEnabled(ModLogLevel.Debug))
                ModLog.Debug(

                "Vehicle route built | preferSide=" + options.PreferBuildingSideArrival +

                " allowUturn=" + options.AllowUturnAtStart +

                " append=" + built.AppendMode +

                " poly=" + built.Points.Count +

                " cost=" + built.GraphCostMeters.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) +

                "m endWp=" + built.Route.EndWaypoint +

                " last=(" + last.x.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "," +

                last.z.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + ")");



            var success = corners.Length >= 2;

            RouteRecalcDiagnostics.RecordPathfind(

                success ? RoutePathfindKind.FullAStar : RoutePathfindKind.Failed,

                RouteRecalcDiagnostics.ElapsedMs(timer));

            return success;

        }



        private static Vec3 ToVec3(Vector3 v) => new Vec3(v.x, v.y, v.z);



        private static Vector3[] ToUnity(IReadOnlyList<Vec3> points)

        {

            var array = new Vector3[points.Count];

            for (var i = 0; i < points.Count; i++)

            {

                var p = points[i];

                array[i] = new Vector3(p.X, p.Y, p.Z);

            }



            return array;

        }

    }

}


