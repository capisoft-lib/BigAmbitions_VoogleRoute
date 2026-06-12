using UnityEngine;
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

        internal static bool TryOpenDialogFromBookmark(BookmarkEntry bookmark)
        {
            if (!BookmarkStore.CanAdd() || bookmark == null)
                return false;

            Vector3 worldPos;
            if (bookmark.HasWorldPosition)
                worldPos = bookmark.WorldPosition;
            else if (bookmark.HasAddress &&
                     DestinationResolver.TryResolveWorldPosition(bookmark.ToAddress(), out worldPos))
            {
                // resolved from address
            }
            else
                return false;

            var address = bookmark.HasAddress ? bookmark.ToAddress() : null;
            var label = string.IsNullOrWhiteSpace(bookmark.LocationLabel)
                ? bookmark.DisplayName
                : bookmark.LocationLabel;
            var worldOnly = bookmark.WorldOnly || address == null;

            CityMapBookmarksPanel.CancelPickMode();
            CityMapBookmarkAddDialog.EnsureCreated();
            CityMapBookmarkAddDialog.Show(address, label, worldPos, worldOnly);
            return true;
        }
    }
}
