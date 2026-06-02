namespace VoogleRoute.Navigation;

/// <summary>Invalidation du graphe Gley au changement de scène.</summary>
internal static class TrafficWaypointProvider
{
    public static void OnSceneChanged() => TrafficWaypointGraph.InvalidateCache();

    public static void Invalidate() => TrafficWaypointGraph.InvalidateCache();
}
