using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace VoogleRoute.Navigation
{
    internal readonly struct EnhancedRouteEdge
    {
        internal readonly int From;
        internal readonly int To;
        internal readonly Vector3 Control;

        internal EnhancedRouteEdge(int from, int to, Vector3 control)
        {
            From = from;
            To = to;
            Control = control;
        }
    }

    internal static class EnhancedRouteEdges
    {
        private const string RelativePath = "Data/big_ambitions_enhanced_routes.csv";

        internal static List<EnhancedRouteEdge> LoadSyntheticTurns(int waypointCount, HashSet<long> authorizedUturnEdges = null)
        {
            var edges = new List<EnhancedRouteEdge>(256);
            var path = Path.Combine(ModStoragePaths.ModRootDirectory, RelativePath);
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
                        if (cols[1] != "synthetic_turn" || (cols[2] != "left" && cols[2] != "uturn"))
                            continue;
                        if (!TryParseInt(cols[3], out var from) || !TryParseInt(cols[10], out var to))
                            continue;
                        if (from < 0 || to < 0 || from >= waypointCount || to >= waypointCount)
                            continue;
                        if (!TryParseFloat(cols[17], out var cx) ||
                            !TryParseFloat(cols[18], out var cy) ||
                            !TryParseFloat(cols[19], out var cz))
                            continue;

                        edges.Add(new EnhancedRouteEdge(from, to, new Vector3(cx, cy, cz)));
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
