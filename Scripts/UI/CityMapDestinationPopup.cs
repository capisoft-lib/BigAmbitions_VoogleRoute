using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VoogleRoute.Navigation;

using Capisoft.Lib.BaUnifiedUI.Controls;
using Capisoft.Lib.BaUnifiedUI.Core;
using Capisoft.Lib.BaUnifiedUI.Fluent;

namespace VoogleRoute.UI
{
    internal static class CityMapDestinationPopup
    {
        private const string RootName = "VoogleRoute_MapDestPopup_v2";
        private const string DragPositionId = "voogleroute:city-map-destination";
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
            VoogleRoutePanelLifecycle.DestroyIfStale(ref _root, RootName, Destroy);
            if (_root != null)
                return;

            var built = BaUi.Modal(RootName, CanvasSortOrder, 0.45f)
                .OnDismiss(Close)
                .Panel(BaPanelRecipe.Modal, PanelWidth, height: PanelHeight)
                .Draggable(DragPositionId)
                .Header(h => h.TitleCenter(ModUiText.MapDestTitle))
                .SkipBody()
                .Build();

            _root = built.Root;
            var scale = built.Scale;
            var metrics = built.Metrics;
            _titleLabel = built.Header.Find("Title")?.GetComponent<TextMeshProUGUI>();

            var body = BaUiWidgets.CreateRect(built.Panel, "Body");
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = new Vector2(built.ContentInset, 72f);
            body.offsetMax = new Vector2(-built.ContentInset, -(metrics.HeaderHeight + 12f));

            _addressLabel = body.gameObject.AddComponent<TextMeshProUGUI>();
            _addressLabel.fontSize = 20f * scale;
            _addressLabel.color = BaUi.Colors.Body;
            _addressLabel.alignment = TextAlignmentOptions.Center;
            _addressLabel.enableWordWrapping = true;
            BaUi.ApplyButtonFont(_addressLabel);

            BaUiWidgets.CreateFooterButton(
                built.Panel, "CancelButton", new Vector2(-130f, 24f), new Vector2(220f, 44f), scale,
                ModUiText.MapDestCancel, BaButtonStyle.Grey, Close);
            BaUiWidgets.CreateFooterButton(
                built.Panel, "ConfirmButton", new Vector2(130f, 24f), new Vector2(220f, 44f), scale,
                ModUiText.MapDestConfirm, BaButtonStyle.Green, Confirm);

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
            BaUiFocus.ReleaseForMovement();

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
