using System;
using BAModAPI;
using BigAmbitions.Mods;
using UnityEngine;
using VoogleRoute.Navigation;
using VoogleRoute.UI;

namespace VoogleRoute
{
    internal static class ModConfig
    {
        internal const float RouteNeonBlueR = 0f;
        internal const float RouteNeonBlueG = 180f / 255f;
        internal const float RouteNeonBlueB = 1f;
        internal const float RouteNeonBlueA = 0.92f;

        private const string DisplayOutsideKey = "display_outside";
        private const string DisplayInsideKey = "display_inside";
        private const string RouteLineKey = "route_line";
        private const string AutoWalkKey = "auto_walk";
        private const string IndoorRouteLineKey = "indoor_route";
        private const string IndoorAutoWalkKey = "indoor_autowalk";
        private const string UseSubwayKey = "use_subway";
        private const string BaseTaxiMultiplierKey = "base_taxi_multiplier";
        private const string ForceCorrectSideArrivalKey = "force_correct_side_arrival";
        private const string AllowUturnAtStartKey = "allow_uturn_at_start";
        private const string AutoEnterDestinationKey = "auto_enter_destination";

        private static ModContext _context;

        internal static bool DisplayOutsideEnabled { get; private set; } = true;
        internal static bool DisplayInsideEnabled { get; private set; } = true;

        internal static bool RouteLineEnabled { get; private set; } = true;
        internal static bool AutoWalkEnabled { get; private set; }
        internal static bool IndoorRouteLineEnabled { get; private set; } = true;
        internal static bool IndoorAutoWalkEnabled { get; private set; }
        internal static bool UseSubwayEnabled { get; private set; } = true;
        internal static bool ForceCorrectSideArrivalEnabled { get; private set; }
        internal static bool AllowUturnAtStartEnabled { get; private set; }
        internal static bool AutoEnterDestinationEnabled { get; private set; } = true;

        internal static float FootLineWidth { get; private set; } = 0.3f;
        internal static float IndoorFootLineWidth { get; private set; } = 0.12f;
        internal static float FootGroundOffset { get; private set; } = 0.40f;
        internal static float VehicleLineWidth { get; private set; } = 0.22f;
        internal static float VehicleGroundOffset { get; private set; } = 0.40f;
        internal static float RecalcIntervalSeconds { get; private set; } = 0.4f;
        internal static float VehicleRecalcIntervalSeconds { get; private set; } = 10f;
        internal static bool ShowPartialPaths { get; private set; }
        internal static bool ShowLineDetection { get; private set; }
        internal static float HudButtonScale { get; private set; } = 1f;
        internal static float NavHudOffsetY { get; private set; } = 16f;

        internal static bool WantsRouteComputation => RouteLineEnabled || AutoWalkEnabled;
        internal static bool IndoorWantsRouteComputation => IndoorRouteLineEnabled || IndoorAutoWalkEnabled;

        private static Color _footLineColor = new Color(RouteNeonBlueR, RouteNeonBlueG, RouteNeonBlueB, RouteNeonBlueA);
        private static Color _vehicleLineColor = new Color(RouteNeonBlueR, RouteNeonBlueG, RouteNeonBlueB, RouteNeonBlueA);
        private static Color _indoorFootLineColor = new Color(RouteNeonBlueR, RouteNeonBlueG, RouteNeonBlueB, RouteNeonBlueA);

        internal static Color FootLineColor => _footLineColor;
        internal static Color VehicleLineColor => _vehicleLineColor;
        internal static Color IndoorFootLineColor => _indoorFootLineColor;
        internal static bool LoggingEnabled { get; private set; }
        internal static ModLogLevel LogLevel { get; private set; } = ModLogLevel.Error;
        internal static int BaseTaxiMultiplier { get; private set; } = 2;

        internal static void Initialize(ModContext context)
        {
            _context = context;
            ModStoragePaths.Initialize(context);
            ModConfigStore.Load();
            VoogleRouteOptionsScheduler.EnsureRunning();
            ModOptionsSaveStore.Initialize();
            BookmarkFileStore.Load(ModConfigStore.Data);
            BookmarkDataSaveStore.Initialize();
            ApplyDevConfig();
            ModLog.Initialize(context);

            ModLog.Info(
                "Config initialized | mod_root=" + ModStoragePaths.ModRootDirectory +
                " config=" + ModConfigStore.ConfigFilePath +
                " bookmarks=" + BookmarkFileStore.FilePath +
                " found=" + ModConfigStore.ConfigFileFound +
                " logging=" + LoggingEnabled +
                " log_level=" + LogLevel.ToString().ToLowerInvariant() +
                " route_line=" + RouteLineEnabled +
                " auto_walk=" + AutoWalkEnabled +
                " indoor_route=" + IndoorRouteLineEnabled +
                " indoor_autowalk=" + IndoorAutoWalkEnabled +
                " use_subway=" + UseSubwayEnabled +
                " force_correct_side=" + ForceCorrectSideArrivalEnabled +
                " allow_uturn_at_start=" + AllowUturnAtStartEnabled +
                " auto_enter_destination=" + AutoEnterDestinationEnabled);

            EnsureOptionsRegistered();
            ModLog.Info("Mod options registered (per-save via modData).");
        }

        internal static void Shutdown()
        {
            ModLog.Shutdown();
            if (_context != null)
                OptionsService.RemoveModOptions(_context.ModId);
            VoogleRouteOptionsScheduler.Shutdown();
            ModOptionsSaveStore.Shutdown();
            _context = null;
            ModStoragePaths.Shutdown();
        }

        internal static void RefreshOptionsRegistered() => EnsureOptionsRegistered();

        internal static void ApplyFromSaveData(ModOptionsSaveData data)
        {
            if (data == null)
                return;

            ModOptionsSaveData.EnsureDefaults(data);
            DisplayOutsideEnabled = data.DisplayOutside;
            DisplayInsideEnabled = data.DisplayInside;
            RouteLineEnabled = data.RouteLine;
            AutoWalkEnabled = data.AutoWalk;
            IndoorRouteLineEnabled = data.IndoorRoute;
            IndoorAutoWalkEnabled = false;
            UseSubwayEnabled = data.UseSubway;
            ForceCorrectSideArrivalEnabled = data.ForceCorrectSideArrival;
            AllowUturnAtStartEnabled = data.AllowUturnAtStart;
            AutoEnterDestinationEnabled = data.AutoEnterDestination;
            BaseTaxiMultiplier = Mathf.Clamp(data.BaseTaxiMultiplier, 1, 10);
            _footLineColor = ReadLineColor(data.FootRouteLineColor);
            _vehicleLineColor = ReadLineColor(data.VehicleRouteLineColor);
            _indoorFootLineColor = ReadLineColor(data.IndoorRouteLineColor);
            Rendering.RouteLineRenderer.ApplyStyle();
        }

        internal static ModOptionsSaveData CaptureSaveData() =>
            new ModOptionsSaveData
            {
                DisplayOutside = DisplayOutsideEnabled,
                DisplayInside = DisplayInsideEnabled,
                RouteLine = RouteLineEnabled,
                AutoWalk = AutoWalkEnabled,
                IndoorRoute = IndoorRouteLineEnabled,
                IndoorAutowalk = false,
                UseSubway = UseSubwayEnabled,
                BaseTaxiMultiplier = BaseTaxiMultiplier,
                ForceCorrectSideArrival = ForceCorrectSideArrivalEnabled,
                AllowUturnAtStart = AllowUturnAtStartEnabled,
                AutoEnterDestination = AutoEnterDestinationEnabled,
                FootRouteLineColor = ColorToArray(_footLineColor),
                VehicleRouteLineColor = ColorToArray(_vehicleLineColor),
                IndoorRouteLineColor = ColorToArray(_indoorFootLineColor)
            };

        internal static void SyncPlayerPrefsForOptionsMenu()
        {
            if (_context == null)
                return;

            var modId = _context.ModId;
            ModGameOptionPrefs.SaveToggle(modId, DisplayOutsideKey, DisplayOutsideEnabled);
            ModGameOptionPrefs.SaveToggle(modId, DisplayInsideKey, DisplayInsideEnabled);
            ModGameOptionPrefs.SaveToggle(modId, RouteLineKey, RouteLineEnabled);
            ModGameOptionPrefs.SaveToggle(modId, AutoWalkKey, AutoWalkEnabled);
            ModGameOptionPrefs.SaveToggle(modId, IndoorRouteLineKey, IndoorRouteLineEnabled);
            ModGameOptionPrefs.SaveToggle(modId, IndoorAutoWalkKey, IndoorAutoWalkEnabled);
            ModGameOptionPrefs.SaveToggle(modId, UseSubwayKey, UseSubwayEnabled);
            ModGameOptionPrefs.SaveToggle(modId, ForceCorrectSideArrivalKey, ForceCorrectSideArrivalEnabled);
            ModGameOptionPrefs.SaveToggle(modId, AllowUturnAtStartKey, AllowUturnAtStartEnabled);
            ModGameOptionPrefs.SaveToggle(modId, AutoEnterDestinationKey, AutoEnterDestinationEnabled);
            ModGameOptionPrefs.SaveInt(modId, BaseTaxiMultiplierKey, BaseTaxiMultiplier);
        }

        internal static void SetDisplayOutsideEnabled(bool value, bool persist = true)
        {
            if (DisplayOutsideEnabled == value)
                return;

            DisplayOutsideEnabled = value;
            if (persist)
                ModOptionsSaveStore.PersistFromModConfig();
            ModLog.Info("Display VoogleRoute outside = " + value);
            if (!value)
            {
                VisitHistoryPanel.Close();
                RouteSettingsUi.Close();
                AutoDriveConfirmPopup.Close();
                Rendering.CityMapRouteLineRenderer.Hide();
                Rendering.RouteLineRenderer.Hide();
            }

            RouteActionPanel.UpdateVisibility();
        }

        internal static void SetDisplayInsideEnabled(bool value, bool persist = true)
        {
            if (DisplayInsideEnabled == value)
                return;

            DisplayInsideEnabled = value;
            if (persist)
                ModOptionsSaveStore.PersistFromModConfig();
            ModLog.Info("Display VoogleRoute inside = " + value);
            if (!value)
            {
                IndoorNavigationService.Reset();
                Rendering.RouteLineRenderer.Hide();
            }

            RouteActionPanel.UpdateVisibility();
        }

        internal static void SetRouteLineEnabled(bool value, bool persist = true)
        {
            if (RouteLineEnabled == value)
                return;

            RouteLineEnabled = value;
            if (persist)
                ModOptionsSaveStore.PersistFromModConfig();
            ModLog.Info("Route line = " + value);
            RouteActionPanel.RefreshVisual();
        }

        internal static void SetAutoWalkEnabled(bool value, bool persist = true)
        {
            if (AutoWalkEnabled == value)
                return;

            AutoWalkEnabled = value;
            if (persist)
                ModOptionsSaveStore.PersistFromModConfig();
            ModLog.Info("Auto-walk = " + value);
            RouteActionPanel.RefreshVisual();
        }

        internal static void SetIndoorRouteLineEnabled(bool value, bool persist = true)
        {
            if (IndoorRouteLineEnabled == value)
                return;

            IndoorRouteLineEnabled = value;
            if (persist)
                ModOptionsSaveStore.PersistFromModConfig();
            ModLog.Info("Indoor route line = " + value + (persist ? "" : " (session)"));
            RouteActionPanel.RefreshVisual();
        }

        internal static void SetIndoorAutoWalkEnabled(bool value, bool persist = true)
        {
            if (IndoorAutoWalkEnabled == value)
                return;

            IndoorAutoWalkEnabled = value;
            if (persist)
                ModOptionsSaveStore.PersistFromModConfig();
            ModLog.Info("Indoor auto-walk = " + value + (persist ? "" : " (session)"));
            RouteActionPanel.RefreshVisual();
        }

        internal static void SetUseSubwayEnabled(bool value, bool persist = true)
        {
            if (UseSubwayEnabled == value)
                return;

            UseSubwayEnabled = value;
            if (persist)
                ModOptionsSaveStore.PersistFromModConfig();
            ModLog.Info("Use subway = " + value);
            if (!value)
                AutoWalkService.ResetSubwayState();
            VoogleRouteLoop.RequestRouteRecalc("use_subway_changed");
        }

        internal static void SetAutoEnterDestinationEnabled(bool value, bool persist = true)
        {
            if (AutoEnterDestinationEnabled == value)
                return;

            AutoEnterDestinationEnabled = value;
            if (persist)
                ModOptionsSaveStore.PersistFromModConfig();
            ModLog.Info("Auto-enter at destination = " + value);
        }

        internal static void SetForceCorrectSideArrivalEnabled(bool value, bool persist = true)
        {
            if (ForceCorrectSideArrivalEnabled == value)
                return;

            ForceCorrectSideArrivalEnabled = value;
            if (persist)
                ModOptionsSaveStore.PersistFromModConfig();
            ModLog.Info("Force correct street-side arrival = " + value);
            VoogleRouteLoop.RequestRouteRecalc("force_correct_side_arrival_changed");
        }

        internal static void SetAllowUturnAtStartEnabled(bool value, bool persist = true)
        {
            if (AllowUturnAtStartEnabled == value)
                return;

            AllowUturnAtStartEnabled = value;
            if (persist)
                ModOptionsSaveStore.PersistFromModConfig();
            ModLog.Info("Allow U-turn at route start = " + value);
            VoogleRouteLoop.RequestRouteRecalc("allow_uturn_at_start_changed");
        }

        internal static void SetBaseTaxiMultiplier(int value, bool persist = true)
        {
            var clamped = Mathf.Clamp(value, 1, 10);
            if (BaseTaxiMultiplier == clamped)
                return;

            BaseTaxiMultiplier = clamped;
            if (persist)
                ModOptionsSaveStore.PersistFromModConfig();
            ModLog.Info("Base taxi multiplier = " + clamped);
        }

        internal static void SetFootLineColor(Color color, bool persist = true)
        {
            _footLineColor = NormalizeLineColor(color);
            if (persist)
                ModOptionsSaveStore.PersistFromModConfig();
            Rendering.RouteLineRenderer.ApplyStyle();
            ModLog.Info("Foot route line color updated.");
        }

        internal static void SetVehicleLineColor(Color color, bool persist = true)
        {
            _vehicleLineColor = NormalizeLineColor(color);
            if (persist)
                ModOptionsSaveStore.PersistFromModConfig();
            Rendering.RouteLineRenderer.ApplyStyle();
            ModLog.Info("Vehicle route line color updated.");
        }

        internal static void SetIndoorFootLineColor(Color color, bool persist = true)
        {
            _indoorFootLineColor = NormalizeLineColor(color);
            if (persist)
                ModOptionsSaveStore.PersistFromModConfig();
            Rendering.RouteLineRenderer.ApplyStyle();
            ModLog.Info("Indoor route line color updated.");
        }

        private static void OnDisplayOutsideOptionChanged(bool value) => SetDisplayOutsideEnabled(value);

        private static void OnDisplayInsideOptionChanged(bool value) => SetDisplayInsideEnabled(value);

        private static void OnRouteLineOptionChanged(bool value) => SetRouteLineEnabled(value);

        private static void OnAutoWalkOptionChanged(bool value) => SetAutoWalkEnabled(value);

        private static void OnIndoorRouteLineOptionChanged(bool value) => SetIndoorRouteLineEnabled(value);

        private static void OnIndoorAutoWalkOptionChanged(bool value) => SetIndoorAutoWalkEnabled(value);

        private static void OnUseSubwayOptionChanged(bool value) => SetUseSubwayEnabled(value);

        private static void OnBaseTaxiMultiplierChanged(int value) => SetBaseTaxiMultiplier(value);

        private static void OnForceCorrectSideArrivalOptionChanged(bool value) =>
            SetForceCorrectSideArrivalEnabled(value);

        private static void OnAllowUturnAtStartOptionChanged(bool value) =>
            SetAllowUturnAtStartEnabled(value);

        private static void OnAutoEnterDestinationOptionChanged(bool value) =>
            SetAutoEnterDestinationEnabled(value);

        private static void EnsureOptionsRegistered()
        {
            if (_context == null)
                return;

            OptionsService.RemoveModOptions(_context.ModId);

            var options = new ModOptions()
                .AddHeader("voogle_route_panel_title")
                .AddToggle(DisplayOutsideKey, "voogle_route_options_display_outside", DisplayOutsideEnabled,
                    OnDisplayOutsideOptionChanged)
                .AddSlider(BaseTaxiMultiplierKey, "voogle_route_options_base_taxi_multiplier", 1, 10,
                    BaseTaxiMultiplier, OnBaseTaxiMultiplierChanged,
                    "voogle_route_options_base_taxi_multiplier_value")
                .AddToggle(ForceCorrectSideArrivalKey, "voogle_route_options_force_correct_side_arrival",
                    ForceCorrectSideArrivalEnabled, OnForceCorrectSideArrivalOptionChanged)
                .AddToggle(AllowUturnAtStartKey, "voogle_route_options_allow_uturn_at_start",
                    AllowUturnAtStartEnabled, OnAllowUturnAtStartOptionChanged)
                .AddToggle(RouteLineKey, "voogle_route_options_route", RouteLineEnabled, OnRouteLineOptionChanged)
                .AddToggle(AutoWalkKey, "voogle_route_options_autowalk", AutoWalkEnabled, OnAutoWalkOptionChanged)
                .AddToggle(AutoEnterDestinationKey, "voogle_route_options_auto_enter_destination",
                    AutoEnterDestinationEnabled, OnAutoEnterDestinationOptionChanged)
                .AddToggle(UseSubwayKey, "voogle_route_options_use_subway", UseSubwayEnabled, OnUseSubwayOptionChanged)
                .AddSplitter()
                .AddToggle(DisplayInsideKey, "voogle_route_options_display_inside", DisplayInsideEnabled,
                    OnDisplayInsideOptionChanged)
                .AddToggle(IndoorRouteLineKey, "voogle_route_options_indoor_route", IndoorRouteLineEnabled,
                    OnIndoorRouteLineOptionChanged)
                .AddToggle(IndoorAutoWalkKey, "voogle_route_options_indoor_autowalk", IndoorAutoWalkEnabled,
                    OnIndoorAutoWalkOptionChanged);

            OptionsService.Register(_context.ModId, options);
        }

        private static void ApplyDevConfig()
        {
            LoggingEnabled = false;
            LogLevel = ModLogLevel.Error;

            try
            {
                var data = ModConfigStore.Data;
                LoggingEnabled = data.Logging;
                LogLevel = ModLog.ParseLevel(data.LogLevel);
                ShowLineDetection = data.ShowLineDetection;
            }
            catch (Exception ex)
            {
                LoggingEnabled = false;
                LogLevel = ModLogLevel.Error;
                Debug.LogWarning("[VoogleRoute] Failed to apply dev config: " + ex.Message);
            }

            ModLog.Configure(LoggingEnabled, LogLevel);
        }

        private static Color NormalizeLineColor(Color color) =>
            new Color(color.r, color.g, color.b, Mathf.Clamp01(color.a <= 0f ? RouteNeonBlueA : color.a));

        private static Color ReadLineColor(float[] components)
        {
            if (components == null || components.Length < 4)
                return new Color(RouteNeonBlueR, RouteNeonBlueG, RouteNeonBlueB, RouteNeonBlueA);

            return new Color(components[0], components[1], components[2], components[3]);
        }

        private static float[] ColorToArray(Color color) =>
            new[] { color.r, color.g, color.b, color.a };
    }
}
