using MelonLoader;

namespace VoogleRoute;

internal static class ModConfig
{
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
        IntersectionArrowColorR = Category.CreateEntry("IntersectionArrowColorR", 0.07f, "Arrow color R");
        IntersectionArrowColorG = Category.CreateEntry("IntersectionArrowColorG", 0.22f, "Arrow color G");
        IntersectionArrowColorB = Category.CreateEntry("IntersectionArrowColorB", 0.42f, "Arrow color B");
        IntersectionArrowWidth = Category.CreateEntry("IntersectionArrowWidth", 0.07f, "Arrow width (m)");
        IntersectionArrowLength = Category.CreateEntry("IntersectionArrowLength", 3f, "Arrow length (m)");
        LineWidth = Category.CreateEntry("LineWidth", 0.5f, "Line width (legacy, see Foot/Vehicle)");
        FootLineWidth = Category.CreateEntry("FootLineWidth", 0.5f, "Line width on foot");
        VehicleLineWidth = Category.CreateEntry("VehicleLineWidth", 0.1f, "Line width in vehicle");
        LineColorR = Category.CreateEntry("LineColorR", 0.07f, "Line color R");
        LineColorG = Category.CreateEntry("LineColorG", 0.22f, "Line color G");
        LineColorB = Category.CreateEntry("LineColorB", 0.42f, "Line color B");
        LineColorA = Category.CreateEntry("LineColorA", 0.92f, "Line color A");
        GroundOffset = Category.CreateEntry("GroundOffset", 0.12f, "Ground offset (m)");
        RecalcIntervalSeconds = Category.CreateEntry("RecalcIntervalSeconds", 0.75f, "Recalc interval on foot (s)");
        VehicleRecalcIntervalSeconds = Category.CreateEntry("VehicleRecalcIntervalSeconds", 2.5f, "Recalc interval in vehicle (s)");
        ShowPartialPaths = Category.CreateEntry("ShowPartialPaths", false, "Show partial NavMesh paths");
        HudButtonScale = Category.CreateEntry("HudButtonScale", 1f, "HUD panel scale");
        NavHudOffsetY = Category.CreateEntry("NavHudOffsetY", 16f,
            "Bottom margin of VOOGLE ROUTE panel (bottom-left anchor)");

        MigrateLegacyGreenLineColors();
    }

    /// <summary>Resets old green / cyan line defaults from On-Map GPS era.</summary>
    private static void MigrateLegacyGreenLineColors()
    {
        var migrated = false;

        if (IsLegacyGreenLine())
        {
            LineColorR.Value = 0.07f;
            LineColorG.Value = 0.22f;
            LineColorB.Value = 0.42f;
            LineColorA.Value = 0.92f;
            migrated = true;
        }

        if (IsLegacyGreenArrow())
        {
            IntersectionArrowColorR.Value = 0.07f;
            IntersectionArrowColorG.Value = 0.22f;
            IntersectionArrowColorB.Value = 0.42f;
            migrated = true;
        }

        if (migrated)
            Category.SaveToFile(false);
    }

    private static bool IsLegacyGreenLine() =>
        LineColorG.Value > 0.65f && LineColorR.Value < 0.2f && LineColorB.Value < 0.35f;

    private static bool IsLegacyGreenArrow() =>
        IntersectionArrowColorG.Value > 0.65f
        && IntersectionArrowColorR.Value < 0.2f
        && IntersectionArrowColorB.Value < 0.35f;

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
}
