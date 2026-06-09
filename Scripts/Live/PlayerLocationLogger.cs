using BaPlayerLocation.Subscriber;
using VoogleRoute.Navigation;

namespace VoogleRoute.Live
{
    /// <summary>INFO log when GPS state, position, or heading changes (via LIB_BaPlayerLocation).</summary>
    internal static class PlayerLocationLogger
    {
        internal static void Initialize() => PlayerLocationSession.Changed += OnLocationChanged;

        internal static void Shutdown() => PlayerLocationSession.Changed -= OnLocationChanged;

        private static void OnLocationChanged(PlayerLocationSnapshot snapshot)
        {
            if (!snapshot.IsAvailable)
            {
                ModLog.Info("GPS state=Unavailable pos=— heading=—");
                return;
            }

            var state = PlayerLocationSnapshotMapper.ToLegacyState(snapshot.MovementKind);
            ModLog.Info(
                "GPS state=" + PlayerLocationSnapshotMapper.FormatState(state) +
                " pos=(" + PlayerLocationSnapshotMapper.FormatPosition(snapshot.Position) + ")" +
                " heading=" + PlayerLocationSnapshotMapper.FormatHeading(snapshot.HeadingDeg));
        }
    }
}
