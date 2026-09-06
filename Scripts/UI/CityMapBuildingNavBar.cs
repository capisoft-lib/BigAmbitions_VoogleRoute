using System.Reflection;
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
        private const float FallbackGap = 10f;
        private const float MinimumGap = 4f;
        private const float MaximumGap = 18f;
        private const float ExtraPanelBottomPadding = 18f;

        private static readonly FieldInfo PanelRectTransformField = typeof(BuildingResume).GetField(
            "panelRectTransform",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static GameObject _root;
        private static Button _sourceButton;
        private static RectTransform _actionRect;
        private static Button _actionButton;
        private static TextMeshProUGUI _actionLabel;

        private static RectTransform _expandedVanillaPanel;
        private static Vector2 _vanillaPanelBaseSize;
        private static Vector2 _vanillaPanelBasePosition;
        private static RectTransform _movedVanillaOptions;
        private static Vector2 _vanillaOptionsBasePosition;
        private static bool _vanillaPanelExpanded;

        internal static void Tick()
        {
            // BuildingResume lays out its own panel every frame. Always hand its original
            // geometry back before it runs, then apply our extra row in LateUpdate.
            RestoreVanillaPanel();

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

        internal static void LateTick()
        {
            if (_root == null || !_root.activeInHierarchy || !ShouldShow(out var resume))
                return;

            var sourceRect = resume.setDestinationButton?.GetComponent<RectTransform>();
            if (sourceRect == null || _actionRect == null)
                return;

            var gap = ResolveVanillaGap(resume, sourceRect);
            ExpandVanillaPanel(resume, sourceRect.rect.height + gap + ExtraPanelBottomPadding);
            PlaceActionBelow(sourceRect, gap);
        }

        internal static void Suppress()
        {
            RestoreVanillaPanel();

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
            layoutElement.ignoreLayout = true;

            _root.AddComponent<CityMapBuildingNavBarLateDriver>();
            RefreshLocalizedText();
        }

        private static float ResolveVanillaGap(BuildingResume resume, RectTransform sourceRect)
        {
            var upperRect = resume.bizManButton?.GetComponent<RectTransform>();
            if (upperRect == null || upperRect.parent != sourceRect.parent)
                return FallbackGap;

            var centerDistance = Mathf.Abs(upperRect.anchoredPosition.y - sourceRect.anchoredPosition.y);
            var measuredGap = centerDistance - (upperRect.rect.height + sourceRect.rect.height) * 0.5f;
            return measuredGap > 0.5f
                ? Mathf.Clamp(measuredGap, MinimumGap, MaximumGap)
                : FallbackGap;
        }

        private static void PlaceActionBelow(RectTransform sourceRect, float gap)
        {
            _actionRect.anchorMin = sourceRect.anchorMin;
            _actionRect.anchorMax = sourceRect.anchorMax;
            _actionRect.pivot = sourceRect.pivot;
            _actionRect.sizeDelta = sourceRect.sizeDelta;
            _actionRect.localScale = sourceRect.localScale;
            _actionRect.localRotation = sourceRect.localRotation;
            _actionRect.anchoredPosition = sourceRect.anchoredPosition +
                                           Vector2.down * (sourceRect.rect.height + gap);

            var localPosition = _actionRect.localPosition;
            localPosition.z = sourceRect.localPosition.z;
            _actionRect.localPosition = localPosition;
            _actionRect.SetSiblingIndex(sourceRect.GetSiblingIndex() + 1);
        }

        private static void ExpandVanillaPanel(BuildingResume resume, float extension)
        {
            var panel = PanelRectTransformField?.GetValue(resume) as RectTransform;
            if (panel == null || extension <= 0f)
                return;

            if (_expandedVanillaPanel != null && _expandedVanillaPanel != panel)
                RestoreVanillaPanel();

            _expandedVanillaPanel = panel;
            _vanillaPanelBaseSize = panel.sizeDelta;
            _vanillaPanelBasePosition = panel.anchoredPosition;
            _vanillaPanelExpanded = true;

            var options = resume.options;
            if (options == panel)
                options = null;
            var optionsWorldPosition = options != null ? options.position : Vector3.zero;
            if (options != null)
            {
                _movedVanillaOptions = options;
                _vanillaOptionsBasePosition = options.anchoredPosition;
            }

            panel.sizeDelta = new Vector2(
                _vanillaPanelBaseSize.x,
                _vanillaPanelBaseSize.y + extension);
            panel.anchoredPosition = new Vector2(
                _vanillaPanelBasePosition.x,
                _vanillaPanelBasePosition.y - extension * (1f - panel.pivot.y));

            // Growing the background downward must not move SET DESTINATION or the
            // existing BizMan button. Keep their vanilla options container fixed.
            if (options != null)
                options.position = optionsWorldPosition;
        }

        private static void RestoreVanillaPanel()
        {
            if (!_vanillaPanelExpanded)
                return;

            if (_expandedVanillaPanel != null)
            {
                _expandedVanillaPanel.sizeDelta = _vanillaPanelBaseSize;
                _expandedVanillaPanel.anchoredPosition = _vanillaPanelBasePosition;
            }

            if (_movedVanillaOptions != null)
                _movedVanillaOptions.anchoredPosition = _vanillaOptionsBasePosition;

            _expandedVanillaPanel = null;
            _movedVanillaOptions = null;
            _vanillaPanelExpanded = false;
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
            RestoreVanillaPanel();

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

    [DefaultExecutionOrder(10000)]
    internal sealed class CityMapBuildingNavBarLateDriver : MonoBehaviour
    {
        private void LateUpdate() => CityMapBuildingNavBar.LateTick();
    }
}
