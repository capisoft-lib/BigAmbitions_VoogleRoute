namespace VoogleRoute.Navigation
{
    /// <summary>
    /// Binds auto-walk to the specific board→exit stations chosen in the planned path,
    /// so proximity near other stations does not start a new ride.
    /// </summary>
    internal static class SubwayLegTracker
    {
        private static string _boardStation = string.Empty;
        private static string _exitStation = string.Empty;
        private static bool _bound;
        private static bool _rideCompleted;

        internal static bool HasBoundLeg => _bound;
        internal static bool IsRideCompleted => _rideCompleted;

        internal static void Bind(string boardStation, string exitStation)
        {
            if (string.IsNullOrEmpty(boardStation) || string.IsNullOrEmpty(exitStation))
            {
                Clear();
                return;
            }

            if (_bound &&
                _rideCompleted &&
                _boardStation == boardStation &&
                _exitStation == exitStation)
                return;

            _boardStation = boardStation;
            _exitStation = exitStation;
            _bound = true;
            _rideCompleted = false;
        }

        internal static void Clear()
        {
            _boardStation = string.Empty;
            _exitStation = string.Empty;
            _bound = false;
            _rideCompleted = false;
        }

        internal static void MarkRideCompleted()
        {
            if (!_bound)
                return;

            _rideCompleted = true;
        }

        internal static bool MatchesPlannedPath(in SubwayNavigationHint subway)
        {
            if (!_bound || _rideCompleted || !subway.Active)
                return false;

            return subway.BoardStationName == _boardStation &&
                   subway.ExitStationName == _exitStation;
        }

        internal static bool ShouldPlanSubway()
        {
            return !_bound || !_rideCompleted;
        }
    }
}
