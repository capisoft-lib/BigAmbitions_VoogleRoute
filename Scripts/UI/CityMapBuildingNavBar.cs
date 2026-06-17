using Helpers;
using Streets;
using TMPro;
using UI;
using UI.InGameUI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VoogleRoute.Navigation;

using Capisoft.Lib.BaUnifiedUI.Chrome;
using Capisoft.Lib.BaUnifiedUI.Controls;
using Capisoft.Lib.BaUnifiedUI.Core;
using Capisoft.Lib.BaUnifiedUI.Fluent;

namespace VoogleRoute.UI
{
    /// <summary>Drive / walk shortcuts beside the vanilla city-map set-destination button.</summary>
    internal static class CityMapBuildingNavBar
    {
        private const string RootName = "VoogleRoute_MapBuildingNav_v1";
        private const int CanvasSortOrder = 11500;
        private const float ButtonWidth = 132f;
        private const float ButtonHeight = 40f;
        private const float ButtonGap = 8f;
        private const float AnchorGapPx = 10f;

        private static GameObject _root;
        private static RectTransform _barRect;
        private static Button _driveButton;
        private static Button _walkButton;
        private static TextMeshProUGUI _driveLabel;
        private static TextMeshProUGUI _walkLabel;

        internal static void Tick()
        {
            if (!ShouldShow())
            {
                if (_root != null)
                    _root.SetActive(false);
                return;
            }

            EnsureCreated();
            if (_root == null)
                return;

            _root.SetActive(true);
            RefreshLocalizedText();
            RefreshInteractable();
            SyncPosition();
        }

        internal static void Suppress()
        {
            if (_root != null)
                _root.SetActive(false);
        }

        internal static void RefreshLocalizedText()
        {
            if (_driveLabel != null)
                _driveLabel.text = ModUiText.MapDriveThere;
            if (_walkLabel != null)
                _walkLabel.text = ModUiText.MapWalkThere;
        }

        private static bool ShouldShow()
        {
            if (!GameState.ShouldShowCityMapBookmarks())
                return false;

            if (IsTaxiMode())
                return false;

            if (!TryGetBuildingResume(out var resume))
                return false;

            var cbc = resume.CityBuildingController;
            if (cbc == null || cbc.building == null)
                return false;

            var destButton = resume.setDestinationButton;
            if (destButton == null || !destButton.gameObject.activeInHierarchy)
                return false;

            return MovementModeDetector.CurrentMode is MovementMode.OnFoot or MovementMode.Vehicle;
        }

        private static void EnsureCreated()
        {
            VoogleRoutePanelLifecycle.DestroyIfStale(ref _root, RootName, Destroy);
            if (_root != null)
                return;

            BaUi.EnsureReady();

            _root = new GameObject(RootName);
            Object.DontDestroyOnLoad(_root);
            BaUiChrome.SetupOverlayCanvas(_root, CanvasSortOrder);
            var stamp = _root.AddComponent<BaUiLayoutStamp>();
            stamp.LayoutRevision = BaUiVersion.LayoutRevision;

            _barRect = BaUiWidgets.CreateRect(_root.transform, "Bar");
            _barRect.anchorMin = Vector2.zero;
            _barRect.anchorMax = Vector2.zero;
            _barRect.pivot = new Vector2(0f, 0.5f);

            var scale = Mathf.Max(1f, ModConfig.HudButtonScale);
            var totalWidth = ButtonWidth * 2f + ButtonGap;
            _barRect.sizeDelta = new Vector2(totalWidth, ButtonHeight);

            var driveResult = BaUiControls.CreateVanillaButton(
                _barRect,
                ModUiText.MapDriveThere,
                BaButtonStyle.Blue,
                scale,
                ButtonWidth,
                ButtonHeight,
                BaUiFocus.Wrap((UnityAction)OnDriveClicked));
            _driveButton = driveResult.Button;
            _driveLabel = driveResult.Label;
            PositionButton(driveResult.Graphic.rectTransform, 0f);

            var walkResult = BaUiControls.CreateVanillaButton(
                _barRect,
                ModUiText.MapWalkThere,
                BaButtonStyle.Blue,
                scale,
                ButtonWidth,
                ButtonHeight,
                BaUiFocus.Wrap((UnityAction)OnWalkClicked));
            _walkButton = walkResult.Button;
            _walkLabel = walkResult.Label;
            PositionButton(walkResult.Graphic.rectTransform, ButtonWidth + ButtonGap);

            _root.SetActive(false);
        }

        private static void PositionButton(RectTransform rect, float x)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);
        }

        private static void RefreshInteractable()
        {
            if (_driveButton == null || _walkButton == null)
                return;

            var inVehicle = MovementModeDetector.CurrentMode == MovementMode.Vehicle;
            var onFoot = MovementModeDetector.CurrentMode == MovementMode.OnFoot;
            _driveButton.interactable = inVehicle;
            _walkButton.interactable = onFoot;
        }

        private static void SyncPosition()
        {
            if (_barRect == null || !TryGetBuildingResume(out var resume))
                return;

            var anchor = resume.setDestinationButton?.GetComponent<RectTransform>();
            if (anchor == null)
                return;

            var cam = GameManager.GetMainCamera();

            var worldCenter = anchor.TransformPoint(anchor.rect.center);
            var screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldCenter);
            var barWidth = _barRect.sizeDelta.x;
            var anchorHalfWidth = anchor.rect.width * 0.5f;

            _barRect.position = new Vector3(
                screenPoint.x - anchorHalfWidth - AnchorGapPx - barWidth,
                screenPoint.y,
                0f);
        }

        private static void OnDriveClicked()
        {
            if (!TryNavigateSelectedBuilding(drive: true))
                return;

            ModLog.Info("City map building nav: drive there.");
        }

        private static void OnWalkClicked()
        {
            if (!TryNavigateSelectedBuilding(drive: false))
                return;

            ModLog.Info("City map building nav: walk there.");
        }

        private static bool TryNavigateSelectedBuilding(bool drive)
        {
            if (!TryGetBuildingResume(out var resume))
                return false;

            var cbc = resume.CityBuildingController;
            var address = cbc?.building?.Address;
            if (address == null)
                return false;

            VanillaDestinationService.SetMapDestination(address);
            DestinationResolver.TrySyncAddressNow(address);

            if (drive)
            {
                if (MovementModeDetector.CurrentMode != MovementMode.Vehicle)
                    return false;

                AutoDriveSkipTravelService.RequestFromBookmark();
                return true;
            }

            BookmarkQuickNavService.RequestWalkFromBookmark();
            return true;
        }

        private static bool TryGetBuildingResume(out BuildingResume resume)
        {
            resume = null;
            try
            {
                if (!InstanceBehavior<UIs>.IsInitialized)
                    return false;

                resume = InstanceBehavior<UIs>.Instance?.buildingResume;
                return resume != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsTaxiMode()
        {
            try
            {
                return CityManager.IsInitialized &&
                       CityManager.Instance?.cityMap?.isTaxiMode == true;
            }
            catch
            {
                return false;
            }
        }

        private static void Destroy()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }

            _barRect = null;
            _driveButton = null;
            _walkButton = null;
            _driveLabel = null;
            _walkLabel = null;
        }
    }
}
