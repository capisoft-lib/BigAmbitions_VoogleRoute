using BaPlayerLocation.Subscriber;
using VoogleRoute.Navigation;

namespace VoogleRoute.Live
{
    /// <summary>INFO log when LIB_BaPlayerLocation delivers a pose change via <see cref="Navigation.PlayerLocationSession"/>.</summary>
    internal static class PlayerLocationLogger
    {
        private const float MinimumLogIntervalSeconds = 1f;
        private static float _nextLogTime;

        internal static void Initialize() => PlayerLocationSession.Changed += OnLocationChanged;

        internal static void Shutdown() => PlayerLocationSession.Changed -= OnLocationChanged;

        private static void OnLocationChanged(PlayerLocationSnapshot snapshot)
        {
            if (!snapshot.IsAvailable)
            {
                if (ModLog.IsEnabled(ModLogLevel.Info) && ShouldLogNow())
                    ModLog.Info("GPS state=Unavailable pos=— heading=—");
                return;
            }

            if (!ModLog.IsEnabled(ModLogLevel.Debug) || !ShouldLogNow())
                return;

            var state = PlayerLocationSnapshotMapper.ToLegacyState(snapshot.MovementKind);
            ModLog.Debug(
                "LIB GPS | state=" + PlayerLocationSnapshotMapper.FormatState(state) +
                " pos=(" + PlayerLocationSnapshotMapper.FormatPosition(snapshot.Position) + ")" +
                " heading=" + PlayerLocationSnapshotMapper.FormatHeading(snapshot.HeadingDeg));
        }

        private static bool ShouldLogNow()
        {
            var now = UnityEngine.Time.unscaledTime;
            if (now < _nextLogTime)
                return false;
            _nextLogTime = now + MinimumLogIntervalSeconds;
            return true;
        }
    }
}
