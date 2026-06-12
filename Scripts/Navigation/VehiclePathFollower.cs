using UnityEngine;

namespace VoogleRoute.Navigation
{
    internal static class VehiclePathFollower
    {
        private const float MinLookaheadMeters = 8f;
        private const float MaxLookaheadMeters = 18f;
        private const float TurnPreviewMeters = 35f;

        private static int _progressSegment;

        internal struct FollowState
        {
            internal Vector3 LookaheadTarget;
            internal float CrossTrackMeters;
            internal float SignedCrossTrackMeters;
            internal float HeadingErrorDegrees;
            internal float UpcomingTurnDegrees;
            internal float DistanceToDestination;
            internal float SegmentHeadingDegrees;
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
                ref _progressSegment,
                maxSegmentJump: 2,
                lookAheadGateMeters: 28f);

            var segStart = path[_progressSegment];
            var segEnd = path[Mathf.Min(_progressSegment + 1, path.Length - 1)];
            var signedCrossTrack = ComputeSignedCrossTrack(position, segStart, segEnd);
            var crossTrack = Mathf.Abs(signedCrossTrack);

            var segDir = FlatDir(segStart, segEnd);
            var segmentHeading = segDir.sqrMagnitude > 0.01f
                ? Mathf.Atan2(segDir.x, segDir.z) * Mathf.Rad2Deg
                : 0f;

            var lookahead = ComputeLookaheadDistance(speedMps, crossTrack);
            var target = SampleLookaheadPoint(path, position, _progressSegment, lookahead);
            var headingError = ComputeHeadingError(position, forward, target);
            var upcomingTurn = EstimateUpcomingTurn(path, _progressSegment, TurnPreviewMeters);
            var distToDest = HorizontalDistance(position, finalDestination);
            var offRoute = crossTrack > 18f &&
                           !PathGeometry.IsWithinRouteCorridor(position, path, 22f);

            return new FollowState
            {
                LookaheadTarget = target,
                CrossTrackMeters = crossTrack,
                SignedCrossTrackMeters = signedCrossTrack,
                HeadingErrorDegrees = headingError,
                UpcomingTurnDegrees = upcomingTurn,
                DistanceToDestination = distToDest,
                SegmentHeadingDegrees = segmentHeading,
                OffRoute = offRoute
            };
        }

        private static float ComputeLookaheadDistance(float speedMps, float crossTrackMeters)
        {
            var baseDist = Mathf.Clamp(6f + speedMps * 0.75f, MinLookaheadMeters, MaxLookaheadMeters);
            if (crossTrackMeters > 3f)
                baseDist = Mathf.Max(MinLookaheadMeters, baseDist - crossTrackMeters * 0.35f);
            return baseDist;
        }

        private static float ComputeSignedCrossTrack(Vector3 position, Vector3 segA, Vector3 segB)
        {
            PathGeometry.HorizontalDistSqToSegment(position, segA, segB, out var t);
            var closest = Vector3.Lerp(segA, segB, t);
            var pathDir = FlatDir(segA, segB);
            var error = position - closest;
            error.y = 0f;
            return Vector3.Cross(pathDir, error).y;
        }

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
