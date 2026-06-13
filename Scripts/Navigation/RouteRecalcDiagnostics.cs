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
        internal static RoutePathfindKind LastPathfindKind { get; private set; } = RoutePathfindKind.None;
        internal static float LastPathfindMs { get; private set; }

        internal static void RecordPathfind(RoutePathfindKind kind, float elapsedMs)
        {
            LastPathfindKind = kind;
            LastPathfindMs = elapsedMs;
        }

        internal static void LogSkip(string requestSource, string skipReason, float elapsedMs)
        {
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
            ModLog.Info("Route cache invalidated | reason=" + reason);
        }

        internal static Stopwatch StartTimer() => Stopwatch.StartNew();

        internal static float ElapsedMs(Stopwatch watch) => (float)watch.Elapsed.TotalMilliseconds;

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
    }
}
