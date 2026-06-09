using UnityEngine;



namespace VoogleRoute.Navigation

{

    /// <summary>

    /// Angle entre voies (axes de circulation), pas la corde synthetic_turn.

    /// </summary>

    internal static class TrafficTurnGeometry

    {

        internal static float SignedLaneTurnDegrees(

            Vector3[] positions,

            int[][] forward,

            int incoming,

            int at,

            int to,

            int afterTo = -1)

        {

            if (incoming < 0 || at < 0 || to < 0 ||

                incoming >= positions.Length || at >= positions.Length || to >= positions.Length)

                return 0f;



            var inDir = FlatDir(positions[incoming], positions[at]);

            var outDir = afterTo >= 0 && afterTo < positions.Length

                ? FlatDir(positions[to], positions[afterTo])

                : ResolveOutgoingLaneDir(positions, forward, at, to);



            if (inDir.sqrMagnitude < 0.01f || outDir.sqrMagnitude < 0.01f)

                return 0f;



            return Vector3.SignedAngle(inDir, outDir, Vector3.up);

        }



        private static Vector3 ResolveOutgoingLaneDir(Vector3[] positions, int[][] forward, int at, int to)

        {

            if (forward == null || to < 0 || to >= forward.Length)

                return Vector3.zero;



            var neighbors = forward[to];

            if (neighbors == null || neighbors.Length == 0)

            {

                var hint = FlatDir(positions[at], positions[to]);

                return hint.sqrMagnitude > 0.01f ? hint : Vector3.zero;

            }



            var posTo = positions[to];

            var edgeHint = FlatDir(positions[at], positions[to]);

            var bestAlign = float.NegativeInfinity;

            var bestDir = Vector3.zero;

            var bestLen = -1f;

            var fallbackDir = Vector3.zero;

            var hasHint = edgeHint.sqrMagnitude > 0.01f;



            for (var i = 0; i < neighbors.Length; i++)

            {

                var n = neighbors[i];

                if (n < 0 || n >= positions.Length)

                    continue;



                var dir = FlatDir(posTo, positions[n]);

                if (dir.sqrMagnitude < 0.01f)

                    continue;



                var len = FlatLength(posTo, positions[n]);

                if (len > bestLen)

                {

                    bestLen = len;

                    fallbackDir = dir;

                }



                if (!hasHint)

                    continue;



                var align = Vector3.Dot(dir, edgeHint);

                if (align <= bestAlign)

                    continue;



                bestAlign = align;

                bestDir = dir;

            }



            if (hasHint && bestAlign > -0.15f)

                return bestDir;



            return fallbackDir.sqrMagnitude > 0.01f ? fallbackDir : edgeHint;

        }



        private static Vector3 FlatDir(Vector3 from, Vector3 to)

        {

            var d = to - from;

            d.y = 0f;

            return d.sqrMagnitude > 0.01f ? d.normalized : Vector3.zero;

        }



        private static float FlatLength(Vector3 a, Vector3 b)

        {

            var dx = a.x - b.x;

            var dz = a.z - b.z;

            return Mathf.Sqrt(dx * dx + dz * dz);

        }

    }

}



