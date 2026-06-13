using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VoogleRoute;
using VoogleRoute.Navigation;

namespace VoogleRoute.UI
{
    /// <summary>Scrollable list of the 50 most recently visited buildings.</summary>
    internal static class VisitHistoryPanel
    {
        private const string RootName = "VoogleRoute_VisitHistory_v6";
        private const float CloseButtonExtraInset = 5f;
        private const int CanvasSortOrder = 11050;
        private const float PanelWidth = 420f;
        private const float ScreenMarginX = 16f;
        private const float ScreenBottomMargin = NavPanelLayout.ScreenMarginMinY;
        private const float PanelGap = 8f;
        private const int VisibleListRowCount = 10;
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

        private static GameObject _root;
        private static RectTransform _panelRect;
        private static RectTransform _closeButtonRect;
        private static TextMeshProUGUI _titleLabel;
        private static RectTransform _listScrollRect;
        private static RectTransform _listScrollContent;
        private static ScrollRect _listScroll;
        private static float _textScale = 1f;
        private static bool _restoreAfterUnblock;

        private static readonly List<RowUi> Rows = new List<RowUi>();
        private static readonly List<(BookmarkDistanceRowKey Key, BookmarkEntry Bookmark)> DistanceRequests =
            new List<(BookmarkDistanceRowKey, BookmarkEntry)>();
        private static readonly Dictionary<BookmarkDistanceRowKey, string> DistanceCache =
            new Dictionary<BookmarkDistanceRowKey, string>();

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
            internal Button AddButton;
            internal TextMeshProUGUI AddLabel;
            internal int HistoryIndex = -1;
        }

        internal static bool IsOpen => _root != null && _root.activeSelf;

        private static float ContentHorizontalInset => NavPanelLayout.ContentInset * 2f;

        private static float BodyContentTop =>
            NavPanelLayout.HeaderHeight + NavPanelLayout.BodyTopPadding;

        private static float ScrollViewportHeight =>
            VisibleListRowCount * RowHeight + (VisibleListRowCount - 1) * RowGap;

        private static float ComputePanelHeight() =>
            NavPanelLayout.HeaderHeight +
            NavPanelLayout.BodyTopPadding +
            ScrollViewportHeight +
            NavPanelLayout.BodyBottomPadding;

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
                    ApplyScreenAnchor();
                    ApplyPanelLayout();
                    LayoutListContent();
                }
                return;
            }

            GameUiStyle.EnsureInitialized();
            VisitHistoryStore.Changed += OnHistoryChanged;

            _root = new GameObject(RootName);
            Object.DontDestroyOnLoad(_root);
            GameStylePanelChrome.SetupOverlayCanvas(_root, CanvasSortOrder);

            var panelHeight = ComputePanelHeight();
            var headerWiden = NavPanelLayout.ComputeWideMapPanelHeaderWidenTrim(PanelWidth);
            var chrome = GameStylePanelChrome.Build(_root.transform, PanelWidth, panelHeight, "Panel", headerWiden);
            _textScale = Mathf.Clamp(chrome.Scale, 0.85f, 1.15f);
            _panelRect = chrome.Panel;
            ApplyScreenAnchor();

            var header = chrome.Header;
            var titleGo = CreateRect(header, "Title");
            titleGo.anchorMin = Vector2.zero;
            titleGo.anchorMax = Vector2.one;
            NavPanelLayout.ApplyHeaderTitleWithRightReserve(
                titleGo,
                _textScale,
                GameUiStyle.ComputeHeaderCloseTitleReserve(_textScale, CloseButtonExtraInset));
            _titleLabel = titleGo.gameObject.AddComponent<TextMeshProUGUI>();
            _titleLabel.fontSize = NavPanelLayout.TitleFontSize * _textScale;
            _titleLabel.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            _titleLabel.color = GameUiStyle.TitleColor;
            _titleLabel.alignment = TextAlignmentOptions.Left;
            GameUiStyle.ApplyTitleFont(_titleLabel);

            _closeButtonRect = GameUiStyle.CreateHeaderCloseButton(
                header, _textScale, (UnityAction)Close, CloseButtonExtraInset).GetComponent<RectTransform>();

            BuildListScroll(chrome.Panel);

            GameStylePanelChrome.ApplyUiLayer(_root);
            _root.SetActive(false);
            RefreshLocalizedText();
            RefreshList(fullDistanceRefresh: true);
        }

        private static void BuildListScroll(RectTransform panel)
        {
            var scrollGo = CreateRect(panel, "ListScroll");
            _listScrollRect = scrollGo;
            scrollGo.anchorMin = new Vector2(0f, 1f);
            scrollGo.anchorMax = new Vector2(1f, 1f);
            scrollGo.pivot = new Vector2(0.5f, 1f);
            scrollGo.anchoredPosition = new Vector2(0f, -BodyContentTop);
            scrollGo.sizeDelta = new Vector2(-ContentHorizontalInset, ScrollViewportHeight);

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

        private static void ApplyScreenAnchor()
        {
            if (_panelRect == null)
                return;

            _panelRect.anchorMin = _panelRect.anchorMax = Vector2.zero;
            _panelRect.pivot = Vector2.zero;
            UpdateScreenPosition();
        }

        private static void UpdateScreenPosition()
        {
            if (_panelRect == null)
                return;

            var x = ScreenMarginX;
            if (CityMapBookmarksPanel.IsVisible)
                x += PanelWidth + PanelGap;

            _panelRect.anchoredPosition = new Vector2(x, ScreenBottomMargin);
        }

        private static void ApplyPanelLayout()
        {
            if (_panelRect == null)
                return;

            SyncRows();
            _panelRect.sizeDelta = new Vector2(PanelWidth, ComputePanelHeight());
            GameStylePanelChrome.RestorePanelChrome(
                _panelRect,
                PanelWidth,
                NavPanelLayout.ComputeWideMapPanelHeaderWidenTrim(PanelWidth));
            UpdateScreenPosition();

            if (_listScrollRect != null)
            {
                _listScrollRect.anchoredPosition = new Vector2(0f, -BodyContentTop);
                _listScrollRect.sizeDelta = new Vector2(-ContentHorizontalInset, ScrollViewportHeight);
            }
        }

        private static void SyncRows()
        {
            if (_listScrollContent == null)
                return;

            var needed = VisitHistoryStore.Count;
            while (Rows.Count < needed)
            {
                var row = CreateRowUi(_listScrollContent, _textScale, "Row" + Rows.Count);
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

        private static void LayoutListContent()
        {
            if (_listScrollContent == null)
                return;

            var y = 0f;
            var activeCount = 0;

            for (var i = 0; i < Rows.Count; i++)
            {
                var row = Rows[i];
                if (row?.Root == null || !row.Root.activeSelf)
                    continue;

                RepositionRow(row, -y);
                y += RowHeight + RowGap;
                activeCount++;
            }

            _listScrollContent.sizeDelta = new Vector2(
                0f,
                activeCount <= 0 ? 0f : activeCount * RowHeight + (activeCount - 1) * RowGap);
        }

        private static void RepositionRow(RowUi row, float rowTop)
        {
            if (row?.Root == null)
                return;

            var rowRect = row.Root.GetComponent<RectTransform>();
            rowRect.anchoredPosition = new Vector2(0f, rowTop);
            rowRect.sizeDelta = new Vector2(0f, RowHeight);
        }

        private static RowUi CreateRowUi(RectTransform panel, float textScale, string name)
        {
            var row = new RowUi();
            row.Root = CreateRect(panel, name).gameObject;
            var rowRect = row.Root.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
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

            var nameRightInset = ComputeRowNameRightInset();
            var nameGo = CreateRect(rowRect, "Name");
            nameGo.anchorMin = Vector2.zero;
            nameGo.anchorMax = new Vector2(1f, 1f);
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
            distGo.anchoredPosition = new Vector2(-ComputeRowDistanceRightInset(), 0f);
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
                RowSetButtonWidth + RowButtonGap + RowSetButtonWidth + RowButtonGap,
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

            var addGo = CreateRect(rowRect, "AddButton");
            LayoutRowButton(addGo, RowSetButtonWidth + RowButtonGap, RowSetButtonWidth, buttonHeight);
            var addImg = GameUiStyle.CreateButtonGraphic(addGo, textScale, GameUiStyle.ApplyButtonGreen, bleedBottom: false);
            row.AddButton = addGo.gameObject.AddComponent<Button>();
            row.AddButton.targetGraphic = addImg;
            GameUiStyle.BindButtonClick(row.AddButton, () => OnAddBookmarkClicked(row));

            var addLabelGo = CreateRect(addGo, "Label");
            Stretch(addLabelGo);
            row.AddLabel = addLabelGo.gameObject.AddComponent<TextMeshProUGUI>();
            row.AddLabel.fontSize = 11f * textScale;
            row.AddLabel.fontStyle = FontStyles.UpperCase;
            row.AddLabel.alignment = TextAlignmentOptions.Center;
            row.AddLabel.color = Color.white;
            row.AddLabel.raycastTarget = false;
            GameUiStyle.ApplyButtonFont(row.AddLabel);

            var btnGo = CreateRect(rowRect, "SetDestButton");
            LayoutRowButton(btnGo, 0f, RowSetButtonWidth, buttonHeight);
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

            return row;
        }

        private static float ComputeRowActionsWidth() =>
            RowSetButtonWidth + RowButtonGap + RowSetButtonWidth + RowButtonGap + RowActionButtonSize;

        private static float ComputeRowDistanceRightInset() =>
            ComputeRowActionsWidth() + RowDistanceToCenterGap;

        private static float ComputeRowNameRightInset() =>
            ComputeRowDistanceRightInset() + RowDistanceWidth + RowNameToDistanceGap;

        private static void LayoutRowButton(RectTransform rect, float rightInset, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-rightInset, 0f);
            rect.sizeDelta = new Vector2(width, height);
        }

        internal static void Open()
        {
            EnsureCreated();
            if (_root == null)
                return;

            _restoreAfterUnblock = false;
            RefreshLocalizedText();
            RefreshList(fullDistanceRefresh: true);
            _root.SetActive(true);
        }

        internal static void Close()
        {
            ModUiFocus.ReleaseForMovement();
            if (_root != null)
                _root.SetActive(false);

            BookmarkRouteDistanceService.Cancel();
        }

        internal static void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        internal static void Tick()
        {
            if (_root == null)
                return;

            UpdateVisibility();

            if (!IsOpen)
                return;

            if (CityMapBookmarksPanel.IsVisible)
                UpdateScreenPosition();

            if (!CityMapBookmarksPanel.BlocksMapInput)
                ModUiFocus.ReleaseForMovement();

            TickDistanceResults();
        }

        internal static void TickOverlay()
        {
            if (!IsOpen)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
                Close();
        }

        private static void UpdateVisibility()
        {
            if (_root == null)
                return;

            if (!GameState.IsBlockingVisitHistory())
            {
                if (_restoreAfterUnblock)
                {
                    _restoreAfterUnblock = false;
                    Open();
                }

                return;
            }

            if (!IsOpen)
                return;

            _restoreAfterUnblock = true;
            Close();
        }

        internal static void RefreshList(bool fullDistanceRefresh = false)
        {
            ApplyPanelLayout();
            RefreshRows();
            LayoutListContent();
            RefreshDistances(fullDistanceRefresh);
        }

        private static void RefreshRows()
        {
            for (var i = 0; i < Rows.Count; i++)
            {
                var ui = Rows[i];
                ui.HistoryIndex = i;
                var bookmark = VisitHistoryStore.GetAt(i);
                if (bookmark == null)
                {
                    ui.Root.SetActive(false);
                    continue;
                }

                ui.Root.SetActive(true);
                ui.NameLabel.text = bookmark.DisplayName;
                ui.SetDestButton.interactable = bookmark.TryGetNavigationTarget(out _);
                ui.AddButton.interactable = BookmarkStore.CanAdd();
                ui.CenterButton.interactable = true;
                ui.NameLabel.color = GameUiStyle.BodyTextColor;
                ApplyRowDistanceLabel(ui, bookmark);
                RefreshRowTypeIcon(ui, bookmark);
            }
        }

        private static void RefreshRowTypeIcon(RowUi ui, BookmarkEntry bookmark)
        {
            if (ui?.TypeIconRoot == null)
                return;

            var hasIcon = bookmark != null &&
                          BookmarkRowIconResolver.TryGetForBookmark(bookmark, out var rowIcon) &&
                          ApplyRowTypeIcon(ui, rowIcon);

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

        private static void RefreshDistances(bool fullDistanceRefresh)
        {
            if (fullDistanceRefresh)
            {
                DistanceCache.Clear();
                RequestDistanceRefresh(BookmarkRouteDistanceService.RequestRefresh);
                return;
            }

            RequestDistanceRefresh(BookmarkRouteDistanceService.RequestCompute);
        }

        private static void ApplyRowDistanceLabel(RowUi ui, BookmarkEntry bookmark)
        {
            if (ui?.DistanceLabel == null)
                return;

            if (bookmark == null || !ui.Root.activeSelf)
            {
                ui.DistanceLabel.text = "—";
                return;
            }

            var key = ToDistanceRowKey(ui);
            if (DistanceCache.TryGetValue(key, out var cached))
                ui.DistanceLabel.text = cached;
            else if (BookmarkRouteDistanceService.IsKeyPending(key))
                ui.DistanceLabel.text = "…";
            else
                ui.DistanceLabel.text = "…";
        }

        private static void RequestDistanceRefresh(
            System.Action<IReadOnlyList<(BookmarkDistanceRowKey Key, BookmarkEntry Bookmark)>> dispatch)
        {
            DistanceRequests.Clear();

            for (var i = 0; i < Rows.Count; i++)
            {
                var ui = Rows[i];
                if (ui?.Root == null || !ui.Root.activeSelf || ui.HistoryIndex < 0)
                    continue;

                var bookmark = VisitHistoryStore.GetAt(ui.HistoryIndex);
                if (bookmark == null)
                    continue;

                var key = ToDistanceRowKey(ui);
                if (DistanceCache.ContainsKey(key) || BookmarkRouteDistanceService.IsKeyPending(key))
                    continue;

                ui.DistanceLabel.text = "…";
                DistanceRequests.Add((key, bookmark));
            }

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
            if (key.Kind != BookmarkDistanceRowKind.History)
                return false;

            for (var i = 0; i < Rows.Count; i++)
            {
                var ui = Rows[i];
                if (ui.HistoryIndex == key.BookmarkIndex)
                {
                    row = ui;
                    return true;
                }
            }

            return false;
        }

        private static BookmarkDistanceRowKey ToDistanceRowKey(RowUi ui) =>
            new BookmarkDistanceRowKey
            {
                Kind = BookmarkDistanceRowKind.History,
                BookmarkIndex = ui.HistoryIndex
            };

        private static void OnCenterClicked(RowUi row)
        {
            var bookmark = VisitHistoryStore.GetAt(row?.HistoryIndex ?? -1);
            if (bookmark == null)
                return;

            CityMapBookmarkFocusService.TryFocusBookmark(bookmark);
        }

        private static void OnSetDestinationClicked(RowUi row)
        {
            var bookmark = VisitHistoryStore.GetAt(row?.HistoryIndex ?? -1);
            if (bookmark == null)
                return;

            if (bookmark.PrefersWorldPosition && bookmark.HasWorldPosition)
            {
                WorldDestinationService.TrySetFromBookmark(bookmark);
                return;
            }

            if (!bookmark.HasAddress)
                return;

            VanillaDestinationService.SetMapDestination(bookmark.ToAddress());
        }

        private static void OnAddBookmarkClicked(RowUi row)
        {
            var bookmark = VisitHistoryStore.GetAt(row?.HistoryIndex ?? -1);
            if (bookmark == null)
                return;

            BookmarkPickService.TryOpenDialogFromBookmark(bookmark);
        }

        private static void OnHistoryChanged() => RefreshList();

        internal static void RefreshLocalizedText()
        {
            if (_titleLabel != null)
                _titleLabel.text = ModUiText.VisitHistoryTitle;

            for (var i = 0; i < Rows.Count; i++)
            {
                if (Rows[i].SetDestLabel != null)
                    Rows[i].SetDestLabel.text = ModUiText.BookmarksSetDestination;
                if (Rows[i].AddLabel != null)
                    Rows[i].AddLabel.text = ModUiText.VisitHistoryAdd;
            }
        }

        internal static void Destroy()
        {
            VisitHistoryStore.Changed -= OnHistoryChanged;
            Close();

            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }

            Rows.Clear();
            _panelRect = null;
            _closeButtonRect = null;
            _titleLabel = null;
            _listScrollRect = null;
            _listScrollContent = null;
            _listScroll = null;
            _restoreAfterUnblock = false;
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
            foreach (var name in new[] { "VoogleRoute_VisitHistory", "VoogleRoute_VisitHistory_v1", "VoogleRoute_VisitHistory_v2", "VoogleRoute_VisitHistory_v3", "VoogleRoute_VisitHistory_v4", "VoogleRoute_VisitHistory_v5" })
            {
                var legacy = GameObject.Find(name);
                if (legacy != null)
                    Object.Destroy(legacy);
            }
        }
    }
}
