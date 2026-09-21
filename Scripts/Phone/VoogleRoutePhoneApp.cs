using System;
using System.Reflection;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI;
using UI.Smartphone;
using UnityEngine;
using VoogleRoute;
using UnityEngine.Events;
using UnityEngine.UI;
using VoogleRoute.UI;

namespace VoogleRoute.Phone
{
    /// <summary>
    /// Home-screen launcher on Aluna's Phone only. The FullMenu template is BizMan's
    /// sidebar and must never be used as a phone entry.
    /// </summary>
    internal static class VoogleRoutePhoneApp
    {
        internal const string ButtonName = "VoogleRoutePhoneAppButton";

        private static readonly FieldInfo AppButtonTemplateField =
            typeof(SmartphoneUI).GetField(
                "appButtonTemplate",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static FullMenu _fullMenu;
        private static GameInstance _save;
        private static GameObject _menuButton;
        private static Sprite _icon;
        private static Texture2D _iconTexture;
        private static bool _registered;
        private static float _nextRegisterAttempt;
        private static bool _loggedWait;

        internal static bool IsRegistered => _registered;

        internal static void Tick()
        {
            var now = Time.unscaledTime;
            if (now < _nextRegisterAttempt)
                return;

            _nextRegisterAttempt = now + 1f;
            try
            {
                if (!GameState.IsWorldReady() || !PhonePagesPresence.IsActive())
                {
                    Shutdown();
                    return;
                }

                var fullMenu = InstanceBehavior<UIs>.Instance?.fullMenu;
                if (fullMenu == null)
                {
                    Shutdown();
                    if (!_loggedWait)
                    {
                        _loggedWait = true;
                        ModLog.Info("Waiting for phone FullMenu before registering Voogle Route.");
                    }

                    return;
                }

                TryRegister(fullMenu);
            }
            catch (Exception exception)
            {
                Shutdown();
                if (!_loggedWait)
                {
                    _loggedWait = true;
                    ModLog.Info("[WARN] Optional phone app unavailable: " + exception.Message);
                }
            }
        }

        internal static void TryRegister(FullMenu fullMenu)
        {
            if (fullMenu == null || SaveGameManager.Current == null || !PhonePagesPresence.IsActive())
            {
                Shutdown();
                return;
            }

            if (_fullMenu != fullMenu || !ReferenceEquals(_save, SaveGameManager.Current) || _menuButton == null)
                Shutdown();
            if (_registered)
                return;

            var template = ResolvePhoneTemplate();
            if (template == null)
            {
                ModLog.Info("[WARN] Phone app button template not found.");
                return;
            }

            var parent = template.parent;
            if (parent == null)
            {
                ModLog.Info("[WARN] Phone app button parent not found.");
                return;
            }

            _fullMenu = fullMenu;
            _save = SaveGameManager.Current;
            _menuButton = UnityEngine.Object.Instantiate(template.gameObject, parent);
            _menuButton.name = ButtonName;
            _menuButton.SetActive(false);

            var canvasGroup = _menuButton.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            var appButton = _menuButton.GetComponent<SmartphoneAppButton>();
            if (appButton != null)
            {
                appButton.SetIcon(CreateIconSprite());
                appButton.HideOutline();
                appButton.UpdateBadgeCount(0);
            }

            var localization = _menuButton.GetComponentInChildren<TextLocalizationComponent>(true);
            if (localization != null)
                localization.enabled = false;

            var label = _menuButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.text = ModUiText.PhoneAppTitle;

            var uiButton = _menuButton.GetComponent<Button>()
                ?? _menuButton.GetComponentInChildren<Button>(true);
            if (uiButton == null)
                throw new InvalidOperationException("Phone app button has no Button component.");

            uiButton.onClick = new Button.ButtonClickedEvent();
            uiButton.onClick.AddListener((UnityAction)Open);

            _menuButton.transform.SetAsLastSibling();
            _menuButton.SetActive(true);
            _registered = true;
            ModLog.Info("Phone app registered on " + parent.name + " | Aluna's Phone Pages active.");
        }

        internal static void RefreshLocalizedText()
        {
            if (_menuButton == null)
                return;

            var label = _menuButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.text = ModUiText.PhoneAppTitle;
        }

        internal static void Shutdown()
        {
            PhoneGpsPanel.Destroy();

            if (_menuButton != null)
            {
                _menuButton.SetActive(false);
                UnityEngine.Object.Destroy(_menuButton);
            }

            if (_icon != null)
                UnityEngine.Object.Destroy(_icon);
            if (_iconTexture != null)
                UnityEngine.Object.Destroy(_iconTexture);

            _menuButton = null;
            _fullMenu = null;
            _save = null;
            _icon = null;
            _iconTexture = null;
            _registered = false;
        }

        private static void Open()
        {
            if (_fullMenu == null
                || !ReferenceEquals(_save, SaveGameManager.Current)
                || !PhonePagesPresence.IsActive())
                return;

            try
            {
                if (_fullMenu.bizMan?.business?.bizManSettings != null
                    && _fullMenu.bizMan.business.bizManSettings.HasUnsavedChanges)
                {
                    global::UI.Notification.Notifications.ShowError("change_character_clothes_unsaved_changes_warning");
                    return;
                }
            }
            catch
            {
                // Ignore ownership reads; still avoid opening BizMan.
            }

            if (_fullMenu != null && FullMenu.IsOpen)
                _fullMenu.Toggle(false);

            PhoneGpsPanel.Show();
        }

        internal static Transform ResolveAppGrid()
        {
            return ResolvePhoneTemplate()?.parent;
        }

        private static Transform ResolvePhoneTemplate()
        {
            var phone = InstanceBehavior<UIs>.Instance?.smartphoneUI;
            var template = phone == null ? null : AppButtonTemplateField?.GetValue(phone) as Transform;
            return template != null && template.GetComponent<SmartphoneAppButton>() != null
                ? template
                : null;
        }

        private static Sprite CreateIconSprite()
        {
            if (_icon != null)
                return _icon;

            const int size = 256;
            _iconTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color[size * size];
            var navy = new Color(0.05f, 0.18f, 0.42f, 1f);
            var cyan = new Color(0.15f, 0.72f, 1f, 1f);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var px = (x + 0.5f) / size;
                    var py = (y + 0.5f) / size;
                    var dx = Mathf.Max(Mathf.Abs(px - 0.5f) - 0.38f, 0f);
                    var dy = Mathf.Max(Mathf.Abs(py - 0.5f) - 0.38f, 0f);
                    if (dx * dx + dy * dy > 0.012f)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    var grid = (Mathf.Abs((px * 8f) % 1f - 0.5f) < 0.04f
                        || Mathf.Abs((py * 8f) % 1f - 0.5f) < 0.04f)
                        ? 0.12f
                        : 0f;
                    var color = Color.Lerp(navy, cyan, grid);

                    var pinX = px - 0.5f;
                    var pinY = py - 0.52f;
                    var head = pinX * pinX + (pinY - 0.08f) * (pinY - 0.08f) < 0.028f;
                    var stem = pinY < 0.08f && Mathf.Abs(pinX) < 0.07f - pinY * 0.45f;
                    if (head || stem)
                        color = Color.white;

                    pixels[y * size + x] = color;
                }
            }

            _iconTexture.SetPixels(pixels);
            _iconTexture.Apply();
            _icon = Sprite.Create(
                _iconTexture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            return _icon;
        }
    }
}
