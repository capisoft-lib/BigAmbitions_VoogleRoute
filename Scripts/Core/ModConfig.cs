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
        private const string IndoorRouteLineKey = "indoor_route";
        private const string IndoorAutoWalkKey = "indoor_autowalk";
    
        private static ModContext _context;
    
        internal static bool RouteLineEnabled { get; private set; } = true;
        internal static bool AutoWalkEnabled { get; private set; }
        internal static bool IndoorRouteLineEnabled { get; private set; } = true;
        internal static bool IndoorAutoWalkEnabled { get; private set; }
    
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
        internal static bool IndoorWantsRouteComputation => IndoorRouteLineEnabled || IndoorAutoWalkEnabled;

        private static Color _lineColor = new Color(RouteNeonBlueR, RouteNeonBlueG, RouteNeonBlueB, RouteNeonBlueA);

        internal static Color LineColor => _lineColor;
        internal static bool LoggingEnabled { get; private set; }
        internal static ModLogLevel LogLevel { get; private set; } = ModLogLevel.Error;

        internal static void Initialize(ModContext context)
        {
            _context = context;
            ModStoragePaths.Initialize(context);
            ModConfigStore.Load();
            ApplyFromStore();
            ModLog.Initialize(context);

            ModLog.Info(
                "Config initialized | mod_root=" + ModStoragePaths.ModRootDirectory +
                " config=" + ModConfigStore.ConfigFilePath +
                " found=" + ModConfigStore.ConfigFileFound +
                " logging=" + LoggingEnabled +
                " log_level=" + LogLevel.ToString().ToLowerInvariant() +
                " route_line=" + RouteLineEnabled +
                " auto_walk=" + AutoWalkEnabled +
                " indoor_route=" + IndoorRouteLineEnabled +
                " indoor_autowalk=" + IndoorAutoWalkEnabled);

            var options = new ModOptions()
                .AddHeader("voogle_route_panel_title")
                .AddToggle(RouteLineKey, "voogle_route_options_route", RouteLineEnabled, OnRouteLineOptionChanged)
                .AddToggle(AutoWalkKey, "voogle_route_options_autowalk", AutoWalkEnabled, OnAutoWalkOptionChanged)
                .AddToggle(IndoorRouteLineKey, "voogle_route_options_indoor_route", IndoorRouteLineEnabled, OnIndoorRouteLineOptionChanged)
                .AddToggle(IndoorAutoWalkKey, "voogle_route_options_indoor_autowalk", IndoorAutoWalkEnabled, OnIndoorAutoWalkOptionChanged);

            OptionsService.Register(context.ModId, options);
            ModLog.Info("Mod options registered (route line, auto-walk).");
        }
    
        internal static void Shutdown()
        {
            ModLog.Shutdown();
            if (_context != null)
                OptionsService.RemoveModOptions(_context.ModId);
            _context = null;
            ModStoragePaths.Shutdown();
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

        internal static void SetIndoorRouteLineEnabled(bool value)
        {
            IndoorRouteLineEnabled = value;
            ModConfigStore.SetIndoorRouteLineEnabled(value);
            ModLog.Info("Indoor route line = " + value);
        }

        internal static void SetIndoorAutoWalkEnabled(bool value)
        {
            IndoorAutoWalkEnabled = value;
            ModConfigStore.SetIndoorAutoWalkEnabled(value);
            ModLog.Info("Indoor auto-walk = " + value);
        }
    
        private static void OnRouteLineOptionChanged(bool value) => SetRouteLineEnabled(value);
    
        private static void OnAutoWalkOptionChanged(bool value) => SetAutoWalkEnabled(value);

        private static void OnIndoorRouteLineOptionChanged(bool value) => SetIndoorRouteLineEnabled(value);

        private static void OnIndoorAutoWalkOptionChanged(bool value) => SetIndoorAutoWalkEnabled(value);

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
                IndoorRouteLineEnabled = data.IndoorRoute;
                IndoorAutoWalkEnabled = data.IndoorAutowalk;
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
