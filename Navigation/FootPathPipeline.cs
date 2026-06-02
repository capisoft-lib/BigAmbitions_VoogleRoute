using VoogleRoute.Rendering;
using UnityEngine;

namespace VoogleRoute.Navigation;

/// <summary>Chemin d'affichage à pied — logique stable, ne pas mélanger avec le véhicule.</summary>
internal static class FootPathPipeline
{
    internal static Vector3[] BuildLinePoints(Vector3[] navCorners, Vector3 origin)
    {
        var cornerPoints = CopyCorners(navCorners);
        var maxSegment = 4f;
        var smoothed = PathGeometry.SmoothCorners(cornerPoints, maxSegment);
        var projected = GroundProjector.ProjectToGround(smoothed.ToArray(), ModConfig.GroundOffset.Value);

        return ModConfig.ShowFullRouteLine.Value
            ? projected
            : PathGeometry.TrimBehindOrigin(projected, origin, 3f);
    }

    internal static Vector3[] BuildTurnCorners(Vector3[] navCorners) =>
        GroundProjector.ProjectToGround(CopyCorners(navCorners), ModConfig.GroundOffset.Value);

    private static Vector3[] CopyCorners(Vector3[] corners)
    {
        var copy = new Vector3[corners.Length];
        for (var i = 0; i < corners.Length; i++)
            copy[i] = corners[i];
        return copy;
    }
}
