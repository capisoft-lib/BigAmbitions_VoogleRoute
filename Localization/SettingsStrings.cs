using System.Collections.Generic;

namespace VoogleRoute.Localization;

internal static class SettingsStrings
{
    private static readonly Dictionary<StringKey, string> English = new()
    {
        [StringKey.SettingsTitle] = "Voogle Route Settings",
        [StringKey.SettingsButton] = "Settings",
        [StringKey.SettingRouteLineColor] = "Navigation line color",
        [StringKey.SettingCheckForUpdates] = "Check for updates on startup",
        [StringKey.SettingAutoDownloadUpdates] = "Download updates automatically",
        [StringKey.SettingPromptInstallUpdate] = "Prompt to install downloaded updates",
        [StringKey.SettingShowTurnGuidance] = "Turn guidance HUD (in vehicle)",
        [StringKey.SettingShowIntersectionArrows] = "Intersection arrows on route",
        [StringKey.SettingShowFullRouteLine] = "Full route line to destination",
        [StringKey.SettingCheckNow] = "Check for updates now",
        [StringKey.SettingClose] = "Close",
        [StringKey.SettingOn] = "ON",
        [StringKey.SettingOff] = "OFF",
        [StringKey.ColorPresetNeonBlue] = "Neon blue",
        [StringKey.ColorPresetGreen] = "Green",
        [StringKey.ColorPresetOrange] = "Orange",
        [StringKey.ColorPresetMagenta] = "Magenta",
        [StringKey.ColorPresetWhite] = "White",
    };

    private static readonly Dictionary<StringKey, string> French = new()
    {
        [StringKey.SettingsTitle] = "Paramètres Voogle Route",
        [StringKey.SettingsButton] = "Réglages",
        [StringKey.SettingRouteLineColor] = "Couleur des traits de navigation",
        [StringKey.SettingCheckForUpdates] = "Vérifier les mises à jour au démarrage",
        [StringKey.SettingAutoDownloadUpdates] = "Télécharger les mises à jour automatiquement",
        [StringKey.SettingPromptInstallUpdate] = "Proposer d'installer les mises à jour téléchargées",
        [StringKey.SettingShowTurnGuidance] = "HUD de virage (en véhicule)",
        [StringKey.SettingShowIntersectionArrows] = "Flèches aux intersections",
        [StringKey.SettingShowFullRouteLine] = "Ligne complète jusqu'à la destination",
        [StringKey.SettingCheckNow] = "Vérifier les mises à jour",
        [StringKey.SettingClose] = "Fermer",
        [StringKey.SettingOn] = "OUI",
        [StringKey.SettingOff] = "NON",
        [StringKey.ColorPresetNeonBlue] = "Bleu néon",
        [StringKey.ColorPresetGreen] = "Vert",
        [StringKey.ColorPresetOrange] = "Orange",
        [StringKey.ColorPresetMagenta] = "Magenta",
        [StringKey.ColorPresetWhite] = "Blanc",
    };

    internal static void MergeInto(Dictionary<StringKey, string> table, string locale)
    {
        var source = locale.StartsWith("fr", StringComparison.OrdinalIgnoreCase) ? French : English;
        foreach (var kv in source)
            table[kv.Key] = kv.Value;
    }
}
