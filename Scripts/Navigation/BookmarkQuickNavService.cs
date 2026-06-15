using VoogleRoute.UI;

namespace VoogleRoute.Navigation
{
    /// <summary>One-tap bookmark navigation from the city map (walk or auto-drive).</summary>
    internal static class BookmarkQuickNavService
    {
        internal static bool IsVehicleMapMode =>
            MovementModeDetector.CurrentMode == MovementMode.Vehicle;

        internal static void RequestDriveFromBookmark() =>
            AutoDriveSkipTravelService.RequestFromBookmark();

        internal static void RequestWalkFromBookmark()
        {
            if (!ModConfig.RouteLineEnabled)
                ModConfig.SetRouteLineEnabled(true);

            if (!ModConfig.AutoWalkEnabled)
                ModConfig.SetAutoWalkEnabled(true);

            CityMapHelper.CloseIfOpen();
            RouteToggleHud.RefreshVisual();
        }
    }
}
