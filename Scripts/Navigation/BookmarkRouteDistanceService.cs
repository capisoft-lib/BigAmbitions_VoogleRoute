using System.Collections.Generic;
using System.Threading;
using BaPlayerLocation.Subscriber;
using Buildings;
using UnityEngine;
using VoogleRoute.Pathfinding.Graph;

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

    internal enum BookmarkDistanceConsumer
    {
        Bookmarks,
        History
    }

    internal readonly struct BookmarkDistanceOrigin
    {
        internal Vector3 Position { get; }
        internal MovementMode Mode { get; }
        internal bool IsIndoor { get; }

        internal BookmarkDistanceOrigin(Vector3 position, MovementMode mode, bool isIndoor)
        {
            Position = position;
            Mode = mode;
            IsIndoor = isIndoor;
        }
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
        private static readonly ConsumerState BookmarksState = new ConsumerState();
        private static readonly ConsumerState HistoryState = new ConsumerState();
        private static bool _historyGetsNextMainThreadCompute;

        private sealed class ConsumerState
        {
            internal int Generation;
            internal readonly HashSet<BookmarkDistanceRowKey> PendingKeys = new HashSet<BookmarkDistanceRowKey>();
            internal readonly List<BookmarkDistanceResult> Completed = new List<BookmarkDistanceResult>();
            internal readonly List<PendingMainThreadCompute> MainThreadPending = new List<PendingMainThreadCompute>();
        }

        private struct PendingMainThreadCompute
        {
            internal int Generation;
            internal BookmarkDistanceRowKey Key;
            internal Vector3 Origin;
            internal Vector3 Target;
            internal VehicleRoutePathOptions PathOptions;
        }

        internal static bool IsKeyPending(BookmarkDistanceConsumer consumer, BookmarkDistanceRowKey key)
        {
            lock (Gate)
                return GetState(consumer).PendingKeys.Contains(key);
        }

        internal static bool IsBusy(BookmarkDistanceConsumer consumer)
        {
            lock (Gate)
            {
                var state = GetState(consumer);
                return state.PendingKeys.Count > 0 ||
                       state.MainThreadPending.Count > 0 ||
                       state.Completed.Count > 0;
            }
        }

        /// <summary>Recomputes every row and cancels in-flight work (e.g. map opened).</summary>
        internal static void RequestRefresh(
            BookmarkDistanceConsumer consumer,
            IReadOnlyList<(BookmarkDistanceRowKey Key, BookmarkEntry Bookmark)> rows)
        {
            var state = GetState(consumer);
            lock (Gate)
            {
                state.Generation++;
                state.Completed.Clear();
                state.PendingKeys.Clear();
                state.MainThreadPending.Clear();
            }

            QueueComputations(consumer, rows);
        }

        /// <summary>Queues only the given rows without cancelling other in-flight calculations.</summary>
        internal static void RequestCompute(
            BookmarkDistanceConsumer consumer,
            IReadOnlyList<(BookmarkDistanceRowKey Key, BookmarkEntry Bookmark)> rows) =>
            QueueComputations(consumer, rows);

        private static void QueueComputations(
            BookmarkDistanceConsumer consumer,
            IReadOnlyList<(BookmarkDistanceRowKey Key, BookmarkEntry Bookmark)> rows)
        {
            if (rows == null || rows.Count == 0)
                return;

            if (!PlayerLocationSession.IsAvailable || !GameState.IsWorldReady())
                return;

            if (!TryGetCurrentOrigin(out var currentOrigin))
                return;

            var origin = currentOrigin.Position;
            var useFoot = currentOrigin.Mode == MovementMode.OnFoot;
            if (!useFoot && !RouteGraphStore.TryEnsureLoaded())
                return;

            var hasPose = MovementModeDetector.TryGetVehiclePose(out _, out var forward);
            var graph = useFoot ? null : RouteGraphStore.Graph;

            var state = GetState(consumer);
            int generation;
            lock (Gate)
                generation = state.Generation;

            for (var i = 0; i < rows.Count; i++)
            {
                var (key, bookmark) = rows[i];
                if (bookmark == null || !bookmark.TryGetNavigationTarget(out var target))
                    continue;

                lock (Gate)
                {
                    if (state.PendingKeys.Contains(key))
                        continue;

                    state.PendingKeys.Add(key);
                }

                // Asking the foot/subway planner for a path from an entrance back
                // to itself can produce a long loop instead of zero, depending on
                // the nearby NavMesh links. Keep this degenerate case exact and free.
                if ((target - origin).sqrMagnitude <= 25f)
                {
                    Complete(
                        consumer,
                        generation,
                        key,
                        Vector3.Distance(origin, target),
                        success: true);
                    continue;
                }

                var pathOptions = VehicleRoutePathOptions.FromMainThread(target);
                ThreadPool.QueueUserWorkItem(_ =>
                    ComputeAsync(consumer, generation, key, origin, target, forward, hasPose, graph, useFoot, pathOptions));
            }
        }

        internal static void TickMainThread()
        {
            var first = _historyGetsNextMainThreadCompute
                ? BookmarkDistanceConsumer.History
                : BookmarkDistanceConsumer.Bookmarks;
            var second = first == BookmarkDistanceConsumer.History
                ? BookmarkDistanceConsumer.Bookmarks
                : BookmarkDistanceConsumer.History;

            if (!TickMainThread(first))
                TickMainThread(second);

            _historyGetsNextMainThreadCompute = !_historyGetsNextMainThreadCompute;
        }

        private static bool TickMainThread(BookmarkDistanceConsumer consumer)
        {
            var state = GetState(consumer);
            PendingMainThreadCompute job;
            lock (Gate)
            {
                if (state.MainThreadPending.Count == 0)
                    return false;

                job = state.MainThreadPending[0];
                state.MainThreadPending.RemoveAt(0);
            }

            var success = FootSubwayRoutePlanner.TryEstimateMeters(job.Origin, job.Target, out var meters);
            Complete(consumer, job.Generation, job.Key, meters, success);
            return true;
        }

        private static void Complete(
            BookmarkDistanceConsumer consumer,
            int generation,
            BookmarkDistanceRowKey key,
            float meters,
            bool success)
        {
            var state = GetState(consumer);
            lock (Gate)
            {
                if (generation != state.Generation)
                    return;

                state.PendingKeys.Remove(key);
                state.Completed.Add(new BookmarkDistanceResult(key, meters, success));
            }
        }

        internal static bool TryDequeueCompleted(
            BookmarkDistanceConsumer consumer,
            out BookmarkDistanceResult result)
        {
            var state = GetState(consumer);
            lock (Gate)
            {
                if (state.Completed.Count == 0)
                {
                    result = default;
                    return false;
                }

                var index = state.Completed.Count - 1;
                result = state.Completed[index];
                state.Completed.RemoveAt(index);
                return true;
            }
        }

        internal static void Cancel(BookmarkDistanceConsumer consumer)
        {
            var state = GetState(consumer);
            lock (Gate)
            {
                Cancel(state);
            }
        }

        internal static void Cancel()
        {
            lock (Gate)
            {
                Cancel(BookmarksState);
                Cancel(HistoryState);
            }
        }

        private static void Cancel(ConsumerState state)
        {
            state.Generation++;
            state.Completed.Clear();
            state.PendingKeys.Clear();
            state.MainThreadPending.Clear();
        }

        private static void ComputeAsync(
            BookmarkDistanceConsumer consumer,
            int generation,
            BookmarkDistanceRowKey key,
            Vector3 origin,
            Vector3 target,
            Vector3 forward,
            bool hasPose,
            RouteGraph graph,
            bool useFoot,
            VehicleRoutePathOptions pathOptions)
        {
            var state = GetState(consumer);
            if (useFoot)
            {
                lock (Gate)
                {
                    if (generation != state.Generation)
                        return;

                    state.MainThreadPending.Add(new PendingMainThreadCompute
                    {
                        Generation = generation,
                        Key = key,
                        Origin = origin,
                        Target = target,
                        PathOptions = pathOptions
                    });
                }

                return;
            }

            float meters;
            bool success;
            try
            {
                success = BookmarkRouteDistance.TryComputeRouteMeters(
                    origin,
                    target,
                    forward,
                    hasPose,
                    graph,
                    pathOptions,
                    out meters);
            }
            catch (System.Exception ex)
            {
                ModLog.Error("Bookmark distance async failed | key=" + key.Kind, ex);
                meters = -1f;
                success = false;
            }

            Complete(consumer, generation, key, meters, success);
        }

        private static ConsumerState GetState(BookmarkDistanceConsumer consumer) =>
            consumer == BookmarkDistanceConsumer.History ? HistoryState : BookmarksState;

        internal static bool TryGetCurrentOrigin(out BookmarkDistanceOrigin origin)
        {
            if (TryGetIndoorBuildingOrigin(out var indoorOrigin))
            {
                origin = new BookmarkDistanceOrigin(indoorOrigin, MovementMode.OnFoot, isIndoor: true);
                return true;
            }

            if (MovementModeDetector.TryGetPathOrigin(out var pathOrigin))
            {
                origin = new BookmarkDistanceOrigin(
                    pathOrigin,
                    MovementModeDetector.CurrentMode,
                    isIndoor: false);
                return true;
            }

            var snapshotPosition = PlayerLocationSession.Snapshot.Position;
            if (snapshotPosition.sqrMagnitude > 0.01f)
            {
                origin = new BookmarkDistanceOrigin(
                    snapshotPosition,
                    MovementModeDetector.CurrentMode,
                    isIndoor: false);
                return true;
            }

            origin = default;
            return false;
        }

        private static bool TryGetIndoorBuildingOrigin(out Vector3 origin)
        {
            origin = default;

            if (MovementModeDetector.IsHamptonsVehicleNavigationContext())
                return false;

            var isIndoor = PlayerLocationSession.IsAvailable &&
                           PlayerLocationSession.Snapshot.MovementKind == MovementKind.Indoor;
            try
            {
                isIndoor |= BuildingManager.IsInsideBuilding;
            }
            catch
            {
                // Use the subscriber movement kind when the game manager is unavailable.
            }

            if (!isIndoor)
                return false;

            try
            {
                var manager = BuildingManager.Instance;
                var address = manager?.buildingRegistration?.Address;
                if (address != null && DestinationResolver.TryResolveWorldPosition(address, out origin))
                    return origin.sqrMagnitude > 0.01f;

                var building = manager?.cityBuildingController;
                var poi = building?.GetPoiPosition();
                if (poi != null)
                {
                    origin = poi.position;
                    return origin.sqrMagnitude > 0.01f;
                }

                if (building != null)
                {
                    origin = building.transform.position;
                    return origin.sqrMagnitude > 0.01f;
                }
            }
            catch
            {
                // A missing indoor registration should leave the distance unavailable.
            }

            origin = default;
            return false;
        }
    }

    internal sealed class BookmarkDistanceRefreshTracker
    {
        private const float RefreshIntervalSeconds = 1.5f;
        private const float FootMovementThresholdMeters = 1f;
        private const float VehicleMovementThresholdMeters = 5f;

        private readonly BookmarkDistanceConsumer _consumer;
        private float _nextRefreshTime;
        private BookmarkDistanceOrigin _lastOrigin;
        private bool _hasLastOrigin;

        internal BookmarkDistanceRefreshTracker(BookmarkDistanceConsumer consumer)
        {
            _consumer = consumer;
        }

        internal void Reset()
        {
            _nextRefreshTime = 0f;
            _lastOrigin = default;
            _hasLastOrigin = false;
        }

        internal void RememberCurrentOrigin()
        {
            _nextRefreshTime = Time.unscaledTime + RefreshIntervalSeconds;
            if (!BookmarkRouteDistanceService.TryGetCurrentOrigin(out var origin))
                return;

            _lastOrigin = origin;
            _hasLastOrigin = true;
        }

        internal bool ShouldRefresh()
        {
            var now = Time.unscaledTime;
            if (now < _nextRefreshTime)
                return false;

            _nextRefreshTime = now + RefreshIntervalSeconds;
            if (BookmarkRouteDistanceService.IsBusy(_consumer) ||
                !BookmarkRouteDistanceService.TryGetCurrentOrigin(out var current))
                return false;

            if (!_hasLastOrigin)
            {
                _lastOrigin = current;
                _hasLastOrigin = true;
                return true;
            }

            var originChanged = HorizontalDistanceSquared(_lastOrigin.Position, current.Position);
            if (current.IsIndoor)
            {
                var buildingChanged = !_lastOrigin.IsIndoor || originChanged > 1f;
                _lastOrigin = current;
                return buildingChanged;
            }

            var threshold = current.Mode == MovementMode.Vehicle
                ? VehicleMovementThresholdMeters
                : FootMovementThresholdMeters;
            var shouldRefresh = _lastOrigin.IsIndoor ||
                                _lastOrigin.Mode != current.Mode ||
                                originChanged >= threshold * threshold;
            if (shouldRefresh)
                _lastOrigin = current;

            return shouldRefresh;
        }

        private static float HorizontalDistanceSquared(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return dx * dx + dz * dz;
        }
    }
}
