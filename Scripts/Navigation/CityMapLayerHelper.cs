using UnityEngine;

namespace VoogleRoute.Navigation
{
    /// <summary>City map camera uses a reduced culling mask — route lines must sit on a visible layer.</summary>
    internal static class CityMapLayerHelper
    {
        private static int _savedLayer = -1;

        internal static int ResolveVisibleLayer()
        {
            try
            {
                if (CityManager.IsInitialized)
                {
                    var map = CityManager.Instance?.cityMap;
                    if (map != null)
                    {
                        var mask = map.cityMapMask.value | map.cityMapMaskLowDetail.value;
                        for (var i = 0; i < 32; i++)
                        {
                            var bit = 1 << i;
                            if ((mask & bit) != 0)
                                return i;
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }

            return 0;
        }

        internal static void ApplyToMapRoute(GameObject root)
        {
            if (root == null)
                return;

            var layer = ResolveVisibleLayer();
            if (_savedLayer < 0)
                _savedLayer = root.layer;

            if (root.layer != layer)
                root.layer = layer;
        }

        internal static void Restore(GameObject root)
        {
            if (root == null || _savedLayer < 0)
                return;

            root.layer = _savedLayer;
        }

        internal static string DescribeCameraMask()
        {
            try
            {
                var cam = GameManager.GetMainCamera();
                if (cam == null)
                    return "camera=null";

                return "culling_mask=0x" + cam.cullingMask.ToString("X") +
                       " layer=" + LayerMask.LayerToName(cam.gameObject.layer);
            }
            catch
            {
                return "camera_error";
            }
        }

        internal static string DescribeMapMask()
        {
            try
            {
                if (!CityManager.IsInitialized)
                    return "city_manager_uninitialized";

                var map = CityManager.Instance?.cityMap;
                if (map == null)
                    return "city_map_null";

                return "city_map_mask=0x" + map.cityMapMask.value.ToString("X") +
                       " low_detail_mask=0x" + map.cityMapMaskLowDetail.value.ToString("X") +
                       " route_layer=" + ResolveVisibleLayer() +
                       " (" + LayerMask.LayerToName(ResolveVisibleLayer()) + ")";
            }
            catch (System.Exception ex)
            {
                return "map_mask_error=" + ex.Message;
            }
        }
    }
}
