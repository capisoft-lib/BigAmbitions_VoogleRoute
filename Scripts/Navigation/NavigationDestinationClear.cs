using UI.Guiders;
using VoogleRoute.Rendering;
using VoogleRoute.UI;

namespace VoogleRoute.Navigation
{
    /// <summary>Clears the active navigation target and route display (map GPS + mod tracker).</summary>
    internal static class NavigationDestinationClear
    {
        internal static void ClearActiveDestination(string reason)
        {
            ClearVanillaMapDestination();
            ResetDestinationGuider();

            DestinationResolver.Clear();
            NavigationTargetTracker.ClearMapGpsTarget(reason);
            PathFinderService.InvalidateCache(reason);

            RouteLineRenderer.Hide();
            AutoWalkService.Reset();
            RouteToggleHud.RefreshVisual();
        }

        private static void ClearVanillaMapDestination()
        {
            try
            {
                if (SaveGameManager.Current != null)
                    SaveGameManager.Current.customDestination = null;
            }
            catch
            {
                // ignore
            }
        }

        private static void ResetDestinationGuider()
        {
            try
            {
                GuidersManager.ResetGuider(DirectionGuiderType.Destination);
            }
            catch
            {
                // ignore
            }
        }
    }
}
