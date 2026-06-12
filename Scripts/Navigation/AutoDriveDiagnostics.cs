using UnityEngine;

namespace VoogleRoute.Navigation
{
    internal static class AutoDriveDiagnostics
    {
        private static string _lastBlockReason = string.Empty;
        private static float _nextStatusLogAt;
        private static float _nextTickLogAt;
        private static bool _wasEnabled;

        internal static void OnEnabled()
        {
            _wasEnabled = true;
            _lastBlockReason = string.Empty;
            AutoDriveLog.Write("enabled by user | modRoot=" + ModStoragePaths.ModRootDirectory);
        }

        internal static void OnDisabled(string reason)
        {
            if (!_wasEnabled && string.IsNullOrEmpty(reason))
                return;

            _wasEnabled = false;
            _lastBlockReason = string.Empty;
            AutoDriveLog.Write("disabled | reason=" + reason);
        }

        internal static void LogBlockedOnce(string reason)
        {
            if (reason == _lastBlockReason)
                return;

            _lastBlockReason = reason;
            AutoDriveLog.Write("BLOCKED: " + reason);
        }

        internal static void ClearBlockReason() => _lastBlockReason = string.Empty;

        internal static void LogApplyThrottled(
            float speed,
            float throttle,
            float brakes,
            float steering,
            float crossTrack,
            float signedCrossTrack,
            float headingError,
            float distDest,
            bool offRoute,
            float obstacleBrake,
            int waypointCount)
        {
            var now = Time.unscaledTime;
            if (now < _nextTickLogAt)
                return;

            _nextTickLogAt = now + 0.75f;
            AutoDriveLog.Write(
                "drive spd=" + speed.ToString("F1") +
                " thr=" + throttle.ToString("F2") +
                " brk=" + brakes.ToString("F2") +
                " str=" + steering.ToString("F2") +
                " xtrack=" + crossTrack.ToString("F1") +
                " sxtrack=" + signedCrossTrack.ToString("F1") +
                " head=" + headingError.ToString("F0") +
                " obs=" + obstacleBrake.ToString("F2") +
                " dest=" + distDest.ToString("F0") +
                " off=" + offRoute +
                " wpts=" + waypointCount);
        }

        internal static void LogStatusThrottled(string status)
        {
            var now = Time.unscaledTime;
            if (now < _nextStatusLogAt)
                return;

            _nextStatusLogAt = now + 2f;
            AutoDriveLog.Write("status | " + status);
        }
    }
}
