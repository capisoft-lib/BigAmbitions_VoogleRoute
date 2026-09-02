using System.Diagnostics;
using System.Globalization;
using UnityEngine;

namespace VoogleRoute.Navigation
{
    internal enum RoutePathfindKind
    {
        None,
        FullAStar,
        NavMeshFoot,
        NavMeshFootSubway,
        Failed
    }

    /// <summary>Structured route recalc / cache logs for in-game performance analysis.</summary>
    internal static class RouteRecalcDiagnostics
    {
        private const double DuplicateFailureLogIntervalSeconds = 5d;
        private const double DuplicateSkipLogIntervalSeconds = 2d;
        private static readonly object FailureLogGate = new object();
        private static string _lastFailureKey;
        private static long _lastFailureTimestamp;
        private static string _lastSkipKey;
        private static long _lastSkipTimestamp;

        internal static RoutePathfindKind LastPathfindKind { get; private set; } = RoutePathfindKind.None;
        internal static float LastPathfindMs { get; private set; }

        internal static void RecordPathfind(RoutePathfindKind kind, float elapsedMs)
        {
            LastPathfindKind = kind;
            LastPathfindMs = elapsedMs;
        }

        internal static void LogSkip(string requestSource, string skipReason, float elapsedMs)
        {
            if (!ModLog.IsEnabled(ModLogLevel.Debug) ||
                !ShouldLogSkip(requestSource, skipReason))
                return;

            ModLog.Debug(
                "Route skip | source=" + requestSource +
                " | reason=" + skipReason +
                " | ms=" + FormatMs(elapsedMs));
        }

        internal static void LogRecalc(
            string requestSource,
            string recalcReason,
            MovementMode mode,
            float totalMs,
            float pathfindMs,
            float pipelineMs,
            int pointCount,
            RoutePathfindKind pathfindKind,
            bool success)
        {
            if (!ModLog.IsEnabled(ModLogLevel.Info))
                return;

            ModLog.Info(
                "Route recalc | source=" + requestSource +
                " | trigger=" + recalcReason +
                " | mode=" + mode +
                " | ms=" + FormatMs(totalMs) +
                " pathfind=" + FormatMs(pathfindMs) +
                " pipeline=" + FormatMs(pipelineMs) +
                " | pathfind_kind=" + pathfindKind +
                " | points=" + pointCount +
                " | success=" + success);
        }

        internal static void LogRecalcFailed(
            string requestSource,
            string recalcReason,
            MovementMode mode,
            float totalMs,
            float pathfindMs,
            RoutePathfindKind pathfindKind,
            string detail)
        {
            if (!ModLog.IsEnabled(ModLogLevel.Info) ||
                !ShouldLogFailure(requestSource, recalcReason, mode, detail))
                return;

            ModLog.Info(
                "Route recalc failed | source=" + requestSource +
                " | trigger=" + recalcReason +
                " | mode=" + mode +
                " | ms=" + FormatMs(totalMs) +
                " pathfind=" + FormatMs(pathfindMs) +
                " | pathfind_kind=" + pathfindKind +
                " | detail=" + detail);
        }

        internal static void LogCacheInvalidated(string reason)
        {
            if (!ModLog.IsEnabled(ModLogLevel.Info))
                return;
            ModLog.Info("Route cache invalidated | reason=" + reason);
        }

        internal static long StartTimer() => Stopwatch.GetTimestamp();

        internal static float ElapsedMs(long startedAt) =>
            (float)((Stopwatch.GetTimestamp() - startedAt) * 1000d / Stopwatch.Frequency);

        internal static string BuildRecalcReason(
            bool forceRecalc,
            bool modeChanged,
            bool targetChanged,
            bool targetMoved,
            bool leftCorridor,
            bool cacheMiss)
        {
            if (forceRecalc)
                return "forced";

            if (modeChanged)
                return "mode_changed";

            if (targetChanged)
                return "target_changed";

            if (targetMoved)
                return "target_moved";

            if (leftCorridor)
                return "left_corridor";

            if (cacheMiss)
                return "cache_miss";

            return "unknown";
        }

        private static string FormatMs(float ms) =>
            ms.ToString("0.###", CultureInfo.InvariantCulture);

        private static bool ShouldLogFailure(
            string requestSource,
            string recalcReason,
            MovementMode mode,
            string detail)
        {
            var key = requestSource + "|" + recalcReason + "|" + mode + "|" + detail;
            var now = Stopwatch.GetTimestamp();
            lock (FailureLogGate)
            {
                if (!string.Equals(_lastFailureKey, key, System.StringComparison.Ordinal) ||
                    (now - _lastFailureTimestamp) / (double)Stopwatch.Frequency >=
                    DuplicateFailureLogIntervalSeconds)
                {
                    _lastFailureKey = key;
                    _lastFailureTimestamp = now;
                    return true;
                }

                return false;
            }
        }

        private static bool ShouldLogSkip(string requestSource, string skipReason)
        {
            var key = requestSource + "|" + skipReason;
            var now = Stopwatch.GetTimestamp();
            lock (FailureLogGate)
            {
                if (!string.Equals(_lastSkipKey, key, System.StringComparison.Ordinal) ||
                    (now - _lastSkipTimestamp) / (double)Stopwatch.Frequency >=
                    DuplicateSkipLogIntervalSeconds)
                {
                    _lastSkipKey = key;
                    _lastSkipTimestamp = now;
                    return true;
                }

                return false;
            }
        }
    }
}
