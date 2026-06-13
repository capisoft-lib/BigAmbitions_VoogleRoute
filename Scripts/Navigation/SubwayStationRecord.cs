using UnityEngine;

namespace VoogleRoute.Navigation
{
    internal sealed class SubwayStationRecord
    {
        internal int Index;
        internal string StationName;
        internal string Neighborhood;
        internal Vector3 WorldPosition;
        internal Vector3 NavPosition;

        internal float HorizontalDistanceTo(Vector3 world) =>
            HorizontalDistance(WorldPosition, world);

        internal static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
