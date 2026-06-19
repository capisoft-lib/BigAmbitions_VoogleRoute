using VoogleRoute.UI;

namespace VoogleRoute.Navigation
{
    /// <summary>One-tap bookmark navigation from the city map (walk or auto-drive).</summary>
    internal static class BookmarkQuickNavService
    {
        internal static bool IsVehicleMapMode =>
            MovementModeDetector.CurrentMode == MovementMode.Vehicle;

        internal static void NavigateFromBookmark(BookmarkEntry bookmark)
        {
            if (!BookmarkDestinationService.TrySetFromBookmark(bookmark))
                return;

            CloseNavigationPanels();

            if (IsVehicleMapMode)
                RequestDriveFromBookmark();
            else
                RequestWalkFromBookmark();
        }

        internal static void CloseNavigationPanels()
        {
            VisitHistoryPanel.Close();
            CityMapHelper.CloseIfOpen();
        }

        internal static void RequestDriveFromBookmark() =>
            AutoDriveSkipTravelService.RequestFromBookmark();

        internal static void RequestWalkFromBookmark()
        {
            if (!ModConfig.RouteLineEnabled)
                ModConfig.SetRouteLineEnabled(true, persist: false);

            if (!ModConfig.AutoWalkEnabled)
                ModConfig.SetAutoWalkEnabled(true, persist: false);

            RouteActionPanel.RefreshVisual();
        }
    }
}
