using Il2Cpp;
using Il2CppUI.MiniMenu;
using Il2CppUI.Smartphone;

namespace VoogleRoute;

internal static class GameState
{
    /// <summary>Partie chargée, ville prête — pas d'écran de chargement actif.</summary>
    public static bool IsPlayable() => IsWorldReady();

    /// <summary>
    /// Contexte où le GPS peut travailler : en jeu, dehors, sans carte / menus / overlays.
    /// </summary>
    public static bool ShouldRunNavigationSystems()
    {
        if (!IsPlayable())
            return false;

        if (IsInsideInterior())
            return false;

        if (IsOverlayBlockingNavigation())
            return false;

        return true;
    }

    /// <summary>Panneau NAVIGATION visible (même contexte que <see cref="ShouldRunNavigationSystems"/>).</summary>
    public static bool ShouldShowNavigationPanel() => ShouldRunNavigationSystems();

    /// <summary>Carte ville, téléphone (FullMenu), menu pause (MiniMenu), etc.</summary>
    public static bool IsOverlayBlockingNavigation()
    {
        if (TryStaticIsOpen<CityMap>())
            return true;

        if (IsFullMenuOpen())
            return true;

        if (IsMiniMenuOpen())
            return true;

        if (TryGameTypeIsOpen("InteriorDesignerUI", "UI.InteriorDesigner.InteriorDesignerUI"))
            return true;

        if (TryGameTypeIsOpen("BlueprintsPanel", "BlueprintsUI.BlueprintsPanel"))
            return true;

        return false;
    }

    public static bool IsWorldReady()
    {
        try
        {
            if (!GameManager.IsInitialized)
                return false;

            var gm = GameManager.Instance;
            if (gm == null || gm.playerController == null)
                return false;

            if (IsSceneLoading())
                return false;

            var save = SaveGameManager.Current;
            if (save == null)
                return false;

            if (!save.CityInitialized)
                return false;

            if (save.BuildingRegistrations == null || save.BuildingRegistrations.Count == 0)
                return false;

            if (!CityManager.IsInitialized)
                return false;

            if (!BuildingManager.IsInitialized)
                return false;
        }
        catch
        {
            return false;
        }

        return true;
    }

    /// <summary>Dehors en ville (pas dans un bâtiment).</summary>
    public static bool IsOutdoor()
    {
        if (!IsWorldReady())
            return false;

        return !IsInsideInterior();
    }

    private static bool IsInsideInterior()
    {
        try
        {
            if (BuildingManager.IsInsideBuilding)
                return true;
            if (IsInsideUndergroundParking())
                return true;
        }
        catch
        {
            return true;
        }

        return false;
    }

    private static bool TryStaticIsOpen<T>() where T : class
    {
        try
        {
            var prop = typeof(T).GetProperty("IsOpen",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            return prop != null && prop.GetValue(null) is true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsFullMenuOpen()
    {
        try
        {
            return FullMenu.IsOpen;
        }
        catch
        {
            return TryGameTypeIsOpen("FullMenu", "UI.Smartphone.FullMenu");
        }
    }

    private static bool IsMiniMenuOpen()
    {
        try
        {
            return MiniMenu.IsOpen;
        }
        catch
        {
            return TryGameTypeIsOpen("MiniMenu", "UI.MiniMenu.MiniMenu");
        }
    }

    private static bool TryGameTypeIsOpen(string typeName, params string[] fullNames)
    {
        try
        {
            var asm = typeof(BuildingManager).Assembly;
            var t = asm.GetType(typeName);
            if (t == null)
            {
                foreach (var full in fullNames)
                {
                    t = asm.GetType(full);
                    if (t != null)
                        break;
                }
            }

            if (t == null)
                return false;

            var prop = t.GetProperty("IsOpen",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            return prop != null && prop.GetValue(null) is true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsSceneLoading()
    {
        try
        {
            var asm = typeof(BuildingManager).Assembly;
            var loadScene = asm.GetType("LoadScene") ?? asm.GetType("UI.Load.LoadScene");
            var field = loadScene?.GetField("isLoading",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (field != null && field.GetValue(null) is true)
                return true;
        }
        catch
        {
            // ignore
        }

        return false;
    }

    private static bool IsInsideUndergroundParking()
    {
        try
        {
            var asm = typeof(BuildingManager).Assembly;
            var t = asm.GetType("UndergroundParkingManager")
                      ?? asm.GetType("Parking.UndergroundParking.UndergroundParkingManager");
            var prop = t?.GetProperty("IsInsideParking",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            return prop != null && prop.GetValue(null) is true;
        }
        catch
        {
            return false;
        }
    }
}
