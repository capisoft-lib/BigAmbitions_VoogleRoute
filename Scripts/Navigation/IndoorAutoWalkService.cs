using System.Collections.Generic;
using Helpers;
using UnityEngine;

namespace VoogleRoute.Navigation
{
    internal static class IndoorAutoWalkService
    {
        private const float ReachRadius = 4.5f;
        private const float ExitYieldRadius = 3.5f;
        private const float MinWaypointSpacing = 8f;
        private const float ReissueIntervalSeconds = 2.5f;
        private const float MinReissueMoveSq = 2f * 2f;

        private static int _waypointIndex;
        private static IndoorExitTarget _lastExitTarget;
        private static float _lastIssueTime = -999f;
        private static Vector3 _lastIssuedDestination;
        private static Vector3[] _cachedWaypoints = System.Array.Empty<Vector3>();
        private static float _lastExitAttemptTime = -999f;

        internal static void Reset()
        {
            _waypointIndex = 0;
            _cachedWaypoints = System.Array.Empty<Vector3>();
            _lastIssueTime = -999f;
            _lastExitTarget = IndoorExitTarget.None;
            _lastExitAttemptTime = -999f;
        }

        internal static bool Tick(bool canNavigate, PathResult path, in IndoorExitTarget exitTarget)
        {
            if (!ModConfig.IndoorAutoWalkEnabled)
            {
                Reset();
                return false;
            }

            if (!canNavigate || !path.Success || !exitTarget.IsValid)
                return false;

            if (ManualMovementInputDetector.HasManualMovementInput())
            {
                DisableFromUserInput();
                return true;
            }

            if (!ExitTargetMatches(exitTarget, _lastExitTarget))
            {
                _lastExitTarget = exitTarget;
                _waypointIndex = 0;
                _cachedWaypoints = System.Array.Empty<Vector3>();
                _lastIssueTime = -999f;
                _lastExitAttemptTime = -999f;
            }

            if (!MovementModeDetector.TryGetPlayerOrigin(out var playerPos))
                return false;

            var player = PlayerHelper.PlayerController;
            if (player == null)
                return false;

            try
            {
                if (player.NavigationDisabled)
                    return false;
            }
            catch
            {
                return false;
            }

            var waypoints = BuildWaypoints(path, exitTarget.WalkPosition);
            if (waypoints.Length == 0)
                return false;

            var exitPosition = exitTarget.WalkPosition;
            if (HorizontalDistance(playerPos, exitPosition) < ExitYieldRadius)
            {
                YieldForVanillaExit(player, exitTarget);
                return true;
            }

            SyncWaypointIndex(waypoints, playerPos);

            var walkTarget = waypoints[_waypointIndex];
            var distToWalkTarget = HorizontalDistance(playerPos, walkTarget);

            if (distToWalkTarget < ReachRadius && _waypointIndex < waypoints.Length - 1)
            {
                _waypointIndex++;
                walkTarget = waypoints[_waypointIndex];
                distToWalkTarget = HorizontalDistance(playerPos, walkTarget);
            }

            if (_waypointIndex >= waypoints.Length - 1 &&
                HorizontalDistance(playerPos, exitPosition) < ReachRadius + 1.5f)
            {
                YieldForVanillaExit(player, exitTarget);
                return true;
            }

            if (ShouldIssueDestination(walkTarget, distToWalkTarget))
                IssueWalkTo(player, walkTarget);

            return false;
        }

        private static void DisableFromUserInput()
        {
            if (!ModConfig.IndoorAutoWalkEnabled)
                return;

            ModConfig.SetIndoorAutoWalkEnabled(false);
            Reset();
            PlayerNavigationRelease.Release();
        }

        private static void YieldForVanillaExit(PlayerController player, in IndoorExitTarget exitTarget)
        {
            PlayerNavigationRelease.Release();

            var now = Time.unscaledTime;
            if (now - _lastExitAttemptTime < 0.75f)
                return;

            _lastExitAttemptTime = now;

            if (IndoorVanillaExitService.TryRequestExit(exitTarget))
            {
                DisableIndoorNavigation();
                return;
            }

            IssueWalkTo(player, exitTarget.WalkPosition);
        }

        private static void DisableIndoorNavigation()
        {
            var changed = false;
            if (ModConfig.IndoorAutoWalkEnabled)
            {
                ModConfig.SetIndoorAutoWalkEnabled(false, persist: false);
                changed = true;
            }

            if (changed)
                Reset();
        }

        private static bool ExitTargetMatches(in IndoorExitTarget a, in IndoorExitTarget b)
        {
            if ((a.WalkPosition - b.WalkPosition).sqrMagnitude > 0.25f)
                return false;

            return a.ExitZoneId == b.ExitZoneId &&
                   a.IsCasinoExit == b.IsCasinoExit &&
                   a.IsParkingExit == b.IsParkingExit;
        }

        private static Vector3[] BuildWaypoints(PathResult path, Vector3 exitPosition)
        {
            if (_cachedWaypoints.Length > 0 && _lastExitTarget.IsValid &&
                HorizontalDistance(_cachedWaypoints[^1], exitPosition) < 0.25f)
                return _cachedWaypoints;

            if (path.Points is not { Length: >= 2 } linePoints)
                return System.Array.Empty<Vector3>();

            var list = PickSpacedPoints(linePoints);
            if (list.Count == 0)
                return System.Array.Empty<Vector3>();

            if (HorizontalDistance(list[^1], exitPosition) > 3f)
                list.Add(exitPosition);

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

        private static void SyncWaypointIndex(Vector3[] waypoints, Vector3 playerPos)
        {
            var start = Mathf.Min(_waypointIndex, waypoints.Length - 1);
            for (var i = start; i < waypoints.Length; i++)
            {
                if (HorizontalDistance(playerPos, waypoints[i]) > ReachRadius * 0.6f)
                {
                    _waypointIndex = i;
                    return;
                }
            }

            _waypointIndex = waypoints.Length - 1;
        }

        private static bool ShouldIssueDestination(Vector3 walkTarget, float distToTarget)
        {
            var now = Time.unscaledTime;
            if (now - _lastIssueTime < 0.35f)
                return false;

            if ((walkTarget - _lastIssuedDestination).sqrMagnitude > MinReissueMoveSq)
                return true;

            if (now - _lastIssueTime >= ReissueIntervalSeconds && distToTarget > ReachRadius + 2f)
                return true;

            return _lastIssueTime < 0f;
        }

        private static void IssueWalkTo(PlayerController player, Vector3 worldPosition)
        {
            try
            {
                player.SetNewDestination(worldPosition, showParticleEffect: false, removeGoal: true);
                _lastIssuedDestination = worldPosition;
                _lastIssueTime = Time.unscaledTime;
                ManualMovementInputDetector.SuppressBriefly();
            }
            catch
            {
                // ignore
            }
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
