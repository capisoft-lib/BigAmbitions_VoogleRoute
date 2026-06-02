using System.Reflection;
using UnityEngine;
using VoogleRoute.Navigation;

namespace VoogleRoute.Localization;

/// <summary>Strings for the active game locale (same codes as <c>LocalizorManager.LoadedLocale</c>).</summary>
internal static class ModLocalization
{
    private static Func<string>? _getLoadedLocale;
    private static string _activeLocale = "en";
    private static float _nextLocalePoll;
    private static bool _resolverReady;

    internal static event Action? LanguageChanged;

    internal static void EnsureInitialized() => EnsureLocaleResolver();

    internal static void PollLanguageChange()
    {
        EnsureLocaleResolver();
        if (Time.unscaledTime < _nextLocalePoll)
            return;
        _nextLocalePoll = Time.unscaledTime + 0.5f;

        var locale = ResolveLocale();
        if (locale == _activeLocale)
            return;

        _activeLocale = locale;
        LanguageChanged?.Invoke();
    }

    internal static string Get(StringKey key) =>
        ModTranslations.TryGet(_activeLocale, key, out var text) ? text : ModTranslations.GetEnglish(key);

    internal static string Meters(int meters) => string.Format(Get(StringKey.MetersFormat), meters);

    internal static string DescribeTurn(TurnKind kind) => kind switch
    {
        TurnKind.Straight => Get(StringKey.TurnStraight),
        TurnKind.SlightLeft => Get(StringKey.TurnSlightLeft),
        TurnKind.Left => Get(StringKey.TurnLeft),
        TurnKind.SharpLeft => Get(StringKey.TurnSharpLeft),
        TurnKind.SlightRight => Get(StringKey.TurnSlightRight),
        TurnKind.Right => Get(StringKey.TurnRight),
        TurnKind.SharpRight => Get(StringKey.TurnSharpRight),
        TurnKind.UTurn => Get(StringKey.TurnUTurn),
        TurnKind.Arrival => Get(StringKey.TurnArrival),
        _ => Get(StringKey.TurnFollowRoute),
    };

    private static void EnsureLocaleResolver()
    {
        if (_resolverReady)
            return;
        _resolverReady = true;

        try
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type? managerType = null;
                try
                {
                    managerType = asm.GetType("Localizor.LocalizorManager");
                }
                catch
                {
                    // ignore broken assemblies
                }

                if (managerType == null)
                    continue;

                var prop = managerType.GetProperty("LoadedLocale",
                    BindingFlags.Public | BindingFlags.Static);
                if (prop == null)
                    continue;

                _getLoadedLocale = () =>
                {
                    try
                    {
                        return prop.GetValue(null) as string ?? "en";
                    }
                    catch
                    {
                        return "en";
                    }
                };
                break;
            }
        }
        catch
        {
            // ignore
        }

        _getLoadedLocale ??= static () => "en";
        _activeLocale = ResolveLocale();
    }

    private static string ResolveLocale()
    {
        try
        {
            var locale = _getLoadedLocale?.Invoke();
            if (!string.IsNullOrWhiteSpace(locale))
                return NormalizeLocale(locale);
        }
        catch
        {
            // Localizor not ready yet
        }

        return "en";
    }

    private static string NormalizeLocale(string locale) => locale.Trim().Replace('_', '-');
}
