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
        private const float UnavailableDisplaySeconds = 3f;
        private const float PanelWidth = 500f;
        private const float PanelHeight = 64f;
        private const float CenterYOffset = -78f;
        private const int DefaultCanvasSortOrder = 9100;

        private static GameObject _root;
        private static TextMeshProUGUI _label;
        private static float _shownAtUnscaled = -1f;
        private static bool _hideRequested;
        private static bool _showingUnavailable;
        private static float _autoHideAtUnscaled = -1f;

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
            _showingUnavailable = false;
            _autoHideAtUnscaled = -1f;
            RefreshLocalizedText();
            _root.SetActive(true);
        }

        internal static void ShowUnavailable()
        {
            EnsureCreated();
            _shownAtUnscaled = Time.unscaledTime;
            _hideRequested = false;
            _showingUnavailable = true;
            _autoHideAtUnscaled = _shownAtUnscaled + UnavailableDisplaySeconds;
            RefreshLocalizedText();
            _root.SetActive(true);
        }

        internal static void RequestHide() => _hideRequested = true;

        internal static void ForceHide()
        {
            _hideRequested = false;
            _shownAtUnscaled = -1f;
            _showingUnavailable = false;
            _autoHideAtUnscaled = -1f;
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

            if (_root == null || !_root.activeSelf)
                return;

            if (_autoHideAtUnscaled >= 0f && Time.unscaledTime >= _autoHideAtUnscaled)
            {
                ForceHide();
                return;
            }

            if (!_hideRequested || _shownAtUnscaled < 0f)
                return;

            if (Time.unscaledTime - _shownAtUnscaled < MinDisplaySeconds)
                return;

            ForceHide();
        }

        internal static void RefreshLocalizedText()
        {
            if (_label == null)
                return;

            _label.text = _showingUnavailable
                ? ModUiText.RouteUnavailable
                : ModUiText.RouteRecalculating;
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
