using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace VoogleRoute.Navigation.Driving
{
    internal enum DrivingStage { Update, Navigation, PhoneUi, Fixed, Projection, Sensor, Plan, Search, Curve, TrafficBinding, Start, Count }

    /// <summary>Allocation-free probes; one aggregate line every five seconds, regardless of optional mod logging.</summary>
    internal static class DrivingPerformance
    {
        private static readonly long[] Ticks = new long[(int)DrivingStage.Count];
        private static readonly long[] Maximum = new long[(int)DrivingStage.Count];
        private static readonly int[] Calls = new int[(int)DrivingStage.Count];
        private static readonly int[] PeakFrame = new int[(int)DrivingStage.Count];
        private static readonly string[] Names = { "update", "navigation", "phone_ui", "fixed", "projection", "sensor", "plan", "search", "curve", "traffic_bind", "start" };
        private static long _windowStart, _lastFrame, _frameTicks, _worstFrame;
        private static int _lastFrameId, _worstFrameId, _frames, _activeFrames, _slow33, _slow100;
        private static int _boxCasts, _overlaps, _componentLookups, _projectionSegments, _reusedPlans;

        internal static long Begin() => Stopwatch.GetTimestamp();
        internal static void End(DrivingStage stage, long started)
        {
            var ticks = Stopwatch.GetTimestamp() - started;
            var i = (int)stage;
            Ticks[i] += ticks; Calls[i]++;
            if (ticks > Maximum[i]) { Maximum[i] = ticks; PeakFrame[i] = Time.frameCount; }
        }
        internal static void Frame()
        {
            var now = Stopwatch.GetTimestamp();
            if (_windowStart == 0) _windowStart = now;
            if (_lastFrame != 0)
            {
                var elapsed = now - _lastFrame;
                _frameTicks += elapsed; _frames++;
                if (PhysicalDriveService.IsActive) _activeFrames++;
                if (elapsed * 1000d / Stopwatch.Frequency > 33.3d) _slow33++;
                if (elapsed * 1000d / Stopwatch.Frequency > 100d) _slow100++;
                if (elapsed > _worstFrame) { _worstFrame = elapsed; _worstFrameId = _lastFrameId; }
            }
            _lastFrame = now; _lastFrameId = Time.frameCount;
        }
        internal static void SensorWork(int boxes, int overlaps, int lookups)
        { _boxCasts += boxes; _overlaps += overlaps; _componentLookups += lookups; }
        internal static void ProjectionWork(int count) => _projectionSegments += count;
        internal static void ReusedPlan() => _reusedPlans++;

        internal static void Flush(bool force = false)
        {
            var now = Stopwatch.GetTimestamp();
            if (_windowStart == 0 || (!force && now - _windowStart < 5L * Stopwatch.Frequency)) return;
            var text = new StringBuilder(1024);
            text.Append("[VoogleRoute][Perf] window_s=").Append(Seconds(now - _windowStart));
            text.Append(" frame=").Append(Time.frameCount).Append(" frames=").Append(_frames).Append(" active_frames=").Append(_activeFrames);
            text.Append(" frame_wall_ms_avg=").Append(Ms(_frameTicks, _frames));
            text.Append(" max=").Append(Ms(_worstFrame, 1)).Append(" peak_frame=").Append(_worstFrameId);
            text.Append(" over33=").Append(_slow33).Append(" over100=").Append(_slow100);
            for (var i = 0; i < Calls.Length; i++)
            {
                if (Calls[i] == 0) continue;
                text.Append(" | ").Append(Names[i]).Append("_ms_avg=").Append(Ms(Ticks[i], Calls[i]));
                text.Append(" max=").Append(Ms(Maximum[i], 1)).Append(" n=").Append(Calls[i]).Append(" peak_frame=").Append(PeakFrame[i]);
            }
            text.Append(" | boxes=").Append(_boxCasts).Append(" overlaps=").Append(_overlaps);
            text.Append(" component_lookups=").Append(_componentLookups).Append(" projection_segments=").Append(_projectionSegments);
            text.Append(" reused_plans=").Append(_reusedPlans);
            Debug.Log(text.ToString());
            Array.Clear(Ticks, 0, Ticks.Length); Array.Clear(Maximum, 0, Maximum.Length);
            Array.Clear(Calls, 0, Calls.Length); Array.Clear(PeakFrame, 0, PeakFrame.Length);
            _frameTicks = _worstFrame = 0;
            _frames = _activeFrames = _slow33 = _slow100 = _worstFrameId = 0;
            _boxCasts = _overlaps = _componentLookups = _projectionSegments = _reusedPlans = 0;
            _windowStart = now;
            if (force) { _lastFrame = 0; _windowStart = 0; }
        }
        internal static void Event(string message) => Debug.Log("[VoogleRoute][PhysicalDrive] " + message +
            " frame=" + Time.frameCount + " realtime_s=" + Time.realtimeSinceStartup.ToString("F3", CultureInfo.InvariantCulture));
        private static string Ms(long ticks, int count) => (ticks * 1000d / Stopwatch.Frequency / Math.Max(1, count)).ToString("F3", CultureInfo.InvariantCulture);
        private static string Seconds(long ticks) => (ticks / (double)Stopwatch.Frequency).ToString("F3", CultureInfo.InvariantCulture);
    }
}
