using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Capisoft.Lib.BaUnifiedUI.Localization;

namespace VoogleRoute.UI
{
    /// <summary>Reads VoogleRoute/Locales/*.json so UI works even when Localizor API mismatches.</summary>
    internal static class ModLocaleLookup
    {
        private static Dictionary<string, string> _strings;
        private static string _loadedLocale;

        internal static void EnsureLoaded()
        {
            var locale = BaUiText.ResolveLoadedLocale();
            if (_strings != null && locale == _loadedLocale)
                return;

            _loadedLocale = locale;
            _strings = new Dictionary<string, string>(System.StringComparer.Ordinal);
            MergeFile("en");
            if (!string.Equals(locale, "en", System.StringComparison.OrdinalIgnoreCase))
                MergeFile(locale);
        }

        internal static void Invalidate() => _loadedLocale = null;

        internal static bool TryGet(string key, out string value)
        {
            EnsureLoaded();
            if (_strings == null || string.IsNullOrWhiteSpace(key))
            {
                value = null;
                return false;
            }

            return _strings.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value);
        }

        private static void MergeFile(string locale)
        {
            var path = ModStoragePaths.PathInModRoot(Path.Combine("Locales", locale + ".json"));
            if (!File.Exists(path))
                return;

            try
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path));
                if (parsed == null)
                    return;

                foreach (var pair in parsed)
                    _strings[pair.Key] = pair.Value;
            }
            catch
            {
                // Keep partial table from other locale files.
            }
        }
    }
}
