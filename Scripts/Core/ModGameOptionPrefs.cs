using UnityEngine;

namespace VoogleRoute
{
    /// <summary>
    /// Same PlayerPrefs keys as Big Ambitions <c>ModOptionsToggleControl</c> (m:{modId}:{optionId}).
    /// Used to mirror per-save modData into the ESC mod-options UI.
    /// </summary>
    internal static class ModGameOptionPrefs
    {
        internal static bool LoadToggle(string modId, string optionId, bool defaultValue)
        {
            var key = BuildKey(modId, optionId);
            if (key == null)
                return defaultValue;

            return UnityEngine.PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) != 0;
        }

        internal static int LoadInt(string modId, string optionId, int defaultValue)
        {
            var key = BuildKey(modId, optionId);
            if (key == null)
                return defaultValue;

            return UnityEngine.PlayerPrefs.GetInt(key, defaultValue);
        }

        internal static void SaveToggle(string modId, string optionId, bool value)
        {
            var key = BuildKey(modId, optionId);
            if (key == null)
                return;

            UnityEngine.PlayerPrefs.SetInt(key, value ? 1 : 0);
        }

        internal static void SaveInt(string modId, string optionId, int value)
        {
            var key = BuildKey(modId, optionId);
            if (key == null)
                return;

            UnityEngine.PlayerPrefs.SetInt(key, value);
        }

        internal static void SaveColor(string modId, string optionId, Color color)
        {
            var key = BuildKey(modId, optionId);
            if (key == null)
                return;

            var bytes = (Color32)color;
            UnityEngine.PlayerPrefs.SetString(
                key,
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "#{0:X2}{1:X2}{2:X2}{3:X2}",
                    bytes.r,
                    bytes.g,
                    bytes.b,
                    bytes.a));
        }

        internal static bool HasKey(string modId, string optionId)
        {
            var key = BuildKey(modId, optionId);
            return key != null && UnityEngine.PlayerPrefs.HasKey(key);
        }

        private static string BuildKey(string modId, string optionId)
        {
            if (string.IsNullOrEmpty(modId) || string.IsNullOrEmpty(optionId))
                return null;

            return "m:" + modId + ":" + optionId;
        }
    }
}
