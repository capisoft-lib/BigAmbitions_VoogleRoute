using Helpers;
using NWH.VehiclePhysics2.Modules.Fuel;
using UnityEngine;
using Vehicles;

namespace VoogleRoute.Navigation
{
    /// <summary>Distance-based fuel cost for auto-drive skip travel (Option A: L/100 km × route distance).</summary>
    internal static class AutoDriveVehicleFuel
    {
        private const float CruisePowerFraction = 0.45f;
        private const float MinCruiseSpeedKmh = 25f;
        private const float MinFuelLiters = 0.001f;

        internal readonly struct Estimate
        {
            internal readonly bool Applies;
            internal readonly float CurrentFuelLiters;
            internal readonly float FuelUsedLiters;
            internal readonly bool HasEnoughFuel;

            internal Estimate(bool applies, float currentFuelLiters, float fuelUsedLiters, bool hasEnoughFuel)
            {
                Applies = applies;
                CurrentFuelLiters = Mathf.Max(0f, currentFuelLiters);
                FuelUsedLiters = Mathf.Max(0f, fuelUsedLiters);
                HasEnoughFuel = hasEnoughFuel;
            }

            internal static Estimate NotApplicable() => new Estimate(false, 0f, 0f, true);
        }

        internal static bool TryEstimate(float distanceMeters, out Estimate estimate)
        {
            estimate = Estimate.NotApplicable();

            if (distanceMeters <= 0f)
                return false;

            try
            {
                if (!VehicleHelper.IsInsideMotorVehicle())
                    return false;

                var vehicle = VehicleHelper.GetCurrentVehicleBase();
                if (vehicle?.vehicleType == null || Mathf.Approximately(vehicle.vehicleType.maxFuel, 0f))
                    return false;

                var save = SaveGameManager.Current;
                if (save?.gameVariables != null && save.gameVariables.disableVehicleFuel)
                    return false;

                if (vehicle is not CarController car || car.fuelModule == null || !car.fuelModule.useFuel)
                    return false;

                var currentFuel = ReadCurrentFuel(vehicle);
                var litersPer100Km = EstimateCruiseLitersPer100Km(car, car.fuelModule);
                if (litersPer100Km <= 0f)
                    return false;

                var fuelUsed = distanceMeters / 100000f * litersPer100Km;
                fuelUsed = Mathf.Max(MinFuelLiters, fuelUsed);
                var hasEnoughFuel = currentFuel + MinFuelLiters >= fuelUsed;

                estimate = new Estimate(true, currentFuel, fuelUsed, hasEnoughFuel);
                return true;
            }
            catch (System.Exception ex)
            {
                ModLog.Info("Auto-drive fuel estimate failed: " + ex.Message);
                return false;
            }
        }

        internal static void ApplyConsumption(VehicleController vehicle, float fuelUsedLiters)
        {
            if (vehicle == null || fuelUsedLiters <= 0f)
                return;

            try
            {
                var nextFuel = Mathf.Max(0f, vehicle.GetCurrentFuel() - fuelUsedLiters);
                vehicle.SetFuel(nextFuel);
                vehicle.SavePosition();
            }
            catch (System.Exception ex)
            {
                ModLog.Info("Auto-drive fuel consumption failed: " + ex.Message);
            }
        }

        private static float ReadCurrentFuel(VehicleController vehicle)
        {
            var fuel = vehicle.GetCurrentFuel();
            if (fuel > 0f || vehicle.vehicleInstance == null)
                return fuel;

            return vehicle.vehicleInstance.fuel;
        }

        private static float EstimateCruiseLitersPer100Km(CarController car, FuelModule module)
        {
            if (module.ConsumptionLitersPer100Kilometers > 0.5f)
                return module.ConsumptionLitersPer100Kilometers;

            var enginePower = ResolveEngineMaxPower(car);
            if (enginePower <= 0f)
                return 0f;

            var efficiency = Mathf.Clamp(module.efficiency, 0.05f, 0.95f);
            var multiplier = Mathf.Max(0.01f, module.consumptionMultiplier);
            var maxConsumptionPerHour = enginePower / 10f * (1f - efficiency) * multiplier;
            var cruiseConsumptionPerHour = maxConsumptionPerHour * CruisePowerFraction;
            var cruiseSpeedKmh = ResolveCruiseSpeedKmh(car);
            return cruiseConsumptionPerHour / cruiseSpeedKmh * 100f;
        }

        private static float ResolveEngineMaxPower(CarController car)
        {
            var engine = car.vehicleController?.powertrain?.engine;
            if (engine != null && engine.maxPower > 0f)
                return engine.maxPower;

            return car.vehicleType?.enginePower ?? 0f;
        }

        private static float ResolveCruiseSpeedKmh(CarController car)
        {
            var maxSpeed = car.vehicleType?.maxSpeed ?? 0;
            if (maxSpeed <= 0)
                return MinCruiseSpeedKmh;

            return Mathf.Max(MinCruiseSpeedKmh, maxSpeed * 0.55f);
        }
    }
}
