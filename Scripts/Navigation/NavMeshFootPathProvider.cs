using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using VoogleRoute;
using VoogleRoute.Pathfinding.Geometry;
using VoogleRoute.Pathfinding.Routing.Foot;

namespace VoogleRoute.Navigation
{
    /// <summary>Unity NavMesh adapter for PathFinding foot routing.</summary>
    internal sealed class NavMeshFootPathProvider : IFootPathProvider
    {
        private static readonly NavMeshPath NavPath = new NavMeshPath();

        internal static NavMeshFootPathProvider Instance { get; } = new();

        public bool TryBuildFootLeg(Vec3 origin, Vec3 target, Vec3 sampleOrigin, out FootLegResult leg)
        {
            leg = new FootLegResult();
            NavMeshPathStatus status;

            if (!FootRouteCalculator.TryCalculate(
                    ToUnity(origin), ToUnity(target), ToUnity(sampleOrigin), NavPath, out _, out status))
                return false;

            if (status == NavMeshPathStatus.PathInvalid)
                return false;

            var corners = NavPath.corners;
            if (corners == null || corners.Length == 0)
                return false;

            var linePoints = FootPathPipeline.BuildLinePoints(corners, ToUnity(origin));
            if (linePoints.Length < 2)
                return false;

            leg = new FootLegResult
            {
                Success = true,
                IsPartial = status == NavMeshPathStatus.PathPartial,
                Points = ToVec3Array(linePoints)
            };
            return true;
        }

        private static Vector3 ToUnity(Vec3 v) => new Vector3(v.X, v.Y, v.Z);

        private static Vec3[] ToVec3Array(Vector3[] points)
        {
            var array = new Vec3[points.Length];
            for (var i = 0; i < points.Length; i++)
                array[i] = new Vec3(points[i].x, points[i].y, points[i].z);
            return array;
        }
    }
}
