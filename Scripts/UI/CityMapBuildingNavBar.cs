using Helpers;
using Streets;
using TMPro;
using UI;
using UI.InGameUI;
using UI.Notification;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VoogleRoute.Navigation;

using Capisoft.Lib.BaUnifiedUI.Core;

namespace VoogleRoute.UI
{
    /// <summary>Adds a real third action button to the vanilla city-map building panel.</summary>
    internal static class CityMapBuildingNavBar
    {
        private const string RootName = "VoogleRoute_MapBuildingAutoAction_v3";
        private static GameObject _root;
        private static Button _sourceButton;
        private static RectTransform _actionRect;
        private static Button _actionButton;
        private static TextMeshProUGUI _actionLabel;

        internal static void Tick()
        {
            if (!ShouldShow(out var resume))
            {
                if (_root != null)
                    _root.SetActive(false);
                return;
            }

            EnsureCreated(resume);
            if (_root == null)
                return;

            _root.SetActive(true);
            RefreshLocalizedText();
            _actionButton.interactable = true;
        }

        internal static void Suppress()
        {
            if (_root != null)
                _root.SetActive(false);
        }

        internal static void RefreshLocalizedText()
        {
            if (_actionLabel != null)
                _actionLabel.text = ResolveActionLabel();
        }

        private static bool ShouldShow(out BuildingResume resume)
        {
            resume = null;

            if (!GameState.ShouldShowCityMapBookmarks() || IsTaxiMode())
                return false;

            if (!TryGetBuildingResume(out resume))
                return false;

            var cbc = resume.CityBuildingController;
            if (cbc == null || cbc.building == null)
                return false;

            var destinationButton = resume.setDestinationButton;
            return destinationButton != null && destinationButton.gameObject.activeInHierarchy;
        }

        private static void EnsureCreated(BuildingResume resume)
        {
            var sourceButton = resume?.setDestinationButton;
            if (sourceButton == null)
                return;

            if (_root != null &&
                (_sourceButton != sourceButton || _root.transform.parent != sourceButton.transform.parent))
            {
                DestroyActionButton();
            }

            if (_root != null)
                return;

            // Clone the actual green SET DESTINATION control in the actual vanilla
            // hierarchy. The source button remains untouched and keeps its own action.
            _root = Object.Instantiate(sourceButton.gameObject, sourceButton.transform.parent, false);
            _root.name = RootName;
            _root.SetActive(false);

            _sourceButton = sourceButton;
            _actionButton = _root.GetComponent<Button>();
            _actionRect = _root.GetComponent<RectTransform>();
            _actionLabel = _root.GetComponentInChildren<TextMeshProUGUI>(true);

            if (_actionButton == null || _actionRect == null || _actionLabel == null)
            {
                ModLog.Error("City map building nav: could not clone the vanilla destination button.");
                DestroyActionButton();
                return;
            }

            // Remove the copied vanilla SET DESTINATION event only from the clone.
            _actionButton.onClick = new Button.ButtonClickedEvent();
            _actionButton.onClick.AddListener(BaUiFocus.Wrap((UnityAction)OnActionClicked));

            var layoutElement = _root.GetComponent<LayoutElement>() ?? _root.AddComponent<LayoutElement>();
            // Options and Panel both use native VerticalLayoutGroups; Panel also
            // has a ContentSizeFitter (vertical PreferredSize). Include our row in
            // that calculation so the background encloses it after canvas layout.
            // Writing panel.sizeDelta in LateUpdate loses to the fitter and leaves
            // this button outside the background. Native spacing/padding already
            // supplies the gap and bottom margin, and collapses when we hide it.
            layoutElement.ignoreLayout = false;
            _root.transform.SetSiblingIndex(sourceButton.transform.GetSiblingIndex() + 1);
            RefreshLocalizedText();
        }

        private static string ResolveActionLabel() =>
            MovementModeDetector.CanUseAutoDrive()
                ? ModUiText.AutoDrive
                : ModUiText.AutoWalk;

        private static void OnActionClicked()
        {
            var useAutoDrive = MovementModeDetector.CanUseAutoDrive();
            if (!TryNavigateSelectedBuilding(useAutoDrive))
                return;

            ModLog.Info("City map building nav: " + (useAutoDrive ? "auto-drive." : "auto-walk."));
        }

        private static bool TryNavigateSelectedBuilding(bool useAutoDrive)
        {
            if (!TryGetBuildingResume(out var resume))
                return false;

            var buildingController = resume.CityBuildingController;
            var address = buildingController?.building?.Address;
            if (address == null)
                return false;

            VanillaDestinationService.SetMapDestination(address);
            if (!DestinationResolver.TrySyncBuildingNow(buildingController) &&
                !DestinationResolver.TrySyncAddressNow(address))
            {
                Notifications.ShowError("voogle_route_autodrive_no_route");
                return false;
            }

            BookmarkQuickNavService.CloseNavigationPanels();

            if (useAutoDrive)
            {
                AutoDriveSkipTravelService.RequestFromMapSelection();
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
                       BigAmbitionsCompatibility.IsTaxiMapMode(CityManager.Instance?.cityMap);
            }
            catch
            {
                return false;
            }
        }

        private static void DestroyActionButton()
        {
            if (_root != null)
                Object.Destroy(_root);

            _root = null;
            _sourceButton = null;
            _actionRect = null;
            _actionButton = null;
            _actionLabel = null;
        }

        private static void Destroy() => DestroyActionButton();
    }
}
