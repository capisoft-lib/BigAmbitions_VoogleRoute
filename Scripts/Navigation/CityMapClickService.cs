using UnityEngine;
using UnityEngine.EventSystems;
using VoogleRoute.UI;

namespace VoogleRoute.Navigation
{
    /// <summary>Detects map taps (not drags) and opens the destination popup.</summary>
    internal static class CityMapClickService
    {
        private const float TapThresholdSq = 64f;

        private static bool _mouseDown;
        private static Vector2 _mouseDownPos;
        private static bool _mouseDownOnUi;

        internal static void Tick()
        {
            if (!GameState.IsCityMapOpen() || CityMapDestinationPopup.IsOpen)
                return;

            if (Input.GetMouseButtonDown(0))
            {
                _mouseDown = true;
                _mouseDownPos = Input.mousePosition;
                _mouseDownOnUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                return;
            }

            if (!_mouseDown || !Input.GetMouseButtonUp(0))
                return;

            _mouseDown = false;
            if (_mouseDownOnUi || EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            var mousePos = (Vector2)Input.mousePosition;
            if ((mousePos - _mouseDownPos).sqrMagnitude > TapThresholdSq)
                return;

            if (!TryRaycastMap(mousePos, out var hitPoint, out var building))
                return;

            if (!MapAddressResolver.TryResolveFromClick(hitPoint, building, out var address, out var label))
            {
                ModLog.Info("Map click: no address near " + hitPoint);
                return;
            }

            CityMapDestinationPopup.Show(address, label, hitPoint);
        }

        private static bool TryRaycastMap(
            Vector2 screenPos,
            out Vector3 hitPoint,
            out CityBuildingController building)
        {
            hitPoint = default;
            building = null;

            var camera = GameManager.GetMainCamera();
            if (camera == null)
                return false;

            var ray = camera.ScreenPointToRay(screenPos);
            var mask = ResolveRaycastMask();

            if (!Physics.Raycast(ray, out var hit, 2000f, mask, QueryTriggerInteraction.Ignore))
                return false;

            hitPoint = hit.point;
            building = hit.collider.GetComponentInParent<CityBuildingController>();
            return true;
        }

        private static int ResolveRaycastMask()
        {
            try
            {
                if (CityManager.IsInitialized)
                {
                    var map = CityManager.Instance?.cityMap;
                    if (map != null)
                        return map.cityMapMask.value | map.cityMapMaskLowDetail.value;
                }
            }
            catch
            {
                // ignore
            }

            return Physics.DefaultRaycastLayers;
        }
    }
}
