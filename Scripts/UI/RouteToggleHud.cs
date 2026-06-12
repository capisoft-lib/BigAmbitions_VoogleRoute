using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VoogleRoute;
using VoogleRoute.Navigation;
using VoogleRoute.Rendering;

namespace VoogleRoute.UI
{
    /// <summary>Panneau VOOGLE ROUTE — sprites et polices vanilla via <see cref="GameUiStyle"/>.</summary>
    internal static class RouteToggleHud
    {
        private const string RootName = "VoogleRoute_HudRoot_v62";

        private static GameObject _root;
        private static RectTransform _panelRect;
        private static bool _legacyCleaned;

        private static Image _routeButtonImage;
        private static TextMeshProUGUI _routeLabel;
        private static Image _autoWalkButtonImage;
        private static TextMeshProUGUI _autoWalkLabel;
        private static TextMeshProUGUI _panelTitleLabel;

        private static bool _lastActive;
        private static bool _lastIndoor;
        private static bool _lastRouteOn;
        private static bool _lastWalkOn;
        private static bool _lastOnFoot;
        private static bool _lastInVehicle;
        private static float _lastOffsetY = float.NaN;
        private static bool _forceApply = true;

        private static readonly Color LabelDisabled = new Color(1f, 1f, 1f, 0.5f);
        private static readonly Color ButtonLabelColor = Color.white;

        internal static void EnsureCreated()
        {
            if (!_legacyCleaned)
            {
                _legacyCleaned = true;
                DestroyLegacyRoots();
            }

            if (_root != null)
                return;

            GameUiStyle.EnsureInitialized();

            _root = new GameObject(RootName);
            Object.DontDestroyOnLoad(_root);

            GameStylePanelChrome.SetupOverlayCanvas(_root, 9000);

            var layout = NavPanelLayout.CreateMetrics(Mathf.Max(1f, ModConfig.HudButtonScale));
            var chrome = GameStylePanelChrome.Build(_root.transform, layout.PanelWidth, layout.PanelHeight, "NavPanel");
            _panelRect = chrome.Panel;
            _panelRect.anchorMin = _panelRect.anchorMax = new Vector2(0f, 0f);
            _panelRect.pivot = new Vector2(0f, 0f);
            _panelRect.anchoredPosition = NavPanelLayout.GetScreenPosition(ModConfig.NavHudOffsetY);

            var header = chrome.Header;
            var titleGo = CreateRect(header, "Title");
            titleGo.anchorMin = Vector2.zero;
            titleGo.anchorMax = Vector2.one;
            NavPanelLayout.ApplyHeaderTitleInsets(titleGo, layout);
            var titlePadY = NavPanelLayout.HeaderTextPaddingY * layout.Scale;
            var titlePadRight = HeaderButtonsReserve(layout.Scale);
            titleGo.offsetMax = new Vector2(-titlePadRight, -titlePadY);
            _panelTitleLabel = titleGo.gameObject.AddComponent<TextMeshProUGUI>();
            _panelTitleLabel.text = ModUiText.PanelTitle;
            _panelTitleLabel.fontSize = NavPanelLayout.TitleFontSize * layout.Scale;
            _panelTitleLabel.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            _panelTitleLabel.color = GameUiStyle.TitleColor;
            _panelTitleLabel.alignment = TextAlignmentOptions.Left;
            _panelTitleLabel.raycastTarget = false;
            GameUiStyle.ApplyTitleFont(_panelTitleLabel);

            CreateBookmarkPinButton(header, layout);
            CreateAddBookmarkButton(header, layout);
            CreateHistoryButton(header, layout);
            CreateSettingsButton(header, layout);

            CreateActionButton(chrome.Panel, "RouteButton", new Vector2(layout.LeftButtonX, layout.ButtonTopY), layout.HalfButtonWidth,
                layout.ButtonHeight, layout.Scale, OnRouteClicked,
                out _routeButtonImage, out _routeLabel);

            CreateActionButton(chrome.Panel, "AutoWalkButton", new Vector2(layout.RightButtonX, layout.ButtonTopY), layout.HalfButtonWidth,
                layout.ButtonHeight, layout.Scale, OnAutoWalkClicked,
                out _autoWalkButtonImage, out _autoWalkLabel);

            _forceApply = true;
            RefreshVisual();
            ModLog.Info("HUD route toggle panel created.");
        }

        private static void CreateActionButton(
            RectTransform panel,
            string name,
            Vector2 topAnchoredPos,
            float width,
            float height,
            float scale,
            UnityAction onClick,
            out Image buttonImage,
            out TextMeshProUGUI label)
        {
            var rect = CreateRect(panel, name);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = topAnchoredPos;
            rect.sizeDelta = new Vector2(width, height);

            buttonImage = GameUiStyle.CreateButtonGraphic(rect, scale, GameUiStyle.ApplyButtonBlue);

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            GameUiStyle.BindButtonClick(button, onClick);

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var labelGo = CreateRect(rect, "Label");
            labelGo.anchorMin = Vector2.zero;
            labelGo.anchorMax = Vector2.one;
            labelGo.offsetMin = new Vector2(NavPanelLayout.ButtonTextPaddingX * scale, 0f);
            labelGo.offsetMax = new Vector2(-NavPanelLayout.ButtonTextPaddingX * scale,
                -NavPanelLayout.ButtonLabelBottomInset * scale);
            label = labelGo.gameObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = NavPanelLayout.ButtonFontSize * scale;
            label.fontStyle = FontStyles.UpperCase;
            label.alignment = TextAlignmentOptions.Center;
            label.color = ButtonLabelColor;
            label.raycastTarget = false;
            GameUiStyle.ApplyButtonFont(label);
        }

        internal static void UpdateVisibility()
        {
            GameUiStyle.EnsureInitialized();

            if (GameUiStyle.ShouldRebuildHud)
            {
                Destroy();
                GameUiStyle.MarkRebuildHandled();
            }

            EnsureCreated();
            if (_root == null)
                return;

            var offsetY = ModConfig.NavHudOffsetY;
            if (_panelRect != null && (_forceApply || !Mathf.Approximately(offsetY, _lastOffsetY)))
            {
                _lastOffsetY = offsetY;
                _panelRect.anchoredPosition = NavPanelLayout.GetScreenPosition(offsetY);
            }

            var indoor = GameState.IsIndoorNavigationContext();
            var active = indoor
                ? GameState.ShouldShowIndoorNavigationPanel()
                : GameState.ShouldShowNavigationPanel() && MovementModeDetector.ShouldShowHudButton();
            if (_forceApply || active != _lastActive)
            {
                _lastActive = active;
                _root.SetActive(active);
            }

            if (!active)
            {
                _forceApply = false;
                return;
            }

            var routeOn = indoor ? ModConfig.IndoorRouteLineEnabled : ModConfig.RouteLineEnabled;
            var walkOn = indoor ? ModConfig.IndoorAutoWalkEnabled : ModConfig.AutoWalkEnabled;
            var onFoot = MovementModeDetector.CurrentMode == MovementMode.OnFoot;
            var inVehicle = MovementModeDetector.CurrentMode == MovementMode.Vehicle;
            if (_forceApply || indoor != _lastIndoor || routeOn != _lastRouteOn || walkOn != _lastWalkOn ||
                onFoot != _lastOnFoot || inVehicle != _lastInVehicle)
            {
                _lastIndoor = indoor;
                _lastRouteOn = routeOn;
                _lastWalkOn = walkOn;
                _lastOnFoot = onFoot;
                _lastInVehicle = inVehicle;
                RefreshVisual();
            }

            _forceApply = false;

            if (active && !RouteSettingsUi.IsOpen && !AutoDriveConfirmPopup.IsOpen &&
                !CityMapBookmarkAddDialog.IsOpen && !VisitHistoryPanel.IsOpen && !GameState.IsCityMapOpen())
                ModUiFocus.ReleaseForMovement();
        }

        internal static void RefreshLocalizedText() => RefreshVisual();

        internal static void RefreshVisual()
        {
            if (_routeButtonImage == null || _routeLabel == null || _autoWalkButtonImage == null || _autoWalkLabel == null)
                return;

            if (_panelTitleLabel != null)
                _panelTitleLabel.text = ModUiText.PanelTitle;

            var indoor = GameState.IsIndoorNavigationContext();
            var routeOn = indoor ? ModConfig.IndoorRouteLineEnabled : ModConfig.RouteLineEnabled;
            if (routeOn)
                GameUiStyle.ApplyButtonBlue(_routeButtonImage);
            else
                GameUiStyle.ApplyButtonGrey(_routeButtonImage);
            _routeLabel.text = indoor
                ? (routeOn ? ModUiText.WayOutOn : ModUiText.WayOutOff)
                : (routeOn ? ModUiText.RouteOn : ModUiText.RouteOff);
            _routeLabel.color = ButtonLabelColor;

            var walkOn = indoor ? ModConfig.IndoorAutoWalkEnabled : ModConfig.AutoWalkEnabled;
            var onFoot = MovementModeDetector.CurrentMode == MovementMode.OnFoot;
            var inVehicle = MovementModeDetector.CurrentMode == MovementMode.Vehicle;
            if (inVehicle)
            {
                GameUiStyle.ApplyButtonGrey(_autoWalkButtonImage);
                _autoWalkLabel.text = ModUiText.AutoDrive;
                _autoWalkLabel.color = AutoDriveSkipTravelService.IsInProgress ? LabelDisabled : ButtonLabelColor;
            }
            else if (!onFoot && !indoor)
            {
                GameUiStyle.ApplyButtonGrey(_autoWalkButtonImage);
                _autoWalkLabel.text = ModUiText.AutoWalk;
                _autoWalkLabel.color = LabelDisabled;
            }
            else
            {
                if (walkOn)
                    GameUiStyle.ApplyButtonGreen(_autoWalkButtonImage);
                else
                    GameUiStyle.ApplyButtonGrey(_autoWalkButtonImage);
                _autoWalkLabel.text = indoor
                    ? (walkOn ? ModUiText.GetOutOn : ModUiText.GetOut)
                    : (walkOn ? ModUiText.WalkOn : ModUiText.AutoWalk);
                _autoWalkLabel.color = ButtonLabelColor;
            }
        }

        internal static void Destroy()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
                _panelRect = null;
                _forceApply = true;
                ClearButtonRefs();
            }
        }

        private static void DestroyLegacyRoots()
        {
            foreach (var name in new[]
                     {
                         "VoogleRoute_HudRoot_sdk",
                         "VoogleRoute_HudRoot_v1", "VoogleRoute_HudRoot_v50", "VoogleRoute_HudRoot_v51",
                         "VoogleRoute_HudRoot_v52", "VoogleRoute_HudRoot_v53", "VoogleRoute_HudRoot_v54",
                         "VoogleRoute_HudRoot_v55",                          "VoogleRoute_HudRoot_v56",
                         "VoogleRoute_HudRoot_v57",
                         "VoogleRoute_HudRoot_v58",
                         "VoogleRoute_HudRoot_v59",
                         "VoogleRoute_HudRoot_v60",
                         "VoogleRoute_HudRoot_v61"
                     })
            {
                var legacy = GameObject.Find(name);
                if (legacy != null)
                    Object.Destroy(legacy);
            }
        }

        private static void ClearButtonRefs()
        {
            _routeButtonImage = null;
            _routeLabel = null;
            _autoWalkButtonImage = null;
            _autoWalkLabel = null;
            _panelTitleLabel = null;
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
        }

        private static float HeaderButtonsReserve(float scale)
        {
            var size = 32f * scale;
            var pad = 8f * scale;
            var gap = 4f * scale;
            return pad + (size + gap) * 4 + pad;
        }

        private static void CreateBookmarkPinButton(RectTransform header, NavPanelLayout.Metrics layout)
        {
            CreateHeaderIconButton(
                header,
                layout,
                "AddBookmarkButton",
                HeaderIconSlot.BookmarkPin,
                GameUiStyle.ApplyButtonBlue,
                image => GameUiStyle.TryApplyOverlayIcon(image, GameUiStyle.ApplyAddIcon),
                "+",
                Color.white,
                (UnityAction)OnBookmarkPinClicked,
                out _,
                out _);
        }

        private static void CreateAddBookmarkButton(RectTransform header, NavPanelLayout.Metrics layout)
        {
            CreateHeaderIconButton(
                header,
                layout,
                "BookmarkPinButton",
                HeaderIconSlot.AddBookmark,
                GameUiStyle.ApplyButtonGreen,
                image => GameUiStyle.TryApplyOverlayIcon(image, GameUiStyle.ApplyCarIcon),
                "\u2295",
                Color.white,
                (UnityAction)OnLastVehicleClicked,
                out _,
                out _);
        }

        private static void CreateHistoryButton(RectTransform header, NavPanelLayout.Metrics layout)
        {
            CreateHeaderIconButton(
                header,
                layout,
                "HistoryButton",
                HeaderIconSlot.History,
                GameUiStyle.ApplyButtonGrey,
                image => GameUiStyle.TryApplyOverlayIcon(image, GameUiStyle.ApplyHistoryIcon),
                "\u23F1",
                Color.white,
                (UnityAction)(() => VisitHistoryPanel.Toggle()),
                out _,
                out _);
        }

        private static void CreateSettingsButton(RectTransform header, NavPanelLayout.Metrics layout)
        {
            CreateHeaderIconButton(
                header,
                layout,
                "SettingsButton",
                HeaderIconSlot.Settings,
                GameUiStyle.ApplyButtonGrey,
                image => GameUiStyle.TryApplyOverlayIcon(image, GameUiStyle.ApplySettingsIcon),
                "\u2699",
                Color.white,
                (UnityAction)(() => RouteSettingsUi.Toggle()),
                out _,
                out _);
        }

        private enum HeaderIconSlot
        {
            Settings = 0,
            History = 1,
            AddBookmark = 2,
            BookmarkPin = 3
        }

        private static void CreateHeaderIconButton(
            RectTransform header,
            NavPanelLayout.Metrics layout,
            string name,
            HeaderIconSlot slot,
            System.Action<Image> buttonStyle,
            System.Func<Image, bool> applyIcon,
            string fallbackGlyph,
            Color fallbackColor,
            UnityAction onClick,
            out Button button,
            out Image buttonImage)
        {
            var scale = layout.Scale;
            var size = 32f * scale;
            var pad = 8f * scale;
            var gap = 4f * scale;
            var rightInset = pad + (int)slot * (size + gap);

            var rect = CreateRect(header, name);
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = new Vector2(-rightInset, NavPanelLayout.SettingsIconOffsetY * scale);

            buttonImage = GameUiStyle.CreateButtonGraphic(rect, scale, buttonStyle, 1f, bleedBottom: false);
            button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            GameUiStyle.BindButtonClick(button, onClick);

            var iconGo = CreateRect(rect, "Icon");
            Stretch(iconGo, 7f * scale, 7f * scale);
            var icon = iconGo.gameObject.AddComponent<Image>();
            icon.raycastTarget = false;
            if (!applyIcon(icon))
            {
                var fallbackGo = CreateRect(iconGo, "Fallback");
                Stretch(fallbackGo, 0f, 0f);
                var fallback = fallbackGo.gameObject.AddComponent<TextMeshProUGUI>();
                fallback.text = fallbackGlyph;
                fallback.fontSize = 18f * scale;
                fallback.alignment = TextAlignmentOptions.Center;
                fallback.color = fallbackColor;
                fallback.raycastTarget = false;
                GameUiStyle.ApplyTitleFont(fallback);
            }
        }

        private static void OnBookmarkPinClicked() => BookmarkPickService.TryOpenDialogAtCurrentPosition();

        private static void OnLastVehicleClicked()
        {
            if (ParkedVehicleDestinationService.TryNavigateToParkedVehicle())
                return;

            ModLog.Info("No parked vehicle position saved yet.");
        }

        private static void OnAddBookmarkClicked()
        {
            if (!GameState.IsCityMapOpen())
            {
                ModLog.Info("Open Voogle Maps to add a bookmark on the map.");
                return;
            }

            CityMapBookmarksPanel.EnsureCreated();
            CityMapBookmarksPanel.BeginPickMode();
        }

        private static void Stretch(RectTransform rect, float padX, float padY)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padX, padY);
            rect.offsetMax = new Vector2(-padX, -padY);
        }

        private static void OnRouteClicked()
        {
            if (GameState.IsIndoorNavigationContext())
            {
                ModConfig.SetIndoorRouteLineEnabled(!ModConfig.IndoorRouteLineEnabled);
                if (!ModConfig.IndoorRouteLineEnabled)
                    RouteLineRenderer.Hide();
                RefreshVisual();
                return;
            }

            ModConfig.SetRouteLineEnabled(!ModConfig.RouteLineEnabled);
            if (!ModConfig.RouteLineEnabled)
                RouteLineRenderer.Hide();
            if (!ModConfig.WantsRouteComputation)
                PathFinderService.InvalidateCache();
            RefreshVisual();
        }

        private static void OnAutoWalkClicked()
        {
            if (GameState.IsIndoorNavigationContext())
            {
                ModConfig.SetIndoorAutoWalkEnabled(!ModConfig.IndoorAutoWalkEnabled);
                if (!ModConfig.IndoorAutoWalkEnabled)
                    IndoorAutoWalkService.Reset();
                RefreshVisual();
                return;
            }

            if (MovementModeDetector.CurrentMode == MovementMode.Vehicle)
            {
                AutoDriveSkipTravelService.RequestFromHud();
                return;
            }

            if (MovementModeDetector.CurrentMode != MovementMode.OnFoot)
                return;

            ModConfig.SetAutoWalkEnabled(!ModConfig.AutoWalkEnabled);
            if (!ModConfig.AutoWalkEnabled)
                AutoWalkService.Reset();
            RefreshVisual();
        }
    }
}
