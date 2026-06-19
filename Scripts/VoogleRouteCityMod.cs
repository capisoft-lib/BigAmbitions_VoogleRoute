using System.Threading.Tasks;
using BAModAPI;
using Capisoft.Lib.BaUnifiedUI.Fluent;
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
            ModLocaleLookup.EnsureLoaded();
            VoogleRouteUiDiagnostics.LogSessionStart(ModStoragePaths.ModRootDirectory);
            VoogleRouteLoop.Initialize(context);

            VoogleRoutePanelLifecycle.PurgeLegacyUiOnCityLoad();
            VoogleRouteUiDiagnostics.LogOrphanRoots("VoogleRoute_ActionPanel");

            _driverObject = new GameObject("VoogleRoute_Driver");
            Object.DontDestroyOnLoad(_driverObject);
            _driverObject.AddComponent<VoogleRouteDriver>();
            ModLog.Info("Update driver attached (VoogleRoute_Driver).");

            RouteLineRenderer.EnsureCreated();
            RouteActionPanel.EnsureCreated();
            RouteSettingsUi.EnsureCreated();
            RouteRecalcBanner.EnsureCreated();
            CityMapBookmarksPanel.EnsureCreated();
            CityMapBookmarkAddDialog.EnsureCreated();
            VisitHistoryPanel.EnsureCreated();

            if (BaUi.ShouldRebuildChrome)
                BaUi.MarkRebuildHandled();

            ModLog.Info("Voogle Route city load complete.");
            return Task.CompletedTask;
        }

        public Task OnUnloadAsync()
        {
            ModLog.Info("Voogle Route city unload starting.");

            if (_driverObject != null)
            {
                Object.Destroy(_driverObject);
                _driverObject = null;
                ModLog.Info("Update driver destroyed.");
            }

            VoogleRouteLoop.Shutdown();
            RouteActionPanel.Destroy();
            AutoDriveConfirmPopup.Destroy();
            RouteSettingsUi.Destroy();
            RouteRecalcBanner.Destroy();
            CityMapBookmarkAddDialog.Destroy();
            CityMapBookmarksPanel.Destroy();
            VisitHistoryPanel.Destroy();
            RouteLineRenderer.Destroy();
            ModLog.Info("UI and route renderers destroyed.");

            DestinationResolver.Clear();
            NavigationTargetTracker.ClearMapGpsTarget("mod unload");
            ParkedVehicleStore.Clear();
            QuickBookmarkStore.Clear();
            VisitHistoryStore.Clear();
            BookmarkDataSaveStore.Shutdown();
            ModConfig.Shutdown();

            RouteGraphStore.Invalidate();
            SubwayStationStore.Invalidate();
            ModLog.Info("Voogle Route city unload complete.");
            _context = null;
            return Task.CompletedTask;
        }
    }
}

