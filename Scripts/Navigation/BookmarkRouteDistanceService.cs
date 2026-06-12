using System.Collections.Generic;
using System.Threading;
using BaPlayerLocation.Subscriber;
using UnityEngine;
using VoogleRoute.Pathfinding.Graph;
using VoogleRoute.UI;

namespace VoogleRoute.Navigation
{
    internal enum BookmarkDistanceRowKind
    {
        LastCar,
        LastHome,
        LastShop,
        Vehicle,
        Bookmark,
        History
    }

    internal struct BookmarkDistanceRowKey : System.IEquatable<BookmarkDistanceRowKey>
    {
        internal BookmarkDistanceRowKind Kind;
        internal int BookmarkIndex;

        public bool Equals(BookmarkDistanceRowKey other) =>
            Kind == other.Kind && BookmarkIndex == other.BookmarkIndex;

        public override bool Equals(object obj) => obj is BookmarkDistanceRowKey other && Equals(other);

        public override int GetHashCode() => ((int)Kind * 397) ^ BookmarkIndex;
    }

    internal readonly struct BookmarkDistanceResult
    {
        internal BookmarkDistanceRowKey Key { get; }
        internal float Meters { get; }
        internal bool Success { get; }

        internal BookmarkDistanceResult(BookmarkDistanceRowKey key, float meters, bool success)
        {
            Key = key;
            Meters = meters;
            Success = success;
        }
    }

    /// <summary>Computes bookmark list distances on a background thread so the city map UI stays responsive.</summary>
    internal static class BookmarkRouteDistanceService
    {
        private static readonly object Gate = new object();
        private static int _generation;
        private static int _pendingCount;
        private static readonly HashSet<BookmarkDistanceRowKey> _pendingKeys = new HashSet<BookmarkDistanceRowKey>();
        private static readonly List<BookmarkDistanceResult> _completed = new List<BookmarkDistanceResult>();

        internal static bool IsRecalcInProgress
        {
            get
            {
                lock (Gate)
                    return _pendingCount > 0;
            }
        }

        internal static bool IsKeyPending(BookmarkDistanceRowKey key)
        {
            lock (Gate)
                return _pendingKeys.Contains(key);
        }

        /// <summary>Recomputes every row and cancels in-flight work (e.g. map opened).</summary>
        internal static void RequestRefresh(IReadOnlyList<(BookmarkDistanceRowKey Key, BookmarkEntry Bookmark)> rows)
        {
            lock (Gate)
            {
                _generation++;
                _pendingCount = 0;
                _completed.Clear();
                _pendingKeys.Clear();
            }

            QueueComputations(rows, replacePendingCount: true);
        }

        /// <summary>Queues only the given rows without cancelling other in-flight calculations.</summary>
        internal static void RequestCompute(IReadOnlyList<(BookmarkDistanceRowKey Key, BookmarkEntry Bookmark)> rows) =>
            QueueComputations(rows, replacePendingCount: false);

        private static void QueueComputations(
            IReadOnlyList<(BookmarkDistanceRowKey Key, BookmarkEntry Bookmark)> rows,
            bool replacePendingCount)
        {
            if (rows == null || rows.Count == 0)
                return;

            if (!PlayerLocationSession.IsAvailable || !GameState.IsWorldReady())
                return;

            if (!TryGetOrigin(out var origin))
                return;

            if (!RouteGraphStore.TryEnsureLoaded())
                return;

            var hasPose = MovementModeDetector.TryGetVehiclePose(out _, out var forward);
            var graph = RouteGraphStore.Graph;

            int generation;
            lock (Gate)
                generation = _generation;

            var queued = 0;
            for (var i = 0; i < rows.Count; i++)
            {
                var (key, bookmark) = rows[i];
                if (bookmark == null || !bookmark.TryGetNavigationTarget(out var target))
                    continue;

                lock (Gate)
                {
                    if (_pendingKeys.Contains(key))
                        continue;

                    _pendingKeys.Add(key);
                }

                queued++;
                ThreadPool.QueueUserWorkItem(_ =>
                    ComputeAsync(generation, key, origin, target, forward, hasPose, graph));
            }

            if (queued == 0)
                return;

            lock (Gate)
            {
                if (replacePendingCount)
                    _pendingCount = queued;
                else
                    _pendingCount += queued;
            }

            RouteRecalcBanner.ShowOnCityMap();
        }

        internal static bool TryDequeueCompleted(out BookmarkDistanceResult result)
        {
            lock (Gate)
            {
                if (_completed.Count == 0)
                {
                    result = default;
                    return false;
                }

                var index = _completed.Count - 1;
                result = _completed[index];
                _completed.RemoveAt(index);
                return true;
            }
        }

        internal static void Cancel()
        {
            lock (Gate)
            {
                _generation++;
                _pendingCount = 0;
                _completed.Clear();
                _pendingKeys.Clear();
            }

            RouteRecalcBanner.ForceHide();
        }

        private static void ComputeAsync(
            int generation,
            BookmarkDistanceRowKey key,
            Vector3 origin,
            Vector3 target,
            Vector3 forward,
            bool hasPose,
            RouteGraph graph)
        {
            var success = BookmarkRouteDistance.TryComputeRouteMeters(
                origin,
                target,
                forward,
                hasPose,
                graph,
                out var meters);

            lock (Gate)
            {
                if (generation != _generation)
                    return;

                _pendingKeys.Remove(key);
                _completed.Add(new BookmarkDistanceResult(key, meters, success));
                if (_pendingCount > 0)
                    _pendingCount--;
            }
        }

        private static bool TryGetOrigin(out Vector3 origin)
        {
            if (MovementModeDetector.TryGetPathOrigin(out origin))
                return true;

            origin = PlayerLocationSession.Snapshot.Position;
            return origin.sqrMagnitude > 0.01f;
        }
    }
}
