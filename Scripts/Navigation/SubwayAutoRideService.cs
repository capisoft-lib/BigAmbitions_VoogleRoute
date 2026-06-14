using System.Collections;
using UnityEngine;
using VoogleRoute.UI;

namespace VoogleRoute.Navigation
{
    /// <summary>Triggers a subway ride programmatically for auto-walk at the board station.</summary>
    internal static class SubwayAutoRideService
    {
        private const float MapOpenTimeoutSeconds = 6f;
        private const float RideStartTimeoutSeconds = 3f;

        private static float _lastAttemptTime = -999f;
        private static string _lastAttemptKey = string.Empty;
        private static bool _rideCoroutineRunning;

        internal static void Reset()
        {
            _lastAttemptTime = -999f;
            _lastAttemptKey = string.Empty;
        }

        internal static bool TryBeginRide(string boardStationName, string exitStationName)
        {
            if (string.IsNullOrEmpty(boardStationName) || string.IsNullOrEmpty(exitStationName))
                return false;

            if (boardStationName == exitStationName)
                return false;

            var attemptKey = boardStationName + ">" + exitStationName;
            var now = Time.unscaledTime;
            if (attemptKey == _lastAttemptKey && now - _lastAttemptTime < 1.5f)
                return _rideCoroutineRunning;

            try
            {
                if (!CityManager.IsInitialized)
                    return false;

                if (SubwaySystem.IsRiding)
                    return true;

                if (SaveGameManager.Current.Money < SubwayStation.PricePerRide)
                    return false;

                var board = FindStation(boardStationName);
                var exit = FindStation(exitStationName);
                if (board == null || exit == null)
                    return false;

                if (_rideCoroutineRunning)
                    return true;

                var host = VoogleRouteDriver.Instance;
                if (host == null)
                    return false;

                _lastAttemptKey = attemptKey;
                _lastAttemptTime = now;
                host.StartCoroutine(BeginRideCoroutine(board, exit));
                return true;
            }
            catch (System.Exception ex)
            {
                ModLog.Error("Subway auto-ride failed", ex);
                return false;
            }
        }

        private static IEnumerator BeginRideCoroutine(SubwayStation board, SubwayStation exit)
        {
            _rideCoroutineRunning = true;
            try
            {
                SuppressVoogleMapUi();

                var cityMap = CityManager.Instance.cityMap;
                var subwaySystem = CityManager.Instance.subwaySystem;
                if (cityMap == null || subwaySystem == null)
                    yield break;

                subwaySystem.lastSubwayStation = board;

                if (!CityMap.IsOpen)
                    cityMap.Toggle();

                var openDeadline = Time.unscaledTime + MapOpenTimeoutSeconds;
                while (!CityMap.IsOpen && Time.unscaledTime < openDeadline)
                    yield return null;

                if (!CityMap.IsOpen)
                    yield break;

                SuppressVoogleMapUi();

                if (!cityMap.isSubwayMode)
                    cityMap.ToggleSubwayMode(true);

                yield return null;
                SuppressVoogleMapUi();

                subwaySystem.TravelTo(exit);

                var rideDeadline = Time.unscaledTime + RideStartTimeoutSeconds;
                while (!SubwaySystem.IsRiding && Time.unscaledTime < rideDeadline)
                    yield return null;
            }
            finally
            {
                _rideCoroutineRunning = false;
            }
        }

        private static void SuppressVoogleMapUi()
        {
            CityMapBookmarksPanel.SuppressForSubwayNavigation();
            CityMapBookmarkAddDialog.Close();
            RouteRecalcBanner.ForceHide();
            VisitHistoryPanel.Close();
        }

        private static SubwayStation FindStation(string stationName)
        {
            var stations = CityManager.Instance?.subwayStations;
            if (stations == null)
                return null;

            for (var i = 0; i < stations.Count; i++)
            {
                var station = stations[i];
                if (station == null)
                    continue;

                if (station.stationName.ToStringFast() == stationName)
                    return station;
            }

            return null;
        }
    }
}
