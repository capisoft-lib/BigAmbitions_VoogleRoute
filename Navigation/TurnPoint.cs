using UnityEngine;

namespace VoogleRoute.Navigation;

public readonly struct TurnPoint
{
    public Vector3 Position { get; init; }
    public TurnKind Kind { get; init; }
    /// <summary>Distance le long du chemin depuis la position du joueur (m).</summary>
    public float DistanceAlongPath { get; init; }
    public float TurnAngleDegrees { get; init; }
    public int CornerIndex { get; init; }
}
