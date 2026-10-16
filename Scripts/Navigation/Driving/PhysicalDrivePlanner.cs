using System.Collections.Generic;
using GleyTrafficSystem;
using Helpers;
using UnityEngine;
using VoogleRoute.Pathfinding.Routing;

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
            if (!NavigationTargetTracker.HasMapGpsTarget || RouteGraphStore.Graph == null) return false;
            using (var geometry = new VehicleControlLease(car, acquire: false))
            {
                if (geometry.Speed > 0.7f) { failure = "voogle_route_physical_start_lane"; return false; }
                var origin = geometry.RearPosition;
                var destination = NavigationTargetTracker.ActiveTarget;
                var options = VehicleRoutePathOptions.FromMainThread(destination, true);
                var query = new VehicleRoutePathOptions(true, false, options.HasArrivalRoadHint, options.ArrivalRoadHint)
                    .ToRouteQuery(PhysicalRoutingGraph.Native(origin), PhysicalRoutingGraph.Native(destination),
                        PhysicalRoutingGraph.Native(car.vehicleController.transform.forward), true);
                var graph = new PhysicalRoutingGraph(RouteGraphStore.Graph, geometry.MinRadius * 1.1f);
                if (!TryFindLaneStart(graph, geometry, out var start))
                { failure = "voogle_route_physical_start_lane"; return false; }
                query = new RouteQuery
                {
                    Origin = query.Origin, Destination = query.Destination, Forward = query.Forward,
                    HasPose = true, ForcedStartWaypoint = start, ForcedEndWaypoint = -1,
                    AllowUturnAtStart = false, PreferBuildingSideArrival = true,
                    HasArrivalRoadHint = query.HasArrivalRoadHint, ArrivalRoadHint = query.ArrivalRoadHint
                };
                if (!WaypointPathfinder.TryFindBestRoute(graph, query, out var route) || route.Path.Count < 2) return false;

                // Retain every graph anchor. Never append the building GPS chord or teleport fallback.
                var raw = new List<Vector3>();
                raw.Add(PhysicalRoutingGraph.Unity(graph.GetPosition(route.Path[0])));
                for (var i = 1; i < route.Path.Count; i++)
                {
                    var a = PhysicalRoutingGraph.Unity(graph.GetPosition(route.Path[i - 1]));
                    var b = PhysicalRoutingGraph.Unity(graph.GetPosition(route.Path[i]));
                    if (graph.TryGetSyntheticTurnControl(route.Path[i - 1], route.Path[i], out var cp) &&
                        !graph.IsLaneChangeEdge(route.Path[i - 1], route.Path[i]))
                    {
                        var c = PhysicalRoutingGraph.Unity(cp);
                        var count = Mathf.Max(8, Mathf.CeilToInt((Vector3.Distance(a, c) + Vector3.Distance(c, b)) / 1.5f));
                        for (var j = 1; j <= count; j++)
                        {
                            var t = j / (float)count;
                            Add(raw, Vector3.Lerp(Vector3.Lerp(a, c, t), Vector3.Lerp(c, b, t), t));
                        }
                    }
                    else Add(raw, b);
                }
                if (!TryRoundCorners(raw, geometry.MinRadius * 1.15f, out var rounded)) return false;
                var samples = new List<Vector3> { rounded[0] };
                for (var i = 1; i < rounded.Count; i++)
                {
                    var n = Mathf.Max(1, Mathf.CeilToInt(DrivingPath.FlatDistance(rounded[i - 1], rounded[i])));
                    for (var j = 1; j <= n; j++) Add(samples, Vector3.Lerp(rounded[i - 1], rounded[i], j / (float)n));
                }
                var path = new DrivingPath(samples, ModConfig.PhysicalDriveSpeedKmh / 3.6f);
                for (var i = 1; i < samples.Count - 1; i++)
                    if (DrivingPath.Curvature(samples[i - 1], samples[i], samples[i + 1]) > 1.2f / geometry.MinRadius)
                        return false;
                if (path.Length < 12f) { failure = "voogle_route_autodrive_too_close"; return false; }
                var progress = path.Project(origin, 0f, out var lateral, searchAhead: 50f);
                var direction = DrivingPath.Flat(path.At(progress + 3f) - path.At(progress));
                if (lateral > 2.5f ||
                    Vector3.Angle(direction, geometry.Vehicle.transform.forward) > 35f)
                { failure = "voogle_route_physical_start_lane"; return false; }
                if (path.Length - progress < 12f) { failure = "voogle_route_autodrive_too_close"; return false; }
                var arrivalAnchor = options.HasArrivalRoadHint ? PhysicalRoutingGraph.Unity(options.ArrivalRoadHint) : destination;
                if (DrivingPath.FlatDistance(samples[samples.Count - 1], arrivalAnchor) > 80f) return false;
                if (AutoDriveVehicleFuel.TryEstimate(path.Length, out var fuel) && !fuel.HasEnoughFuel)
                { failure = "voogle_route_autodrive_insufficient_fuel"; return false; }

                plan = new PhysicalDrivePlan { Car = car, Destination = destination, Path = path };
                BindTraffic(plan, route.Path);
                plan.Path.ApplyBrakingEnvelope();
                ModLog.Info("Physical drive planned: " + Mathf.RoundToInt(path.Length) + " m, " + samples.Count + " samples.");
                return true;
            }
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

        private static bool TryRoundCorners(List<Vector3> raw, float radius, out List<Vector3> result)
        {
            result = new List<Vector3> { raw[0] };
            for (var i = 1; i < raw.Count - 1; i++)
            {
                var incoming = DrivingPath.Flat(raw[i] - raw[i - 1]).normalized;
                var outgoing = DrivingPath.Flat(raw[i + 1] - raw[i]).normalized;
                var angle = Vector3.SignedAngle(incoming, outgoing, Vector3.up);
                if (Mathf.Abs(angle) < 28f) { Add(result, raw[i]); continue; }
                if (Mathf.Abs(angle) > 125f) return false;
                var tangent = radius * Mathf.Tan(Mathf.Abs(angle) * Mathf.Deg2Rad * 0.5f);
                if (tangent > Mathf.Min(DrivingPath.FlatDistance(raw[i - 1], raw[i]),
                    DrivingPath.FlatDistance(raw[i], raw[i + 1])) * 0.45f) return false;
                var entry = raw[i] - incoming * tangent;
                var exit = raw[i] + outgoing * tangent;
                var center = entry + Vector3.Cross(Vector3.up, incoming) * (Mathf.Sign(angle) * radius);
                Add(result, entry);
                var segments = Mathf.Max(3, Mathf.CeilToInt(Mathf.Abs(angle) * Mathf.Deg2Rad * radius));
                for (var j = 1; j <= segments; j++)
                    Add(result, center + Quaternion.AngleAxis(angle * j / segments, Vector3.up) * (entry - center));
                result[result.Count - 1] = exit;
            }
            Add(result, raw[raw.Count - 1]);
            return result.Count >= 2;
        }

        private static void Add(List<Vector3> list, Vector3 p)
        { if (list.Count == 0 || Vector3.SqrMagnitude(p - list[list.Count - 1]) > 0.01f) list.Add(p); }

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
