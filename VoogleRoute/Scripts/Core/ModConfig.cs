using System.Globalization;
using System.IO;
using BAModAPI;
using BigAmbitions.Mods;
using UnityEngine;

namespace VoogleRoute
{
    
    internal static class ModConfig
    {
        internal const float RouteNeonBlueR = 0f;
        internal const float RouteNeonBlueG = 180f / 255f;
        internal const float RouteNeonBlueB = 1f;
        internal const float RouteNeonBlueA = 0.92f;
    
        private const string RouteLineKey = "route_line";
        private const string AutoWalkKey = "auto_walk";
    
        private static ModContext _context;
    
        internal static bool RouteLineEnabled { get; private set; } = true;
        internal static bool AutoWalkEnabled { get; private set; }
    
        internal static float FootLineWidth { get; private set; } = 0.3f;
        internal static float FootGroundOffset { get; private set; } = 0.35f;
        internal static float VehicleLineWidth { get; private set; } = 0.22f;
        internal static float VehicleGroundOffset { get; private set; } = 0.28f;
        internal static float RecalcIntervalSeconds { get; private set; } = 0.75f;
        internal static float VehicleRecalcIntervalSeconds { get; private set; } = 10f;
        internal static bool ShowPartialPaths { get; private set; }
        internal static float HudButtonScale { get; private set; } = 1f;
        internal static float NavHudOffsetY { get; private set; } = 16f;
    
        internal static bool WantsRouteComputation => RouteLineEnabled || AutoWalkEnabled;

        private const string LineColorFileName = "line_color.txt";

        private static Color _lineColor = new Color(RouteNeonBlueR, RouteNeonBlueG, RouteNeonBlueB, RouteNeonBlueA);

        internal static Color LineColor => _lineColor;

        internal static void Initialize(ModContext context)
        {
            _context = context;
            LoadLineColor();
    
            var options = new ModOptions()
                .AddHeader("voogle_route_panel_title")
                .AddToggle(RouteLineKey, "voogle_route_options_route", RouteLineEnabled, OnRouteLineOptionChanged)
                .AddToggle(AutoWalkKey, "voogle_route_options_autowalk", AutoWalkEnabled, OnAutoWalkOptionChanged);
    
            OptionsService.Register(context.ModId, options);
        }
    
        internal static void Shutdown()
        {
            if (_context != null)
                OptionsService.RemoveModOptions(_context.ModId);
            _context = null;
        }
    
        internal static void SetRouteLineEnabled(bool value)
        {
            RouteLineEnabled = value;
            if (_context != null)
                _context.Logger.Info("Route line = " + value);
        }
    
        internal static void SetAutoWalkEnabled(bool value)
        {
            AutoWalkEnabled = value;
            if (_context != null)
                _context.Logger.Info("Auto-walk = " + value);
        }
    
        private static void OnRouteLineOptionChanged(bool value) => SetRouteLineEnabled(value);
    
        private static void OnAutoWalkOptionChanged(bool value) => SetAutoWalkEnabled(value);

        internal static void SetRouteLineColor(Color color)
        {
            _lineColor = new Color(color.r, color.g, color.b, Mathf.Clamp01(color.a <= 0f ? RouteNeonBlueA : color.a));
            SaveLineColor();
            Rendering.RouteLineRenderer.ApplyStyle();
            if (_context != null)
                _context.Logger.Info("Route line color updated.");
        }

        private static string LineColorFilePath =>
            ModStoragePaths.FileInModRoot(LineColorFileName);

        private static void LoadLineColor()
        {
            try
            {
                var path = LineColorFilePath;
                if (!File.Exists(path))
                    return;

                var parts = File.ReadAllText(path).Split(',');
                if (parts.Length < 4)
                    return;

                _lineColor = new Color(
                    float.Parse(parts[0], CultureInfo.InvariantCulture),
                    float.Parse(parts[1], CultureInfo.InvariantCulture),
                    float.Parse(parts[2], CultureInfo.InvariantCulture),
                    float.Parse(parts[3], CultureInfo.InvariantCulture));
            }
            catch
            {
                // Keep default neon blue.
            }
        }

        private static void SaveLineColor()
        {
            try
            {
                var text = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3}",
                    _lineColor.r,
                    _lineColor.g,
                    _lineColor.b,
                    _lineColor.a);
                File.WriteAllText(LineColorFilePath, text);
            }
            catch
            {
                // Non-fatal if disk write fails.
            }
        }
    }
}
