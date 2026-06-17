using Helpers;
using Parking.UndergroundParking;
using UI.Purchase;
using VoogleRoute;

namespace VoogleRoute.Navigation
{
    /// <summary>Triggers the same building exit flow as <see cref="ExitZoneDespawner"/>.</summary>
    internal static class IndoorVanillaExitService
    {
        internal static bool TryRequestExit(in IndoorExitTarget exit)
        {
            if (!exit.IsValid)
                return false;

            if (!ModConfig.AutoEnterDestinationEnabled)
                return false;

            try
            {
                if (!BuildingManager.IsInsideBuilding && !UndergroundParkingManager.IsInsideParking)
                {
                    LogBlocked("not_inside");
                    return false;
                }

                if (PurchaseUI.IsPanelOpen)
                {
                    LogBlocked("purchase_ui");
                    return false;
                }

                var playerController = PlayerHelper.PlayerController;
                if (playerController == null)
                {
                    LogBlocked("no_player");
                    return false;
                }

                PlayerNavigationRelease.Release();

                var manager = BuildingManager.Instance;
                if (manager == null || manager.enteringBuilding)
                {
                    LogBlocked("entering_building");
                    return false;
                }

                if (!PlayerHelper.HasPaidForAllItems())
                {
                    LogBlocked("unpaid_items");
                    return false;
                }

                if (!PlayerHelper.CanLeaveHome())
                {
                    LogBlocked("cant_leave_home");
                    return false;
                }

                if (exit.IsParkingExit)
                {
                    UndergroundParkingManager.ExitParking();
                    return true;
                }

                if (exit.IsCasinoExit)
                {
                    InstanceBehavior<CasinoBoatManager>.Instance?.KickOutPlayer();
                    return true;
                }

                manager.ExitFromBuilding(exit.ExitZoneId);
                return true;
            }
            catch
            {
                LogBlocked("exception");
                return false;
            }
        }

        private static void LogBlocked(string reason) =>
            ModLog.Info("Indoor exit blocked: " + reason);
    }
}
