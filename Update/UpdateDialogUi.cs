using System;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VoogleRoute.UI;
using Object = UnityEngine.Object;

namespace VoogleRoute.Update;

/// <summary>Modal update prompts (above navigation HUD).</summary>
internal static class UpdateDialogUi
{
    private const string RootName = "VoogleRoute_UpdateDialog";
    private const int CanvasSortOrder = 12000;
    private const float PanelWidth = 520f;
    private const float PanelPadding = 20f;
    private const float MessageMinHeight = 72f;
    private const float ButtonHeight = 40f;
    private const float ButtonGap = 10f;

    private static GameObject? _root;
    private static TextMeshProUGUI? _messageLabel;
    private static TextMeshProUGUI? _statusLabel;
    private static GameObject? _buttonRow;
    private static Image? _leftButtonImage;
    private static Image? _rightButtonImage;
    private static TextMeshProUGUI? _leftLabel;
    private static TextMeshProUGUI? _rightLabel;
    private static Button? _leftButton;
    private static Button? _rightButton;

    internal static bool IsVisible => _root != null && _root.activeSelf;

    internal static void EnsureCreated()
    {
        if (_root != null)
            return;

        GameUiStyle.EnsureInitialized();

        _root = new GameObject(RootName);
        Object.DontDestroyOnLoad(_root);

        var canvas = _root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CanvasSortOrder;

        var scaler = _root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        _root.AddComponent<GraphicRaycaster>();

        var dim = CreateRect(_root.transform, "Dimmer");
        Stretch(dim);
        var dimImg = dim.gameObject.AddComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.55f);
        dimImg.raycastTarget = true;

        var panel = CreateRect(_root.transform, "Panel");
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(PanelWidth, 220f);

        var panelBg = panel.gameObject.AddComponent<Image>();
        GameUiStyle.ApplyPanelBg(panelBg);
        panelBg.raycastTarget = true;

        var header = CreateRect(panel, "Header");
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.anchoredPosition = Vector2.zero;
        header.sizeDelta = new Vector2(0f, 44f);
        var headerImg = header.gameObject.AddComponent<Image>();
        GameUiStyle.ApplyHeaderBg(headerImg);

        var title = CreateRect(header, "Title");
        Stretch(title, 12f, 8f);
        var titleTmp = title.gameObject.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "VOOGLE ROUTE";
        titleTmp.fontSize = 18f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = GameUiStyle.TitleColor;
        GameUiStyle.ApplyTitleFont(titleTmp);

        var body = CreateRect(panel, "Body");
        body.anchorMin = new Vector2(0f, 0f);
        body.anchorMax = new Vector2(1f, 1f);
        body.offsetMin = new Vector2(PanelPadding, PanelPadding + ButtonHeight + ButtonGap);
        body.offsetMax = new Vector2(-PanelPadding, -48f);

        var messageGo = CreateRect(body, "Message");
        Stretch(messageGo);
        messageGo.offsetMax = new Vector2(0f, -24f);
        _messageLabel = messageGo.gameObject.AddComponent<TextMeshProUGUI>();
        _messageLabel.fontSize = 15f;
        _messageLabel.alignment = TextAlignmentOptions.TopLeft;
        _messageLabel.color = Color.white;
        _messageLabel.enableWordWrapping = true;
        _messageLabel.overflowMode = TextOverflowModes.Overflow;
        GameUiStyle.ApplyButtonFont(_messageLabel);

        var statusGo = CreateRect(body, "Status");
        statusGo.anchorMin = new Vector2(0f, 0f);
        statusGo.anchorMax = new Vector2(1f, 0f);
        statusGo.pivot = new Vector2(0.5f, 0f);
        statusGo.sizeDelta = new Vector2(0f, 20f);
        _statusLabel = statusGo.gameObject.AddComponent<TextMeshProUGUI>();
        _statusLabel.fontSize = 13f;
        _statusLabel.alignment = TextAlignmentOptions.Center;
        _statusLabel.color = new Color(0.75f, 0.85f, 1f, 1f);
        GameUiStyle.ApplyButtonFont(_statusLabel);

        _buttonRow = CreateRect(panel, "Buttons").gameObject;
        var buttonRowRt = _buttonRow.GetComponent<RectTransform>()!;
        buttonRowRt.anchorMin = new Vector2(0f, 0f);
        buttonRowRt.anchorMax = new Vector2(1f, 0f);
        buttonRowRt.pivot = new Vector2(0.5f, 0f);
        buttonRowRt.anchoredPosition = new Vector2(0f, PanelPadding);
        buttonRowRt.sizeDelta = new Vector2(-PanelPadding * 2f, ButtonHeight);

        var halfW = (PanelWidth - PanelPadding * 2f - ButtonGap) * 0.5f;
        CreateDialogButton(_buttonRow.transform, "Left", new Vector2(-(halfW + ButtonGap) * 0.5f, 0f), halfW,
            out _leftButtonImage, out _leftLabel, out _leftButton);
        CreateDialogButton(_buttonRow.transform, "Right", new Vector2((halfW + ButtonGap) * 0.5f, 0f), halfW,
            out _rightButtonImage, out _rightLabel, out _rightButton);

        _root.SetActive(false);
    }

    internal static void ShowPrimary(
        string message,
        UnityEngine.Events.UnityAction onLater,
        UnityEngine.Events.UnityAction onNow)
    {
        EnsureCreated();
        if (_root == null)
            return;

        SetMessage(message);
        SetStatus("");
        SetButtonsEnabled(true);
        WireButton(_leftButton, _leftLabel, _leftButtonImage, "Later", GameUiStyle.ApplyButtonGrey, onLater);
        WireButton(_rightButton, _rightLabel, _rightButtonImage, "Now", GameUiStyle.ApplyButtonBlue, onNow);
        _root.SetActive(true);
    }

    internal static void ShowBackgroundPrompt(
        string message,
        UnityEngine.Events.UnityAction onNo,
        UnityEngine.Events.UnityAction onYes)
    {
        EnsureCreated();
        if (_root == null)
            return;

        SetMessage(message);
        SetStatus("");
        SetButtonsEnabled(true);
        WireButton(_leftButton, _leftLabel, _leftButtonImage, "No", GameUiStyle.ApplyButtonGrey, onNo);
        WireButton(_rightButton, _rightLabel, _rightButtonImage, "Yes", GameUiStyle.ApplyButtonGreen, onYes);
        _root.SetActive(true);
    }

    internal static void SetStatus(string text)
    {
        if (_statusLabel != null)
            _statusLabel.text = text;
    }

    internal static void SetButtonsEnabled(bool enabled)
    {
        if (_leftButton != null)
            _leftButton.interactable = enabled;
        if (_rightButton != null)
            _rightButton.interactable = enabled;
    }

    internal static void Hide()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    internal static void Destroy()
    {
        if (_root != null)
        {
            Object.Destroy(_root);
            _root = null;
        }

        _messageLabel = null;
        _statusLabel = null;
        _buttonRow = null;
        _leftButton = null;
        _rightButton = null;
    }

    private static void SetMessage(string text)
    {
        if (_messageLabel != null)
            _messageLabel.text = text;
    }

    private static void WireButton(
        Button? button,
        TextMeshProUGUI? label,
        Image? image,
        string text,
        Action<Image> style,
        UnityEngine.Events.UnityAction action)
    {
        if (label != null)
            label.text = text;
        if (image != null)
            style(image);
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private static void CreateDialogButton(
        Transform parent,
        string name,
        Vector2 anchoredPos,
        float width,
        out Image buttonImage,
        out TextMeshProUGUI label,
        out Button button)
    {
        var rect = CreateRect(parent, name);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(width, ButtonHeight);

        buttonImage = rect.gameObject.AddComponent<Image>();
        GameUiStyle.ApplyButtonBlue(buttonImage);

        button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        button.colors = colors;

        var labelGo = CreateRect(rect, "Label");
        Stretch(labelGo);
        label = labelGo.gameObject.AddComponent<TextMeshProUGUI>();
        label.fontSize = 16f;
        label.fontStyle = FontStyles.UpperCase;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        GameUiStyle.ApplyButtonFont(label);
    }

    private static RectTransform CreateRect(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rt, float padX = 0f, float padY = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padX, padY);
        rt.offsetMax = new Vector2(-padX, -padY);
    }
}
