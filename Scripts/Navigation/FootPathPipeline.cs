using UnityEngine;
using VoogleRoute;
using VoogleRoute.Rendering;

namespace VoogleRoute.Navigation
{
    
    internal static class FootPathPipeline
    {
        internal static Vector3[] BuildLinePoints(Vector3[] navCorners, Vector3 origin)
        {
            var cornerPoints = CopyCorners(navCorners);
            var smoothed = PathGeometry.SmoothCorners(cornerPoints, 4f);
            return GroundProjector.ProjectToGround(smoothed.ToArray(), ModConfig.FootGroundOffset);
        }

        /// <summary>Raw NavMesh corners with Y offset only — matches vanilla walk geometry.</summary>
        internal static Vector3[] BuildVanillaLinePoints(Vector3[] navCorners, float yOffset)
        {
            var copy = CopyCorners(navCorners);
            var lift = Vector3.up * yOffset;
            for (var i = 0; i < copy.Length; i++)
                copy[i] += lift;
            return copy;
        }
    
        private static Vector3[] CopyCorners(Vector3[] corners)
        {
            var copy = new Vector3[corners.Length];
            for (var i = 0; i < corners.Length; i++)
                copy[i] = corners[i];
            return copy;
        }
    }
}
