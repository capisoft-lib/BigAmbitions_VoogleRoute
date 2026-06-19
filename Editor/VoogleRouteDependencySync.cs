#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VoogleRoute.Editor
{
    /// <summary>
    /// Keeps bundled LIB_BaUnifiedUI player DLL in sync for Unity Mod Builder packaging.
    /// Uses a distinct filename so Unity does not confuse it with LIB_BaUnifiedUI.asmdef.
    /// </summary>
    [InitializeOnLoad]
    public static class VoogleRouteDependencySync
    {
        internal const string BundledUiDllFileName = "LIB_BaUnifiedUI.PlayerMode.dll";
        private const string LegacyBundledUiDllFileName = "LIB_BaUnifiedUI.dll";
        private const string DestAssetPath = "Assets/Mods/VoogleRoute/Dependencies/" + BundledUiDllFileName;

        static VoogleRouteDependencySync()
        {
            EditorApplication.delayCall += OnDelayCall;
        }

        private static void OnDelayCall() => TrySyncFromOutput();

        [MenuItem("Big Ambitions/Mods/Voogle Route/Sync bundled dependencies")]
        public static void SyncFromMenu()
        {
            if (TrySyncFromOutput(forceLog: true))
                AssetDatabase.Refresh();
            else
                Debug.LogWarning(
                    "[VoogleRoute] LIB_BaUnifiedUI.dll not found. Build LIB_BaUnifiedUI in Mod Builder first, " +
                    "or run tools/sync-dependencies.ps1.");
        }

        private static bool TrySyncFromOutput(bool forceLog = false)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
            var candidates = new[]
            {
                Path.Combine(projectRoot, "Output", "LIB_BaUnifiedUI", "LIB_BaUnifiedUI.dll"),
                Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                    "AppData", "LocalLow", "Hovgaard Games", "Big Ambitions", "ModsLocal",
                    "LIB_BaUnifiedUI", "LIB_BaUnifiedUI.dll")
            };

            string source = null;
            foreach (var path in candidates)
            {
                if (File.Exists(path))
                {
                    source = path;
                    break;
                }
            }

            if (source == null)
                return false;

            var destDir = Path.Combine(projectRoot, "Assets", "Mods", "VoogleRoute", "Dependencies");
            Directory.CreateDirectory(destDir);

            RemoveLegacyBundledDll(destDir);

            var destAbsolute = Path.Combine(destDir, BundledUiDllFileName);
            if (File.Exists(destAbsolute))
            {
                var srcTime = File.GetLastWriteTimeUtc(source);
                var dstTime = File.GetLastWriteTimeUtc(destAbsolute);
                if (srcTime <= dstTime)
                    return true;
            }

            File.Copy(source, destAbsolute, overwrite: true);
            AssetDatabase.ImportAsset(DestAssetPath, ImportAssetOptions.ForceUpdate);

            if (forceLog)
                Debug.Log("[VoogleRoute] Synced " + BundledUiDllFileName + " into Dependencies for Mod Builder.");

            return true;
        }

        private static void RemoveLegacyBundledDll(string destDir)
        {
            var legacyDll = Path.Combine(destDir, LegacyBundledUiDllFileName);
            if (!File.Exists(legacyDll))
                return;

            File.Delete(legacyDll);
            var legacyMeta = legacyDll + ".meta";
            if (File.Exists(legacyMeta))
                File.Delete(legacyMeta);
        }
    }
}
#endif
