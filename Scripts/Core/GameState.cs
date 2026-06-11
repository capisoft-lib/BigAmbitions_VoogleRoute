using BaPlayerLocation.Subscriber;
using Parking.UndergroundParking;
using UI.MiniMenu;
using UI.Smartphone;
using VoogleRoute.Navigation;

namespace VoogleRoute
{
    
    internal static class GameState
    {
        internal static bool IsPlayable() => IsWorldReady();
    
        internal static bool ShouldRunNavigationSystems()
        {
            if (!IsPlayable())
                return false;
    
            if (IsInsideInterior())
                return false;
    
            if (IsOverlayBlockingNavigation())
                return false;
    
            return true;
        }

        internal static bool IsIndoorNavigationContext()
        {
            if (!IsPlayable() || IsOverlayBlockingNavigation())
                return false;

            try
            {
                if (UndergroundParkingManager.IsInsideParking)
                    return false;
            }
            catch
            {
                // ignore
            }

            if (!PlayerLocationSession.IsAvailable)
                return false;

            return PlayerLocationSession.Snapshot.MovementKind == MovementKind.Indoor;
        }

        internal static bool ShouldShowIndoorNavigationPanel() => IsIndoorNavigationContext();
    
        internal static bool ShouldShowNavigationPanel() => ShouldRunNavigationSystems();
    
        internal static bool IsOverlayBlockingNavigation()
        {
            try
            {
                if (CityMap.IsOpen)
                    return true;
    
                if (FullMenu.IsOpen)
                    return true;
    
                if (MiniMenu.IsOpen)
                    return true;
            }
            catch
            {
                return true;
            }
    
            return false;
        }
    
        internal static bool IsWorldReady()
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
    
        internal static bool IsOutdoor()
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
            }
            catch
            {
                return true;
            }
    
            return false;
        }
    
        private static bool IsSceneLoading()
        {
            try
            {
                var asm = typeof(BuildingManager).Assembly;
                var loadScene = asm.GetType("LoadScene") ?? asm.GetType("UI.Load.LoadScene");
                var field = loadScene?.GetField("isLoading",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (field != null && field.GetValue(null) is bool loading && loading)
                    return true;
            }
            catch
            {
                // ignore
            }
    
            return false;
        }
    }
}
