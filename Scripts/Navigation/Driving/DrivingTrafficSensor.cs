using GleyTrafficSystem;
using UnityEngine;

namespace VoogleRoute.Navigation.Driving
{
    internal sealed class DrivingTrafficSensor
    {
        private readonly RaycastHit[] _hits = new RaycastHit[48];
        private readonly Collider[] _crossing = new Collider[64];
        internal bool WaitingForSignal { get; private set; }

        internal float SpeedLimit(PhysicalDrivePlan plan, VehicleControlLease car, float progress, float cruise)
        {
            WaitingForSignal = false;
            var speed = car.Speed;
            var limit = cruise;
            var frontOffset = car.FrontZ - car.RearAxleZ;
            var horizon = Mathf.Clamp(speed * speed / 5f + speed + 8f, 12f, 70f);
            foreach (var marker in plan.Traffic)
            {
                var distance = marker.Distance - progress - frontOffset;
                if (distance < -1.5f) continue;
                if (distance > horizon) break;
                var wp = marker.Waypoint;
                var blocked = wp.stop || wp.temporaryDisabled;
                if (wp.giveWay)
                {
                    // CanChange reads the live intersection's path occupancy, as vanilla traffic does.
                    blocked |= !wp.CanChange() || CrossingTraffic(wp.position, car);
                }
                if (!blocked) continue;
                WaitingForSignal = true;
                limit = Mathf.Min(limit, StoppingSpeed(distance - 2f));
            }

            // Sweep along the upcoming curve, with a bumper-width box above the road surface.
            var start = progress + frontOffset;
            var height = car.RearPosition.y + 0.45f - plan.Path.At(progress).y;
            for (var distance = 0f; distance < horizon && start + distance < plan.Path.Length; distance += 4f)
            {
                var from = plan.Path.At(start + distance);
                var to = plan.Path.At(Mathf.Min(plan.Path.Length, start + distance + 4f));
                var delta = to - from;
                if (delta.sqrMagnitude < 0.01f) continue;
                var count = Physics.BoxCastNonAlloc(from + Vector3.up * height,
                    new Vector3(car.HalfWidth, 0.4f, 0.25f), delta.normalized, _hits,
                    Quaternion.LookRotation(DrivingPath.Flat(delta)), delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
                if (count == _hits.Length) return 0f; // Crowded sweep: stop rather than ignore truncated results.
                for (var i = 0; i < count; i++)
                {
                    var hit = _hits[i];
                    if (Ignore(hit.collider, car) || hit.normal.y > 0.65f) continue;
                    var gap = distance + hit.distance;
                    var moving = hit.collider.GetComponentInParent<VehicleComponent>();
                    var velocity = moving != null ? moving.GetVelocity() :
                        hit.rigidbody != null ? hit.rigidbody.velocity : Vector3.zero;
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
            var count = Physics.OverlapSphereNonAlloc(junction + Vector3.up, 12f, _crossing, ~0, QueryTriggerInteraction.Ignore);
            if (count == _crossing.Length) return true;
            for (var i = 0; i < count; i++)
            {
                var collider = _crossing[i];
                if (Ignore(collider, car)) continue;
                var vehicle = collider.GetComponentInParent<VehicleComponent>();
                var body = collider.attachedRigidbody;
                if (vehicle == null && body == null) continue;
                var velocity = vehicle != null ? vehicle.GetVelocity() : body.velocity;
                var offset = DrivingPath.Flat(collider.bounds.center - junction);
                var v = DrivingPath.Flat(velocity);
                if (v.sqrMagnitude < 0.3f) continue;
                var t = Mathf.Clamp(-Vector3.Dot(offset, v) / v.sqrMagnitude, 0f, 3f);
                if ((offset + v * t).sqrMagnitude < 16f) return true;
            }
            return false;
        }

        private static bool Ignore(Collider c, VehicleControlLease car) => c == null || c.isTrigger ||
            c.attachedRigidbody == car.Body || c.transform.IsChildOf(car.Car.transform) ||
            c.transform.IsChildOf(car.Vehicle.transform);
        private static float StoppingSpeed(float gap) => Mathf.Sqrt(4f * Mathf.Max(0f, gap));
    }
}
