using UnityEngine;
using VoogleRoute;

namespace VoogleRoute.Navigation
{
    internal static class CityMapHelper
    {
        internal static void CloseIfOpen()
        {
            try
            {
                if (!CityMap.IsOpen)
                    return;

                if (CityManager.IsInitialized)
                {
                    var map = CityManager.Instance?.cityMap;
                    if (map != null)
                    {
                        map.Toggle();
                        ModLog.Info("City map closed after new GPS destination.");
                        return;
                    }
                }

                var filters = Object.FindObjectOfType<CityMapFilters>();
                filters?.CloseCityMap();
                ModLog.Info("City map closed via filters after new GPS destination.");
            }
            catch (System.Exception ex)
            {
                ModLog.Error("Failed to close city map", ex);
            }
        }
    }
}
