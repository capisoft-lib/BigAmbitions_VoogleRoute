using VoogleRoute.UI;

namespace VoogleRoute.Navigation
{
    internal static class BookmarkPickService
    {
        internal static bool TryOpenDialogAtCurrentPosition()
        {
            if (!BookmarkStore.CanAdd())
                return false;

            if (!MapAddressResolver.TryResolveCurrentPlayer(
                    out var worldPos,
                    out var address,
                    out var label))
            {
                ModLog.Info("Bookmark pick: could not resolve current player position.");
                return false;
            }

            CityMapBookmarksPanel.CancelPickMode();
            CityMapBookmarkAddDialog.EnsureCreated();
            CityMapBookmarkAddDialog.Show(address, label, worldPos, worldOnly: true);
            return true;
        }
    }
}
