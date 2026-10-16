using GleyTrafficSystem;
using System.Collections.Generic;
using UnityEngine;

namespace VoogleRoute.Navigation.Driving
{
    internal sealed class DrivingTrafficSensor
    {
        private readonly RaycastHit[] _hits = new RaycastHit[48];
        private readonly Collider[] _crossing = new Collider[64];
        private readonly Dictionary<int, ColliderInfo> _colliders = new Dictionary<int, ColliderInfo>(128);
        private readonly HashSet<int> _seenObstacles = new HashSet<int>();
        private PhysicalDrivePlan _plan;
        private int _firstMarker, _boxes, _overlaps, _lookups;
        internal bool WaitingForSignal { get; private set; }

        private struct ColliderInfo
        {
            internal Collider Collider;
            internal Rigidbody Body;
            internal VehicleComponent Vehicle;
            internal ThirdPersonCharacter Human;
            internal bool Ignore;
        }

        internal void Reset()
        {
            _plan = null; _firstMarker = 0;
            _colliders.Clear(); _seenObstacles.Clear();
            WaitingForSignal = false;
        }

        internal float SpeedLimit(PhysicalDrivePlan plan, VehicleControlLease car, float progress, float cruise)
        {
            if (_plan != plan) { Reset(); _plan = plan; }
            _boxes = _overlaps = _lookups = 0;
            _seenObstacles.Clear();
            try { return CalculateLimit(plan, car, progress, cruise); }
            finally { DrivingPerformance.SensorWork(_boxes, _overlaps, _lookups); }
        }

        private float CalculateLimit(PhysicalDrivePlan plan, VehicleControlLease car, float progress, float cruise)
        {
            WaitingForSignal = false;
            var speed = car.Speed;
            var limit = cruise;
            var frontOffset = car.FrontZ - car.RearAxleZ;
            var horizon = Mathf.Clamp(speed * speed / 4f + speed * 0.8f + 8f, 12f, 150f);
            while (_firstMarker < plan.Traffic.Count && plan.Traffic[_firstMarker].Distance - progress - frontOffset < -1.5f)
                _firstMarker++;
            for (var markerIndex = _firstMarker; markerIndex < plan.Traffic.Count; markerIndex++)
            {
                var marker = plan.Traffic[markerIndex];
                var distance = marker.Distance - progress - frontOffset;
                if (distance < -1.5f) continue;
                if (distance > horizon) break;
                var wp = marker.Waypoint;
                var runLight = marker.SignalControlled && ModConfig.RunRedLightsEnabled;
                var blocked = wp.temporaryDisabled || (wp.stop && !runLight);
                if (wp.giveWay && !runLight)
                {
                    // CanChange reads the live intersection's path occupancy, as vanilla traffic does.
                    blocked |= !wp.CanChange();
                }
                // A red-light override bypasses the signal, while crossing vehicles still take priority.
                if (wp.giveWay || runLight) blocked |= CrossingTraffic(wp.position, car);
                if (!blocked) continue;
                WaitingForSignal = true;
                limit = Mathf.Min(limit, StoppingSpeed(distance - 2f));
            }

            if (ModConfig.StopForPedestriansEnabled)
                limit = Mathf.Min(limit, PedestrianLimit(plan, car, progress, frontOffset, horizon, cruise));

            // Sweep along the upcoming curve, with a bumper-width box above the road surface.
            var start = progress + frontOffset;
            var height = car.RearPosition.y + 0.45f - plan.Path.At(progress).y;
            var distanceAlongPath = 0f;
            while (distanceAlongPath < horizon && start + distanceAlongPath < plan.Path.Length)
            {
                var sweepStart = start + distanceAlongPath;
                var sweepEnd = Mathf.Min(plan.Path.Length, sweepStart + 4f);
                var merged = false;
                if (distanceAlongPath >= 8f)
                {
                    var longerEnd = Mathf.Min(plan.Path.Length,
                        start + Mathf.Min(distanceAlongPath + 12f, Mathf.Ceil(horizon / 4f) * 4f));
                    if (longerEnd > sweepEnd + 1f && plan.Path.IsStraightSweep(sweepStart, longerEnd))
                    { sweepEnd = longerEnd; merged = true; }
                }
                var from = plan.Path.At(sweepStart);
                var to = plan.Path.At(sweepEnd);
                var delta = to - from;
                var gapBase = distanceAlongPath;
                distanceAlongPath += sweepEnd - sweepStart;
                if (delta.sqrMagnitude < 0.01f) continue;
                _boxes++;
                var count = Physics.BoxCastNonAlloc(from + Vector3.up * height,
                    // Expanded merged boxes conservatively cover the original short sweeps.
                    new Vector3(car.HalfWidth + (merged ? 0.15f : 0f), merged ? 0.42f : 0.4f,
                        merged ? 0.25f + car.HalfWidth : 0.25f), delta.normalized, _hits,
                    Quaternion.LookRotation(DrivingPath.Flat(delta)), delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
                if (count == _hits.Length) return 0f; // Crowded sweep: stop rather than ignore truncated results.
                for (var i = 0; i < count; i++)
                {
                    var hit = _hits[i];
                    if (hit.collider == null || hit.normal.y > 0.65f || !_seenObstacles.Add(hit.collider.GetInstanceID())) continue;
                    var info = Info(hit.collider, car);
                    if (info.Ignore || (!ModConfig.StopForPedestriansEnabled && info.Human != null)) continue;
                    var gap = gapBase + hit.distance;
                    var velocity = Velocity(info);
                    var lead = Mathf.Max(0f, Vector3.Dot(DrivingPath.Flat(velocity), DrivingPath.Flat(delta).normalized));
                    var buffer = 2.5f + speed * 0.8f;
                    var followSpeed = Mathf.Min(lead + Mathf.Max(0f, gap - buffer) * 0.6f,
                        Mathf.Sqrt(lead * lead + 5f * Mathf.Max(0f, gap - 2.5f)));
                    if (gap < 2.5f) followSpeed = 0f;
                    limit = Mathf.Min(limit, followSpeed);
                }
            }
            return limit;
        }

        private bool CrossingTraffic(Vector3 junction, VehicleControlLease car)
        {
            _overlaps++;
            var count = Physics.OverlapSphereNonAlloc(junction + Vector3.up, 12f, _crossing, ~0, QueryTriggerInteraction.Ignore);
            if (count == _crossing.Length) return true;
            for (var i = 0; i < count; i++)
            {
                var collider = _crossing[i];
                var info = Info(collider, car);
                if (info.Ignore || (!ModConfig.StopForPedestriansEnabled && info.Human != null)) continue;
                var vehicle = info.Vehicle;
                var body = info.Body;
                if (vehicle == null && body == null && info.Human == null) continue;
                var velocity = Velocity(info);
                var offset = DrivingPath.Flat(collider.bounds.center - junction);
                var v = DrivingPath.Flat(velocity);
                if (v.sqrMagnitude < 0.3f) continue;
                var t = Mathf.Clamp(-Vector3.Dot(offset, v) / v.sqrMagnitude, 0f, 3f);
                if ((offset + v * t).sqrMagnitude < 16f) return true;
            }
            return false;
        }

        private float PedestrianLimit(PhysicalDrivePlan plan, VehicleControlLease car, float progress,
            float frontOffset, float horizon, float cruise)
        {
            // Street pedestrians can be NavMesh agents with trigger/disabled collision bodies.
            // Use the live spawner's list as well as the existing solid-collider sweeps.
            var pedestrians = CityManager.Instance?.pedestrianSpawner?.activePedestrians;
            if (pedestrians == null) return cruise;
            var limit = cruise;
            var origin = car.RearPosition;
            var rangeSquared = (horizon + 8f) * (horizon + 8f);
            foreach (var pedestrian in pedestrians)
            {
                var human = pedestrian?.tpc;
                if (human == null || human.isPlayer || !human.gameObject.activeInHierarchy) continue;
                var position = human.transform.position;
                var offset = DrivingPath.Flat(position - origin);
                if (offset.sqrMagnitude > rangeSquared || Mathf.Abs(position.y - origin.y) > 3f) continue;
                if (offset.sqrMagnitude < 25f && Vector3.Dot(offset, car.Vehicle.transform.forward) < -0.5f) continue;
                var agent = human.navmeshAgent;
                var velocity = agent != null && agent.enabled && agent.isOnNavMesh ? agent.velocity : human.velocity;
                var arrivalTime = Mathf.Clamp(offset.magnitude / Mathf.Max(car.Speed, 3f), 0f, 3f);
                limit = Mathf.Min(limit, PedestrianPointLimit(plan.Path, position, progress, frontOffset, horizon, car.HalfWidth));
                limit = Mathf.Min(limit, PedestrianPointLimit(plan.Path, position + velocity * arrivalTime,
                    progress, frontOffset, horizon, car.HalfWidth));
            }
            if (limit < cruise) WaitingForSignal = true;
            return limit;
        }

        private static float PedestrianPointLimit(DrivingPath path, Vector3 position, float progress,
            float frontOffset, float horizon, float halfWidth)
        {
            var along = path.Project(position, progress, out var lateral, searchAhead: horizon);
            var gap = along - progress - frontOffset;
            if (lateral > halfWidth + 0.65f || gap < -1.5f || gap > horizon) return float.PositiveInfinity;
            return StoppingSpeed(gap - 2.5f);
        }

        private static Vector3 Velocity(ColliderInfo info)
        {
            if (info.Vehicle != null) return info.Vehicle.GetVelocity();
            if (info.Human != null)
            {
                var agent = info.Human.navmeshAgent;
                return agent != null && agent.enabled && agent.isOnNavMesh ? agent.velocity : info.Human.velocity;
            }
            return info.Body != null ? info.Body.velocity : Vector3.zero;
        }

        private ColliderInfo Info(Collider c, VehicleControlLease car)
        {
            if (c == null) return new ColliderInfo { Ignore = true };
            var id = c.GetInstanceID();
            var body = c.attachedRigidbody;
            if (_colliders.TryGetValue(id, out var cached) && cached.Collider == c && cached.Body == body) return cached;
            var ignored = c.isTrigger || body == car.Body || c.transform.IsChildOf(car.Car.transform) ||
                c.transform.IsChildOf(car.Vehicle.transform);
            var info = new ColliderInfo { Collider = c, Body = body, Ignore = ignored };
            if (!ignored)
            {
                _lookups++;
                info.Vehicle = c.GetComponentInParent<VehicleComponent>();
                if (info.Vehicle == null)
                {
                    _lookups++;
                    info.Human = c.GetComponentInParent<ThirdPersonCharacter>();
                    if (info.Human != null && info.Human.isPlayer) info.Ignore = true;
                }
            }
            if (_colliders.Count >= 512) _colliders.Clear();
            _colliders[id] = info;
            return info;
        }
        private static float StoppingSpeed(float gap) => Mathf.Sqrt(4f * Mathf.Max(0f, gap));
    }
}
