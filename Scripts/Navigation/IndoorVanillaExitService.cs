using Helpers;
using Parking.UndergroundParking;
using UI.Purchase;

namespace VoogleRoute.Navigation
{
    /// <summary>Triggers the same building exit flow as <see cref="ExitZoneDespawner"/>.</summary>
    internal static class IndoorVanillaExitService
    {
        internal static bool TryRequestExit(in IndoorExitTarget exit)
        {
            if (!exit.IsValid)
                return false;

            try
            {
                if (!BuildingManager.IsInsideBuilding && !UndergroundParkingManager.IsInsideParking)
                    return false;

                if (PurchaseUI.IsPanelOpen)
                    return false;

                var playerController = PlayerHelper.PlayerController;
                if (playerController == null)
                    return false;

                if (playerController.NavigationDisabled && !VehicleHelper.IsInsideMotorVehicle())
                    return false;

                if (playerController.hasOnGoalReachedAction)
                    return false;

                var manager = BuildingManager.Instance;
                if (manager == null || manager.enteringBuilding)
                    return false;

                if (!PlayerHelper.HasPaidForAllItems())
                    return false;

                if (!PlayerHelper.CanLeaveHome())
                    return false;

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
                return false;
            }
        }
    }
}
