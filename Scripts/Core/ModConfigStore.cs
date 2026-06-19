using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;
using VoogleRoute.Navigation;

namespace VoogleRoute
{
    /// <summary>Dev-only settings in config.json (not per-save, not ESC mod options).</summary>
    internal static class ModConfigStore
    {
        private const string LegacyLineColorFileName = "line_color.txt";

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static ModConfigData _data = CreateDefaultData();
        private static bool _configFileFound;

        internal static bool ConfigFileFound => _configFileFound;

        /// <summary>Legacy file payload (migration source for per-save mod options and bookmarks).</summary>
        internal static ModConfigData Data => _data;

        internal static string ConfigFilePath =>
            ModStoragePaths.FileInModRoot(ModStoragePaths.ConfigFileName);

        internal static void Load()
        {
            _data = CreateDefaultData();
            _configFileFound = false;

            try
            {
                var path = ConfigFilePath;
                if (File.Exists(path))
                {
                    _configFileFound = true;
                    var json = File.ReadAllText(path);
                    var loaded = JsonSerializer.Deserialize<ModConfigData>(json, JsonOptions);
                    if (loaded != null)
                        _data = loaded;

                    if (!json.Contains("\"use_subway\"", StringComparison.Ordinal))
                        _data.UseSubway = true;

                    if (!json.Contains("\"auto_enter_destination\"", StringComparison.Ordinal))
                        _data.AutoEnterDestination = true;
                }

                EnsureDefaults(_data);
                MigrateLegacyFiles();
                SaveDevOnly();
            }
            catch (Exception ex)
            {
                _data = CreateDefaultData();
                EnsureDefaults(_data);
                Debug.LogWarning("[VoogleRoute] Failed to read config.json: " + ex.Message);
            }
        }

        internal static void SaveDevOnly()
        {
            try
            {
                var devOnly = new ModConfigData
                {
                    Logging = _data.Logging,
                    LogLevel = _data.LogLevel,
                    ShowLineDetection = _data.ShowLineDetection
                };

                var path = ConfigFilePath;
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                var json = JsonSerializer.Serialize(devOnly, JsonOptions);
                File.WriteAllText(path, json);
                _configFileFound = true;
            }
            catch
            {
                // Non-fatal if disk write fails.
            }
        }

        internal static void StripBookmarkDataAndSave()
        {
            _data.Bookmarks = null;
            _data.QuickBookmarks = null;
            SaveDevOnly();
        }

        private static ModConfigData CreateDefaultData()
        {
            return new ModConfigData
            {
                Logging = false,
                LogLevel = "error",
                ShowLineDetection = false,
                RouteLineColor = DefaultRouteLineColor(),
                FootRouteLineColor = DefaultRouteLineColor(),
                VehicleRouteLineColor = DefaultRouteLineColor(),
                IndoorRouteLineColor = DefaultRouteLineColor(),
                DisplayOutside = true,
                DisplayInside = true,
                IndoorRoute = true,
                IndoorAutowalk = false,
                UseSubway = true,
                ForceCorrectSideArrival = false,
                AllowUturnAtStart = false,
                AutoEnterDestination = true,
                BaseTaxiMultiplier = 2
            };
        }

        private static void EnsureDefaults(ModConfigData data)
        {
            if (data == null)
                return;

            if (string.IsNullOrWhiteSpace(data.LogLevel))
                data.LogLevel = "error";

            if (data.RouteLineColor == null || data.RouteLineColor.Length < 4)
                data.RouteLineColor = DefaultRouteLineColor();

            if (data.FootRouteLineColor == null || data.FootRouteLineColor.Length < 4)
                data.FootRouteLineColor = (float[])data.RouteLineColor.Clone();

            if (data.VehicleRouteLineColor == null || data.VehicleRouteLineColor.Length < 4)
                data.VehicleRouteLineColor = (float[])data.RouteLineColor.Clone();

            if (data.IndoorRouteLineColor == null || data.IndoorRouteLineColor.Length < 4)
                data.IndoorRouteLineColor = (float[])data.FootRouteLineColor.Clone();

            if (data.BaseTaxiMultiplier < 1)
                data.BaseTaxiMultiplier = 2;

            if (data.Bookmarks == null)
                data.Bookmarks = new List<BookmarkConfigEntry>();

            if (data.QuickBookmarks == null)
                data.QuickBookmarks = new QuickBookmarksConfig();
        }

        private static float[] DefaultRouteLineColor() =>
            new[]
            {
                ModConfig.RouteNeonBlueR,
                ModConfig.RouteNeonBlueG,
                ModConfig.RouteNeonBlueB,
                ModConfig.RouteNeonBlueA
            };

        private static bool MigrateLegacyFiles()
        {
            return TryMigrateLineColor();
        }

        private static bool TryMigrateLineColor()
        {
            var path = ModStoragePaths.PathInModRoot(LegacyLineColorFileName);
            if (!File.Exists(path))
                return false;

            try
            {
                var parts = File.ReadAllText(path).Split(',');
                if (parts.Length < 4)
                    return false;

                _data.RouteLineColor = new[]
                {
                    float.Parse(parts[0], CultureInfo.InvariantCulture),
                    float.Parse(parts[1], CultureInfo.InvariantCulture),
                    float.Parse(parts[2], CultureInfo.InvariantCulture),
                    float.Parse(parts[3], CultureInfo.InvariantCulture)
                };
                _data.FootRouteLineColor = (float[])_data.RouteLineColor.Clone();
                _data.VehicleRouteLineColor = (float[])_data.RouteLineColor.Clone();

                TryDeleteLegacyFile(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void TryDeleteLegacyFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Non-fatal if another process still holds the file.
            }
        }
    }

    internal sealed class ModConfigData
    {
        [JsonPropertyName("logging")]
        public bool Logging { get; set; }

        [JsonPropertyName("log_level")]
        public string LogLevel { get; set; } = "error";

        [JsonPropertyName("show_line_detection")]
        public bool ShowLineDetection { get; set; }

        [JsonPropertyName("route_line_color")]
        public float[] RouteLineColor { get; set; }

        [JsonPropertyName("foot_route_line_color")]
        public float[] FootRouteLineColor { get; set; }

        [JsonPropertyName("vehicle_route_line_color")]
        public float[] VehicleRouteLineColor { get; set; }

        [JsonPropertyName("indoor_route_line_color")]
        public float[] IndoorRouteLineColor { get; set; }

        [JsonPropertyName("display_outside")]
        public bool DisplayOutside { get; set; } = true;

        [JsonPropertyName("display_inside")]
        public bool DisplayInside { get; set; } = true;

        [JsonPropertyName("indoor_route")]
        public bool IndoorRoute { get; set; } = true;

        [JsonPropertyName("indoor_autowalk")]
        public bool IndoorAutowalk { get; set; }

        [JsonPropertyName("use_subway")]
        public bool UseSubway { get; set; } = true;

        [JsonPropertyName("base_taxi_multiplier")]
        public int BaseTaxiMultiplier { get; set; } = 2;

        [JsonPropertyName("force_correct_side_arrival")]
        public bool ForceCorrectSideArrival { get; set; }

        [JsonPropertyName("allow_uturn_at_start")]
        public bool AllowUturnAtStart { get; set; }

        [JsonPropertyName("auto_enter_destination")]
        public bool AutoEnterDestination { get; set; } = true;

        [JsonPropertyName("bookmarks")]
        public List<BookmarkConfigEntry> Bookmarks { get; set; }

        [JsonPropertyName("quick_bookmarks")]
        public QuickBookmarksConfig QuickBookmarks { get; set; }
    }
}
