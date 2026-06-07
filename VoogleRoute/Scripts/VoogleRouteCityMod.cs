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
            ModConfig.Initialize(context);
            VoogleRouteLoop.Initialize(context);

            LegacyTurnHudCleanup.DestroyAll();

            _driverObject = new GameObject("VoogleRoute_Driver");
            Object.DontDestroyOnLoad(_driverObject);
            _driverObject.AddComponent<VoogleRouteDriver>();

            RouteLineRenderer.EnsureCreated();
            RouteToggleHud.EnsureCreated();
            RouteSettingsUi.EnsureCreated();

            context.Logger.Info("Voogle Route loaded.");
            return Task.CompletedTask;
        }

        public Task OnUnloadAsync()
        {
            LegacyTurnHudCleanup.DestroyAll();

            if (_driverObject != null)
            {
                Object.Destroy(_driverObject);
                _driverObject = null;
            }

            ModConfig.Shutdown();
            VoogleRouteLoop.Shutdown();
            RouteToggleHud.Destroy();
            RouteSettingsUi.Destroy();
            RouteLineRenderer.Destroy();
            DestinationResolver.Clear();
            NavigationTargetTracker.ClearMapGpsTarget("mod unload");

            if (_context != null)
                _context.Logger.Info("Voogle Route unloaded.");
            _context = null;
            return Task.CompletedTask;
        }
    }
}
