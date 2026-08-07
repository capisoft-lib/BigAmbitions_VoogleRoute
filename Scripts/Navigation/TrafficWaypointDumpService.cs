using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using GleyTrafficSystem;
using GleyUrbanAssets;
using UnityEngine;

namespace VoogleRoute.Navigation
{
    /// <summary>
    /// One-shot BA 1.0 beta export of the raw Gley road graph used by the offline
    /// enhanced-route generator. Remove the Tick call once the refreshed graph is baked.
    /// </summary>
    internal static class TrafficWaypointDumpService
    {
        private const float RetryIntervalSeconds = 2f;
        private static bool _completed;
        private static float _nextAttemptTime;

        internal static void Tick()
        {
            if (_completed || !GameState.IsWorldReady() || Time.unscaledTime < _nextAttemptTime)
                return;

            _nextAttemptTime = Time.unscaledTime + RetryIntervalSeconds;

            try
            {
                var scene = CurrentSceneData.GetSceneInstance();
                var waypoints = scene?.allWaypoints;
                if (waypoints == null || waypoints.Length == 0)
                    return;

                var dumpPath = WriteDump(waypoints);
                _completed = true;
                Debug.Log(
                    "[VoogleRoute] ROAD_WAYPOINT_DUMP_COMPLETE" +
                    " count=" + waypoints.Length.ToString(CultureInfo.InvariantCulture) +
                    " path=" + dumpPath);
            }
            catch (Exception ex)
            {
                Debug.LogError("[VoogleRoute] ROAD_WAYPOINT_DUMP_FAILED " + ex);
            }
        }

        internal static void Reset()
        {
            _completed = false;
            _nextAttemptTime = 0f;
        }

        private static string WriteDump(Waypoint[] waypoints)
        {
            var dumpDirectory = ModStoragePaths.PathInModRoot("WaypointDumps");
            Directory.CreateDirectory(dumpDirectory);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            var finalPath = System.IO.Path.Combine(dumpDirectory, "waypoints_all_" + timestamp + ".csv");
            var temporaryPath = finalPath + ".tmp";

            var ordered = new List<Waypoint>(waypoints.Length);
            for (var i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                    ordered.Add(waypoints[i]);
            }

            ordered.Sort((left, right) => left.listIndex.CompareTo(right.listIndex));

            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.WriteLine("listIndex,name,posX,posY,posZ,neighbors,disabled");
                for (var i = 0; i < ordered.Count; i++)
                    WriteWaypoint(writer, ordered[i]);
            }

            File.Move(temporaryPath, finalPath);
            return finalPath;
        }

        private static void WriteWaypoint(StreamWriter writer, Waypoint waypoint)
        {
            writer.Write(waypoint.listIndex.ToString(CultureInfo.InvariantCulture));
            writer.Write(',');
            writer.Write(EscapeCsv(waypoint.name));
            writer.Write(',');
            writer.Write(waypoint.position.x.ToString("0.000", CultureInfo.InvariantCulture));
            writer.Write(',');
            writer.Write(waypoint.position.y.ToString("0.000", CultureInfo.InvariantCulture));
            writer.Write(',');
            writer.Write(waypoint.position.z.ToString("0.000", CultureInfo.InvariantCulture));
            writer.Write(',');
            writer.Write(EscapeCsv(JoinIndices(waypoint.neighbors)));
            writer.Write(',');
            writer.WriteLine(waypoint.temporaryDisabled ? "1" : "0");
        }

        private static string JoinIndices(List<int> indices)
        {
            if (indices == null || indices.Count == 0)
                return string.Empty;

            var builder = new StringBuilder(indices.Count * 6);
            for (var i = 0; i < indices.Count; i++)
            {
                if (i > 0)
                    builder.Append(';');
                builder.Append(indices[i].ToString(CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
                return value;

            return '"' + value.Replace("\"", "\"\"") + '"';
        }
    }
}
