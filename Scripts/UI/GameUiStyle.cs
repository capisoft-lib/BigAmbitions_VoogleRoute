using System;
using TMPro;
using UnityEngine;
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
        private const string FontRegularName = "Rubik-Regular SDF";
        private const string FontBoldName = "Rubik-Bold SDF";
        private const string FontMediumName = "Rubik-Medium SDF";

        internal static readonly Color PanelColor = Color.white;
        internal static readonly Color White = Color.white;
        internal static readonly Color TitleColor = new Color(0.15f, 0.17f, 0.22f, 1f);

        private static bool _initialized;
        private static bool _wasReady;
        private static Sprite _panelBg;
        private static Sprite _headerBg;
        private static Sprite _iconBg;
        private static Sprite _btnBlue;
        private static Sprite _btnGrey;
        private static Sprite _btnGreen;
        private static Sprite _settingsIcon;
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
                image.color = TitleColor;
                image.preserveAspect = true;
                image.type = Image.Type.Simple;
            }
            else
            {
                image.sprite = null;
                image.color = TitleColor;
            }
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

            if (_settingsIcon == null && IsSettingsIconName(s.name))
                _settingsIcon = s;
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
