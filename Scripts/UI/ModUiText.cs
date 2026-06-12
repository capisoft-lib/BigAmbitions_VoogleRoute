using Localizor;
using UnityEngine;
using VoogleRoute.Navigation;

namespace VoogleRoute.UI
{
    internal static class ModUiText
    {
        private static string _activeLocale = string.Empty;
        private static float _nextLocalePoll;
        internal static string PanelTitle => Loc("voogle_route_panel_title", "VOOGLE ROUTE");
        internal static string RouteOn => Loc("voogle_route_route_on", "ROUTE ON");
        internal static string RouteOff => Loc("voogle_route_route_off", "ROUTE OFF");
        internal static string AutoWalk => Loc("voogle_route_autowalk", "AUTO-WALK");
        internal static string AutoDrive => Loc("voogle_route_autodrive", "AUTO-DRIVE");
        internal static string DriveOn => Loc("voogle_route_drive_on", "DRIVE ON");
        internal static string WalkOn => Loc("voogle_route_walk_on", "WALK ON");
        internal static string WayOutOn => Loc("voogle_route_way_out_on", "WAY OUT");
        internal static string WayOutOff => Loc("voogle_route_way_out_off", "WAY OUT OFF");
        internal static string GetOut => Loc("voogle_route_get_out", "GET OUT");
        internal static string GetOutOn => Loc("voogle_route_get_out_on", "GET OUT ON");
        internal static string SettingsTitle => Loc("voogle_route_settings_title", "VOOGLE ROUTE SETTINGS");
        internal static string SettingRouteLineColor => Loc("voogle_route_setting_route_color", "Route line color");
        internal static string SettingChooseColor => Loc("voogle_route_setting_choose_color", "CHOOSE COLOR");
        internal static string SettingClose => Loc("voogle_route_setting_close", "CLOSE");
        internal static string ColorPresetNeonBlue => Loc("voogle_route_color_neon_blue", "Neon blue");
        internal static string ColorPresetGreen => Loc("voogle_route_color_green", "Green");
        internal static string ColorPresetOrange => Loc("voogle_route_color_orange", "Orange");
        internal static string ColorPresetMagenta => Loc("voogle_route_color_magenta", "Magenta");
        internal static string ColorPresetWhite => Loc("voogle_route_color_white", "White");
        internal static string RouteRecalculating =>
            Loc("voogle_route_recalculating", "Recalculating route...");
        internal static string MapDestTitle =>
            Loc("voogle_route_map_dest_title", "Set destination");
        internal static string MapDestConfirm =>
            Loc("voogle_route_map_dest_confirm", "SET DESTINATION");
        internal static string MapDestCancel =>
            Loc("voogle_route_map_dest_cancel", "CANCEL");
        internal static string AutoDrivePopupTitle =>
            Loc("voogle_route_autodrive_popup_title", "AUTO-DRIVE");
        internal static string AutoDrivePopupBody =>
            Loc("voogle_route_autodrive_popup_body", "Estimated travel time: ~{minutes}\nEstimated arrival: {arrival}\nDistance: ~{distance} m");
        internal static string AutoDriveConfirm =>
            Loc("voogle_route_autodrive_popup_confirm", "DRIVE");
        internal static string AutoDriveCancel =>
            Loc("voogle_route_autodrive_popup_cancel", "CANCEL");
        internal static string BookmarksTitle =>
            Loc("voogle_route_bookmarks_title", "BOOKMARKS");
        internal static string BookmarksSearchPlaceholder =>
            Loc("voogle_route_bookmarks_search", "Search bookmarks...");
        internal static string BookmarksAdd =>
            Loc("voogle_route_bookmarks_add", "ADD BOOKMARK");
        internal static string BookmarksClearAll =>
            Loc("voogle_route_bookmarks_clear_all", "CLEAR ALL");
        internal static string BookmarksSetDestination =>
            Loc("voogle_route_bookmarks_set_dest", "SET");
        internal static string BookmarksCenter =>
            Loc("voogle_route_bookmarks_center", "CENTER");
        internal static string QuickBookmarkLastCar =>
            Loc("voogle_route_bookmarks_last_car", "Last Car");
        internal static string QuickBookmarkLastHome =>
            Loc("voogle_route_bookmarks_last_home", "Last Home");
        internal static string QuickBookmarkLastShop =>
            Loc("voogle_route_bookmarks_last_shop", "Last Shop");

        internal static string QuickBookmarkLabel(QuickBookmarkKind kind) =>
            kind switch
            {
                QuickBookmarkKind.LastCar => QuickBookmarkLastCar,
                QuickBookmarkKind.LastHome => QuickBookmarkLastHome,
                QuickBookmarkKind.LastShop => QuickBookmarkLastShop,
                _ => ""
            };
        internal static string BookmarksPickHint =>
            Loc("voogle_route_bookmarks_pick_hint", "Click a location on the map.");
        internal static string VisitHistoryTitle =>
            Loc("voogle_route_visit_history_title", "HISTORY");
        internal static string VisitHistoryAdd =>
            Loc("voogle_route_visit_history_add", "ADD");
        internal static string BookmarkAddTitle =>
            Loc("voogle_route_bookmark_add_title", "Add bookmark");
        internal static string BookmarkNamePlaceholder =>
            Loc("voogle_route_bookmark_name_placeholder", "Bookmark name");
        internal static string BookmarkAddConfirm =>
            Loc("voogle_route_bookmark_add_confirm", "ADD");
        internal static string BookmarkAddCancel =>
            Loc("voogle_route_bookmark_add_cancel", "CANCEL");

        internal static string FormatBookmarkCoordinates(Vector3 worldPos) =>
            Loc("voogle_route_bookmark_coords", "Coordinates: {x}, {z}")
                .Replace("{x}", Mathf.RoundToInt(worldPos.x).ToString())
                .Replace("{z}", Mathf.RoundToInt(worldPos.z).ToString());

        internal static string FormatAutoDrivePopupBody(float travelMinutes, float distanceMeters)
        {
            var minutesText = FormatTravelMinutes(travelMinutes);
            var arrivalText = FormatArrivalTime(travelMinutes);
            var distanceText = Mathf.Max(0, Mathf.RoundToInt(distanceMeters)).ToString();
            return AutoDrivePopupBody
                .Replace("{minutes}", minutesText)
                .Replace("{arrival}", arrivalText)
                .Replace("{distance}", distanceText);
        }

        private static string FormatTravelMinutes(float travelMinutes)
        {
            var total = Mathf.Max(1, Mathf.RoundToInt(travelMinutes));
            if (total < 60)
                return total + " min";

            var hours = total / 60;
            var minutes = total % 60;
            return minutes > 0 ? hours + " h " + minutes + " min" : hours + " h";
        }

        private static string FormatArrivalTime(float travelMinutes)
        {
            try
            {
                var save = SaveGameManager.Current;
                if (save != null)
                {
                    var currentMinutes = Mathf.RoundToInt(save.Hour * 60f + save.Minute);
                    var arrivalMinutes = currentMinutes + Mathf.Max(1, Mathf.RoundToInt(travelMinutes));
                    arrivalMinutes %= 24 * 60;
                    if (arrivalMinutes < 0)
                        arrivalMinutes += 24 * 60;

                    var hour = arrivalMinutes / 60;
                    var minute = arrivalMinutes % 60;
                    return hour.ToString("00") + ":" + minute.ToString("00");
                }
            }
            catch
            {
                // Save data can be unavailable while scenes or UI overlays are transitioning.
            }

            return "--:--";
        }

        internal static void PollLanguageChange()
        {
            var now = Time.unscaledTime;
            if (now < _nextLocalePoll)
                return;
            _nextLocalePoll = now + 0.5f;

            var locale = ResolveLoadedLocale();
            if (locale == _activeLocale)
                return;

            _activeLocale = locale;
            RouteToggleHud.RefreshLocalizedText();
            RouteSettingsUi.RefreshLocalizedText();
            RouteRecalcBanner.RefreshLocalizedText();
            AutoDriveConfirmPopup.RefreshLocalizedText();
            CityMapBookmarksPanel.RefreshLocalizedText();
            CityMapBookmarkAddDialog.RefreshLocalizedText();
            VisitHistoryPanel.RefreshLocalizedText();
        }

        private static string Loc(string key, string fallback)
        {
            try
            {
                var text = key.GetLocalization();
                if (!string.IsNullOrWhiteSpace(text) && text != key)
                    return text;
            }
            catch
            {
                // clé mod pas encore enregistrée
            }

            return fallback;
        }

        private static string ResolveLoadedLocale()
        {
            try
            {
                var locale = LocalizorManager.LoadedLocale;
                if (!string.IsNullOrWhiteSpace(locale))
                    return locale.Trim().Replace('_', '-');
            }
            catch
            {
                // Localizor pas encore prêt
            }

            return "en";
        }
    }
}
