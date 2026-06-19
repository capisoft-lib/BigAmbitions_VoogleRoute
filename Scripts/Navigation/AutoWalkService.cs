using Helpers;
using UnityEngine;
using VoogleRoute;

namespace VoogleRoute.Navigation
{
    internal static class AutoWalkService
    {
        private const float ReachRadius = 4.5f;
        private const float BoardStationReachRadius = 8f;
        private const float ReissueIntervalSeconds = 2.5f;
        private const float MinReissueMoveSq = 2f * 2f;
        private const float AwaitingRideRetryWalkSeconds = 1.25f;

        private enum SubwayWalkPhase
        {
            None,
            ToBoardStation,
            AwaitingRide,
            ToDestination
        }

        private static float _lastTargetChangeTime = -1f;
        private static Vector3 _lastIssuedDestination;
        private static float _lastIssueTime = -999f;
        private static SubwayWalkPhase _subwayPhase = SubwayWalkPhase.None;
        private static SubwayWalkPhase _lastSubwayPhase = SubwayWalkPhase.None;
        private static float _awaitingRideSince = -1f;

        internal static void Reset()
        {
            _lastIssueTime = -999f;
            SubwayLegTracker.Clear();
            ResetSubwayState();
        }

        internal static void ResetSubwayState()
        {
            _subwayPhase = SubwayWalkPhase.None;
            _lastSubwayPhase = SubwayWalkPhase.None;
            _awaitingRideSince = -1f;
            SubwayNavigationNotifier.Reset();
            SubwayAutoRideService.Reset();
        }

        internal static void OnSubwayRideCompleted()
        {
            SubwayLegTracker.MarkRideCompleted();

            var showHint = _subwayPhase == SubwayWalkPhase.AwaitingRide;
            if (_subwayPhase != SubwayWalkPhase.ToDestination)
            {
                _subwayPhase = SubwayWalkPhase.ToDestination;
                _lastSubwayPhase = SubwayWalkPhase.ToDestination;
                ForceReissueWalk();
            }

            if (showHint)
                SubwayNavigationNotifier.ShowContinueHint();
        }

        internal static bool Tick(bool canNavigate, PathResult path)
        {
            if (!ModConfig.AutoWalkEnabled)
            {
                Reset();
                return false;
            }

            if (!MovementModeDetector.IsEffectivelyOnFootForNavigation())
                return false;

            // Near destination the route cache is cleared (path.Success=false) — still disable auto-walk.
            if (canNavigate &&
                JobDestinationSync.IsInDeliveryMissionContext() &&
                NavigationProximityService.IsNearActiveDestination())
            {
                CompleteDeliveryJobStop();
                return true;
            }

            if (!canNavigate || !path.Success)
                return false;

            if (ManualMovementInputDetector.HasManualMovementInput())
            {
                if (_subwayPhase != SubwayWalkPhase.AwaitingRide)
                {
                    DisableFromUserInput();
                    return true;
                }
            }

            if (NavigationTargetTracker.LastChangeTime != _lastTargetChangeTime)
            {
                _lastTargetChangeTime = NavigationTargetTracker.LastChangeTime;
                Reset();
            }

            SyncSubwayPhase(path);

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

            if (_subwayPhase == SubwayWalkPhase.AwaitingRide)
                return TickAwaitingRide(player, playerPos, path);

            var walkTarget = ResolveWalkTarget(path);
            var distToWalkTarget = HorizontalDistance(playerPos, walkTarget);

            if (_subwayPhase == SubwayWalkPhase.ToBoardStation &&
                SubwayLegTracker.MatchesPlannedPath(path.Subway))
            {
                var boardTarget = ResolveBoardApproachPosition(path);
                var distToBoard = HorizontalDistance(playerPos, boardTarget);
                if (distToBoard <= BoardStationReachRadius)
                    return BeginAwaitingRide(player, playerPos, path);

                if (distToWalkTarget > ReachRadius + 1.5f &&
                    ShouldIssueDestination(walkTarget, distToWalkTarget))
                    IssueWalkTo(player, walkTarget);

                if (distToBoard > BoardStationReachRadius * 0.75f &&
                    ShouldIssueDestination(boardTarget, distToBoard))
                    IssueWalkTo(player, boardTarget);

                return false;
            }

            if (distToWalkTarget < ReachRadius + 1.5f)
            {
                if (JobDestinationSync.ShouldDeferDestinationArrivalHandling())
                    CompleteDeliveryJobStop();
                else
                    DisableAtDestination();
                return true;
            }

            if (ShouldIssueDestination(walkTarget, distToWalkTarget))
                IssueWalkTo(player, walkTarget);

            return false;
        }

        private static bool TickAwaitingRide(PlayerController player, Vector3 playerPos, PathResult path)
        {
            if (!SubwayLegTracker.MatchesPlannedPath(path.Subway) || !ModConfig.UseSubwayEnabled)
            {
                _subwayPhase = SubwayLegTracker.IsRideCompleted
                    ? SubwayWalkPhase.ToDestination
                    : SubwayWalkPhase.None;
                return false;
            }

            try
            {
                if (SubwaySystem.IsRiding)
                {
                    _awaitingRideSince = -1f;
                    return false;
                }
            }
            catch
            {
                // ignore
            }

            if (_awaitingRideSince < 0f)
                _awaitingRideSince = Time.unscaledTime;

            if (!SubwayAutoRideService.TryBeginRide(path.Subway.BoardStationName, path.Subway.ExitStationName))
            {
                if (path.Subway.BoardStationName == path.Subway.ExitStationName)
                {
                    ModLog.Info("Subway auto-ride skipped: board and exit station are the same.");
                    _subwayPhase = SubwayWalkPhase.None;
                    _lastSubwayPhase = SubwayWalkPhase.None;
                    _awaitingRideSince = -1f;
                    return false;
                }

                var boardTarget = ResolveBoardApproachPosition(path);
                if (Time.unscaledTime - _awaitingRideSince >= AwaitingRideRetryWalkSeconds &&
                    HorizontalDistance(playerPos, boardTarget) > 2.5f)
                    IssueWalkTo(player, boardTarget);

                return false;
            }

            return false;
        }

        private static bool BeginAwaitingRide(PlayerController player, Vector3 playerPos, PathResult path)
        {
            if (!SubwayLegTracker.MatchesPlannedPath(path.Subway))
                return false;

            _subwayPhase = SubwayWalkPhase.AwaitingRide;
            _lastSubwayPhase = SubwayWalkPhase.AwaitingRide;
            _awaitingRideSince = Time.unscaledTime;
            ForceReissueWalk();

            if (!SubwayAutoRideService.TryBeginRide(path.Subway.BoardStationName, path.Subway.ExitStationName))
            {
                if (path.Subway.BoardStationName == path.Subway.ExitStationName)
                {
                    ModLog.Info("Subway auto-ride skipped: board and exit station are the same.");
                    _subwayPhase = SubwayWalkPhase.None;
                    _lastSubwayPhase = SubwayWalkPhase.None;
                    _awaitingRideSince = -1f;
                    return false;
                }

                var boardTarget = ResolveBoardApproachPosition(path);
                if (HorizontalDistance(playerPos, boardTarget) > 2.5f)
                    IssueWalkTo(player, boardTarget);
                else
                    SubwayNavigationNotifier.ShowBoardHint(path.Subway.ExitStationName);
            }

            return false;
        }

        private static void SyncSubwayPhase(PathResult path)
        {
            if (_subwayPhase == SubwayWalkPhase.ToDestination || SubwayLegTracker.IsRideCompleted)
                return;

            if (!ModConfig.UseSubwayEnabled || !SubwayLegTracker.MatchesPlannedPath(path.Subway))
            {
                if (_subwayPhase == SubwayWalkPhase.AwaitingRide)
                    return;

                if (_subwayPhase != SubwayWalkPhase.None)
                {
                    _subwayPhase = SubwayWalkPhase.None;
                    ForceReissueWalk();
                }

                return;
            }

            if (_subwayPhase == SubwayWalkPhase.None)
            {
                _subwayPhase = SubwayWalkPhase.ToBoardStation;
                ForceReissueWalk();
            }

            if (_subwayPhase != _lastSubwayPhase)
            {
                _lastSubwayPhase = _subwayPhase;
                ForceReissueWalk();
            }
        }

        private static void ForceReissueWalk()
        {
            _lastIssueTime = -999f;
        }

        private static Vector3 ResolveBoardApproachPosition(PathResult path)
        {
            if (path.Subway.BoardWorldPosition.sqrMagnitude > 0.01f)
                return path.Subway.BoardWorldPosition;

            if (SubwayStationStore.TryFindByName(path.Subway.BoardStationName, out var station))
                return station.WorldPosition;

            return path.Subway.BoardNavPosition;
        }

        private static Vector3 ResolveWalkTarget(PathResult path)
        {
            if (_subwayPhase == SubwayWalkPhase.ToBoardStation && path.Subway.Active)
                return ResolveBoardApproachPosition(path);

            return NavigationTargetTracker.ActiveTarget;
        }

        private static void CompleteDeliveryJobStop()
        {
            var target = NavigationTargetTracker.ActiveTarget;
            BuildingDestinationEnterService.TryDeliveryJobStopInteract(target);
            PrepareForDestinationArrival();
        }

        private static void DisableAtDestination()
        {
            var target = NavigationTargetTracker.ActiveTarget;
            var source = NavigationTargetTracker.LastSource;

            PrepareForDestinationArrival();
            NavigationAutoEnterService.TryOnArrival(target, source);
            NavigationDestinationClear.ClearActiveDestination("autowalk_arrival");
        }

        internal static void PrepareForDestinationArrival()
        {
            StopAutoWalkSession();
            PlayerNavigationRelease.Release();
        }

        private static void DisableFromUserInput() => DisableAutoWalk();

        private static void DisableAutoWalk()
        {
            if (!StopAutoWalkSession())
                return;

            PlayerNavigationRelease.Release();
        }

        private static bool StopAutoWalkSession()
        {
            if (!ModConfig.AutoWalkEnabled)
                return false;

            ModConfig.SetAutoWalkEnabled(false, persist: false);
            Reset();
            return true;
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
