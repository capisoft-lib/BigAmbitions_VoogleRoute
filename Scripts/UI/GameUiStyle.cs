using System;
using Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VoogleRoute.UI
{
    /// <summary>Sprites et polices vanilla (grey-round-bordered, Gradient-Blue-Round, Rubik).</summary>
    internal static class GameUiStyle
    {
        private const string PanelBgName = "grey-round-bordered";
        private const string HeaderBgName = "darkgreybox-header@2x";
        private const string IconBgName = "Gradient-Blue-Round";
        private const string BtnBlueName = "Gradient-Blue-Round";
        private const string BtnGreyName = "Gradient-Gray-Border-Round";
        private const string BtnGreenName = "Gradient-Green-Round";
        private const string BtnRedName = "Gradient-Red-Round";
        private const string FontRegularName = "Rubik-Regular SDF";
        private const string FontBoldName = "Rubik-Bold SDF";
        private const string FontMediumName = "Rubik-Medium SDF";

        internal static readonly Color PanelColor = Color.white;
        internal static readonly Color White = Color.white;
        internal static readonly Color TitleColor = new Color(0.15f, 0.17f, 0.22f, 1f);
        internal static readonly Color BodyTextColor = new Color(0.92f, 0.94f, 0.96f, 1f);
        internal static readonly Color MutedBodyTextColor = new Color(0.75f, 0.78f, 0.82f, 1f);
        internal static readonly Color CarPoiBackgroundColor = new Color(0.25f, 0.58f, 0.82f, 1f);

        internal const float RowCloseButtonSize = 28f;

        private static bool _initialized;
        private static bool _wasReady;
        private static Sprite _panelBg;
        private static Sprite _headerBg;
        private static Sprite _iconBg;
        private static Sprite _btnBlue;
        private static Sprite _btnGrey;
        private static Sprite _btnGreen;
        private static Sprite _btnRed;
        private static Sprite _settingsIcon;
        private static Sprite _pinIcon;
        private static Sprite _addIcon;
        private static Sprite _carIcon;
        private static Sprite _focusIcon;
        private static Sprite _historyIcon;
        private static TMP_FontAsset _fontRegular;
        private static TMP_FontAsset _fontBold;
        private static TMP_FontAsset _fontMedium;

        internal static bool ShouldRebuildHud { get; private set; }

        internal static bool IsReady =>
            _panelBg != null && _headerBg != null && _btnBlue != null && _btnGrey != null && _fontBold != null;

        internal static void EnsureInitialized()
        {
            if (!_initialized)
                _initialized = true;

            if (IsReady)
                return;

            Discover();

            if (IsReady && !_wasReady)
                ShouldRebuildHud = true;
            _wasReady = IsReady;
        }

        internal static void MarkRebuildHandled() => ShouldRebuildHud = false;

        internal static void ApplyPanelBg(Image image)
        {
            ApplySliced(image, _panelBg, PanelColor, _panelBg == null ? PanelColor : White);
            image.pixelsPerUnitMultiplier = NavPanelLayout.FramePixelsPerUnit;
        }

        internal static void ApplyHeaderBg(Image image)
        {
            ApplySliced(image, _headerBg, new Color(0.78f, 0.8f, 0.83f, 1f));
            image.pixelsPerUnitMultiplier = NavPanelLayout.FramePixelsPerUnit;
        }

        internal static void ApplyButtonBlue(Image image)
        {
            ApplySliced(image, _btnBlue, new Color(0.25f, 0.58f, 0.82f, 1f));
            image.pixelsPerUnitMultiplier = NavPanelLayout.ButtonPixelsPerUnit;
        }

        internal static void ApplyButtonGrey(Image image)
        {
            ApplySliced(image, _btnGrey, new Color(0.36f, 0.41f, 0.46f, 1f));
            image.pixelsPerUnitMultiplier = NavPanelLayout.ButtonPixelsPerUnit;
        }

        internal static void ApplyButtonGreen(Image image)
        {
            var vanillaContinueGreen = new Color(0.47f, 0.73f, 0.38f, 1f);
            ApplySliced(image, _btnGreen != null ? _btnGreen : _btnGrey, vanillaContinueGreen);
            image.pixelsPerUnitMultiplier = NavPanelLayout.ButtonPixelsPerUnit;
        }

        internal static void ApplyButtonRed(Image image)
        {
            var vanillaRed = new Color(0.78f, 0.28f, 0.28f, 1f);
            ApplySliced(image, _btnRed != null ? _btnRed : _btnGrey, vanillaRed);
            image.pixelsPerUnitMultiplier = NavPanelLayout.ButtonPixelsPerUnit;
        }

        internal static void ApplyTitleFont(TextMeshProUGUI text)
        {
            var font = _fontRegular != null ? _fontRegular : (_fontMedium != null ? _fontMedium : _fontBold);
            if (font != null)
                text.font = font;
        }

        internal static void ApplyButtonFont(TextMeshProUGUI text)
        {
            var font = _fontMedium != null ? _fontMedium : _fontBold;
            if (font != null)
                text.font = font;
        }

        internal static void ApplySettingsIcon(Image image)
        {
            if (_settingsIcon != null)
            {
                image.sprite = _settingsIcon;
                image.color = White;
                image.preserveAspect = true;
                image.type = Image.Type.Simple;
            }
            else
            {
                image.sprite = null;
                image.color = White;
            }
        }

        internal static void ApplyPinIcon(Image image)
        {
            if (_pinIcon != null)
            {
                image.sprite = _pinIcon;
                image.color = White;
                image.preserveAspect = true;
                image.type = Image.Type.Simple;
            }
            else
            {
                image.sprite = null;
                image.color = White;
            }
        }

        internal static void ApplyAddIcon(Image image)
        {
            if (_addIcon != null)
            {
                image.sprite = _addIcon;
                image.color = White;
                image.preserveAspect = true;
                image.type = Image.Type.Simple;
            }
            else
            {
                image.sprite = null;
                image.color = White;
            }
        }

        internal static void ApplyCarIcon(Image image)
        {
            if (_carIcon != null)
            {
                image.sprite = _carIcon;
                image.color = White;
                image.preserveAspect = true;
                image.type = Image.Type.Simple;
            }
            else
            {
                image.sprite = null;
                image.color = White;
            }
        }

        internal static void ApplyFocusIcon(Image image)
        {
            if (_focusIcon != null)
            {
                image.sprite = _focusIcon;
                image.color = White;
                image.preserveAspect = true;
                image.type = Image.Type.Simple;
            }
            else
            {
                image.sprite = null;
                image.color = White;
            }
        }

        internal static void ApplyHistoryIcon(Image image)
        {
            if (_historyIcon != null)
            {
                image.sprite = _historyIcon;
                image.color = White;
                image.preserveAspect = true;
                image.type = Image.Type.Simple;
            }
            else
            {
                image.sprite = null;
                image.color = White;
            }
        }

        internal const float HeaderCloseButtonSize = 30f;
        internal const float HeaderCloseButtonOffsetX = -5f;
        internal const float HeaderCloseButtonOffsetY = 1f;

        internal static float ComputeHeaderCloseTitleReserve(float scale, float extraOffsetX = 0f)
        {
            var inset = -(HeaderCloseButtonOffsetX - extraOffsetX) * scale;
            return inset + HeaderCloseButtonSize * scale + NavPanelLayout.HeaderIconButtonPad * scale;
        }

        internal static Button CreateHeaderCloseButton(
            Transform header,
            float scale,
            UnityAction onClick,
            float extraOffsetX = 0f)
        {
            var go = new GameObject("CloseButton", typeof(RectTransform));
            go.transform.SetParent(header, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            var buttonSize = HeaderCloseButtonSize * scale;
            rect.anchoredPosition = new Vector2(
                (HeaderCloseButtonOffsetX - extraOffsetX) * scale,
                HeaderCloseButtonOffsetY * scale);
            rect.sizeDelta = new Vector2(buttonSize, buttonSize);

            var image = CreateButtonGraphic(rect, scale, ApplyButtonRed, bleedBottom: false);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            BindButtonClick(button, onClick);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(rect, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var text = labelGo.AddComponent<TextMeshProUGUI>();
            text.text = "\u00d7";
            text.fontSize = 22f * scale;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            ApplyButtonFont(text);
            return button;
        }

        /// <summary>White overlay icon on a colored button; hides the image when no sprite is available.</summary>
        internal static bool TryApplyOverlayIcon(Image image, Action<Image> applyIcon)
        {
            applyIcon(image);
            var hasSprite = image.sprite != null;
            image.enabled = hasSprite;
            return hasSprite;
        }

        internal static bool TryApplyPinOverlayIcon(Image image)
        {
            ApplyPinIcon(image);
            if (image.sprite != null)
            {
                image.enabled = true;
                return true;
            }

            ApplyFocusIcon(image);
            if (image.sprite != null)
            {
                image.enabled = true;
                return true;
            }

            image.enabled = false;
            return false;
        }

        internal static bool TryGetCarIcon(out Sprite sprite)
        {
            EnsureInitialized();
            sprite = _carIcon;
            return sprite != null;
        }

        internal static void ApplyPoiIconBackground(Image image, Color tint)
        {
            if (_iconBg != null)
            {
                image.sprite = _iconBg;
                image.color = tint;
                image.preserveAspect = true;
                image.type = Image.Type.Simple;
            }
            else
            {
                image.sprite = null;
                image.color = tint;
            }
        }

        internal static void BindButtonClick(Button button, UnityAction onClick)
        {
            if (button == null || onClick == null)
                return;

            button.onClick.AddListener(ModUiFocus.Wrap(onClick));
        }

        internal static Button CreateRowCloseButton(Transform parent, float scale, UnityAction onClick)
        {
            var go = new GameObject("DeleteButton", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();

            var image = CreateButtonGraphic(rect, scale, ApplyButtonRed, bleedBottom: false);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            BindButtonClick(button, onClick);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(rect, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var text = labelGo.AddComponent<TextMeshProUGUI>();
            text.text = "\u00d7";
            text.fontSize = 20f * scale;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            ApplyButtonFont(text);
            return button;
        }

        internal static Image CreateButtonGraphic(
            RectTransform buttonRoot,
            float scale,
            Action<Image> applyStyle,
            float bleedBottomMultiplier = 1f,
            bool bleedBottom = true)
        {
            Image img;
            if (!bleedBottom)
            {
                img = buttonRoot.gameObject.AddComponent<Image>();
                img.raycastTarget = true;
                applyStyle(img);
                return img;
            }

            var graphicGo = new GameObject("Graphic");
            graphicGo.transform.SetParent(buttonRoot, false);
            var rt = graphicGo.AddComponent<RectTransform>();
            NavPanelLayout.StretchButtonGraphic(rt, scale, bleedBottomMultiplier);
            img = graphicGo.AddComponent<Image>();
            img.raycastTarget = true;
            applyStyle(img);
            return img;
        }

        private static void ApplySliced(Image image, Sprite sprite, Color fallbackTint)
            => ApplySliced(image, sprite, fallbackTint, White);

        private static void ApplySliced(Image image, Sprite sprite, Color fallbackTint, Color spriteTint)
        {
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = spriteTint;
                var b = sprite.border;
                image.type = b.x > 0.01f || b.y > 0.01f || b.z > 0.01f || b.w > 0.01f
                    ? Image.Type.Sliced
                    : Image.Type.Simple;
            }
            else
            {
                image.color = fallbackTint;
            }

            image.pixelsPerUnitMultiplier = 1f;
            image.preserveAspect = false;
        }

        private static void Discover()
        {
            try
            {
                var sprites = Resources.FindObjectsOfTypeAll<Sprite>();
                for (var i = 0; i < sprites.Length; i++)
                    CaptureSprite(sprites[i]);

                var images = Resources.FindObjectsOfTypeAll<Image>();
                for (var i = 0; i < images.Length; i++)
                {
                    var image = images[i];
                    if (image != null)
                        CaptureSprite(image.sprite);
                }
            }
            catch
            {
                // ressources pas encore prêtes
            }

            try
            {
                if (_fontRegular == null || _fontBold == null || _fontMedium == null)
                {
                    var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                    for (var i = 0; i < fonts.Length; i++)
                    {
                        var f = fonts[i];
                        if (f == null)
                            continue;
                        if (f.name == FontRegularName && _fontRegular == null)
                            _fontRegular = f;
                        else if (f.name == FontBoldName && _fontBold == null)
                            _fontBold = f;
                        else if (f.name == FontMediumName && _fontMedium == null)
                            _fontMedium = f;
                    }

                    if (_fontBold == null && fonts.Length > 0)
                        _fontBold = fonts[0];
                }
            }
            catch
            {
                // polices pas encore prêtes
            }

            ResolvePreferredFocusIcon();
            ResolvePreferredPinIcon();
            ResolvePreferredAddIcon();
            ResolvePreferredCarIcon();
            ResolvePreferredHistoryIcon();
        }

        private static void ResolvePreferredPinIcon()
        {
            if (TryFindSpriteExact(new[]
                {
                    "icon-pin",
                    "map-pin",
                    "icon-pushpin",
                    "pushpin",
                    "icon-location",
                    "icon-locate",
                    "icon-marker"
                }, out var sprite))
            {
                _pinIcon = sprite;
                return;
            }

            if (TryFindSpriteNameContains("pin", out sprite, "spin", "shop", "cart", "shipping")
                || TryFindSpriteNameContains("location", out sprite)
                || TryFindSpriteNameContains("marker", out sprite))
            {
                _pinIcon = sprite;
            }
        }

        private static void ResolvePreferredAddIcon()
        {
            if (TryFindSpriteExact(new[] { "icon-add", "icon-plus", "plus", "add" }, out var sprite))
                _addIcon = sprite;
        }

        private static void ResolvePreferredCarIcon()
        {
            if (TryGetVanillaVehiclePoiIcon(out var vanillaIcon))
            {
                _carIcon = vanillaIcon;
                return;
            }

            if (TryFindSpriteExact(new[] { "icon-car", "icon-vehicle" }, out var sprite))
                _carIcon = sprite;
            else if (_carIcon != null && !IsTrustedCarIconName(_carIcon.name))
                _carIcon = null;
        }

        private static bool TryGetVanillaVehiclePoiIcon(out Sprite sprite)
        {
            sprite = null;
            try
            {
                if (!InstanceBehavior<GlobalReferences>.IsInitialized)
                    return false;

                sprite = InstanceBehavior<GlobalReferences>.Instance?.vehiclePOIIcon;
                return sprite != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsTrustedCarIconName(string name) =>
            string.Equals(name, "icon-car", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "icon-vehicle", StringComparison.OrdinalIgnoreCase);

        private static bool TryFindSpriteExact(string[] names, out Sprite sprite)
        {
            sprite = null;
            try
            {
                var sprites = Resources.FindObjectsOfTypeAll<Sprite>();
                foreach (var preferred in names)
                {
                    for (var i = 0; i < sprites.Length; i++)
                    {
                        var candidate = sprites[i];
                        if (candidate != null &&
                            string.Equals(candidate.name, preferred, StringComparison.OrdinalIgnoreCase))
                        {
                            sprite = candidate;
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }

            return false;
        }

        private static bool TryFindSpriteNameContains(string needle, out Sprite sprite, params string[] exclude)
        {
            sprite = null;
            try
            {
                var sprites = Resources.FindObjectsOfTypeAll<Sprite>();
                for (var i = 0; i < sprites.Length; i++)
                {
                    var candidate = sprites[i];
                    if (candidate == null)
                        continue;

                    var name = candidate.name;
                    if (name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    var skip = false;
                    for (var j = 0; j < exclude.Length; j++)
                    {
                        if (name.IndexOf(exclude[j], StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            skip = true;
                            break;
                        }
                    }

                    if (skip)
                        continue;

                    sprite = candidate;
                    return true;
                }
            }
            catch
            {
                // ignore
            }

            return false;
        }

        private static bool IsShoppingCartSpriteName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            return name.IndexOf("cart", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("shopping", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void ResolvePreferredFocusIcon()
        {
            try
            {
                var sprites = Resources.FindObjectsOfTypeAll<Sprite>();
                foreach (var preferred in new[]
                         {
                             "icon-locate",
                             "icon-target",
                             "icon-crosshair"
                         })
                {
                    for (var i = 0; i < sprites.Length; i++)
                    {
                        var sprite = sprites[i];
                        if (sprite != null &&
                            string.Equals(sprite.name, preferred, StringComparison.OrdinalIgnoreCase))
                        {
                            _focusIcon = sprite;
                            return;
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }
        }

        private static void CaptureSprite(Sprite s)
        {
            if (s == null)
                return;
            if (s.name == PanelBgName && _panelBg == null)
                _panelBg = s;
            if (s.name == HeaderBgName && _headerBg == null)
                _headerBg = s;
            if (s.name == IconBgName && _iconBg == null)
                _iconBg = s;
            if (s.name == BtnBlueName && _btnBlue == null)
                _btnBlue = s;
            if (s.name == BtnGreyName && _btnGrey == null)
                _btnGrey = s;
            if (s.name == BtnGreenName && _btnGreen == null)
                _btnGreen = s;
            if (s.name == BtnRedName && _btnRed == null)
                _btnRed = s;

            if (_settingsIcon == null && IsSettingsIconName(s.name))
                _settingsIcon = s;

            if (_pinIcon == null && IsPinIconName(s.name))
                _pinIcon = s;

            if (_addIcon == null && IsAddIconName(s.name))
                _addIcon = s;

            if (_carIcon == null && IsCarIconName(s.name))
                _carIcon = s;

            if (_focusIcon == null && IsFocusIconName(s.name))
                _focusIcon = s;

            if (_historyIcon == null && IsHistoryIconName(s.name))
                _historyIcon = s;
        }

        private static void ResolvePreferredHistoryIcon()
        {
            if (_historyIcon != null)
                return;

            if (TryFindSpriteExact(new[]
                {
                    "icon-history",
                    "icon-clock",
                    "icon-time",
                    "icon-recent"
                }, out _historyIcon))
                return;

            TryFindSpriteNameContains("history", out _historyIcon, "story");
        }

        private static bool IsHistoryIconName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            if (string.Equals(name, "icon-history", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "icon-clock", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "icon-time", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "icon-recent", StringComparison.OrdinalIgnoreCase))
                return true;

            return name.IndexOf("history", StringComparison.OrdinalIgnoreCase) >= 0
                   || (name.IndexOf("clock", StringComparison.OrdinalIgnoreCase) >= 0
                       && name.IndexOf("alarm", StringComparison.OrdinalIgnoreCase) < 0);
        }

        private static bool IsFocusIconName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            if (string.Equals(name, "icon-locate", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "icon-target", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "icon-crosshair", StringComparison.OrdinalIgnoreCase))
                return true;

            return name.IndexOf("crosshair", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("locate", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsCarIconName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            return IsTrustedCarIconName(name);
        }

        private static bool IsPinIconName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            if (string.Equals(name, "icon-pin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "map-pin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "icon-pushpin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "pushpin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "icon-location", StringComparison.OrdinalIgnoreCase))
                return true;

            if (name.IndexOf("spin", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            return name.IndexOf("pushpin", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("map-pin", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsAddIconName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            if (string.Equals(name, "icon-add", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "icon-plus", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "plus", StringComparison.OrdinalIgnoreCase))
                return true;

            if (name.IndexOf("refresh", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("sync", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("spin", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            return name.EndsWith("-add", StringComparison.OrdinalIgnoreCase)
                   || name.EndsWith("-plus", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSettingsIconName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            return name.IndexOf("setting", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("gear", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("cog", StringComparison.OrdinalIgnoreCase) >= 0
                   || string.Equals(name, "icon-options", StringComparison.OrdinalIgnoreCase);
        }
    }
}
