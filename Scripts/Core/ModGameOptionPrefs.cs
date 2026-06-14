using UnityEngine;

namespace VoogleRoute
{
    /// <summary>
    /// Same PlayerPrefs keys as Big Ambitions <c>ModOptionsToggleControl</c> (m:{modId}:{optionId}).
    /// The game only invokes option callbacks when ESC → Options → Mod is opened; routing must read prefs at init.
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

        internal static void SaveToggle(string modId, string optionId, bool value)
        {
            var key = BuildKey(modId, optionId);
            if (key == null)
                return;

            UnityEngine.PlayerPrefs.SetInt(key, value ? 1 : 0);
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
