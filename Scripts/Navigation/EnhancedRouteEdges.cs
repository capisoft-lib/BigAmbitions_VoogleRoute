using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using VoogleRoute.Pathfinding.Geometry;
using VoogleRoute.Pathfinding.Routing;

namespace VoogleRoute.Navigation
{
    internal readonly struct EnhancedRouteEdge
    {
        internal readonly int From;
        internal readonly int To;
        internal readonly Vector3 Control;
        internal readonly float ArcLengthMeters;
        internal readonly float AbsAngleDegrees;

        internal EnhancedRouteEdge(int from, int to, Vector3 control, float arcLengthMeters, float absAngleDegrees)
        {
            From = from;
            To = to;
            Control = control;
            ArcLengthMeters = arcLengthMeters;
            AbsAngleDegrees = absAngleDegrees;
        }
    }

    internal static class EnhancedRouteEdges
    {
        internal static List<EnhancedRouteEdge> LoadSyntheticTurns(int waypointCount, HashSet<long> authorizedUturnEdges = null)
        {
            var edges = new List<EnhancedRouteEdge>(256);
            var path = ModStoragePaths.PathInModRoot(ModStoragePaths.EnhancedRoutesCsv);
            if (!File.Exists(path))
                return edges;

            try
            {
                using (var reader = new StreamReader(path))
                {
                    var header = reader.ReadLine();
                    if (string.IsNullOrEmpty(header))
                        return edges;

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var cols = line.Split(',');
                        if (cols.Length < 23)
                            continue;
                        if (cols[1] != "synthetic_turn" ||
                            (cols[2] != "left" && cols[2] != "uturn" && cols[2] != "straight"))
                            continue;
                        if (!TryParseInt(cols[3], out var from) || !TryParseInt(cols[10], out var to))
                            continue;
                        if (from < 0 || to < 0 || from >= waypointCount || to >= waypointCount)
                            continue;
                        if (!TryParseFloat(cols[7], out var fx) || !TryParseFloat(cols[9], out var fz) ||
                            !TryParseFloat(cols[14], out var tx) || !TryParseFloat(cols[16], out var tz))
                            continue;
                        if (!TryParseFloat(cols[17], out var cx) ||
                            !TryParseFloat(cols[18], out var cy) ||
                            !TryParseFloat(cols[19], out var cz))
                            continue;

                        var fromV = new Vec3(fx, 0f, fz);
                        var toV = new Vec3(tx, 0f, tz);
                        var control = new Vector3(cx, cy, cz);
                        var arcLength = cols.Length > 23 &&
                                        TryParseFloat(cols[23], out var csvArc) &&
                                        csvArc > 0f
                            ? csvArc
                            : ManeuverGeometry.SyntheticTurnTravelMeters(
                                fromV,
                                toV,
                                new Vec3(cx, cy, cz));
                        var absAngle = TryParseFloat(cols[20], out var angleDegrees)
                            ? Mathf.Abs(angleDegrees)
                            : 0f;

                        edges.Add(new EnhancedRouteEdge(from, to, control, arcLength, absAngle));
                        if (cols[2] == "uturn" && authorizedUturnEdges != null)
                            authorizedUturnEdges.Add(EdgeKey(from, to));
                    }
                }
            }
            catch
            {
                // Missing or invalid CSV: graph keeps Gley forward edges only.
            }

            return edges;
        }

        internal static long EdgeKey(int from, int to) => ((long)from << 32) ^ (uint)to;

        private static bool TryParseInt(string value, out int result) =>
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

        private static bool TryParseFloat(string value, out float result) =>
            float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }
}
