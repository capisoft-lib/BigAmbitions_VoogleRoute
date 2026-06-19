using System;
using System.Collections.Generic;
using System.IO;
using BAModAPI;
using UnityEngine;

namespace VoogleRoute
{
    /// <summary>Per-save mod options (ESC → Options → Mods + route colors).</summary>
    internal static class ModOptionsSaveStore
    {
        private const string ModDataKey = "VoogleRoute.modOptions.v1";

        private static string _boundSaveId;
        private static bool _migratedGlobalConfig;
        private static bool _loading;

        internal static ModOptionsSaveData Current { get; private set; } = ModOptionsSaveData.CreateDefault();

        internal static void Initialize()
        {
            _migratedGlobalConfig = false;
            ReloadForCurrentSave(force: true);
        }

        internal static void ReloadForCurrentSave(bool force = false)
        {
            var saveId = ResolveSaveId();
            if (!force && saveId == _boundSaveId)
                return;

            _boundSaveId = saveId;
            _migratedGlobalConfig = false;

            _loading = true;
            try
            {
                Current = LoadForCurrentSave();
                ModConfig.ApplyFromSaveData(Current);
                ModConfig.SyncPlayerPrefsForOptionsMenu();
            }
            finally
            {
                _loading = false;
            }

            VoogleRouteOptionsScheduler.RequestRefresh();
        }

        internal static void PersistFromModConfig()
        {
            if (_loading)
                return;

            if (string.IsNullOrEmpty(_boundSaveId))
            {
                _boundSaveId = ResolveSaveId();
                if (string.IsNullOrEmpty(_boundSaveId))
                    return;
            }

            Current = ModConfig.CaptureSaveData();
            WriteToModData(Current);
        }

        private static ModOptionsSaveData LoadForCurrentSave()
        {
            if (TryLoadFromModData(out var data))
                return data;

            if (TryMigrateFromGlobalConfig(out data))
            {
                WriteToModData(data);
                return data;
            }

            return ModOptionsSaveData.CreateDefault();
        }

        private static bool TryLoadFromModData(out ModOptionsSaveData data)
        {
            data = null;
            var save = SaveGameManager.Current;
            if (save?.modData == null ||
                !save.modData.TryGetValue(ModDataKey, out var json) ||
                string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                data = ModOptionsJsonCodec.Deserialize(json);
                return data != null;
            }
            catch (Exception ex)
            {
                ModLog.Error("[WARN] Failed to read mod options from save modData: " + ex.Message);
                return false;
            }
        }

        private static bool TryMigrateFromGlobalConfig(out ModOptionsSaveData data)
        {
            data = null;
            if (_migratedGlobalConfig)
                return false;

            _migratedGlobalConfig = true;

            try
            {
                var legacy = ModConfigStore.Data;
                if (legacy == null)
                    return false;

                data = ModOptionsSaveData.FromLegacyConfig(legacy);
                ModLog.Info("Migrating mod options from config.json into save modData.");
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Error("[WARN] Failed to migrate mod options from config.json: " + ex.Message);
                return false;
            }
        }

        private static void WriteToModData(ModOptionsSaveData data)
        {
            var save = SaveGameManager.Current;
            if (save == null || string.IsNullOrEmpty(_boundSaveId))
                return;

            try
            {
                ModOptionsSaveData.EnsureDefaults(data);
                save.modData ??= new Dictionary<string, string>();
                save.modData[ModDataKey] = ModOptionsJsonCodec.Serialize(data);
            }
            catch (Exception ex)
            {
                ModLog.Error("[WARN] Failed to write mod options to save modData: " + ex.Message);
            }
        }

        internal static void Shutdown()
        {
            _boundSaveId = null;
            _migratedGlobalConfig = false;
            _loading = false;
            Current = ModOptionsSaveData.CreateDefault();
        }

        private static string ResolveSaveId()
        {
            try
            {
                var save = SaveGameManager.Current;
                if (save == null)
                    return null;

                var characterId = save.characterId;
                var saveName = save.SaveGameName;
                if (string.IsNullOrWhiteSpace(characterId) && string.IsNullOrWhiteSpace(saveName))
                    return null;

                return Sanitize(characterId ?? "character") + "__" + Sanitize(saveName ?? "save");
            }
            catch
            {
                return null;
            }
        }

        private static string Sanitize(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');
            return value.Trim();
        }
    }

    internal sealed class ModOptionsSaveData
    {
        public bool DisplayOutside { get; set; } = true;
        public bool DisplayInside { get; set; } = true;
        public bool RouteLine { get; set; } = true;
        public bool AutoWalk { get; set; }
        public bool IndoorRoute { get; set; } = true;
        public bool IndoorAutowalk { get; set; }
        public bool UseSubway { get; set; } = true;
        public int BaseTaxiMultiplier { get; set; } = 2;
        public bool ForceCorrectSideArrival { get; set; }
        public bool AllowUturnAtStart { get; set; }
        public bool AutoEnterDestination { get; set; } = true;
        public float[] FootRouteLineColor { get; set; }
        public float[] VehicleRouteLineColor { get; set; }
        public float[] IndoorRouteLineColor { get; set; }

        internal static ModOptionsSaveData CreateDefault()
        {
            var defaults = new ModOptionsSaveData
            {
                FootRouteLineColor = DefaultRouteLineColor(),
                VehicleRouteLineColor = DefaultRouteLineColor(),
                IndoorRouteLineColor = DefaultRouteLineColor()
            };
            EnsureDefaults(defaults);
            return defaults;
        }

        internal static ModOptionsSaveData FromLegacyConfig(ModConfigData legacy)
        {
            var data = new ModOptionsSaveData
            {
                DisplayOutside = legacy.DisplayOutside,
                DisplayInside = legacy.DisplayInside,
                IndoorRoute = legacy.IndoorRoute,
                IndoorAutowalk = legacy.IndoorAutowalk,
                UseSubway = legacy.UseSubway,
                BaseTaxiMultiplier = legacy.BaseTaxiMultiplier,
                ForceCorrectSideArrival = legacy.ForceCorrectSideArrival,
                AllowUturnAtStart = legacy.AllowUturnAtStart,
                AutoEnterDestination = legacy.AutoEnterDestination,
                FootRouteLineColor = CloneColor(legacy.FootRouteLineColor, legacy.RouteLineColor),
                VehicleRouteLineColor = CloneColor(legacy.VehicleRouteLineColor, legacy.RouteLineColor),
                IndoorRouteLineColor = CloneColor(legacy.IndoorRouteLineColor, legacy.FootRouteLineColor, legacy.RouteLineColor)
            };
            EnsureDefaults(data);
            return data;
        }

        internal static void EnsureDefaults(ModOptionsSaveData data)
        {
            if (data == null)
                return;

            if (data.FootRouteLineColor == null || data.FootRouteLineColor.Length < 4)
                data.FootRouteLineColor = DefaultRouteLineColor();

            if (data.VehicleRouteLineColor == null || data.VehicleRouteLineColor.Length < 4)
                data.VehicleRouteLineColor = DefaultRouteLineColor();

            if (data.IndoorRouteLineColor == null || data.IndoorRouteLineColor.Length < 4)
                data.IndoorRouteLineColor = DefaultRouteLineColor();

            if (data.BaseTaxiMultiplier < 1)
                data.BaseTaxiMultiplier = 2;
        }

        private static float[] DefaultRouteLineColor() =>
            new[]
            {
                ModConfig.RouteNeonBlueR,
                ModConfig.RouteNeonBlueG,
                ModConfig.RouteNeonBlueB,
                ModConfig.RouteNeonBlueA
            };

        private static float[] CloneColor(float[] primary, float[] fallback)
        {
            if (primary != null && primary.Length >= 4)
                return (float[])primary.Clone();

            if (fallback != null && fallback.Length >= 4)
                return (float[])fallback.Clone();

            return DefaultRouteLineColor();
        }

        private static float[] CloneColor(float[] primary, float[] fallback, float[] legacy)
        {
            if (primary != null && primary.Length >= 4)
                return (float[])primary.Clone();

            if (fallback != null && fallback.Length >= 4)
                return (float[])fallback.Clone();

            if (legacy != null && legacy.Length >= 4)
                return (float[])legacy.Clone();

            return DefaultRouteLineColor();
        }
    }
}
