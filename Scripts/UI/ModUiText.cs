using Localizor;
using UnityEngine;

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
        internal static string WalkOn => Loc("voogle_route_walk_on", "WALK ON");
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
