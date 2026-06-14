using UnityEngine;

namespace VoogleRoute.Navigation
{
    internal enum RoutePathSegmentKind
    {
        Foot,
        Subway
    }

    internal struct RoutePathSegment
    {
        internal RoutePathSegmentKind Kind;
        internal Vector3[] Points;
    }

    internal struct SubwayNavigationHint
    {
        internal bool Active;
        internal string BoardStationName;
        internal string ExitStationName;
        internal Vector3 BoardNavPosition;
        internal Vector3 ExitNavPosition;
        internal Vector3 BoardWorldPosition;
        internal Vector3 ExitWorldPosition;

        internal static SubwayNavigationHint None => default;
    }
}
