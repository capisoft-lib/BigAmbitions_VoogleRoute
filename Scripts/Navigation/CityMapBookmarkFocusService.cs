using System.Reflection;
using UnityEngine;

namespace VoogleRoute.Navigation
{
    internal static class CityMapBookmarkFocusService
    {
        internal static bool TryFocusBookmark(BookmarkEntry bookmark)
        {
            if (bookmark == null || !GameState.IsCityMapOpen())
                return false;

            try
            {
                if (!CityManager.IsInitialized)
                    return false;

                var map = CityManager.Instance?.cityMap;
                if (map == null)
                    return false;

                var worldPos = ResolveFocusPosition(bookmark);
                if (worldPos.sqrMagnitude < 0.01f)
                    return false;

                if (TryResolveBuilding(bookmark, worldPos, out var building))
                {
                    map.UpdateBuildingToFocus(building);
                    ModLog.Info("Bookmark focused on map (building): " + bookmark.DisplayName);
                    return true;
                }

                FocusCameraOnPosition(map, worldPos);
                ModLog.Info("Bookmark focused on map (position): " + bookmark.DisplayName);
                return true;
            }
            catch (System.Exception ex)
            {
                ModLog.Error("Bookmark map focus failed", ex);
                return false;
            }
        }

        private static Vector3 ResolveFocusPosition(BookmarkEntry bookmark)
        {
            if (bookmark.PrefersWorldPosition && bookmark.HasWorldPosition)
                return bookmark.WorldPosition;

            if (bookmark.HasAddress &&
                DestinationResolver.TryResolveWorldPosition(bookmark.ToAddress(), out var resolved) &&
                resolved.sqrMagnitude > 0.01f)
                return resolved;

            return bookmark.WorldPosition;
        }

        private static bool TryResolveBuilding(
            BookmarkEntry bookmark,
            Vector3 worldPos,
            out CityBuildingController building)
        {
            building = null;
            if (bookmark.PrefersWorldPosition)
                return false;

            if (bookmark.HasAddress)
            {
                building = CityManager.Instance?.FindCityBuildingController(bookmark.ToAddress());
                if (building != null)
                    return true;
            }

            return MapAddressResolver.TryFindNearestBuildingAt(worldPos, out building);
        }

        private static void FocusCameraOnPosition(CityMap map, Vector3 worldPos)
        {
            map.SetCameraPosition(worldPos);
            TrySyncCityMapCameraParent(worldPos);

            if (map.cityMapCam != null)
            {
                map.cityMapCam.MoveCameraToTarget(worldPos);
                map.cityMapCam.ForceUpdateCameraPosition();
            }
        }

        private static void TrySyncCityMapCameraParent(Vector3 worldPos)
        {
            try
            {
                var gm = GameManager.Instance;
                if (gm == null)
                    return;

                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
                if (typeof(GameManager).GetField("citymapCamera", flags)?.GetValue(gm) is Component camera &&
                    camera.transform.parent != null)
                    camera.transform.parent.position = worldPos;
            }
            catch
            {
                // Best-effort sync; cityMapCam path above may still succeed.
            }
        }
    }
}
