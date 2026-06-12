using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VoogleRoute.Navigation;

namespace VoogleRoute.UI
{
    internal static class CityMapDestinationPopup
    {
        private const string RootName = "VoogleRoute_MapDestPopup";
        private const int CanvasSortOrder = 12000;
        private const float PanelWidth = 520f;
        private const float PanelHeight = 220f;

        private static GameObject _root;
        private static TextMeshProUGUI _titleLabel;
        private static TextMeshProUGUI _addressLabel;

        private static Address _pendingAddress;
        private static Vector3 _pendingWorldPos;

        internal static bool IsOpen => _root != null && _root.activeSelf;

        internal static void EnsureCreated()
        {
            if (_root != null)
                return;

            GameUiStyle.EnsureInitialized();

            _root = new GameObject(RootName);
            Object.DontDestroyOnLoad(_root);
            GameStylePanelChrome.SetupOverlayCanvas(_root, CanvasSortOrder);

            var dim = CreateRect(_root.transform, "Dimmer");
            Stretch(dim);
            var dimImg = dim.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.45f);
            dimImg.raycastTarget = true;
            var dimBtn = dim.gameObject.AddComponent<Button>();
            dimBtn.targetGraphic = dimImg;
            dimBtn.onClick.AddListener((UnityAction)Close);

            var chrome = GameStylePanelChrome.Build(_root.transform, PanelWidth, PanelHeight, "Panel");
            var scale = chrome.Scale;
            var metrics = chrome.Metrics;

            var header = chrome.Header;
            var titleGo = CreateRect(header, "Title");
            titleGo.anchorMin = Vector2.zero;
            titleGo.anchorMax = Vector2.one;
            NavPanelLayout.ApplyHeaderTitleInsets(titleGo, metrics);
            _titleLabel = titleGo.gameObject.AddComponent<TextMeshProUGUI>();
            _titleLabel.fontSize = NavPanelLayout.TitleFontSize * scale;
            _titleLabel.fontStyle = FontStyles.Bold;
            _titleLabel.color = GameUiStyle.TitleColor;
            _titleLabel.alignment = TextAlignmentOptions.Center;
            GameUiStyle.ApplyTitleFont(_titleLabel);

            var body = CreateRect(chrome.Panel, "Body");
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = new Vector2(chrome.ContentInset, 72f);
            body.offsetMax = new Vector2(-chrome.ContentInset, -(metrics.HeaderHeight + 12f));

            _addressLabel = body.gameObject.AddComponent<TextMeshProUGUI>();
            _addressLabel.fontSize = 20f * scale;
            _addressLabel.color = GameUiStyle.TitleColor;
            _addressLabel.alignment = TextAlignmentOptions.Center;
            _addressLabel.enableWordWrapping = true;
            GameUiStyle.ApplyButtonFont(_addressLabel);

            CreateFooterButton(chrome.Panel, "CancelButton", new Vector2(-130f, 24f), scale,
                ModUiText.MapDestCancel, GameUiStyle.ApplyButtonGrey, Close);
            CreateFooterButton(chrome.Panel, "ConfirmButton", new Vector2(130f, 24f), scale,
                ModUiText.MapDestConfirm, GameUiStyle.ApplyButtonGreen, Confirm);

            _root.SetActive(false);
        }

        internal static void Show(Address address, string displayLabel, Vector3 worldPos)
        {
            EnsureCreated();
            _pendingAddress = address;
            _pendingWorldPos = worldPos;
            RefreshLocalizedText();
            _addressLabel.text = displayLabel;
            _root.SetActive(true);
        }

        internal static void Close()
        {
            if (_root != null)
                _root.SetActive(false);

            _pendingAddress = null;
        }

        internal static void RefreshLocalizedText()
        {
            if (_titleLabel != null)
                _titleLabel.text = ModUiText.MapDestTitle;
        }

        private static void Confirm()
        {
            if (_pendingAddress != null)
                VanillaDestinationService.SetMapDestination(_pendingAddress);

            ModLog.Info("Map destination confirmed at " + _pendingWorldPos);
            Close();
        }

        private static void CreateFooterButton(
            RectTransform panel,
            string name,
            Vector2 anchoredPos,
            float scale,
            string label,
            System.Action<Image> style,
            UnityAction onClick)
        {
            var rect = CreateRect(panel, name);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(220f, 44f);

            var img = GameUiStyle.CreateButtonGraphic(rect, scale, style);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            button.onClick.AddListener(onClick);

            var labelGo = CreateRect(rect, "Label");
            labelGo.anchorMin = Vector2.zero;
            labelGo.anchorMax = Vector2.one;
            var tmp = labelGo.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18f * scale;
            tmp.fontStyle = FontStyles.UpperCase;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            GameUiStyle.ApplyButtonFont(tmp);
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
                Object.Destroy(_root);
                _root = null;
                _titleLabel = null;
                _addressLabel = null;
            }
        }
    }
}
