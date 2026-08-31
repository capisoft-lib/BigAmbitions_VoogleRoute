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

            if (!ModConfig.DisplayOutsideEnabled)
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

                if (MovementModeDetector.IsHamptonsVehicleNavigationContext())
                    return false;

                if (BuildingManager.IsInsideBuilding)
                    return true;
            }
            catch
            {
                // ignore
            }

            if (!PlayerLocationSession.IsAvailable)
                return false;

            return PlayerLocationSession.Snapshot.MovementKind == MovementKind.Indoor;
        }

        internal static bool ShouldShowIndoorNavigationPanel() =>
            ModConfig.DisplayInsideEnabled && IsIndoorNavigationContext();

        internal static bool ShouldShowNavigationPanel() =>
            ModConfig.DisplayOutsideEnabled && ShouldRunNavigationSystems();

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

            if (!ModConfig.DisplayOutsideEnabled)
                return false;

            if (IsSubwayNavigationActive())
                return false;

            return !IsCityMapSubOverlayOpen();
        }

        /// <summary>Visit history / map bookmarks: allow on city map unless a modal overlay is open.</summary>
        internal static bool IsBlockingVisitHistory()
        {
            if (IsSubwayNavigationActive())
                return true;

            if (IsCityMapOpen())
                return IsCityMapSubOverlayOpen();

            return IsOverlayBlockingNavigation();
        }

        internal static bool IsCityMapSubwayMode()
        {
            try
            {
                if (!CityManager.IsInitialized)
                    return false;

                return CityManager.Instance?.cityMap?.isSubwayMode == true;
            }
            catch
            {
                return false;
            }
        }

        internal static bool IsSubwayNavigationActive()
        {
            if (IsCityMapSubwayMode())
                return true;

            try
            {
                return SubwaySystem.IsRiding;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsCityMapSubOverlayOpen()
        {
            try
            {
                if (BigAmbitionsCompatibility.IsAnyVideoGamePlaying())
                    return true;

                if (IsBizManBusinessPanelOpen())
                    return true;

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

                        if (hud.purchaseUI.IsDoingPurchase)
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

        /// <summary>BizMan business view open on the city map (FullMenu may lag behind bizMan.business activation).</summary>
        private static bool IsBizManBusinessPanelOpen()
        {
            try
            {
                if (!InstanceBehavior<UIs>.IsInitialized)
                    return false;

                var business = InstanceBehavior<UIs>.Instance?.fullMenu?.bizMan?.business;
                if (business == null)
                    return false;

                return business.gameObject.activeInHierarchy && business.gameObject.activeSelf;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Route overlay on the 3D city map (M) — independent from ground navigation.</summary>
        internal static bool ShouldRunMapRouteOverlay()
        {
            if (!IsPlayable())
                return false;

            if (!IsCityMapOpen())
                return false;

            if (!ModConfig.DisplayOutsideEnabled)
                return false;

            if (IsSubwayNavigationActive())
                return false;

            return true;
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
                // Computer games use ActivityWithoutUI, so IsPanelOpen stays false.
                if (BigAmbitionsCompatibility.IsAnyVideoGamePlaying())
                    return true;

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

        internal static bool IsInsideInteriorForDiagnostics() => IsInsideInterior();
    
        private static bool IsInsideInterior()
        {
            try
            {
                if (MovementModeDetector.IsHamptonsVehicleNavigationContext())
                    return false;

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
