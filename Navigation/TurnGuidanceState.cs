using UnityEngine;

namespace VoogleRoute.Navigation;

public readonly struct TurnGuidanceState
{
    public bool HasGuidance { get; init; }
    public TurnPoint? NextTurn { get; init; }
    public TurnPoint[] UpcomingTurns { get; init; }
    public float DistanceToDestination { get; init; }
    public string InstructionLine1 { get; init; }
    public string InstructionLine2 { get; init; }
}
