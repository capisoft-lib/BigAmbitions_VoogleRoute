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
                }

                EnsureDefaults(_data);

                if (MigrateLegacyFiles())
                    Save();
            }
            catch (Exception ex)
            {
                _data = CreateDefaultData();
                EnsureDefaults(_data);
                Debug.LogWarning("[VoogleRoute] Failed to read config.json: " + ex.Message);
            }
        }

        internal static void Save()
        {
            try
            {
                EnsureDefaults(_data);
                _data.Bookmarks = null;
                _data.QuickBookmarks = null;
                var path = ConfigFilePath;
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                var json = JsonSerializer.Serialize(_data, JsonOptions);
                File.WriteAllText(path, json);
                _configFileFound = true;
            }
            catch
            {
                // Non-fatal if disk write fails.
            }
        }

        internal static void SetFootRouteLineColor(Color color)
        {
            _data.FootRouteLineColor = ColorToArray(color);
            Save();
        }

        internal static void SetVehicleRouteLineColor(Color color)
        {
            _data.VehicleRouteLineColor = ColorToArray(color);
            Save();
        }

        private static float[] ColorToArray(Color color) =>
            new[] { color.r, color.g, color.b, color.a };

        internal static void SetIndoorRouteLineEnabled(bool value)
        {
            _data.IndoorRoute = value;
            Save();
        }

        internal static void SetIndoorAutoWalkEnabled(bool value)
        {
            _data.IndoorAutowalk = value;
            Save();
        }

        internal static void SetBaseTaxiMultiplier(int value)
        {
            _data.BaseTaxiMultiplier = Mathf.Clamp(value, 1, 10);
            Save();
        }

        internal static void StripBookmarkDataAndSave()
        {
            _data.Bookmarks = null;
            _data.QuickBookmarks = null;
            Save();
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
                IndoorRoute = true,
                IndoorAutowalk = false,
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

        [JsonPropertyName("indoor_route")]
        public bool IndoorRoute { get; set; } = true;

        [JsonPropertyName("indoor_autowalk")]
        public bool IndoorAutowalk { get; set; }

        [JsonPropertyName("base_taxi_multiplier")]
        public int BaseTaxiMultiplier { get; set; } = 2;

        [JsonPropertyName("bookmarks")]
        public List<BookmarkConfigEntry> Bookmarks { get; set; }

        [JsonPropertyName("quick_bookmarks")]
        public QuickBookmarksConfig QuickBookmarks { get; set; }
    }
}
