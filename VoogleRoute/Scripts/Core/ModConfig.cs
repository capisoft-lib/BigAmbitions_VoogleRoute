using System;
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
        internal static float FootGroundOffset { get; private set; } = 0.40f;
        internal static float VehicleLineWidth { get; private set; } = 0.22f;
        internal static float VehicleGroundOffset { get; private set; } = 0.40f;
        internal static float RecalcIntervalSeconds { get; private set; } = 0.75f;
        internal static float VehicleRecalcIntervalSeconds { get; private set; } = 10f;
        internal static bool ShowPartialPaths { get; private set; }
        internal static bool ShowLineDetection { get; private set; }
        internal static float HudButtonScale { get; private set; } = 1f;
        internal static float NavHudOffsetY { get; private set; } = 16f;
    
        internal static bool WantsRouteComputation => RouteLineEnabled || AutoWalkEnabled;

        private static Color _lineColor = new Color(RouteNeonBlueR, RouteNeonBlueG, RouteNeonBlueB, RouteNeonBlueA);

        internal static Color LineColor => _lineColor;
        internal static bool LoggingEnabled { get; private set; }
        internal static ModLogLevel LogLevel { get; private set; } = ModLogLevel.Error;

        internal static void Initialize(ModContext context)
        {
            _context = context;
            ModConfigStore.Load();
            ApplyFromStore();
            ModLog.Initialize(context);

            ModLog.Info(
                "Config initialized | path=" + ModConfigStore.ConfigFilePath +
                " found=" + ModConfigStore.ConfigFileFound +
                " logging=" + LoggingEnabled +
                " log_level=" + LogLevel.ToString().ToLowerInvariant() +
                " route_line=" + RouteLineEnabled +
                " auto_walk=" + AutoWalkEnabled);

            var options = new ModOptions()
                .AddHeader("voogle_route_panel_title")
                .AddToggle(RouteLineKey, "voogle_route_options_route", RouteLineEnabled, OnRouteLineOptionChanged)
                .AddToggle(AutoWalkKey, "voogle_route_options_autowalk", AutoWalkEnabled, OnAutoWalkOptionChanged);

            OptionsService.Register(context.ModId, options);
            ModLog.Info("Mod options registered (route line, auto-walk).");
        }
    
        internal static void Shutdown()
        {
            ModLog.Shutdown();
            if (_context != null)
                OptionsService.RemoveModOptions(_context.ModId);
            _context = null;
        }
    
        internal static void SetRouteLineEnabled(bool value)
        {
            RouteLineEnabled = value;
            ModLog.Info("Route line = " + value);
        }
    
        internal static void SetAutoWalkEnabled(bool value)
        {
            AutoWalkEnabled = value;
            ModLog.Info("Auto-walk = " + value);
        }
    
        private static void OnRouteLineOptionChanged(bool value) => SetRouteLineEnabled(value);
    
        private static void OnAutoWalkOptionChanged(bool value) => SetAutoWalkEnabled(value);

        internal static void SetRouteLineColor(Color color)
        {
            _lineColor = new Color(color.r, color.g, color.b, Mathf.Clamp01(color.a <= 0f ? RouteNeonBlueA : color.a));
            ModConfigStore.SetRouteLineColor(_lineColor);
            Rendering.RouteLineRenderer.ApplyStyle();
            ModLog.Info("Route line color updated.");
        }

        private static void ApplyFromStore()
        {
            LoggingEnabled = false;
            LogLevel = ModLogLevel.Error;

            try
            {
                var data = ModConfigStore.Data;
                LoggingEnabled = data.Logging;
                LogLevel = ModLog.ParseLevel(data.LogLevel);
                ShowLineDetection = data.ShowLineDetection;
                _lineColor = ReadLineColor(data.RouteLineColor);
            }
            catch (Exception ex)
            {
                LoggingEnabled = false;
                LogLevel = ModLogLevel.Error;
                Debug.LogWarning("[VoogleRoute] Failed to apply config.json: " + ex.Message);
            }

            ModLog.Configure(LoggingEnabled, LogLevel);
        }

        private static Color ReadLineColor(float[] components)
        {
            if (components == null || components.Length < 4)
                return new Color(RouteNeonBlueR, RouteNeonBlueG, RouteNeonBlueB, RouteNeonBlueA);

            return new Color(components[0], components[1], components[2], components[3]);
        }
    }
}
