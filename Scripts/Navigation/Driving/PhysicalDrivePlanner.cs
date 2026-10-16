using System.Collections.Generic;
using GleyTrafficSystem;
using Helpers;
using UnityEngine;
using VoogleRoute.Pathfinding.Routing;
using System.Globalization;

namespace VoogleRoute.Navigation.Driving
{
    internal sealed class PhysicalDrivePlan
    {
        internal CarController Car;
        internal Vector3 Destination;
        internal DrivingPath Path;
        internal readonly List<TrafficMarker> Traffic = new List<TrafficMarker>();
        internal sealed class TrafficMarker
        {
            internal Waypoint Waypoint;
            internal float Distance;
        }
    }

    internal static class PhysicalDrivePlanner
    {
        internal static bool TryBuild(out PhysicalDrivePlan plan, out string failure)
        {
            plan = null;
            failure = "voogle_route_physical_no_route";
            var car = VehicleHelper.GetCurrentVehicleBase() as CarController;
            if (car?.vehicleController == null || !MovementModeDetector.CanUseAutoDrive())
            { failure = "voogle_route_autodrive_not_in_vehicle"; return false; }
            if (!NavigationTargetTracker.HasMapGpsTarget || RouteGraphStore.Graph == null)
                return Reject("missing_destination_or_graph", "", out failure);
            using (var geometry = new VehicleControlLease(car, acquire: false))
            {
                Debug.Log("[VoogleRoute][PhysicalDrive] plan_request car=" + car.name + " rear=" + geometry.RearPosition.ToString("F2") +
                    " forward=" + geometry.Vehicle.transform.forward.ToString("F2") + " destination=" + NavigationTargetTracker.ActiveTarget.ToString("F2") +
                    " wheelbase=" + geometry.Wheelbase.ToString("F2", CultureInfo.InvariantCulture) +
                    " steer=" + geometry.MaxSteer.ToString("F1", CultureInfo.InvariantCulture) +
                    " radius=" + geometry.MinRadius.ToString("F2", CultureInfo.InvariantCulture));
                if (geometry.Speed > 0.7f)
                    return Reject("vehicle_moving", "speed=" + geometry.Speed, out failure, "voogle_route_physical_start_lane");
                var origin = geometry.RearPosition;
                var destination = NavigationTargetTracker.ActiveTarget;
                var options = VehicleRoutePathOptions.FromMainThread(destination);
                var query = new VehicleRoutePathOptions(options.PreferBuildingSideArrival, false, options.HasArrivalRoadHint, options.ArrivalRoadHint)
                    .ToRouteQuery(PhysicalRoutingGraph.Native(origin), PhysicalRoutingGraph.Native(destination),
                        PhysicalRoutingGraph.Native(car.vehicleController.transform.forward), true);
                var graph = new PhysicalRoutingGraph(RouteGraphStore.Graph, geometry.MinRadius * 1.1f);
                if (!TryFindLaneStart(graph, geometry, out var start))
                    return Reject("start_lane_not_found", "", out failure, "voogle_route_physical_start_lane");
                query = new RouteQuery
                {
                    Origin = query.Origin, Destination = query.Destination, Forward = query.Forward,
                    HasPose = true, ForcedStartWaypoint = start, ForcedEndWaypoint = -1,
                    AllowUturnAtStart = false, PreferBuildingSideArrival = options.PreferBuildingSideArrival,
                    HasArrivalRoadHint = query.HasArrivalRoadHint, ArrivalRoadHint = query.ArrivalRoadHint
                };
                if (!DrivingRouteGeometry.TryBuild(graph, query, geometry.MinRadius * 1.08f,
                    out var route, out var samples, out var geometryDetail))
                    return Reject("trajectory_unavailable", geometryDetail + " start=" + start,
                        out failure, geometryDetail.StartsWith("graph_search_failed attempt=0")
                            ? "voogle_route_physical_no_route" : "voogle_route_physical_turn_tight");
                Debug.Log("[VoogleRoute][PhysicalDrive] curve_fit_ok " + geometryDetail);
                var path = new DrivingPath(samples, ModConfig.PhysicalDriveSpeedKmh / 3.6f);
                if (path.Length < 12f) return Reject("destination_too_close", "", out failure, "voogle_route_autodrive_too_close");
                var progress = path.Project(origin, 0f, out var lateral, searchAhead: 50f);
                var direction = DrivingPath.Flat(path.At(progress + 3f) - path.At(progress));
                if (lateral > 2.5f ||
                    Vector3.Angle(direction, geometry.Vehicle.transform.forward) > 35f)
                    return Reject("start_not_aligned", "lateral=" + lateral + " progress=" + progress,
                        out failure, "voogle_route_physical_start_lane");
                if (path.Length - progress < 12f)
                    return Reject("destination_too_close", "", out failure, "voogle_route_autodrive_too_close");
                var arrivalAnchor = options.HasArrivalRoadHint ? PhysicalRoutingGraph.Unity(options.ArrivalRoadHint) : destination;
                if (DrivingPath.FlatDistance(samples[samples.Count - 1], arrivalAnchor) > 80f)
                    return Reject("arrival_too_far", "end=" + samples[samples.Count - 1] + " anchor=" + arrivalAnchor, out failure);
                if (AutoDriveVehicleFuel.TryEstimate(path.Length, out var fuel) && !fuel.HasEnoughFuel)
                    return Reject("insufficient_fuel", "", out failure, "voogle_route_autodrive_insufficient_fuel");

                plan = new PhysicalDrivePlan { Car = car, Destination = destination, Path = path };
                BindTraffic(plan, route.Path);
                plan.Path.ApplyBrakingEnvelope();
                Debug.Log("[VoogleRoute][PhysicalDrive] plan_ok distance=" + Mathf.RoundToInt(path.Length) +
                    " samples=" + samples.Count + " traffic_markers=" + plan.Traffic.Count);
                return true;
            }
        }

        private static bool Reject(string stage, string detail, out string failure,
            string key = "voogle_route_physical_no_route")
        {
            failure = key;
            var displayed = PathFinderService.TryGetCachedRouteForDisplay(out var visible);
            Debug.Log("[VoogleRoute][PhysicalDrive] plan_rejected stage=" + stage + " " + detail +
                " displayed_route=" + displayed + " displayed_points=" + (visible.Points?.Length ?? 0));
            return false;
        }

        private static bool TryFindLaneStart(PhysicalRoutingGraph graph, VehicleControlLease car, out int start)
        {
            var candidates = new int[96];
            var count = graph.CollectNearest(PhysicalRoutingGraph.Native(car.RearPosition), 40f, candidates);
            start = -1;
            var best = 2.5f * 2.5f;
            foreach (var index in new System.ArraySegment<int>(candidates, 0, count))
            {
                var a = PhysicalRoutingGraph.Unity(graph.GetPosition(index));
                foreach (var next in graph.GetForwardNeighbors(index))
                {
                    if (graph.IsSyntheticTurnEdge(index, next)) continue;
                    var b = PhysicalRoutingGraph.Unity(graph.GetPosition(next));
                    var d = DrivingPath.Flat(b - a);
                    if (d.sqrMagnitude < 1f || Vector3.Angle(d, car.Vehicle.transform.forward) > 30f) continue;
                    var t = Vector3.Dot(DrivingPath.Flat(car.RearPosition - a), d) / d.sqrMagnitude;
                    if (t < -0.01f || t > 1f) continue;
                    var projected = Vector3.Lerp(a, b, Mathf.Clamp01(t));
                    if (Mathf.Abs(projected.y - car.RearPosition.y) > 2f) continue;
                    var error = DrivingPath.Flat(car.RearPosition - projected).sqrMagnitude;
                    if (error >= best) continue;
                    best = error; start = index;
                }
            }
            return start >= 0;
        }

        private static void BindTraffic(PhysicalDrivePlan plan, IReadOnlyList<int> nodes)
        {
            var scene = GleyUrbanAssets.CurrentSceneData.GetSceneInstance();
            var waypoints = scene?.allWaypoints;
            if (waypoints == null) throw new System.InvalidOperationException("Live traffic graph is unavailable.");
            var grid = new Dictionary<Vector2Int, List<Waypoint>>();
            foreach (var wp in waypoints)
            {
                if (wp == null || wp.temporaryDisabled) continue;
                var key = Cell(wp.position);
                if (!grid.TryGetValue(key, out var bucket)) grid[key] = bucket = new List<Waypoint>();
                bucket.Add(wp);
            }
            var seen = new HashSet<int>();
            foreach (var node in nodes)
            {
                var p = PhysicalRoutingGraph.Unity(RouteGraphStore.Graph.GetPosition(node));
                var cell = Cell(p);
                Waypoint nearest = null;
                var best = 1.5f * 1.5f;
                for (var x = -1; x <= 1; x++) for (var z = -1; z <= 1; z++)
                {
                    if (!grid.TryGetValue(cell + new Vector2Int(x, z), out var bucket)) continue;
                    foreach (var wp in bucket)
                    {
                        var d = (wp.position - p).sqrMagnitude;
                        if (d >= best) continue;
                        best = d; nearest = wp;
                    }
                }
                if (nearest == null || !seen.Add(nearest.listIndex)) continue;
                var distance = NearestDistance(plan.Path, nearest.position);
                plan.Traffic.Add(new PhysicalDrivePlan.TrafficMarker { Waypoint = nearest, Distance = distance });
                if (nearest.maxSpeed > 0)
                {
                    var index = System.Array.BinarySearch(plan.Path.Distances, distance);
                    if (index < 0) index = ~index;
                    // Apply the native road limit until the next native road anchor.
                    for (var j = Mathf.Clamp(index - 1, 0, plan.Path.SpeedLimits.Length - 1);
                         j < plan.Path.SpeedLimits.Length && plan.Path.Distances[j] < distance + 15f; j++)
                        plan.Path.SpeedLimits[j] = Mathf.Min(plan.Path.SpeedLimits[j], nearest.maxSpeed / 3.6f);
                }
            }
            plan.Traffic.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            if (plan.Traffic.Count < 2) throw new System.InvalidOperationException("Cannot match live road controls to the route.");
        }

        private static Vector2Int Cell(Vector3 p) => new Vector2Int(Mathf.FloorToInt(p.x / 8f), Mathf.FloorToInt(p.z / 8f));
        private static float NearestDistance(DrivingPath path, Vector3 p)
        {
            var best = float.PositiveInfinity; var distance = 0f;
            for (var i = 0; i < path.Points.Length; i++)
            {
                var d = (path.Points[i] - p).sqrMagnitude;
                if (d >= best) continue;
                best = d; distance = path.Distances[i];
            }
            return distance;
        }
    }
}
