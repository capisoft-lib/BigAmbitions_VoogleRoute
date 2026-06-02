using VoogleRoute.Rendering;
using UnityEngine;
using UnityEngine.AI;

namespace VoogleRoute.Navigation;

public readonly struct PathResult
{
    public bool Success { get; init; }
    public bool IsPartial { get; init; }
    public Vector3[] Points { get; init; }

    public int PointCount => Points?.Length ?? 0;

    public static PathResult None => new() { Points = Array.Empty<Vector3>() };
}

public static class PathFinderService
{
    private static readonly NavMeshPath NavPath = new();
    private static Vector3 _lastOrigin;
    private static float _lastCalcTime;
    private static MovementMode _lastMode = MovementMode.Unavailable;
    private static PathResult _cached = new() { Points = Array.Empty<Vector3>() };

    public static Vector3[] LastFinalPoints { get; private set; } = Array.Empty<Vector3>();

    /// <summary>Coins NavMesh projetés au sol — utilisés pour les virages aux croisements.</summary>
    public static Vector3[] LastTurnCorners { get; private set; } = Array.Empty<Vector3>();

    public static PathResult GetRoute(bool forceRecalc = false)
    {
        if (!GameState.ShouldRunNavigationSystems())
            return SilentEmpty();

        if (!NavigationTargetTracker.HasTarget)
        {
            LastFinalPoints = Array.Empty<Vector3>();
            LastTurnCorners = Array.Empty<Vector3>();
            return Empty("pas de cible");
        }

        if (!MovementModeDetector.TryGetPathOrigin(out var origin))
        {
            LastFinalPoints = Array.Empty<Vector3>();
            LastTurnCorners = Array.Empty<Vector3>();
            return Empty("origine introuvable");
        }

        var target = NavigationTargetTracker.ActiveTarget;
        var mode = MovementModeDetector.CurrentMode;
        var modeChanged = mode != _lastMode;
        _lastMode = mode;

        var interval = mode == MovementMode.Vehicle
            ? Mathf.Max(1.5f, ModConfig.VehicleRecalcIntervalSeconds.Value)
            : Mathf.Max(0.5f, ModConfig.RecalcIntervalSeconds.Value);
        var movedThreshold = mode == MovementMode.Vehicle ? 3600f : 225f;
        var moved = (origin - _lastOrigin).sqrMagnitude > movedThreshold;
        var targetChanged = Time.unscaledTime - NavigationTargetTracker.LastChangeTime < 0.05f;

        if (!forceRecalc && !modeChanged && !moved && !targetChanged &&
            Time.unscaledTime - _lastCalcTime < interval)
            return _cached;

        _lastOrigin = origin;
        _lastCalcTime = Time.unscaledTime;

        var sampleOrigin = origin;
        if (MovementModeDetector.TryGetPlayerOrigin(out var feet))
            sampleOrigin = feet;

        NavMeshQueryFilter pathFilterUsed;
        bool calculateOk;
        Vector3[] navCorners;
        NavMeshPathStatus status;

        if (mode == MovementMode.Vehicle)
        {
            calculateOk = VehicleRouteCalculator.TryCalculate(
                origin, target, sampleOrigin, NavPath, out pathFilterUsed, out navCorners, out status);
        }
        else
        {
            calculateOk = FootRouteCalculator.TryCalculate(origin, target, sampleOrigin, NavPath, out pathFilterUsed);
            status = calculateOk ? NavPath.status : NavMeshPathStatus.PathInvalid;
            navCorners = NavPath.corners ?? System.Array.Empty<Vector3>();
        }

        var rawCornerCount = navCorners.Length;

        if (!calculateOk || status == NavMeshPathStatus.PathInvalid)
        {
            return Cache(Empty("path invalide"));
        }

        var isPartial = status == NavMeshPathStatus.PathPartial;
        if (isPartial && !ModConfig.ShowPartialPaths.Value)
        {
            return Cache(Empty("path partiel (ShowPartialPaths=false)"));
        }

        if (rawCornerCount == 0)
        {
            return Cache(Empty("0 corners"));
        }

        Vector3[] linePoints;
        int smoothCount;
        int projectedCount;

        if (mode == MovementMode.Vehicle)
        {
            LastTurnCorners = VehiclePathPipeline.BuildTurnCorners(navCorners, pathFilterUsed);
            linePoints = VehiclePathPipeline.BuildLinePoints(navCorners, origin, target, pathFilterUsed);
            smoothCount = linePoints.Length;
            projectedCount = linePoints.Length;
        }
        else
        {
            LastTurnCorners = FootPathPipeline.BuildTurnCorners(navCorners);
            linePoints = FootPathPipeline.BuildLinePoints(navCorners, origin);
            smoothCount = linePoints.Length;
            projectedCount = linePoints.Length;
        }

        var success = linePoints.Length >= 2;
        LastFinalPoints = linePoints;
        return Cache(new PathResult
        {
            Success = success,
            IsPartial = isPartial,
            Points = linePoints
        });
    }

    public static void InvalidateCache()
    {
        _cached = new PathResult { Points = Array.Empty<Vector3>() };
        _lastMode = MovementMode.Unavailable;
        LastFinalPoints = Array.Empty<Vector3>();
        LastTurnCorners = Array.Empty<Vector3>();
        TrafficWaypointProvider.Invalidate();
    }

    private static PathResult Cache(PathResult result)
    {
        _cached = result;
        return result;
    }

    private static PathResult Empty(string reason) =>
        new() { Points = Array.Empty<Vector3>() };

    private static PathResult SilentEmpty() =>
        new() { Points = Array.Empty<Vector3>() };

}
