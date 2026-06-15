using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VoogleRoute.Navigation;

namespace VoogleRoute.UI
{
    internal static class CityMapBookmarksPanel
    {
        private const string RootName = "VoogleRoute_BookmarksPanel_v20";
        private const int VisibleListRowCount = 8;
        private const int CanvasSortOrder = 11000;
        /// <summary>Wider than RouteToggleHud (370) — header/frame via GameStylePanelChrome.Build scale.</summary>
        private const float PanelWidth = 420f;
        private const float SearchBarHeight = 28f;
        private const float SearchBarTopMargin = 8f;
        private const float RowHeight = 34f;
        private const float RowGap = 4f;
        private const float RowTypeIconSize = 26f;
        private const float RowActionButtonSize = 28f;
        private const float RowSetButtonWidth = 44f;
        private const float RowButtonGap = 2f;
        private const float RowButtonPadY = 3f;
        private const float RowDistanceWidth = 52f;
        private const float RowDistanceToCenterGap = 6f;
        private const float RowNameToDistanceGap = 6f;
        private const float FooterTopMargin = 8f;
        private const float ScreenMarginX = 16f;
        private const float ScreenBottomMargin = NavPanelLayout.ScreenMarginMinY;

        private static GameObject _root;
        private static RectTransform _panelRect;
        private static TextMeshProUGUI _titleLabel;
        private static TMP_InputField _searchField;
        private static TextMeshProUGUI _searchPlaceholder;
        private static TextMeshProUGUI _pickHintLabel;
        private static TextMeshProUGUI _addButtonLabel;
        private static TextMeshProUGUI _clearButtonLabel;
        private static RectTransform _contentPanel;
        private static RectTransform _searchBarRect;
        private static RectTransform _pickHintRect;
        private static RectTransform _addFooterRect;
        private static RectTransform _clearFooterRect;
        private static RectTransform _listScrollRect;
        private static RectTransform _listScrollContent;
        private static ScrollRect _listScroll;
        private static float _textScale = 1f;

        private static readonly List<RowUi> QuickRows = new List<RowUi>();
        private static readonly List<RowUi> VehicleRows = new List<RowUi>();
        private static readonly List<RowUi> Rows = new List<RowUi>();
        private static readonly List<(BookmarkDistanceRowKey Key, BookmarkEntry Bookmark)> DistanceRequests =
            new List<(BookmarkDistanceRowKey, BookmarkEntry)>();
        private static readonly Dictionary<BookmarkDistanceRowKey, string> DistanceCache =
            new Dictionary<BookmarkDistanceRowKey, string>();
        private static int _lastBookmarkCount;
        private static string _searchFilter = "";
        private static bool _pickMode;
        private static MovementMode _lastMapActionMode = MovementMode.Unavailable;

        private enum RowKind
        {
            Bookmark,
            Vehicle,
            LastCar,
            LastHome,
            LastShop
        }

        private sealed class RowUi
        {
            internal GameObject Root;
            internal GameObject TypeIconRoot;
            internal Image TypeIcon;
            internal TextMeshProUGUI NameLabel;
            internal TextMeshProUGUI DistanceLabel;
            internal Button CenterButton;
            internal Image CenterButtonImage;
            internal TextMeshProUGUI CenterFallbackLabel;
            internal Button SetDestButton;
            internal TextMeshProUGUI SetDestLabel;
            internal Button DriveButton;
            internal TextMeshProUGUI DriveLabel;
            internal Button DeleteButton;
            internal RowKind Kind = RowKind.Bookmark;
            internal int BookmarkIndex = -1;
            internal int VehicleIndex = -1;
        }

        internal static bool IsVisible => _root != null && _root.activeSelf;
        internal static bool IsSearchFocused => _searchField != null && _searchField.isFocused;
        internal static bool IsPickMode => _pickMode;
        internal static bool BlocksMapInput =>
            IsSearchFocused || CityMapBookmarkAddDialog.IsOpen || CityMapBookmarkAddDialog.IsNameFocused;

        private static float BodyContentTop =>
            NavPanelLayout.HeaderHeight + NavPanelLayout.BodyTopPadding;

        private static float ContentHorizontalInset => NavPanelLayout.ContentInset * 2f;

        private static float HalfButtonWidth =>
            (PanelWidth - ContentHorizontalInset - NavPanelLayout.ButtonGap) * 0.5f;

        private static float LeftButtonX =>
            -(HalfButtonWidth + NavPanelLayout.ButtonGap) * 0.5f;

        private static float RightButtonX =>
            (HalfButtonWidth + NavPanelLayout.ButtonGap) * 0.5f;

        internal static void EnsureCreated()
        {
            DestroyLegacyRoot();

            if (_root != null && _root.name != RootName)
                Destroy();

            if (_root != null)
            {
                GameStylePanelChrome.ApplyUiLayer(_root);
                if (_panelRect != null)
                {
                    AnchorBottomLeft(_panelRect);
                    ApplyPanelLayout();
                    LayoutListContent();
                }
                return;
            }

            GameUiStyle.EnsureInitialized();
            BookmarkStore.Changed += OnBookmarksChanged;
            QuickBookmarkStore.Changed += OnQuickBookmarksChanged;

            _root = new GameObject(RootName);
            Object.DontDestroyOnLoad(_root);
            GameStylePanelChrome.SetupOverlayCanvas(_root, CanvasSortOrder);

            var panelHeight = ComputePanelHeight();
            var headerWiden = NavPanelLayout.ComputeWideMapPanelHeaderWidenTrim(PanelWidth);
            var chrome = GameStylePanelChrome.Build(_root.transform, PanelWidth, panelHeight, "Panel", headerWiden);
            _textScale = Mathf.Clamp(chrome.Scale, 0.85f, 1.15f);
            _panelRect = chrome.Panel;
            _contentPanel = chrome.Panel;
            AnchorBottomLeft(_panelRect);

            var header = chrome.Header;
            var titleGo = CreateRect(header, "Title");
            titleGo.anchorMin = Vector2.zero;
            titleGo.anchorMax = Vector2.one;
            NavPanelLayout.ApplyHeaderTitleWithRightReserve(
                titleGo,
                _textScale,
                NavPanelLayout.ComputeHeaderIconsTitleReserve(1, _textScale));
            _titleLabel = titleGo.gameObject.AddComponent<TextMeshProUGUI>();
            _titleLabel.fontSize = NavPanelLayout.TitleFontSize * _textScale;
            _titleLabel.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            _titleLabel.color = GameUiStyle.TitleColor;
            _titleLabel.alignment = TextAlignmentOptions.Left;
            GameUiStyle.ApplyTitleFont(_titleLabel);

            CreateHeaderHistoryButton(header, _textScale);

            BuildQuickRows(chrome.Panel, _textScale);
            BuildSearchBar(chrome.Panel, _textScale);
            BuildPickHint(chrome.Panel, _textScale);
            BuildListScroll(chrome.Panel);
            BuildFooter(chrome.Panel, _textScale);

            GameStylePanelChrome.ApplyUiLayer(_root);
            _root.SetActive(false);
            _lastBookmarkCount = BookmarkStore.All.Count;
            RefreshLocalizedText();
            RefreshList();
        }

        private static float QuickRowsBlockHeight =>
            QuickBookmarkStore.SlotCount * RowHeight + (QuickBookmarkStore.SlotCount - 1) * RowGap;

        private static float ScrollViewportHeight =>
            VisibleListRowCount * RowHeight + (VisibleListRowCount - 1) * RowGap;

        private static float ComputePanelHeight()
        {
            var header = NavPanelLayout.HeaderHeight;
            var quickRowsBlock = QuickRowsBlockHeight + 6f;
            var searchBlock = SearchBarTopMargin + SearchBarHeight;
            var footer = FooterTopMargin + NavPanelLayout.ButtonHeight + NavPanelLayout.BodyBottomPadding;
            var pickHint = 20f;
            return header + NavPanelLayout.BodyTopPadding + quickRowsBlock + searchBlock + pickHint +
                   ScrollViewportHeight + footer;
        }

        private static float SearchBarTopOffset() =>
            BodyContentTop + QuickRowsBlockHeight + 6f + SearchBarTopMargin;

        private static float BookmarkListTopOffset() =>
            SearchBarTopOffset() + SearchBarHeight + 22f;

        private static void AnchorBottomLeft(RectTransform panel)
        {
            panel.anchorMin = panel.anchorMax = Vector2.zero;
            panel.pivot = Vector2.zero;
            panel.anchoredPosition = new Vector2(ScreenMarginX, ScreenBottomMargin);
        }

        private static void BuildQuickRows(RectTransform panel, float textScale)
        {
            var listTop = -BodyContentTop;
            var kinds = new[]
            {
                RowKind.LastCar,
                RowKind.LastHome,
                RowKind.LastShop
            };

            for (var i = 0; i < kinds.Length; i++)
            {
                var rowTop = listTop - i * (RowHeight + RowGap);
                var row = CreateRowUi(panel, textScale, "QuickRow" + i, rowTop, showDeleteButton: false);
                row.Kind = kinds[i];
                QuickRows.Add(row);
            }
        }

        private static void BuildSearchBar(RectTransform panel, float textScale)
        {
            var searchGo = CreateRect(panel, "SearchBar");
            _searchBarRect = searchGo;
            searchGo.anchorMin = new Vector2(0f, 1f);
            searchGo.anchorMax = new Vector2(1f, 1f);
            searchGo.pivot = new Vector2(0.5f, 1f);
            searchGo.sizeDelta = new Vector2(-ContentHorizontalInset, SearchBarHeight);

            var bgGo = CreateRect(searchGo, "Background");
            Stretch(bgGo);
            bgGo.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);

            var textAreaGo = CreateRect(searchGo, "TextArea");
            textAreaGo.anchorMin = Vector2.zero;
            textAreaGo.anchorMax = Vector2.one;
            textAreaGo.offsetMin = new Vector2(8f, 4f);
            textAreaGo.offsetMax = new Vector2(-8f, -4f);

            var placeholderGo = CreateRect(textAreaGo, "Placeholder");
            Stretch(placeholderGo);
            _searchPlaceholder = placeholderGo.gameObject.AddComponent<TextMeshProUGUI>();
            _searchPlaceholder.fontSize = 14f * textScale;
            _searchPlaceholder.color = new Color(1f, 1f, 1f, 0.45f);
            _searchPlaceholder.fontStyle = FontStyles.Italic;
            _searchPlaceholder.alignment = TextAlignmentOptions.MidlineLeft;
            GameUiStyle.ApplyButtonFont(_searchPlaceholder);

            var textGo = CreateRect(textAreaGo, "Text");
            Stretch(textGo);
            var textLabel = textGo.gameObject.AddComponent<TextMeshProUGUI>();
            textLabel.fontSize = 14f * textScale;
            textLabel.color = GameUiStyle.BodyTextColor;
            textLabel.alignment = TextAlignmentOptions.MidlineLeft;
            GameUiStyle.ApplyButtonFont(textLabel);

            _searchField = searchGo.gameObject.AddComponent<TMP_InputField>();
            _searchField.textViewport = textAreaGo;
            _searchField.textComponent = textLabel;
            _searchField.placeholder = _searchPlaceholder;
            _searchField.lineType = TMP_InputField.LineType.SingleLine;
            _searchField.onValueChanged.AddListener(OnSearchChanged);
            _searchField.onSelect.AddListener(_ => OnSearchFieldSelected());

            var guard = searchGo.gameObject.AddComponent<InputHotkeyGuard>();
            guard.Bind(_searchField);
        }

        private static void BuildPickHint(RectTransform panel, float textScale)
        {
            var hintGo = CreateRect(panel, "PickHint");
            _pickHintRect = hintGo;
            hintGo.anchorMin = new Vector2(0f, 1f);
            hintGo.anchorMax = new Vector2(1f, 1f);
            hintGo.pivot = new Vector2(0.5f, 1f);
            hintGo.sizeDelta = new Vector2(-ContentHorizontalInset, 18f);
            _pickHintLabel = hintGo.gameObject.AddComponent<TextMeshProUGUI>();
            _pickHintLabel.fontSize = 13f * textScale;
            _pickHintLabel.color = new Color(0.9f, 0.75f, 0.35f, 1f);
            _pickHintLabel.alignment = TextAlignmentOptions.MidlineLeft;
            _pickHintLabel.fontStyle = FontStyles.Italic;
            GameUiStyle.ApplyButtonFont(_pickHintLabel);
            _pickHintLabel.gameObject.SetActive(false);
        }

        private static void BuildListScroll(RectTransform panel)
        {
            var scrollGo = CreateRect(panel, "ListScroll");
            _listScrollRect = scrollGo;
            scrollGo.anchorMin = new Vector2(0f, 1f);
            scrollGo.anchorMax = new Vector2(1f, 1f);
            scrollGo.pivot = new Vector2(0.5f, 1f);

            _listScroll = scrollGo.gameObject.AddComponent<ScrollRect>();
            _listScroll.horizontal = false;
            _listScroll.vertical = true;
            _listScroll.movementType = ScrollRect.MovementType.Clamped;
            _listScroll.scrollSensitivity = 24f;

            var viewport = CreateRect(scrollGo, "Viewport");
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            _listScroll.viewport = viewport;

            var content = CreateRect(viewport, "Content");
            _listScrollContent = content;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            _listScroll.content = content;
        }

        private static void SyncBookmarkRows()
        {
            if (_listScrollContent == null)
                return;

            var needed = BookmarkStore.All.Count;
            while (Rows.Count < needed)
            {
                var row = CreateRowUi(
                    _listScrollContent,
                    _textScale,
                    "Row" + Rows.Count,
                    0f,
                    showDeleteButton: true);
                row.Kind = RowKind.Bookmark;
                Rows.Add(row);
            }

            while (Rows.Count > needed)
            {
                var last = Rows.Count - 1;
                if (Rows[last].Root != null)
                    Object.Destroy(Rows[last].Root);
                Rows.RemoveAt(last);
            }
        }

        private static void SyncVehicleRows()
        {
            if (_listScrollContent == null)
                return;

            PlayerVehicleBookmarkStore.Refresh();
            var needed = PlayerVehicleBookmarkStore.Count;

            while (VehicleRows.Count < needed)
            {
                var index = VehicleRows.Count;
                var row = CreateRowUi(
                    _listScrollContent,
                    _textScale,
                    "VehicleRow" + index,
                    0f,
                    showDeleteButton: false);
                row.Kind = RowKind.Vehicle;
                VehicleRows.Add(row);
            }

            while (VehicleRows.Count > needed)
            {
                var last = VehicleRows.Count - 1;
                if (VehicleRows[last].Root != null)
                    Object.Destroy(VehicleRows[last].Root);
                VehicleRows.RemoveAt(last);
            }
        }

        private static float ContentRowsBlockHeight(int rowCount) =>
            rowCount <= 0 ? 0f : rowCount * RowHeight + (rowCount - 1) * RowGap;

        private static void LayoutListContent()
        {
            if (_listScrollContent == null)
                return;

            var y = 0f;
            var activeCount = 0;

            for (var i = 0; i < VehicleRows.Count; i++)
            {
                var row = VehicleRows[i];
                if (row?.Root == null || !row.Root.activeSelf)
                    continue;

                RepositionRow(row, -y, insideScroll: true);
                y += RowHeight + RowGap;
                activeCount++;
            }

            for (var i = 0; i < Rows.Count; i++)
            {
                var row = Rows[i];
                if (row?.Root == null || !row.Root.activeSelf)
                    continue;

                RepositionRow(row, -y, insideScroll: true);
                y += RowHeight + RowGap;
                activeCount++;
            }

            _listScrollContent.sizeDelta = new Vector2(0f, ContentRowsBlockHeight(activeCount));
        }

        private static void ApplyPanelLayout()
        {
            if (_panelRect == null)
                return;

            SyncVehicleRows();
            SyncBookmarkRows();
            _panelRect.sizeDelta = new Vector2(PanelWidth, ComputePanelHeight());
            GameStylePanelChrome.RestorePanelChrome(
                _panelRect,
                PanelWidth,
                NavPanelLayout.ComputeWideMapPanelHeaderWidenTrim(PanelWidth));

            if (_searchBarRect != null)
                _searchBarRect.anchoredPosition = new Vector2(0f, -SearchBarTopOffset());

            if (_pickHintRect != null)
                _pickHintRect.anchoredPosition = new Vector2(0f, -(SearchBarTopOffset() + SearchBarHeight + 4f));

            var listTop = BookmarkListTopOffset();
            if (_listScrollRect != null)
            {
                _listScrollRect.anchoredPosition = new Vector2(0f, -listTop);
                _listScrollRect.sizeDelta = new Vector2(-ContentHorizontalInset, ScrollViewportHeight);
            }

            var footerY = -(listTop + ScrollViewportHeight + FooterTopMargin);
            if (_addFooterRect != null)
                _addFooterRect.anchoredPosition = new Vector2(LeftButtonX, footerY);
            if (_clearFooterRect != null)
                _clearFooterRect.anchoredPosition = new Vector2(RightButtonX, footerY);
        }

        private static void RepositionRow(RowUi row, float rowTop, bool insideScroll = false)
        {
            if (row?.Root == null)
                return;

            var rowRect = row.Root.GetComponent<RectTransform>();
            rowRect.anchoredPosition = new Vector2(0f, rowTop);
            if (insideScroll)
                rowRect.sizeDelta = new Vector2(0f, RowHeight);
        }

        private static RowUi CreateRowUi(
            RectTransform panel,
            float textScale,
            string name,
            float rowTop,
            bool showDeleteButton)
        {
            var row = new RowUi();
            row.Root = CreateRect(panel, name).gameObject;
            var rowRect = row.Root.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, rowTop);
            rowRect.sizeDelta = new Vector2(-ContentHorizontalInset, RowHeight);

            var buttonHeight = RowHeight - RowButtonPadY * 2f;

            var iconGo = CreateRect(rowRect, "TypeIcon");
            iconGo.anchorMin = iconGo.anchorMax = new Vector2(0f, 0.5f);
            iconGo.pivot = new Vector2(0f, 0.5f);
            iconGo.anchoredPosition = Vector2.zero;
            iconGo.sizeDelta = new Vector2(RowTypeIconSize, RowTypeIconSize);
            row.TypeIconRoot = iconGo.gameObject;

            var iconFgGo = CreateRect(iconGo, "Foreground");
            Stretch(iconFgGo);
            iconFgGo.offsetMin = new Vector2(2f, 2f);
            iconFgGo.offsetMax = new Vector2(-2f, -2f);
            row.TypeIcon = iconFgGo.gameObject.AddComponent<Image>();
            row.TypeIcon.raycastTarget = false;
            row.TypeIcon.preserveAspect = true;
            row.TypeIcon.color = Color.white;
            row.TypeIconRoot.SetActive(false);

            var nameGo = CreateRect(rowRect, "Name");
            nameGo.anchorMin = Vector2.zero;
            nameGo.anchorMax = new Vector2(1f, 1f);
            var nameRightInset = ComputeRowNameRightInset(showDeleteButton);
            nameGo.offsetMin = new Vector2(RowTypeIconSize + 4f, 0f);
            nameGo.offsetMax = new Vector2(-nameRightInset, 0f);
            row.NameLabel = nameGo.gameObject.AddComponent<TextMeshProUGUI>();
            row.NameLabel.fontSize = 13f * textScale;
            row.NameLabel.color = GameUiStyle.BodyTextColor;
            row.NameLabel.alignment = TextAlignmentOptions.MidlineLeft;
            row.NameLabel.overflowMode = TextOverflowModes.Ellipsis;
            row.NameLabel.enableWordWrapping = false;
            GameUiStyle.ApplyButtonFont(row.NameLabel);

            var distGo = CreateRect(rowRect, "Distance");
            distGo.anchorMin = new Vector2(1f, 0f);
            distGo.anchorMax = new Vector2(1f, 1f);
            distGo.pivot = new Vector2(1f, 0.5f);
            distGo.anchoredPosition = new Vector2(-ComputeRowDistanceRightInset(showDeleteButton), 0f);
            distGo.sizeDelta = new Vector2(RowDistanceWidth, 0f);
            row.DistanceLabel = distGo.gameObject.AddComponent<TextMeshProUGUI>();
            row.DistanceLabel.fontSize = 12f * textScale;
            row.DistanceLabel.color = GameUiStyle.MutedBodyTextColor;
            row.DistanceLabel.alignment = TextAlignmentOptions.MidlineRight;
            row.DistanceLabel.overflowMode = TextOverflowModes.Overflow;
            GameUiStyle.ApplyButtonFont(row.DistanceLabel);

            var centerGo = CreateRect(rowRect, "CenterButton");
            LayoutRowButton(
                centerGo,
                RowSetButtonWidth + RowButtonGap + RowSetButtonWidth + RowButtonGap +
                (showDeleteButton ? RowActionButtonSize + RowButtonGap : 0f),
                RowActionButtonSize,
                buttonHeight);
            row.CenterButtonImage = GameUiStyle.CreateButtonGraphic(
                centerGo, textScale, GameUiStyle.ApplyButtonBlue, 1f, bleedBottom: false);
            row.CenterButton = centerGo.gameObject.AddComponent<Button>();
            row.CenterButton.targetGraphic = row.CenterButtonImage;
            GameUiStyle.BindButtonClick(row.CenterButton, () => OnCenterClicked(row));

            var centerIconGo = CreateRect(centerGo, "Icon");
            Stretch(centerIconGo);
            var iconPad = 6f * textScale;
            centerIconGo.offsetMin = new Vector2(iconPad, iconPad);
            centerIconGo.offsetMax = new Vector2(-iconPad, -iconPad);
            var centerIcon = centerIconGo.gameObject.AddComponent<Image>();
            centerIcon.raycastTarget = false;
            if (!GameUiStyle.TryApplyOverlayIcon(centerIcon, GameUiStyle.ApplyFocusIcon))
            {
                var fallbackGo = CreateRect(centerIconGo, "Fallback");
                Stretch(fallbackGo);
                row.CenterFallbackLabel = fallbackGo.gameObject.AddComponent<TextMeshProUGUI>();
                row.CenterFallbackLabel.text = "\u2295";
                row.CenterFallbackLabel.fontSize = 14f * textScale;
                row.CenterFallbackLabel.alignment = TextAlignmentOptions.Center;
                row.CenterFallbackLabel.color = Color.white;
                row.CenterFallbackLabel.raycastTarget = false;
                GameUiStyle.ApplyButtonFont(row.CenterFallbackLabel);
            }

            var btnGo = CreateRect(rowRect, "SetDestButton");
            LayoutRowButton(
                btnGo,
                showDeleteButton ? RowActionButtonSize + RowButtonGap : 0f,
                RowSetButtonWidth,
                buttonHeight);
            var btnImg = GameUiStyle.CreateButtonGraphic(btnGo, textScale, GameUiStyle.ApplyButtonBlue, bleedBottom: false);
            row.SetDestButton = btnGo.gameObject.AddComponent<Button>();
            row.SetDestButton.targetGraphic = btnImg;
            GameUiStyle.BindButtonClick(row.SetDestButton, () => OnSetDestinationClicked(row));

            var btnLabelGo = CreateRect(btnGo, "Label");
            Stretch(btnLabelGo);
            row.SetDestLabel = btnLabelGo.gameObject.AddComponent<TextMeshProUGUI>();
            row.SetDestLabel.fontSize = 11f * textScale;
            row.SetDestLabel.fontStyle = FontStyles.UpperCase;
            row.SetDestLabel.alignment = TextAlignmentOptions.Center;
            row.SetDestLabel.color = Color.white;
            row.SetDestLabel.raycastTarget = false;
            GameUiStyle.ApplyButtonFont(row.SetDestLabel);

            var driveGo = CreateRect(rowRect, "DriveButton");
            LayoutRowButton(
                driveGo,
                RowSetButtonWidth + RowButtonGap + (showDeleteButton ? RowActionButtonSize + RowButtonGap : 0f),
                RowSetButtonWidth,
                buttonHeight);
            var driveImg = GameUiStyle.CreateButtonGraphic(driveGo, textScale, GameUiStyle.ApplyButtonGreen, bleedBottom: false);
            row.DriveButton = driveGo.gameObject.AddComponent<Button>();
            row.DriveButton.targetGraphic = driveImg;
            GameUiStyle.BindButtonClick(row.DriveButton, () => OnNavigateClicked(row));

            var driveLabelGo = CreateRect(driveGo, "Label");
            Stretch(driveLabelGo);
            row.DriveLabel = driveLabelGo.gameObject.AddComponent<TextMeshProUGUI>();
            row.DriveLabel.fontSize = 11f * textScale;
            row.DriveLabel.fontStyle = FontStyles.UpperCase;
            row.DriveLabel.alignment = TextAlignmentOptions.Center;
            row.DriveLabel.color = Color.white;
            row.DriveLabel.raycastTarget = false;
            GameUiStyle.ApplyButtonFont(row.DriveLabel);
            row.DriveLabel.text = ResolveMapActionLabel();

            if (showDeleteButton)
            {
                row.DeleteButton = GameUiStyle.CreateRowCloseButton(
                    rowRect, textScale, () => OnDeleteClicked(row));
                LayoutRowButton(row.DeleteButton.GetComponent<RectTransform>(), 0f, RowActionButtonSize, buttonHeight);
            }

            return row;
        }

        private static float ComputeRowActionsWidth(bool showDeleteButton) =>
            RowSetButtonWidth + RowButtonGap + RowSetButtonWidth + RowButtonGap + RowActionButtonSize +
            (showDeleteButton ? RowButtonGap + RowActionButtonSize : 0f);

        private static float ComputeRowDistanceRightInset(bool showDeleteButton) =>
            ComputeRowActionsWidth(showDeleteButton) + RowDistanceToCenterGap;

        private static float ComputeRowNameRightInset(bool showDeleteButton) =>
            ComputeRowDistanceRightInset(showDeleteButton) + RowDistanceWidth + RowNameToDistanceGap;

        private static void LayoutRowButton(RectTransform rect, float rightInset, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-rightInset, 0f);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void BuildFooter(RectTransform panel, float textScale)
        {
            var addGo = CreateRect(panel, "AddButton");
            _addFooterRect = addGo;
            addGo.anchorMin = addGo.anchorMax = new Vector2(0.5f, 1f);
            addGo.pivot = new Vector2(0.5f, 1f);
            addGo.sizeDelta = new Vector2(HalfButtonWidth, NavPanelLayout.ButtonHeight);
            var addImg = GameUiStyle.CreateButtonGraphic(addGo, textScale, GameUiStyle.ApplyButtonBlue);
            var addBtn = addGo.gameObject.AddComponent<Button>();
            addBtn.targetGraphic = addImg;
            GameUiStyle.BindButtonClick(addBtn, OnAddBookmarkClicked);
            _addButtonLabel = CreateButtonLabel(addGo, textScale);

            var clearGo = CreateRect(panel, "ClearButton");
            _clearFooterRect = clearGo;
            clearGo.anchorMin = clearGo.anchorMax = new Vector2(0.5f, 1f);
            clearGo.pivot = new Vector2(0.5f, 1f);
            clearGo.sizeDelta = new Vector2(HalfButtonWidth, NavPanelLayout.ButtonHeight);
            var clearImg = GameUiStyle.CreateButtonGraphic(clearGo, textScale, GameUiStyle.ApplyButtonRed);
            var clearBtn = clearGo.gameObject.AddComponent<Button>();
            clearBtn.targetGraphic = clearImg;
            GameUiStyle.BindButtonClick(clearBtn, OnClearAllClicked);
            _clearButtonLabel = CreateButtonLabel(clearGo, textScale);
            ApplyPanelLayout();
        }

        private static void CreateHeaderHistoryButton(RectTransform header, float scale)
        {
            var size = NavPanelLayout.HeaderIconButtonSize * scale;
            var rightInset = NavPanelLayout.ComputeHeaderIconRightInset(0, scale);

            var rect = CreateRect(header, "HistoryButton");
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = new Vector2(-rightInset, NavPanelLayout.SettingsIconOffsetY * scale);

            var buttonImage = GameUiStyle.CreateButtonGraphic(
                rect, scale, GameUiStyle.ApplyButtonGrey, 1f, bleedBottom: false);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            GameUiStyle.BindButtonClick(button, (UnityAction)(() => VisitHistoryPanel.Toggle()));

            var iconGo = CreateRect(rect, "Icon");
            Stretch(iconGo);
            iconGo.offsetMin = new Vector2(7f * scale, 7f * scale);
            iconGo.offsetMax = new Vector2(-7f * scale, -7f * scale);
            var icon = iconGo.gameObject.AddComponent<Image>();
            icon.raycastTarget = false;
            if (!GameUiStyle.TryApplyOverlayIcon(icon, GameUiStyle.ApplyHistoryIcon))
            {
                var fallbackGo = CreateRect(iconGo, "Fallback");
                Stretch(fallbackGo);
                var fallback = fallbackGo.gameObject.AddComponent<TextMeshProUGUI>();
                fallback.text = "\u23F1";
                fallback.fontSize = 18f * scale;
                fallback.alignment = TextAlignmentOptions.Center;
                fallback.color = Color.white;
                fallback.raycastTarget = false;
                GameUiStyle.ApplyTitleFont(fallback);
            }
        }

        private static TextMeshProUGUI CreateButtonLabel(RectTransform button, float scale)
        {
            var labelGo = CreateRect(button, "Label");
            Stretch(labelGo);
            var tmp = labelGo.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 14f * scale;
            tmp.fontStyle = FontStyles.UpperCase;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            GameUiStyle.ApplyButtonFont(tmp);
            return tmp;
        }

        internal static void Tick()
        {
            if (GameState.IsSubwayNavigationActive())
            {
                SuppressForSubwayNavigation();
                return;
            }

            var shouldShow = GameState.ShouldShowCityMapBookmarks();
            if (_root == null)
                return;

            if (shouldShow != _root.activeSelf)
            {
                _root.SetActive(shouldShow);
                if (shouldShow)
                {
                    _lastMapActionMode = MovementMode.Unavailable;
                    RefreshList(fullDistanceRefresh: true);
                }
                else
                    CancelPickMode();
            }

            if (!shouldShow)
            {
                BookmarkRouteDistanceService.Cancel();
                return;
            }

            RefreshMapActionModeIfChanged();

            if (!BlocksMapInput)
                MaintainMapNavigationSelection();

            TickDistanceResults();
        }

        internal static void RefreshList(bool fullDistanceRefresh = false, int addedBookmarkIndex = -1)
        {
            ApplyPanelLayout();
            RefreshQuickRows();
            RefreshVehicleRows();
            RefreshBookmarkRows();
            LayoutListContent();
            RefreshDistances(fullDistanceRefresh, addedBookmarkIndex);
            RefreshPickHint();
        }

        private static void RefreshVehicleRows()
        {
            for (var i = 0; i < VehicleRows.Count; i++)
            {
                var ui = VehicleRows[i];
                ui.VehicleIndex = i;
                if (!PlayerVehicleBookmarkStore.TryGetAt(i, out var bookmark))
                {
                    ui.Root.SetActive(false);
                    continue;
                }

                var visible = bookmark.MatchesFilter(_searchFilter);
                ui.Root.SetActive(visible);
                if (!visible)
                    continue;

                ui.NameLabel.text = bookmark.DisplayName;
                ui.SetDestButton.interactable = CanSetDestination(RowKind.Vehicle, bookmark);
                ApplyNavigateButtonState(ui, RowKind.Vehicle, bookmark);
                ui.CenterButton.interactable = true;
                ui.NameLabel.color = GameUiStyle.BodyTextColor;
                ApplyRowDistanceLabel(ui);
                RefreshRowTypeIcon(ui, bookmark);
            }
        }

        private static void RefreshBookmarkRows()
        {
            for (var i = 0; i < Rows.Count; i++)
            {
                var ui = Rows[i];
                ui.Kind = RowKind.Bookmark;
                ui.BookmarkIndex = i;
                var bookmark = BookmarkStore.GetAt(i);
                if (bookmark == null || !bookmark.MatchesFilter(_searchFilter))
                {
                    ui.Root.SetActive(false);
                    continue;
                }

                ui.Root.SetActive(true);
                ui.NameLabel.text = bookmark.DisplayName;
                ui.SetDestButton.interactable = CanSetDestination(RowKind.Bookmark, bookmark);
                ApplyNavigateButtonState(ui, RowKind.Bookmark, bookmark);
                ApplyRowDistanceLabel(ui);
                RefreshRowTypeIcon(ui, bookmark);
            }
        }

        private static void RefreshQuickRows()
        {
            var mutedName = new Color(0.55f, 0.58f, 0.62f, 1f);
            for (var i = 0; i < QuickRows.Count; i++)
            {
                var ui = QuickRows[i];
                ui.NameLabel.text = GetQuickRowTitle(ui.Kind);
                var hasData = TryGetRowBookmark(ui, out var bookmark);
                ui.NameLabel.color = hasData ? GameUiStyle.BodyTextColor : mutedName;
                ui.CenterButton.interactable = hasData;
                ui.SetDestButton.interactable = hasData && CanSetDestination(ui.Kind, bookmark);
                ApplyNavigateButtonState(ui, ui.Kind, hasData ? bookmark : null);
                ApplyRowDistanceLabel(ui, hasData);
                RefreshRowTypeIcon(ui, bookmark);
            }
        }

        private static string GetQuickRowTitle(RowKind kind) =>
            kind switch
            {
                RowKind.LastCar => ModUiText.QuickBookmarkLastCar,
                RowKind.LastHome => ModUiText.QuickBookmarkLastHome,
                RowKind.LastShop => ModUiText.QuickBookmarkLastShop,
                _ => ""
            };

        private static void RefreshRowTypeIcon(RowUi ui, BookmarkEntry bookmark)
        {
            if (ui?.TypeIconRoot == null)
                return;

            var hasIcon = ui.Kind switch
            {
                RowKind.LastCar => BookmarkRowIconResolver.TryGetForQuickRow(
                    QuickBookmarkKind.LastCar, out var carIcon) && ApplyRowTypeIcon(ui, carIcon),
                RowKind.LastHome => BookmarkRowIconResolver.TryGetForQuickRow(
                    QuickBookmarkKind.LastHome, out var homeIcon) && ApplyRowTypeIcon(ui, homeIcon),
                RowKind.LastShop => BookmarkRowIconResolver.TryGetForQuickRow(
                    QuickBookmarkKind.LastShop, out var shopIcon) && ApplyRowTypeIcon(ui, shopIcon),
                RowKind.Vehicle => BookmarkRowIconResolver.TryGetForVehicleRow(out var vehicleIcon) &&
                                    ApplyRowTypeIcon(ui, vehicleIcon),
                _ => bookmark != null &&
                     BookmarkRowIconResolver.TryGetForBookmark(bookmark, out var bookmarkIcon) &&
                     ApplyRowTypeIcon(ui, bookmarkIcon)
            };

            if (!hasIcon)
                ui.TypeIconRoot.SetActive(false);
        }

        private static bool ApplyRowTypeIcon(RowUi ui, BookmarkRowIcon rowIcon)
        {
            if (!rowIcon.HasIcon)
                return false;

            ui.TypeIconRoot.SetActive(true);
            ui.TypeIcon.sprite = rowIcon.Icon;
            ui.TypeIcon.enabled = true;
            return true;
        }

        private static void RefreshDistances(bool fullDistanceRefresh = false, int addedBookmarkIndex = -1)
        {
            if (fullDistanceRefresh)
            {
                DistanceCache.Clear();
                RequestDistanceRefresh(BookmarkRouteDistanceService.RequestRefresh);
                return;
            }

            if (addedBookmarkIndex >= 0)
            {
                RequestDistanceForBookmark(addedBookmarkIndex);
                return;
            }

            RequestDistanceRefresh(BookmarkRouteDistanceService.RequestCompute);
        }

        private static void ApplyRowDistanceLabel(RowUi ui, bool hasData = true)
        {
            if (ui?.DistanceLabel == null)
                return;

            if (!hasData || IsInactiveListRow(ui))
            {
                ui.DistanceLabel.text = "—";
                return;
            }

            var key = ToDistanceRowKey(ui);
            if (DistanceCache.TryGetValue(key, out var cached))
                ui.DistanceLabel.text = cached;
            else if (BookmarkRouteDistanceService.IsKeyPending(key))
                ui.DistanceLabel.text = "…";
            else if (TryGetRowBookmark(ui, out var bookmark) && bookmark != null)
                ui.DistanceLabel.text = "…";
            else
                ui.DistanceLabel.text = "—";
        }

        private static void RequestDistanceForBookmark(int bookmarkIndex)
        {
            var bookmark = BookmarkStore.GetAt(bookmarkIndex);
            if (bookmark == null)
                return;

            var key = new BookmarkDistanceRowKey
            {
                Kind = BookmarkDistanceRowKind.Bookmark,
                BookmarkIndex = bookmarkIndex
            };

            if (DistanceCache.ContainsKey(key) || BookmarkRouteDistanceService.IsKeyPending(key))
                return;

            if (TryFindRow(key, out var ui))
                ui.DistanceLabel.text = "…";

            DistanceRequests.Clear();
            DistanceRequests.Add((key, bookmark));
            BookmarkRouteDistanceService.RequestCompute(DistanceRequests);
        }

        private static void InvalidateBookmarkDistanceCache()
        {
            var keys = new List<BookmarkDistanceRowKey>();
            foreach (var key in DistanceCache.Keys)
            {
                if (key.Kind == BookmarkDistanceRowKind.Bookmark)
                    keys.Add(key);
            }

            for (var i = 0; i < keys.Count; i++)
                DistanceCache.Remove(keys[i]);
        }

        private static void InvalidateQuickDistanceCache()
        {
            var keys = new List<BookmarkDistanceRowKey>();
            foreach (var key in DistanceCache.Keys)
            {
                if (key.Kind is BookmarkDistanceRowKind.LastCar
                    or BookmarkDistanceRowKind.LastHome
                    or BookmarkDistanceRowKind.LastShop)
                    keys.Add(key);
            }

            for (var i = 0; i < keys.Count; i++)
                DistanceCache.Remove(keys[i]);
        }

        private static bool IsInactiveListRow(RowUi ui) =>
            ui == null ||
            !ui.Root.activeSelf ||
            (ui.Kind == RowKind.Bookmark && ui.BookmarkIndex < 0) ||
            (ui.Kind == RowKind.Vehicle && ui.VehicleIndex < 0);

        private static void QueueDistanceRefresh(List<RowUi> rows)
        {
            for (var i = 0; i < rows.Count; i++)
            {
                var ui = rows[i];
                if (IsInactiveListRow(ui))
                    continue;

                if (!TryGetRowBookmark(ui, out var bookmark) || bookmark == null)
                    continue;

                var key = ToDistanceRowKey(ui);
                if (DistanceCache.ContainsKey(key) || BookmarkRouteDistanceService.IsKeyPending(key))
                    continue;

                ui.DistanceLabel.text = "…";
                DistanceRequests.Add((key, bookmark));
            }
        }

        private static void RequestDistanceRefresh(System.Action<IReadOnlyList<(BookmarkDistanceRowKey Key, BookmarkEntry Bookmark)>> dispatch)
        {
            DistanceRequests.Clear();
            QueueDistanceRefresh(QuickRows);
            QueueDistanceRefresh(VehicleRows);
            QueueDistanceRefresh(Rows);
            if (DistanceRequests.Count == 0)
                return;

            dispatch(DistanceRequests);
            DistanceRequests.Clear();
        }

        private static void TickDistanceResults()
        {
            while (BookmarkRouteDistanceService.TryDequeueCompleted(out var result))
                ApplyDistanceResult(result);

            if (!BookmarkRouteDistanceService.IsRecalcInProgress)
                RouteRecalcBanner.RequestHide();
        }

        private static void ApplyDistanceResult(BookmarkDistanceResult result)
        {
            var text = result.Success
                ? BookmarkRouteDistance.FormatDistance(result.Meters)
                : "—";
            DistanceCache[result.Key] = text;

            if (!TryFindRow(result.Key, out var ui) || ui?.DistanceLabel == null)
                return;

            ui.DistanceLabel.text = text;
        }

        private static bool TryFindRow(BookmarkDistanceRowKey key, out RowUi row)
        {
            row = null;
            if (key.Kind == BookmarkDistanceRowKind.Bookmark)
            {
                for (var i = 0; i < Rows.Count; i++)
                {
                    var ui = Rows[i];
                    if (ui.Kind == RowKind.Bookmark && ui.BookmarkIndex == key.BookmarkIndex)
                    {
                        row = ui;
                        return true;
                    }
                }

                return false;
            }

            if (key.Kind == BookmarkDistanceRowKind.Vehicle)
            {
                for (var i = 0; i < VehicleRows.Count; i++)
                {
                    var ui = VehicleRows[i];
                    if (ui.Kind == RowKind.Vehicle && ui.VehicleIndex == key.BookmarkIndex)
                    {
                        row = ui;
                        return true;
                    }
                }

                return false;
            }

            var quickKind = key.Kind switch
            {
                BookmarkDistanceRowKind.LastCar => RowKind.LastCar,
                BookmarkDistanceRowKind.LastHome => RowKind.LastHome,
                BookmarkDistanceRowKind.LastShop => RowKind.LastShop,
                _ => RowKind.Bookmark
            };

            for (var i = 0; i < QuickRows.Count; i++)
            {
                if (QuickRows[i].Kind == quickKind)
                {
                    row = QuickRows[i];
                    return true;
                }
            }

            return false;
        }

        private static BookmarkDistanceRowKey ToDistanceRowKey(RowUi ui) =>
            new BookmarkDistanceRowKey
            {
                Kind = ui.Kind switch
                {
                    RowKind.LastCar => BookmarkDistanceRowKind.LastCar,
                    RowKind.LastHome => BookmarkDistanceRowKind.LastHome,
                    RowKind.LastShop => BookmarkDistanceRowKind.LastShop,
                    RowKind.Vehicle => BookmarkDistanceRowKind.Vehicle,
                    _ => BookmarkDistanceRowKind.Bookmark
                },
                BookmarkIndex = ui.Kind == RowKind.Vehicle ? ui.VehicleIndex : ui.BookmarkIndex
            };

        internal static void RefreshLocalizedText()
        {
            if (_titleLabel != null)
                _titleLabel.text = ModUiText.BookmarksTitle;
            if (_searchPlaceholder != null)
                _searchPlaceholder.text = ModUiText.BookmarksSearchPlaceholder;
            if (_addButtonLabel != null)
                _addButtonLabel.text = ModUiText.BookmarksAdd;
            if (_clearButtonLabel != null)
                _clearButtonLabel.text = ModUiText.BookmarksClearAll;

            RefreshRowButtonLabels(QuickRows);
            RefreshRowButtonLabels(VehicleRows);
            RefreshRowButtonLabels(Rows);
            RefreshActionButtonLabels();

            RefreshPickHint();
        }

        private static void RefreshMapActionModeIfChanged()
        {
            var mode = BookmarkQuickNavService.IsVehicleMapMode
                ? MovementMode.Vehicle
                : MovementMode.OnFoot;

            if (mode == _lastMapActionMode)
                return;

            _lastMapActionMode = mode;
            RefreshActionButtonLabels();
            RefreshNavigateButtonStates();
        }

        private static string ResolveMapActionLabel() =>
            BookmarkQuickNavService.IsVehicleMapMode
                ? ModUiText.BookmarksDrive
                : ModUiText.BookmarksWalk;

        private static void RefreshActionButtonLabels()
        {
            var label = ResolveMapActionLabel();
            RefreshActionButtonLabels(QuickRows, label);
            RefreshActionButtonLabels(VehicleRows, label);
            RefreshActionButtonLabels(Rows, label);
        }

        private static void RefreshActionButtonLabels(List<RowUi> rows, string label)
        {
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].DriveLabel != null)
                    rows[i].DriveLabel.text = label;
            }
        }

        private static void RefreshNavigateButtonStates()
        {
            for (var i = 0; i < QuickRows.Count; i++)
            {
                TryGetRowBookmark(QuickRows[i], out var bookmark);
                ApplyNavigateButtonState(QuickRows[i], QuickRows[i].Kind, bookmark);
            }

            for (var i = 0; i < VehicleRows.Count; i++)
            {
                PlayerVehicleBookmarkStore.TryGetAt(i, out var bookmark);
                ApplyNavigateButtonState(VehicleRows[i], RowKind.Vehicle, bookmark);
            }

            for (var i = 0; i < Rows.Count; i++)
            {
                ApplyNavigateButtonState(Rows[i], RowKind.Bookmark, BookmarkStore.GetAt(i));
            }
        }

        private static void RefreshRowButtonLabels(List<RowUi> rows)
        {
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].SetDestLabel != null)
                    rows[i].SetDestLabel.text = ModUiText.BookmarksSetDestination;
            }
        }

        private static void ApplyNavigateButtonState(RowUi ui, RowKind kind, BookmarkEntry bookmark)
        {
            if (ui?.DriveButton == null)
                return;

            ui.DriveButton.interactable = CanNavigateToRow(kind, bookmark);
        }

        private static bool CanNavigateToRow(RowKind kind, BookmarkEntry bookmark)
        {
            if (BookmarkQuickNavService.IsVehicleMapMode && AutoDriveSkipTravelService.IsInProgress)
                return false;

            return CanSetDestination(kind, bookmark);
        }

        private static void RefreshPickHint()
        {
            if (_pickHintLabel == null)
                return;

            _pickHintLabel.gameObject.SetActive(_pickMode);
            if (_pickMode)
                _pickHintLabel.text = ModUiText.BookmarksPickHint;
        }

        internal static void BeginPickMode()
        {
            if (!BookmarkStore.CanAdd())
                return;

            _pickMode = true;
            ModUiFocus.ReleaseForMovement();
            RefreshPickHint();
        }

        /// <summary>
        /// CityMapCam skips pan/zoom while GameManager.HasInputSelected (UI layer selection).
        /// Clear stray button focus so WASD/arrows still move the map.
        /// </summary>
        private static void MaintainMapNavigationSelection()
        {
            if (IsTextInputSelected())
                return;

            ModUiFocus.ReleaseForMovement();
        }

        private static bool IsTextInputSelected()
        {
            var selected = EventSystem.current?.currentSelectedGameObject;
            return selected != null && selected.GetComponentInParent<TMP_InputField>() != null;
        }

        internal static void CancelPickMode()
        {
            _pickMode = false;
            RefreshPickHint();
        }

        private static void OnSearchChanged(string value)
        {
            _searchFilter = value ?? "";
            ApplyPanelLayout();
            RefreshQuickRows();
            RefreshVehicleRows();
            RefreshBookmarkRows();
            RefreshPickHint();
        }

        private static void OnSearchFieldSelected()
        {
            if (_searchField == null || EventSystem.current == null)
                return;

            EventSystem.current.SetSelectedGameObject(_searchField.gameObject);
        }

        private static void OnAddBookmarkClicked()
        {
            if (!BookmarkStore.CanAdd())
                return;

            BeginPickMode();
        }

        private static void OnClearAllClicked()
        {
            BookmarkStore.ClearAll();
            InvalidateBookmarkDistanceCache();
            RefreshList();
        }

        private static void OnDeleteClicked(RowUi row)
        {
            if (row == null || row.Kind != RowKind.Bookmark || row.BookmarkIndex < 0)
                return;

            BookmarkStore.TryRemoveAt(row.BookmarkIndex);
            InvalidateBookmarkDistanceCache();
            RefreshList();
        }

        private static void OnCenterClicked(RowUi row)
        {
            if (!TryGetRowBookmark(row, out var bookmark) || bookmark == null)
                return;

            CityMapBookmarkFocusService.TryFocusBookmark(bookmark);
        }

        private static bool CanSetDestination(RowKind kind, BookmarkEntry bookmark)
        {
            if (bookmark == null)
                return kind == RowKind.LastCar && ParkedVehicleStore.HasParkedPosition;

            return bookmark.TryGetNavigationTarget(out _);
        }

        private static void OnSetDestinationClicked(RowUi row)
        {
            if (!TrySetRowDestination(row))
                return;

            ModLog.Info("Bookmark destination set from panel row.");
        }

        private static void OnNavigateClicked(RowUi row)
        {
            if (!TrySetRowDestination(row))
                return;

            if (BookmarkQuickNavService.IsVehicleMapMode)
                BookmarkQuickNavService.RequestDriveFromBookmark();
            else
                BookmarkQuickNavService.RequestWalkFromBookmark();
        }

        private static bool TrySetRowDestination(RowUi row)
        {
            if (row?.Kind == RowKind.LastCar)
                return BookmarkDestinationService.TrySetLastCar();

            if (!TryGetRowBookmark(row, out var bookmark) || bookmark == null)
                return false;

            return BookmarkDestinationService.TrySetFromBookmark(bookmark);
        }

        private static bool TryGetRowBookmark(RowUi row, out BookmarkEntry bookmark)
        {
            bookmark = null;
            if (row == null)
                return false;

            switch (row.Kind)
            {
                case RowKind.LastCar:
                    return QuickBookmarkStore.TryGet(QuickBookmarkKind.LastCar, out bookmark);
                case RowKind.LastHome:
                    return QuickBookmarkStore.TryGet(QuickBookmarkKind.LastHome, out bookmark);
                case RowKind.LastShop:
                    return QuickBookmarkStore.TryGet(QuickBookmarkKind.LastShop, out bookmark);
                case RowKind.Vehicle:
                    return PlayerVehicleBookmarkStore.TryGetAt(row.VehicleIndex, out bookmark);
                case RowKind.Bookmark:
                    bookmark = BookmarkStore.GetAt(row.BookmarkIndex);
                    return bookmark != null;
                default:
                    return false;
            }
        }

        private static void OnBookmarksChanged()
        {
            var count = BookmarkStore.All.Count;
            var addedOnly = count == _lastBookmarkCount + 1;
            var addedIndex = addedOnly ? count - 1 : -1;
            if (!addedOnly)
                InvalidateBookmarkDistanceCache();

            _lastBookmarkCount = count;
            RefreshList(addedBookmarkIndex: addedIndex);
        }

        private static void OnQuickBookmarksChanged()
        {
            InvalidateQuickDistanceCache();
            RefreshList();
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void DestroyLegacyRoot()
        {
            foreach (var name in new[]
                     {
                         "VoogleRoute_BookmarksPanel",
                         "VoogleRoute_BookmarksPanel_v2",
                         "VoogleRoute_BookmarksPanel_v3",
                         "VoogleRoute_BookmarksPanel_v4",
                         "VoogleRoute_BookmarksPanel_v5",
                         "VoogleRoute_BookmarksPanel_v6",
                         "VoogleRoute_BookmarksPanel_v7",
                         "VoogleRoute_BookmarksPanel_v8",
                         "VoogleRoute_BookmarksPanel_v9",
                         "VoogleRoute_BookmarksPanel_v10",
                         "VoogleRoute_BookmarksPanel_v11",
                         "VoogleRoute_BookmarksPanel_v12",
                         "VoogleRoute_BookmarksPanel_v13",
                         "VoogleRoute_BookmarksPanel_v14",
                         "VoogleRoute_BookmarksPanel_v15",
                         "VoogleRoute_BookmarksPanel_v16",
                         "VoogleRoute_BookmarksPanel_v17",
                         "VoogleRoute_BookmarksPanel_v18",
                         "VoogleRoute_BookmarksPanel_v19"
                     })
            {
                var legacy = GameObject.Find(name);
                if (legacy != null)
                    Object.Destroy(legacy);
            }
        }

        internal static void SuppressForSubwayNavigation()
        {
            CancelPickMode();
            CityMapBookmarkAddDialog.Close();
            BookmarkRouteDistanceService.Cancel();
            if (_root != null)
                _root.SetActive(false);
        }

        internal static RectTransform GetVisualTestPanelRect() =>
            _panelRect != null && _root != null && _root.activeInHierarchy ? _panelRect : null;

        internal static void Destroy()
        {
            BookmarkStore.Changed -= OnBookmarksChanged;
            QuickBookmarkStore.Changed -= OnQuickBookmarksChanged;
            CancelPickMode();

            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }

            QuickRows.Clear();
            VehicleRows.Clear();
            Rows.Clear();
            _panelRect = null;
            _contentPanel = null;
            _searchBarRect = null;
            _pickHintRect = null;
            _addFooterRect = null;
            _clearFooterRect = null;
            _listScrollRect = null;
            _listScrollContent = null;
            _listScroll = null;
            _titleLabel = null;
            _searchField = null;
            _searchPlaceholder = null;
            _pickHintLabel = null;
            _addButtonLabel = null;
            _clearButtonLabel = null;
        }
    }
}
