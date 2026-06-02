using Il2Cpp;
using Il2CppHelpers;
using UnityEngine;

namespace VoogleRoute.Navigation;

/// <summary>
/// Marche automatique à pied en réutilisant <see cref="PlayerController.SetNewDestination"/> (comme un clic sol).
/// Avance le long des coins NavMesh du GPS carte.
/// </summary>
internal static class AutoWalkService
{
    private const float ReachRadius = 4.5f;
    private const float MinWaypointSpacing = 10f;
    private const float ReissueIntervalSeconds = 2.5f;
    private const float MinReissueMoveSq = 2f * 2f;

    private static int _waypointIndex;
    private static float _lastTargetChangeTime = -1f;
    private static Vector3 _lastIssuedDestination;
    private static float _lastIssueTime = -999f;
    private static Vector3[] _cachedWaypoints = Array.Empty<Vector3>();

    public static void Reset()
    {
        _waypointIndex = 0;
        _cachedWaypoints = Array.Empty<Vector3>();
        _lastIssueTime = -999f;
    }

    public static void Tick(bool canNavigate, PathResult path)
    {
        if (!ModConfig.AutoWalkEnabled.Value)
        {
            Reset();
            return;
        }

        if (MovementModeDetector.CurrentMode != MovementMode.OnFoot)
            return;

        if (!canNavigate || !path.Success)
            return;

        if (NavigationTargetTracker.LastChangeTime != _lastTargetChangeTime)
        {
            _lastTargetChangeTime = NavigationTargetTracker.LastChangeTime;
            _waypointIndex = 0;
            _cachedWaypoints = Array.Empty<Vector3>();
            _lastIssueTime = -999f;
        }

        if (!MovementModeDetector.TryGetPlayerOrigin(out var playerPos))
            return;

        var player = PlayerHelper.PlayerController;
        if (player == null)
            return;

        try
        {
            if (player.NavigationDisabled)
                return;
        }
        catch
        {
            return;
        }

        var waypoints = BuildWaypoints(path, NavigationTargetTracker.ActiveTarget);
        if (waypoints.Length == 0)
            return;
        SyncWaypointIndex(waypoints, playerPos);

        var walkTarget = waypoints[_waypointIndex];
        var distToWalkTarget = HorizontalDistance(playerPos, walkTarget);

        if (distToWalkTarget < ReachRadius && _waypointIndex < waypoints.Length - 1)
        {
            _waypointIndex++;
            walkTarget = waypoints[_waypointIndex];
            distToWalkTarget = HorizontalDistance(playerPos, walkTarget);
        }

        var finalDest = NavigationTargetTracker.ActiveTarget;
        if (_waypointIndex >= waypoints.Length - 1 &&
            HorizontalDistance(playerPos, finalDest) < ReachRadius + 1.5f)
            return;

        if (ShouldIssueDestination(walkTarget, distToWalkTarget))
            IssueWalkTo(player, walkTarget);
    }

    private static Vector3[] BuildWaypoints(PathResult path, Vector3 finalTarget)
    {
        if (_cachedWaypoints.Length > 0 &&
            NavigationTargetTracker.LastChangeTime == _lastTargetChangeTime)
            return _cachedWaypoints;

        var corners = PathFinderService.LastTurnCorners;
        List<Vector3> list;
        if (corners.Length >= 2)
            list = PickSpacedPoints(corners);
        else if (path.Points is { Length: >= 2 } linePoints)
            list = PickSpacedPoints(linePoints);
        else
            return Array.Empty<Vector3>();

        if (list.Count == 0)
            return Array.Empty<Vector3>();

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
        }
        catch (System.Exception ex)
        {
            _ = ex;
        }
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
