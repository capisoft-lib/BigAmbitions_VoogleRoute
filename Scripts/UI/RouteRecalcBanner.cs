using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VoogleRoute.UI
{
    /// <summary>Transient banner shown while a corridor recalc is running.</summary>
    internal static class RouteRecalcBanner
    {
        private const string RootName = "VoogleRoute_RecalcBanner";
        private const float MinDisplaySeconds = 0.8f;
        private const float PanelWidth = 500f;
        private const float PanelHeight = 64f;
        private const float CenterYOffset = -78f;
        private const int DefaultCanvasSortOrder = 9100;
        private const int CityMapCanvasSortOrder = 11900;

        private static GameObject _root;
        private static Canvas _canvas;
        private static TextMeshProUGUI _label;
        private static float _shownAtUnscaled = -1f;
        private static bool _hideRequested;
        private static bool _allowOnCityMap;

        internal static void EnsureCreated()
        {
            if (_root != null)
                return;

            GameUiStyle.EnsureInitialized();

            _root = new GameObject(RootName);
            Object.DontDestroyOnLoad(_root);
            GameStylePanelChrome.SetupOverlayCanvas(_root, DefaultCanvasSortOrder);
            _canvas = _root.GetComponent<Canvas>();

            var panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(_root.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, CenterYOffset);
            rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            var bg = panel.AddComponent<Image>();
            bg.raycastTarget = false;
            GameUiStyle.ApplyPanelBg(bg);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(panel.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(18f, 12f);
            labelRect.offsetMax = new Vector2(-18f, -12f);

            _label = labelGo.AddComponent<TextMeshProUGUI>();
            _label.fontSize = 18f;
            _label.fontStyle = FontStyles.Bold;
            _label.color = GameUiStyle.TitleColor;
            _label.alignment = TextAlignmentOptions.Center;
            _label.raycastTarget = false;
            GameUiStyle.ApplyTitleFont(_label);

            _root.SetActive(false);
        }

        internal static void Show()
        {
            EnsureCreated();
            _shownAtUnscaled = Time.unscaledTime;
            _hideRequested = false;
            RefreshLocalizedText();
            _root.SetActive(true);
        }

        internal static void ShowOnCityMap()
        {
            _allowOnCityMap = true;
            if (_canvas != null)
                _canvas.sortingOrder = CityMapCanvasSortOrder;
            Show();
        }

        internal static void RequestHide() => _hideRequested = true;

        internal static void ForceHide()
        {
            _hideRequested = false;
            _shownAtUnscaled = -1f;
            _allowOnCityMap = false;
            if (_canvas != null)
                _canvas.sortingOrder = DefaultCanvasSortOrder;
            if (_root != null)
                _root.SetActive(false);
        }

        internal static void Tick()
        {
            if (GameState.IsOverlayBlockingNavigation() && !_allowOnCityMap)
            {
                ForceHide();
                return;
            }

            if (_root == null || !_root.activeSelf || !_hideRequested || _shownAtUnscaled < 0f)
                return;

            if (Time.unscaledTime - _shownAtUnscaled < MinDisplaySeconds)
                return;

            ForceHide();
        }

        internal static void RefreshLocalizedText()
        {
            if (_label == null)
                return;

            _label.text = ModUiText.RouteRecalculating;
        }

        internal static void Destroy()
        {
            ForceHide();
            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
                _canvas = null;
                _label = null;
            }
        }
    }
}
