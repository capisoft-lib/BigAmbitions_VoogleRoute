using UnityEngine;

namespace VoogleRoute.UI
{
    /// <summary>Gabarit du panneau VOOGLE ROUTE (recette vanilla MoreByUs).</summary>
    internal static class NavPanelLayout
    {
        public const float PanelWidth = 370f;
        public const float ContentInset = 18f;
        public const float HeaderHeight = 48f;
        public const float HeaderTextPaddingX = 18f;
        public const float HeaderTextPaddingY = 7f;
        public const float BodyTopPadding = 5f;
        public const float BodyBottomPadding = 8f;
        public const float ButtonHeight = 40f;
        public const float ButtonGap = 8f;
        public const float ButtonTextPaddingX = 12f;
        public const float TitleFontSize = 18f;
        public const float ButtonFontSize = 16f;
        public const float ButtonLabelBottomInset = 0f;
        public const float ButtonPixelsPerUnit = 2.5f;
        public const float ButtonGraphicBleedBottom = 2f;

        public const float FrameBleedWidth = 24f;
        public const float FrameBleedHeight = 26f;
        public const float FrameOffsetX = -2f;
        public const float FrameOffsetY = -13f;
        public const float FramePixelsPerUnit = 2.45f;

        public const float HeaderTrimWidthBase = 11f;
        public const float HeaderTrimOffsetXBase = -0.5f;
        public const float HeaderLeftExtend = 2f;
        public const float BodyVisibleLeft = 26f;
        public const float BodyVisibleRight = 373f;
        public const float HeaderSliceBorderLeft = BodyVisibleLeft - 3f;
        public const float HeaderSliceBorderRight = 10f;
        public const float SettingsHeaderTightenPerSide = 2f;
        /// <summary>Élargit le header settings pour supprimer le gap latéral (px ref panel 370, négatif = plus large).</summary>
        public static float SettingsPanelHeaderWidenTrim => -(HeaderTrimWidthBase - HeaderLeftExtend);
        /// <summary>Extension gauche seule du header settings (px ref panel 370, droite inchangée).</summary>
        public const float SettingsHeaderLeftFlush = 2f;
        /// <summary>Ajustement vertical icône engrenage (px ref panel 370, négatif = vers le bas).</summary>
        public const float SettingsIconOffsetY = 1f;

        public const float ScreenMarginX = 16f;
        public const float ScreenMarginMinY = 36f;

        public static void StretchButtonGraphic(RectTransform rt, float scale, float bleedBottomMultiplier = 1f)
        {
            var bleed = ButtonGraphicBleedBottom * bleedBottomMultiplier * scale;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0f, -bleed);
            rt.offsetMax = Vector2.zero;
        }

        public static void ComputeHeaderRectHudTrim(
            float panelWidth,
            float scale,
            float extraTrimWidth,
            out float sizeDeltaX,
            out float anchoredPositionX)
        {
            var trimW = (HeaderTrimWidthBase - HeaderLeftExtend + extraTrimWidth) * scale;
            if (trimW <= 0f)
            {
                sizeDeltaX = 0f;
                anchoredPositionX = 0f;
                return;
            }

            var trimOffset = (HeaderTrimOffsetXBase - HeaderLeftExtend * 0.5f) * scale;
            sizeDeltaX = -trimW;
            anchoredPositionX = trimOffset;
        }

        internal struct Metrics
        {
            public float Scale;
            public float PanelWidth;
            public float PanelHeight;
            public float HeaderHeight;
            public float ContentInset;
            public float ContentWidth;
            public float BodyTopPadding;
            public float BodyBottomPadding;
            public float ButtonHeight;
            public float ButtonGap;
            public float HalfButtonWidth;

            public Metrics(float scale)
            {
                Scale = scale;
                PanelWidth = NavPanelLayout.PanelWidth * scale;
                ContentInset = NavPanelLayout.ContentInset * scale;
                ContentWidth = PanelWidth - ContentInset * 2f;
                HeaderHeight = NavPanelLayout.HeaderHeight * scale;
                BodyTopPadding = NavPanelLayout.BodyTopPadding * scale;
                BodyBottomPadding = NavPanelLayout.BodyBottomPadding * scale;
                ButtonHeight = NavPanelLayout.ButtonHeight * scale;
                ButtonGap = NavPanelLayout.ButtonGap * scale;
                HalfButtonWidth = (ContentWidth - ButtonGap) * 0.5f;
                var bodyH = BodyTopPadding + ButtonHeight + BodyBottomPadding;
                PanelHeight = HeaderHeight + bodyH;
            }

            public float ButtonTopY => -(HeaderHeight + BodyTopPadding);
            public float LeftButtonX => -(HalfButtonWidth + ButtonGap) * 0.5f;
            public float RightButtonX => (HalfButtonWidth + ButtonGap) * 0.5f;
        }

        public static Metrics CreateMetrics(float scale) => new Metrics(scale);

        public static void ApplyBodyFrame(RectTransform rect, float scale) => ApplyFrame(rect, scale);

        public static void ApplyHeaderFrame(RectTransform rect, in Metrics m) =>
            GameStylePanelChrome.ApplyHeaderFrameAligned(rect, m);

        private static void ApplyFrame(RectTransform rect, float scale)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(FrameOffsetX * scale, FrameOffsetY * scale);
            rect.sizeDelta = new Vector2(FrameBleedWidth * scale, FrameBleedHeight * scale);
        }

        public static void ApplyHeaderTitleInsets(RectTransform rect, in Metrics m)
        {
            var padX = HeaderTextPaddingX * m.Scale;
            var padY = HeaderTextPaddingY * m.Scale;
            rect.offsetMin = new Vector2(padX, padY);
            rect.offsetMax = new Vector2(-padX, -padY);
        }

        public static Vector2 GetScreenPosition(float offsetY)
        {
            var bottomMargin = offsetY > 0f ? Mathf.Max(ScreenMarginMinY, offsetY) : ScreenMarginMinY;
            return new Vector2(ScreenMarginX, bottomMargin);
        }
    }
}
