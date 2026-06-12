using System.Threading.Tasks;
using BAModAPI;
using UnityEngine;
using VoogleRoute.Navigation;
using VoogleRoute.Rendering;
using VoogleRoute.UI;

[assembly: RegisterModClass(typeof(VoogleRoute.VoogleRouteCityMod))]

namespace VoogleRoute
{
    [ModEntryOnCityLoad]
    public sealed class VoogleRouteCityMod : IModBigAmbitions
    {
        private ModContext _context;
        private GameObject _driverObject;

        public string[] RelativeAssetBundlePaths => System.Array.Empty<string>();

        public Task OnLoadAsync(ModContext context)
        {
            _context = context;
            ModLog.Info(
                "Voogle Route city load | mod_id=" + context.ModId +
                " | required_mod=LIB_BaPlayerLocation");

            ModConfig.Initialize(context);
            VoogleRouteLoop.Initialize(context);

            LegacyTurnHudCleanup.DestroyAll();
            ModLog.Info("Legacy turn HUD cleanup done.");

            _driverObject = new GameObject("VoogleRoute_Driver");
            Object.DontDestroyOnLoad(_driverObject);
            _driverObject.AddComponent<VoogleRouteDriver>();
            ModLog.Info("Update driver attached (VoogleRoute_Driver).");

            RouteLineRenderer.EnsureCreated();
            RouteToggleHud.EnsureCreated();
            RouteSettingsUi.EnsureCreated();
            RouteRecalcBanner.EnsureCreated();

            ModLog.Info("Voogle Route city load complete.");
            return Task.CompletedTask;
        }

        public Task OnUnloadAsync()
        {
            ModLog.Info("Voogle Route city unload starting.");

            LegacyTurnHudCleanup.DestroyAll();

            if (_driverObject != null)
            {
                Object.Destroy(_driverObject);
                _driverObject = null;
                ModLog.Info("Update driver destroyed.");
            }

            VoogleRouteLoop.Shutdown();
            AutoDriveLog.Shutdown();
            RouteToggleHud.Destroy();
            AutoDriveConfirmPopup.Destroy();
            RouteSettingsUi.Destroy();
            RouteRecalcBanner.Destroy();
            RouteLineRenderer.Destroy();
            ModLog.Info("UI and route renderers destroyed.");

            DestinationResolver.Clear();
            NavigationTargetTracker.ClearMapGpsTarget("mod unload");
            ModConfig.Shutdown();

            ModLog.Info("Voogle Route city unload complete.");
            _context = null;
            return Task.CompletedTask;
        }
    }
}

