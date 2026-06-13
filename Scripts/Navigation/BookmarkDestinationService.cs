using Streets;

namespace VoogleRoute.Navigation
{
    /// <summary>Sets Voogle / mod navigation targets from bookmark rows.</summary>
    internal static class BookmarkDestinationService
    {
        internal static bool TrySetLastCar() =>
            ParkedVehicleDestinationService.TryNavigateToParkedVehicle();

        internal static bool TrySetFromBookmark(BookmarkEntry bookmark)
        {
            if (bookmark == null)
                return false;

            if (bookmark.PrefersWorldPosition && bookmark.HasWorldPosition)
                return WorldDestinationService.TrySetFromBookmark(bookmark);

            if (!bookmark.HasAddress)
                return false;

            var address = bookmark.ToAddress();
            VanillaDestinationService.SetMapDestination(address);
            DestinationResolver.TrySyncAddressNow(address);
            ModLog.Info("Bookmark set as destination: " + bookmark.DisplayName);
            return true;
        }
    }
}
