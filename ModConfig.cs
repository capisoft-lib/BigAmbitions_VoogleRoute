using MelonLoader;

namespace VoogleRoute;

internal static class ModConfig
{
    /// <summary>Nexus header route glow (#00B4FF).</summary>
    internal const float RouteNeonBlueR = 0f;
    internal const float RouteNeonBlueG = 180f / 255f;
    internal const float RouteNeonBlueB = 1f;
    internal const float RouteNeonBlueA = 0.92f;

    internal static MelonPreferences_Category Category = null!;

    internal static MelonPreferences_Entry<bool> RouteLineEnabled = null!;
    internal static MelonPreferences_Entry<float> LineWidth = null!;
    internal static MelonPreferences_Entry<float> FootLineWidth = null!;
    internal static MelonPreferences_Entry<float> VehicleLineWidth = null!;
    internal static MelonPreferences_Entry<float> LineColorR = null!;
    internal static MelonPreferences_Entry<float> LineColorG = null!;
    internal static MelonPreferences_Entry<float> LineColorB = null!;
    internal static MelonPreferences_Entry<float> LineColorA = null!;
    internal static MelonPreferences_Entry<float> GroundOffset = null!;
    internal static MelonPreferences_Entry<float> RecalcIntervalSeconds = null!;
    internal static MelonPreferences_Entry<bool> ShowPartialPaths = null!;
    internal static MelonPreferences_Entry<float> HudButtonScale = null!;
    internal static MelonPreferences_Entry<float> NavHudOffsetY = null!;
    internal static MelonPreferences_Entry<bool> AutoWalkEnabled = null!;
    internal static MelonPreferences_Entry<bool> ShowDebugOverlay = null!;
    internal static MelonPreferences_Entry<float> VehicleRecalcIntervalSeconds = null!;
    internal static MelonPreferences_Entry<bool> ShowTurnGuidance = null!;
    internal static MelonPreferences_Entry<bool> ShowIntersectionArrows = null!;
    internal static MelonPreferences_Entry<bool> ShowFullRouteLine = null!;
    internal static MelonPreferences_Entry<float> MinTurnAngleDegrees = null!;
    internal static MelonPreferences_Entry<int> MaxIntersectionMarkers = null!;
    internal static MelonPreferences_Entry<float> IntersectionMarkerRangeMeters = null!;
    internal static MelonPreferences_Entry<float> IntersectionArrowColorR = null!;
    internal static MelonPreferences_Entry<float> IntersectionArrowColorG = null!;
    internal static MelonPreferences_Entry<float> IntersectionArrowColorB = null!;
    internal static MelonPreferences_Entry<float> IntersectionArrowWidth = null!;
    internal static MelonPreferences_Entry<float> IntersectionArrowLength = null!;
    internal static MelonPreferences_Entry<bool> CheckForUpdates = null!;
    internal static MelonPreferences_Entry<bool> AutoDownloadUpdates = null!;
    internal static MelonPreferences_Entry<bool> PromptInstallUpdate = null!;

    internal static void Init()
    {
        Category = MelonPreferences.CreateCategory("VoogleRoute", "Voogle Route");
        ShowDebugOverlay = Category.CreateEntry("ShowDebugOverlay", false, "[Dev] In-game debug overlay (no console spam)");
        RouteLineEnabled = Category.CreateEntry("RouteLineEnabled", true, "Show route line on the ground");
        AutoWalkEnabled = Category.CreateEntry("AutoWalkEnabled", false, "Auto-walk to map destination (on foot)");
        ShowTurnGuidance = Category.CreateEntry("ShowTurnGuidance", true, "Turn HUD (distance + instruction)");
        ShowIntersectionArrows = Category.CreateEntry("ShowIntersectionArrows", true, "Ground arrows at intersections");
        ShowFullRouteLine = Category.CreateEntry("ShowFullRouteLine", true, "Full line to destination");
        MinTurnAngleDegrees = Category.CreateEntry("MinTurnAngleDegrees", 22f, "Min. angle to count as a turn (°)");
        MaxIntersectionMarkers = Category.CreateEntry("MaxIntersectionMarkers", 6, "Max. arrow markers ahead");
        IntersectionMarkerRangeMeters = Category.CreateEntry("IntersectionMarkerRangeMeters", 300f, "Arrow marker range (m)");
        IntersectionArrowColorR = Category.CreateEntry("IntersectionArrowColorR", RouteNeonBlueR, "Arrow color R");
        IntersectionArrowColorG = Category.CreateEntry("IntersectionArrowColorG", RouteNeonBlueG, "Arrow color G");
        IntersectionArrowColorB = Category.CreateEntry("IntersectionArrowColorB", RouteNeonBlueB, "Arrow color B");
        IntersectionArrowWidth = Category.CreateEntry("IntersectionArrowWidth", 0.07f, "Arrow width (m)");
        IntersectionArrowLength = Category.CreateEntry("IntersectionArrowLength", 3f, "Arrow length (m)");
        LineWidth = Category.CreateEntry("LineWidth", 0.5f, "Line width (legacy, see Foot/Vehicle)");
        FootLineWidth = Category.CreateEntry("FootLineWidth", 0.5f, "Line width on foot");
        VehicleLineWidth = Category.CreateEntry("VehicleLineWidth", 0.1f, "Line width in vehicle");
        LineColorR = Category.CreateEntry("LineColorR", RouteNeonBlueR, "Line color R");
        LineColorG = Category.CreateEntry("LineColorG", RouteNeonBlueG, "Line color G");
        LineColorB = Category.CreateEntry("LineColorB", RouteNeonBlueB, "Line color B");
        LineColorA = Category.CreateEntry("LineColorA", RouteNeonBlueA, "Line color A");
        GroundOffset = Category.CreateEntry("GroundOffset", 0.12f, "Ground offset (m)");
        RecalcIntervalSeconds = Category.CreateEntry("RecalcIntervalSeconds", 0.75f, "Recalc interval on foot (s)");
        VehicleRecalcIntervalSeconds = Category.CreateEntry("VehicleRecalcIntervalSeconds", 2.5f, "Recalc interval in vehicle (s)");
        ShowPartialPaths = Category.CreateEntry("ShowPartialPaths", false, "Show partial NavMesh paths");
        HudButtonScale = Category.CreateEntry("HudButtonScale", 1f, "HUD panel scale");
        NavHudOffsetY = Category.CreateEntry("NavHudOffsetY", 16f,
            "Bottom margin of VOOGLE ROUTE panel (bottom-left anchor)");
        CheckForUpdates = Category.CreateEntry("CheckForUpdates", true, "Check for mod updates on startup");
        AutoDownloadUpdates = Category.CreateEntry("AutoDownloadUpdates", false,
            "Download updates automatically when a newer version is found");
        PromptInstallUpdate = Category.CreateEntry("PromptInstallUpdate", true,
            "Show in-game prompt to install after an update is downloaded");

        MigrateLegacyRouteColors();
    }

    /// <summary>Align saved prefs with Nexus neon blue (#00B4FF).</summary>
    private static void MigrateLegacyRouteColors()
    {
        var migrated = false;

        if (!IsRouteNeonBlue(LineColorR.Value, LineColorG.Value, LineColorB.Value))
        {
            ApplyRouteNeonBlueToLinePrefs();
            migrated = true;
        }

        if (!IsRouteNeonBlue(IntersectionArrowColorR.Value, IntersectionArrowColorG.Value, IntersectionArrowColorB.Value))
        {
            ApplyRouteNeonBlueToArrowPrefs();
            migrated = true;
        }

        if (migrated)
            Category.SaveToFile(false);
    }

    private static void ApplyRouteNeonBlueToLinePrefs()
    {
        LineColorR.Value = RouteNeonBlueR;
        LineColorG.Value = RouteNeonBlueG;
        LineColorB.Value = RouteNeonBlueB;
        LineColorA.Value = RouteNeonBlueA;
    }

    private static void ApplyRouteNeonBlueToArrowPrefs()
    {
        IntersectionArrowColorR.Value = RouteNeonBlueR;
        IntersectionArrowColorG.Value = RouteNeonBlueG;
        IntersectionArrowColorB.Value = RouteNeonBlueB;
    }

    private static bool IsRouteNeonBlue(float r, float g, float b) =>
        r < 0.06f && g > 0.64f && g < 0.76f && b > 0.96f;

    /// <summary>Recalcul itinéraire / résolution cible (ligne ou marche auto).</summary>
    internal static bool WantsRouteComputation =>
        RouteLineEnabled.Value || AutoWalkEnabled.Value;

    internal static UnityEngine.Color LineColor => new(
        LineColorR.Value,
        LineColorG.Value,
        LineColorB.Value,
        LineColorA.Value);

    internal static UnityEngine.Color IntersectionArrowColor => new(
        IntersectionArrowColorR.Value,
        IntersectionArrowColorG.Value,
        IntersectionArrowColorB.Value,
        0.95f);

    internal static void SetRouteLineColor(UnityEngine.Color color, bool save = true)
    {
        LineColorR.Value = color.r;
        LineColorG.Value = color.g;
        LineColorB.Value = color.b;
        LineColorA.Value = color.a;
        IntersectionArrowColorR.Value = color.r;
        IntersectionArrowColorG.Value = color.g;
        IntersectionArrowColorB.Value = color.b;

        if (save)
            Category.SaveToFile(false);

        Rendering.RouteLineRenderer.ApplyStyle();
        Rendering.IntersectionArrowRenderer.ApplyStyle();
        Navigation.PathFinderService.InvalidateCache();
    }

    internal static void Save() => Category.SaveToFile(false);
}
