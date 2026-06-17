using System;
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
    internal static class AutoDriveConfirmPopup
    {
        private const string RootName = "VoogleRoute_AutoDrivePopup_v2";
        private const int CanvasSortOrder = 12000;
        private const float PanelWidth = 560f;
        private const float PanelHeight = 260f;
        private const float HeaderHeight = 48f;
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
            VoogleRoutePanelLifecycle.DestroyIfStale(ref _root, RootName, Destroy);
            if (_root != null)
                return;

            var built = BaUi.Modal(RootName, CanvasSortOrder, 0.45f)
                .OnDismiss(Close)
                .Panel(BaPanelRecipe.Modal, PanelWidth, height: PanelHeight)
                .Header(h => h.TitleCenter(ModUiText.AutoDrivePopupTitle))
                .SkipBody()
                .Build();

            _root = built.Root;
            var scale = built.Scale;
            var textScale = Mathf.Clamp(scale, 0.85f, 1.15f);
            _titleLabel = built.Header.Find("Title")?.GetComponent<TextMeshProUGUI>();

            var body = BaUiWidgets.CreateRect(built.Panel, "Body");
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = new Vector2(built.ContentInset, FooterBottomInset + FooterButtonHeight + BodyBottomGap);
            body.offsetMax = new Vector2(-built.ContentInset, -(HeaderHeight + BodyTopGap));

            _bodyLabel = body.gameObject.AddComponent<TextMeshProUGUI>();
            _bodyLabel.fontSize = BodyFontSize * textScale;
            _bodyLabel.color = BodyTextColor;
            _bodyLabel.alignment = TextAlignmentOptions.Center;
            _bodyLabel.enableWordWrapping = true;
            _bodyLabel.raycastTarget = false;
            BaUi.ApplyButtonFont(_bodyLabel);

            var buttonWidth = (PanelWidth - built.ContentInset * 2f - ButtonGap * textScale) * 0.5f;
            var buttonX = (buttonWidth + ButtonGap * textScale) * 0.5f;
            BaUiWidgets.CreateFooterButton(
                built.Panel, "CancelButton", new Vector2(-buttonX, FooterBottomInset),
                new Vector2(buttonWidth, FooterButtonHeight), textScale,
                ModUiText.AutoDriveCancel, BaButtonStyle.Grey, Close);
            BaUiWidgets.CreateFooterButton(
                built.Panel, "ConfirmButton", new Vector2(buttonX, FooterBottomInset),
                new Vector2(buttonWidth, FooterButtonHeight), textScale,
                ModUiText.AutoDriveConfirm, BaButtonStyle.Green, Confirm);

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
            BaUiFocus.ReleaseForMovement();

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
                    _pendingPlan.DistanceMeters,
                    _pendingPlan.UsesFuel,
                    _pendingPlan.FuelUsedLiters);
            }
        }

        private static void Confirm()
        {
            var plan = _pendingPlan;
            Close();
            if (!plan.Success)
                return;

            CityMapHelper.CloseIfOpen();
            AutoDriveSkipTravelService.StartTravel(plan);
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
