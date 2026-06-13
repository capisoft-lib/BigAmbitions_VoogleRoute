using BaPlayerLocation.Subscriber;
using Helpers;
using PlayerActivity;
using Parking.UndergroundParking;
using UI;
using UI.InteriorDesigner;
using UI.MiniMenu;
using UI.Purchase;
using UI.PurchaseVehicle;
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

        internal static bool IsCityMapOpen()
        {
            try
            {
                return CityMap.IsOpen;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Bookmarks panel: map open and no BizMan/purchase/dialog overlay on top.</summary>
        internal static bool ShouldShowCityMapBookmarks()
        {
            if (!IsCityMapOpen())
                return false;

            return !IsCityMapSubOverlayOpen();
        }

        /// <summary>Visit history / map bookmarks: allow on city map unless a modal overlay is open.</summary>
        internal static bool IsBlockingVisitHistory()
        {
            if (IsCityMapOpen())
                return IsCityMapSubOverlayOpen();

            return IsOverlayBlockingNavigation();
        }

        private static bool IsCityMapSubOverlayOpen()
        {
            try
            {
                if (FullMenu.IsOpen)
                    return true;

                if (MiniMenu.IsOpen)
                    return true;

                if (InteriorDesignerUI.IsOpen || PurchaseUI.IsPanelOpen || PurchaseVehicleUI.IsPanelOpen)
                    return true;

                if (PlayerActivityUI.IsPanelOpen)
                    return true;

                if (BuildingPreview.isPreviewing)
                    return true;

                if (HudConfirm.isOpen)
                    return true;

                if (InstanceBehavior<UIs>.IsInitialized)
                {
                    var uis = InstanceBehavior<UIs>.Instance;
                    var hud = uis?.playerHUD;
                    if (hud != null)
                    {
                        if (hud.dialogUI.isPanelOpen || hud.manageCargoUI.isPanelOpen || hud.jobOfferPanel.isPanelOpen)
                            return true;

                        if (hud.purchaseUI.isDoingPurchase)
                            return true;
                    }

                    if (uis.notificationsListUI != null && uis.notificationsListUI.isVisible)
                        return true;
                }
            }
            catch
            {
                return true;
            }

            return false;
        }

        /// <summary>Route overlay on the 3D city map (M) — independent from ground navigation.</summary>
        internal static bool ShouldRunMapRouteOverlay()
        {
            if (!IsPlayable())
                return false;

            return IsCityMapOpen();
        }

        /// <summary>Pathfinding for ground HUD or city-map overlay (map open blocks HUD nav, not map route).</summary>
        internal static bool ShouldRunPathfinding()
        {
            return ShouldRunNavigationSystems() || ShouldRunMapRouteOverlay();
        }
    
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

                if (InteriorDesignerUI.IsOpen || PurchaseUI.IsPanelOpen || PurchaseVehicleUI.IsPanelOpen)
                    return true;

                if (PlayerActivityUI.IsPanelOpen)
                    return true;

                if (InstanceBehavior<UIs>.IsInitialized)
                {
                    var hud = InstanceBehavior<UIs>.Instance?.playerHUD;
                    if (hud != null)
                    {
                        if (hud.dialogUI.isPanelOpen || hud.manageCargoUI.isPanelOpen || hud.jobOfferPanel.isPanelOpen)
                            return true;
                    }

                    if (InstanceBehavior<UIs>.Instance.notificationsListUI != null
                        && InstanceBehavior<UIs>.Instance.notificationsListUI.isVisible)
                        return true;
                }

                if (BuildingPreview.isPreviewing)
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
