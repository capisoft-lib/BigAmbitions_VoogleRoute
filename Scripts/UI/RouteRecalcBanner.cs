using TMPro;
using UnityEngine;

using Capisoft.Lib.BaUnifiedUI.Fluent;

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

        private static GameObject _root;
        private static TextMeshProUGUI _label;
        private static float _shownAtUnscaled = -1f;
        private static bool _hideRequested;

        internal static void EnsureCreated()
        {
            if (_root != null)
                return;

            var built = BaUi.Banner(RootName, DefaultCanvasSortOrder, PanelWidth, PanelHeight, CenterYOffset);
            _root = built.Root;
            _label = built.Label;
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

        internal static void RequestHide() => _hideRequested = true;

        internal static void ForceHide()
        {
            _hideRequested = false;
            _shownAtUnscaled = -1f;
            if (_root != null)
                _root.SetActive(false);
        }

        internal static void Tick()
        {
            if (GameState.IsSubwayNavigationActive())
            {
                ForceHide();
                return;
            }

            if (GameState.IsOverlayBlockingNavigation())
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
                _label = null;
            }
        }
    }
}
