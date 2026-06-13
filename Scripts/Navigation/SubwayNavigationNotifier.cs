using System;
using System.Collections.Generic;
using Localizor;
using UI.Notification;

namespace VoogleRoute.Navigation
{
    internal static class SubwayNavigationNotifier
    {
        private static bool _boardHintShown;

        internal static void Reset()
        {
            _boardHintShown = false;
        }

        internal static void ShowBoardHint(string exitStationName)
        {
            if (_boardHintShown)
                return;

            _boardHintShown = true;

            try
            {
                Notifications.Show(
                    NotificationType.Info,
                    "voogle_route_subway_board_hint",
                    new Dictionary<string, string> { { "station", LocalizeStation(exitStationName) } },
                    6f,
                    null,
                    null,
                    notificationSound: true,
                    trackOnSaveGame: false);
            }
            catch (Exception ex)
            {
                ModLog.Error("Subway board notification failed", ex);
            }
        }

        internal static void ShowContinueHint()
        {
            try
            {
                Notifications.Show(
                    NotificationType.Info,
                    "voogle_route_subway_continue",
                    null,
                    4f,
                    null,
                    null,
                    notificationSound: false,
                    trackOnSaveGame: false);
            }
            catch (Exception ex)
            {
                ModLog.Error("Subway continue notification failed", ex);
            }
        }

        private static string LocalizeStation(string stationName)
        {
            if (string.IsNullOrEmpty(stationName))
                return stationName;

            try
            {
                return ("subwaystation_" + stationName).GetLocalization();
            }
            catch
            {
                return stationName;
            }
        }
    }
}
