using UnityEngine;
using VoogleRoute.Localization;

namespace VoogleRoute.Navigation;

public static class TurnGuidanceService
{
    private const float MinDistanceBeforeTurnMeters = 12f;

    public static TurnGuidanceState Update(Vector3 playerPosition, Vector3 destination)
    {
        var corners = PathFinderService.LastTurnCorners;
        if (corners.Length < 2)
            return Empty();

        var turns = BuildTurnPoints(corners);
        if (turns.Count == 0)
            return Empty();

        var progress = GetPathProgress(corners, playerPosition);
        TurnPoint? next = null;
        var upcoming = new List<TurnPoint>();

        foreach (var t in turns)
        {
            var dist = t.DistanceAlongPath - progress.DistanceAlongPath;
            if (dist < MinDistanceBeforeTurnMeters)
                continue;

            var enriched = t with { DistanceAlongPath = dist };
            upcoming.Add(enriched);
            next ??= enriched;

            if (upcoming.Count >= ModConfig.MaxIntersectionMarkers.Value)
                break;
        }

        var distToDest = progress.RemainingDistanceToEnd;
        if (next == null)
        {
            if (distToDest > MinDistanceBeforeTurnMeters)
            {
                return new TurnGuidanceState
                {
                    HasGuidance = true,
                    NextTurn = null,
                    UpcomingTurns = Array.Empty<TurnPoint>(),
                    DistanceToDestination = distToDest,
                    InstructionLine1 = ModLocalization.Meters(Mathf.RoundToInt(distToDest)),
                    InstructionLine2 = ModLocalization.Get(StringKey.ContinueStraightToDestination)
                };
            }

            return new TurnGuidanceState
            {
                HasGuidance = true,
                NextTurn = null,
                UpcomingTurns = Array.Empty<TurnPoint>(),
                DistanceToDestination = distToDest,
                InstructionLine1 = distToDest < 25f
                    ? ModLocalization.Get(StringKey.Arrival)
                    : ModLocalization.Meters(Mathf.RoundToInt(distToDest)),
                InstructionLine2 = distToDest < 25f
                    ? ModLocalization.Get(StringKey.DestinationNear)
                    : ModLocalization.Get(StringKey.FollowRoute)
            };
        }

        var n = next.Value;
        return new TurnGuidanceState
        {
            HasGuidance = true,
            NextTurn = n,
            UpcomingTurns = upcoming.ToArray(),
            DistanceToDestination = distToDest,
            InstructionLine1 = ModLocalization.Meters(Mathf.RoundToInt(n.DistanceAlongPath)),
            InstructionLine2 = ModLocalization.DescribeTurn(n.Kind)
        };
    }

    private static TurnGuidanceState Empty() => new()
    {
        UpcomingTurns = Array.Empty<TurnPoint>(),
        InstructionLine1 = "",
        InstructionLine2 = ""
    };

    private readonly struct PathProgress
    {
        public float DistanceAlongPath { get; init; }
        public float RemainingDistanceToEnd { get; init; }
    }

    private static PathProgress GetPathProgress(Vector3[] corners, Vector3 playerPosition)
    {
        var bestIdx = 0;
        var bestSq = float.MaxValue;
        for (var i = 0; i < corners.Length; i++)
        {
            var sq = (corners[i] - playerPosition).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                bestIdx = i;
            }
        }

        var along = 0f;
        for (var i = 1; i <= bestIdx; i++)
            along += FlatDistance(corners[i - 1], corners[i]);

        var rem = 0f;
        for (var i = bestIdx + 1; i < corners.Length; i++)
            rem += FlatDistance(corners[i - 1], corners[i]);

        return new PathProgress { DistanceAlongPath = along, RemainingDistanceToEnd = rem };
    }

    private static List<TurnPoint> BuildTurnPoints(Vector3[] corners)
    {
        var list = new List<TurnPoint>();
        var minAngle = ModConfig.MinTurnAngleDegrees.Value;
        var cumulative = 0f;

        for (var i = 1; i < corners.Length - 1; i++)
        {
            cumulative += FlatDistance(corners[i - 1], corners[i]);
            var kind = ClassifyTurn(corners, i, minAngle);
            if (kind == TurnKind.Straight)
                continue;

            list.Add(new TurnPoint
            {
                Position = corners[i],
                Kind = kind,
                DistanceAlongPath = cumulative,
                TurnAngleDegrees = ComputeTurnAngle(corners, i),
                CornerIndex = i
            });
        }

        if (corners.Length >= 2)
            cumulative += FlatDistance(corners[^2], corners[^1]);

        list.Add(new TurnPoint
        {
            Position = corners[^1],
            Kind = TurnKind.Arrival,
            DistanceAlongPath = cumulative,
            TurnAngleDegrees = 0f,
            CornerIndex = corners.Length - 1
        });

        return list;
    }

    private static float ComputeTurnAngle(Vector3[] corners, int i)
    {
        var a = Flat(corners[i] - corners[i - 1]);
        var b = Flat(corners[i + 1] - corners[i]);
        if (a.sqrMagnitude < 0.01f || b.sqrMagnitude < 0.01f)
            return 0f;
        return Vector3.SignedAngle(a, b, Vector3.up);
    }

    private static TurnKind ClassifyTurn(Vector3[] corners, int i, float minAngle)
    {
        var signed = ComputeTurnAngle(corners, i);
        var angle = Mathf.Abs(signed);
        if (angle < minAngle)
            return TurnKind.Straight;

        // SignedAngle Unity (Y up) : signe inversé par rapport au repère route du jeu.
        var right = signed > 0f;
        if (angle < 35f) return right ? TurnKind.SlightRight : TurnKind.SlightLeft;
        if (angle < 75f) return right ? TurnKind.Right : TurnKind.Left;
        if (angle < 135f) return right ? TurnKind.SharpRight : TurnKind.SharpLeft;
        return TurnKind.UTurn;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        var dx = a.x - b.x;
        var dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);
}
