using System.Collections.Generic;
using UnityEngine;

namespace VoogleRoute.Navigation
{
    internal static class AutoDriveService
    {
        private const float MinWaypointSpacing = 14f;
        private const float ArrivalDisableMeters = 22f;
        private const float ArrivalSpeedMps = 1.4f;
        private const float EnableGraceSeconds = 0.6f;

        private static float _lastTargetChangeTime = -1f;
        private static float _enabledAt = -999f;
        private static Vector3[] _cachedWaypoints = System.Array.Empty<Vector3>();
        private static bool _wasEnabled;

        internal static void Reset()
        {
            VehiclePathFollower.Reset();
            VehicleDriveController.Reset();
            VehicleInputApplicator.Release();
            _cachedWaypoints = System.Array.Empty<Vector3>();
            AutoDriveDiagnostics.ClearBlockReason();
        }

        internal static void NotifyEnabled()
        {
            _enabledAt = Time.unscaledTime;
            _wasEnabled = true;
            AutoDriveDiagnostics.OnEnabled();
        }

        internal static bool Tick(bool canNavigate, PathResult path)
        {
            if (!ModConfig.AutoDriveEnabled)
            {
                if (_wasEnabled)
                {
                    _wasEnabled = false;
                    AutoDriveDiagnostics.OnDisabled("toggle_off");
                    Reset();
                }

                return false;
            }

            _wasEnabled = true;

            if (MovementModeDetector.CurrentMode != MovementMode.Vehicle)
            {
                AutoDriveDiagnostics.LogBlockedOnce(
                    "movement mode is " + MovementModeDetector.CurrentMode + " (need Vehicle)");
                VehicleInputApplicator.Release();
                return false;
            }

            if (!canNavigate)
            {
                AutoDriveDiagnostics.LogBlockedOnce("canNavigate=false (check GPS target + LIB)");
                VehicleInputApplicator.Release();
                return false;
            }

            if (!path.Success || path.Points == null || path.Points.Length < 2)
            {
                AutoDriveDiagnostics.LogBlockedOnce(
                    "path invalid success=" + path.Success +
                    " points=" + (path.Points?.Length ?? 0));
                VehicleInputApplicator.Release();
                return false;
            }

            if (Time.unscaledTime - _enabledAt >= EnableGraceSeconds &&
                ManualVehicleInputDetector.HasManualVehicleInput())
            {
                AutoDriveDiagnostics.OnDisabled("manual_input");
                DisableFromUserInput();
                return true;
            }

            if (NavigationTargetTracker.LastChangeTime != _lastTargetChangeTime)
            {
                _lastTargetChangeTime = NavigationTargetTracker.LastChangeTime;
                VehiclePathFollower.Reset();
                VehicleDriveController.Reset();
                _cachedWaypoints = System.Array.Empty<Vector3>();
                AutoDriveDiagnostics.LogStatusThrottled("destination changed, path reset");
            }

            if (!VehicleInputApplicator.TryGetPlayerVehicle(out var vehicle))
                return false;

            if (!MovementModeDetector.TryGetVehiclePose(out var position, out var forward))
            {
                AutoDriveDiagnostics.LogBlockedOnce("TryGetVehiclePose failed");
                return false;
            }

            var waypoints = BuildWaypoints(path, NavigationTargetTracker.ActiveTarget);
            if (waypoints.Length < 2)
            {
                AutoDriveDiagnostics.LogBlockedOnce(
                    "too few waypoints after decimation count=" + waypoints.Length +
                    " raw=" + path.Points.Length);
                return false;
            }

            var speed = vehicle.Speed;
            var finalTarget = NavigationTargetTracker.ActiveTarget;
            var follow = VehiclePathFollower.Evaluate(waypoints, position, forward, speed, finalTarget);
            var command = VehicleDriveController.Compute(follow, speed);
            VehicleInputApplicator.Apply(vehicle, command);

            AutoDriveDiagnostics.LogApplyThrottled(
                speed,
                command.Throttle,
                command.Brakes,
                command.Steering,
                follow.CrossTrackMeters,
                follow.HeadingErrorDegrees,
                follow.DistanceToDestination,
                follow.OffRoute,
                waypoints.Length);

            if (follow.OffRoute)
                AutoDriveDiagnostics.LogStatusThrottled("off-route crossTrack=" + follow.CrossTrackMeters.ToString("F1"));

            if (follow.DistanceToDestination <= ArrivalDisableMeters && speed <= ArrivalSpeedMps)
            {
                AutoDriveDiagnostics.OnDisabled("arrived");
                DisableAtDestination();
                return true;
            }

            return false;
        }

        private static void DisableAtDestination() => DisableAutoDrive();

        private static void DisableFromUserInput() => DisableAutoDrive();

        private static void DisableAutoDrive()
        {
            if (!ModConfig.AutoDriveEnabled)
                return;

            ModConfig.SetAutoDriveEnabled(false);
            Reset();
        }

        private static Vector3[] BuildWaypoints(PathResult path, Vector3 finalTarget)
        {
            if (_cachedWaypoints.Length > 0 &&
                NavigationTargetTracker.LastChangeTime == _lastTargetChangeTime)
                return _cachedWaypoints;

            if (path.Points is not { Length: >= 2 } linePoints)
                return System.Array.Empty<Vector3>();

            var list = PickSpacedPoints(linePoints);
            if (list.Count == 0)
                return System.Array.Empty<Vector3>();

            if (HorizontalDistance(list[^1], finalTarget) > 3f)
                list.Add(finalTarget);

            _cachedWaypoints = list.ToArray();
            return _cachedWaypoints;
        }

        private static List<Vector3> PickSpacedPoints(IReadOnlyList<Vector3> points)
        {
            var list = new List<Vector3>(points.Count);
            var minSq = MinWaypointSpacing * MinWaypointSpacing;
            foreach (var p in points)
            {
                if (list.Count == 0 || (p - list[^1]).sqrMagnitude >= minSq)
                    list.Add(p);
            }

            if (list.Count == 0 && points.Count > 0)
                list.Add(points[^1]);

            return list;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
