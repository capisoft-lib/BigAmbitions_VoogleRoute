using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VoogleRoute.Navigation;

namespace VoogleRoute.UI
{
    internal static class AutoDriveConfirmPopup
    {
        private const string RootName = "VoogleRoute_AutoDrivePopup";
        private const int CanvasSortOrder = 12000;
        private const float PanelWidth = 560f;
        private const float PanelHeight = 260f;
        private const float HeaderHeight = 48f;
        private const float TitleFontSize = 18f;
        private const float BodyFontSize = 18f;
        private const float FooterButtonHeight = 44f;
        private const float FooterBottomInset = 24f;
        private const float ButtonGap = 18f;
        private const float BodyTopGap = 18f;
        private const float BodyBottomGap = 18f;

        private static readonly Color BodyTextColor = new Color(0.92f, 0.94f, 0.96f, 1f);

        private static GameObject _root;
        private static TextMeshProUGUI _titleLabel;
        private static TextMeshProUGUI _bodyLabel;
        private static AutoDriveSkipPlanner.Plan _pendingPlan;

        internal static bool IsOpen => _root != null && _root.activeSelf;

        internal static void EnsureCreated()
        {
            if (_root != null)
                return;

            GameUiStyle.EnsureInitialized();

            _root = new GameObject(RootName);
            UnityEngine.Object.DontDestroyOnLoad(_root);
            GameStylePanelChrome.SetupOverlayCanvas(_root, CanvasSortOrder);

            var dim = CreateRect(_root.transform, "Dimmer");
            Stretch(dim);
            var dimImg = dim.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.45f);
            dimImg.raycastTarget = true;
            var dimBtn = dim.gameObject.AddComponent<Button>();
            dimBtn.targetGraphic = dimImg;
            dimBtn.onClick.AddListener(ModUiFocus.Wrap((UnityAction)Close));

            var chrome = GameStylePanelChrome.Build(_root.transform, PanelWidth, PanelHeight, "Panel");
            var scale = chrome.Scale;
            var textScale = Mathf.Clamp(scale, 0.85f, 1.15f);

            var header = chrome.Header;
            ApplyModalHeaderFrame(header, scale);
            var titleGo = CreateRect(header, "Title");
            titleGo.anchorMin = Vector2.zero;
            titleGo.anchorMax = Vector2.one;
            ApplyHeaderTitleInsets(titleGo, textScale);
            _titleLabel = titleGo.gameObject.AddComponent<TextMeshProUGUI>();
            _titleLabel.fontSize = TitleFontSize * textScale;
            _titleLabel.fontStyle = FontStyles.Bold;
            _titleLabel.color = GameUiStyle.TitleColor;
            _titleLabel.alignment = TextAlignmentOptions.Center;
            _titleLabel.raycastTarget = false;
            GameUiStyle.ApplyTitleFont(_titleLabel);

            var body = CreateRect(chrome.Panel, "Body");
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = new Vector2(chrome.ContentInset, FooterBottomInset + FooterButtonHeight + BodyBottomGap);
            body.offsetMax = new Vector2(-chrome.ContentInset, -(HeaderHeight + BodyTopGap));

            _bodyLabel = body.gameObject.AddComponent<TextMeshProUGUI>();
            _bodyLabel.fontSize = BodyFontSize * textScale;
            _bodyLabel.color = BodyTextColor;
            _bodyLabel.alignment = TextAlignmentOptions.Center;
            _bodyLabel.enableWordWrapping = true;
            _bodyLabel.raycastTarget = false;
            GameUiStyle.ApplyButtonFont(_bodyLabel);

            var buttonWidth = (PanelWidth - chrome.ContentInset * 2f - ButtonGap * textScale) * 0.5f;
            var buttonX = (buttonWidth + ButtonGap * textScale) * 0.5f;
            CreateFooterButton(chrome.Panel, "CancelButton", new Vector2(-buttonX, FooterBottomInset), buttonWidth, textScale,
                ModUiText.AutoDriveCancel, GameUiStyle.ApplyButtonGrey, Close);
            CreateFooterButton(chrome.Panel, "ConfirmButton", new Vector2(buttonX, FooterBottomInset), buttonWidth, textScale,
                ModUiText.AutoDriveConfirm, GameUiStyle.ApplyButtonGreen, Confirm);

            _root.SetActive(false);
        }

        internal static void Show(AutoDriveSkipPlanner.Plan plan)
        {
            EnsureCreated();
            _pendingPlan = plan;
            RefreshLocalizedText();
            _root.SetActive(true);
        }

        internal static void Close()
        {
            ModUiFocus.ReleaseForMovement();

            if (_root != null)
                _root.SetActive(false);

            _pendingPlan = default;
        }

        internal static void TickOverlay()
        {
            if (!IsOpen)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
                Close();
        }

        internal static void RefreshLocalizedText()
        {
            if (_titleLabel != null)
                _titleLabel.text = ModUiText.AutoDrivePopupTitle;

            if (_bodyLabel != null && _pendingPlan.Success)
            {
                _bodyLabel.text = ModUiText.FormatAutoDrivePopupBody(
                    _pendingPlan.TravelMinutes,
                    _pendingPlan.DistanceMeters);
            }
        }

        private static void Confirm()
        {
            var plan = _pendingPlan;
            Close();
            if (plan.Success)
                AutoDriveSkipTravelService.StartTravel(plan);
        }

        private static void CreateFooterButton(
            RectTransform panel,
            string name,
            Vector2 anchoredPos,
            float width,
            float scale,
            string label,
            Action<Image> style,
            UnityAction onClick)
        {
            var rect = CreateRect(panel, name);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(width, FooterButtonHeight);

            var img = GameUiStyle.CreateButtonGraphic(rect, scale, style);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            GameUiStyle.BindButtonClick(button, onClick);

            var labelGo = CreateRect(rect, "Label");
            labelGo.anchorMin = Vector2.zero;
            labelGo.anchorMax = Vector2.one;
            labelGo.offsetMin = new Vector2(NavPanelLayout.ButtonTextPaddingX * scale, 0f);
            labelGo.offsetMax = new Vector2(-NavPanelLayout.ButtonTextPaddingX * scale, 0f);
            var tmp = labelGo.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = NavPanelLayout.ButtonFontSize * scale;
            tmp.fontStyle = FontStyles.UpperCase;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            GameUiStyle.ApplyButtonFont(tmp);
        }

        private static void ApplyModalHeaderFrame(RectTransform header, float scale)
        {
            var leftExtend = NavPanelLayout.SettingsHeaderLeftFlush * scale;

            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.anchoredPosition = Vector2.zero;
            header.sizeDelta = Vector2.zero;
            header.offsetMin = new Vector2(-leftExtend, -HeaderHeight);
            header.offsetMax = Vector2.zero;
        }

        private static void ApplyHeaderTitleInsets(RectTransform rect, float scale)
        {
            rect.offsetMin = new Vector2(NavPanelLayout.HeaderTextPaddingX * scale, NavPanelLayout.HeaderTextPaddingY * scale);
            rect.offsetMax = new Vector2(-NavPanelLayout.HeaderTextPaddingX * scale, -NavPanelLayout.HeaderTextPaddingY * scale);
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        internal static void Destroy()
        {
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
                _titleLabel = null;
                _bodyLabel = null;
            }
        }
    }
}
