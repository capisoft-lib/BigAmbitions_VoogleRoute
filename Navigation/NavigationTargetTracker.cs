using UnityEngine;

namespace VoogleRoute.Navigation;

/// <summary>Cible uniquement pour le GPS carte (customDestination), jamais les clics / NavMesh du jeu.</summary>
public static class NavigationTargetTracker
{
    public const string MapSource = "map.customDestination";

    public static bool HasTarget { get; private set; }
    public static bool HasMapGpsTarget => HasTarget;
    public static Vector3 ActiveTarget { get; private set; }
    public static float LastChangeTime { get; private set; }
    public static string LastSource { get; private set; } = "";

    public static void SetMapGpsTarget(Vector3 target)
    {
        if (HasTarget && (ActiveTarget - target).sqrMagnitude < 0.25f)
            return;

        ActiveTarget = target;
        HasTarget = true;
        LastSource = MapSource;
        LastChangeTime = Time.unscaledTime;
    }

    public static void ClearMapGpsTarget(string reason)
    {
        if (!HasTarget)
            return;
        HasTarget = false;
        LastSource = reason;
        LastChangeTime = Time.unscaledTime;
    }
}
