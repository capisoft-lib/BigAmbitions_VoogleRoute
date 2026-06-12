using UnityEngine;

namespace VoogleRoute.Navigation
{
    internal static class VehiclePathFollower
    {
        private const float MinLookaheadMeters = 6f;
        private const float MaxLookaheadMeters = 24f;
        private const float TurnPreviewMeters = 40f;

        private static int _progressSegment;

        internal struct FollowState
        {
            internal Vector3 LookaheadTarget;
            internal float CrossTrackMeters;
            internal float HeadingErrorDegrees;
            internal float UpcomingTurnDegrees;
            internal float DistanceToDestination;
            internal bool OffRoute;
        }

        internal static void Reset() => _progressSegment = 0;

        internal static FollowState Evaluate(
            Vector3[] path,
            Vector3 position,
            Vector3 forward,
            float speedMps,
            Vector3 finalDestination)
        {
            if (path == null || path.Length < 2)
            {
                return new FollowState
                {
                    LookaheadTarget = finalDestination,
                    DistanceToDestination = HorizontalDistance(position, finalDestination),
                    OffRoute = true
                };
            }

            _progressSegment = PathGeometry.FindProgressSegmentIndex(
                path,
                position,
                forward,
                ref _progressSegment);

            var segStart = path[_progressSegment];
            var segEnd = path[Mathf.Min(_progressSegment + 1, path.Length - 1)];
            var crossTrack = Mathf.Sqrt(
                PathGeometry.HorizontalDistSqToSegment(position, segStart, segEnd, out _));

            var lookahead = ComputeLookaheadDistance(speedMps);
            var target = SampleLookaheadPoint(path, position, _progressSegment, lookahead);
            var headingError = ComputeHeadingError(position, forward, target);
            var upcomingTurn = EstimateUpcomingTurn(path, _progressSegment, TurnPreviewMeters);
            var distToDest = HorizontalDistance(position, finalDestination);
            var offRoute = crossTrack > 12f ||
                           !PathGeometry.IsWithinRouteCorridor(position, path, 16f);

            return new FollowState
            {
                LookaheadTarget = target,
                CrossTrackMeters = crossTrack,
                HeadingErrorDegrees = headingError,
                UpcomingTurnDegrees = upcomingTurn,
                DistanceToDestination = distToDest,
                OffRoute = offRoute
            };
        }

        private static float ComputeLookaheadDistance(float speedMps) =>
            Mathf.Clamp(4f + speedMps * 0.55f, MinLookaheadMeters, MaxLookaheadMeters);

        private static Vector3 SampleLookaheadPoint(
            Vector3[] path,
            Vector3 position,
            int startSegment,
            float lookaheadMeters)
        {
            var remaining = lookaheadMeters;
            var seg = Mathf.Clamp(startSegment, 0, path.Length - 2);

            PathGeometry.HorizontalDistSqToSegment(position, path[seg], path[seg + 1], out var tOnSeg);
            var segStart = Vector3.Lerp(path[seg], path[seg + 1], tOnSeg);
            var segRemain = HorizontalDistance(segStart, path[seg + 1]);
            if (segRemain >= remaining)
                return segStart + FlatDir(segStart, path[seg + 1]) * remaining;

            remaining -= segRemain;
            for (var i = seg + 1; i < path.Length - 1; i++)
            {
                var legLen = HorizontalDistance(path[i], path[i + 1]);
                if (legLen >= remaining)
                    return path[i] + FlatDir(path[i], path[i + 1]) * remaining;

                remaining -= legLen;
            }

            return path[^1];
        }

        private static float EstimateUpcomingTurn(Vector3[] path, int startSegment, float previewMeters)
        {
            if (path.Length < 3)
                return 0f;

            var seg = Mathf.Clamp(startSegment, 0, path.Length - 2);
            var walked = 0f;
            var maxTurn = 0f;

            while (walked < previewMeters && seg < path.Length - 2)
            {
                var inDir = FlatDir(path[seg], path[seg + 1]);
                var outDir = FlatDir(path[seg + 1], path[seg + 2]);
                if (inDir.sqrMagnitude > 0.01f && outDir.sqrMagnitude > 0.01f)
                {
                    var turn = Mathf.Abs(Vector3.SignedAngle(inDir, outDir, Vector3.up));
                    if (turn > maxTurn)
                        maxTurn = turn;
                }

                walked += HorizontalDistance(path[seg], path[seg + 1]);
                seg++;
            }

            return maxTurn;
        }

        private static float ComputeHeadingError(Vector3 position, Vector3 forward, Vector3 target)
        {
            var toTarget = target - position;
            toTarget.y = 0f;
            forward.y = 0f;
            if (toTarget.sqrMagnitude < 0.01f || forward.sqrMagnitude < 0.01f)
                return 0f;

            var desired = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
            var current = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            return Mathf.DeltaAngle(current, desired);
        }

        private static Vector3 FlatDir(Vector3 from, Vector3 to)
        {
            var dir = to - from;
            dir.y = 0f;
            var len = dir.magnitude;
            return len > 0.001f ? dir / len : Vector3.forward;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
