using System.Collections.Generic;
using Capisoft.Lib.BaUnifiedUI.Controls;
using Capisoft.Lib.BaUnifiedUI.Core;
using Capisoft.Lib.BaUnifiedUI.Fluent;
using Capisoft.Lib.BaUnifiedUI.Layout;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VoogleRoute;
using VoogleRoute.Navigation;

namespace VoogleRoute.UI
{
    /// <summary>Scrollable list of the 50 most recently visited buildings.</summary>
    internal static class VisitHistoryPanel
    {
        private const string RootName = "VoogleRoute_VisitHistory_v22";
        private const string DragPositionId = "voogleroute:visit-history";
        private const float CloseButtonExtraInset = 5f;
        private const int CanvasSortOrder = 11050;
        private const float PanelWidth = 420f;
        private const float ScreenMarginX = 16f;
        private const float ScreenBottomMargin = BaUi.Layout.ScreenMarginMinY;
        private const float PanelGap = 8f;
        private const int VisibleListRowCount = 10;

        private static GameObject _root;
        private static RectTransform _panelRect;
        private static BaUiDragState _dragState;
        private static TextMeshProUGUI _titleLabel;
        private static BaUiScrollList _scrollList;
        private static float _textScale = 1f;
        private static float _panelHeight;
        private static bool _restoreAfterUnblock;
        private static bool _mapVisibilitySnapshotActive;
        private static bool _normalVisibilityBeforeMap;
        private static int _ignoreEscapeFrame = -1;

        private static readonly List<HistoryRow> Rows = new List<HistoryRow>();
        private static readonly List<(BookmarkDistanceRowKey Key, BookmarkEntry Bookmark)> DistanceRequests =
            new List<(BookmarkDistanceRowKey, BookmarkEntry)>();
        private static readonly Dictionary<BookmarkDistanceRowKey, string> DistanceCache =
            new Dictionary<BookmarkDistanceRowKey, string>();
        private static readonly BookmarkDistanceRefreshTracker DistanceRefreshTracker =
            new BookmarkDistanceRefreshTracker(BookmarkDistanceConsumer.History);

        private static bool _historyMapMode;

        private sealed class HistoryRow
        {
            internal BaUiListRow Ui;
            internal int HistoryIndex = -1;
        }

        internal static bool IsOpen => _root != null && _root.activeSelf;

        private static float ContentHorizontalInset => BaUi.Layout.ContentInset * 2f;

        private static float BodyContentTop =>
            BaUi.Layout.HeaderHeight + BaUi.Layout.BodyTopPadding;

        internal static void EnsureCreated()
        {
            VoogleRoutePanelLifecycle.DestroyIfStale(ref _root, RootName, Destroy);
            if (_root != null)
            {
                BaUi.ApplyLayer(_root);
                if (_panelRect != null)
                {
                    ApplyPanelLayout();
                    LayoutListContent();
                }
                return;
            }

            BaUi.EnsureReady();
            VisitHistoryStore.Changed += OnHistoryChanged;

            BaUiScrollList scrollList = null;
            var built = BaUi.Overlay(RootName, CanvasSortOrder)
                .Panel(BaPanelRecipe.WideMapPanel, PanelWidth)
                .Draggable(DragPositionId)
                .Header(h => h
                    .TitleLeft(ModUiText.VisitHistoryTitle)
                    .CloseButton(Close, CloseButtonExtraInset))
                .Content(c => scrollList = c.ScrollList(VisibleListRowCount))
                .Build();

            _root = built.Root;
            _textScale = Mathf.Clamp(built.Scale, 0.85f, 1.15f);
            _panelRect = built.Panel;
            _dragState = built.Drag;
            _panelHeight = built.PanelHeight;
            _scrollList = scrollList;
            ApplyScreenAnchor();

            _titleLabel = built.Header.Find("Title")?.GetComponent<TextMeshProUGUI>();

            BaUi.ApplyLayer(_root);
            _root.SetActive(false);
            RefreshLocalizedText();
            RefreshList(fullDistanceRefresh: true);
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
            _panelRect.sizeDelta = new Vector2(PanelWidth, _panelHeight);
            BaUiWidgets.RestoreDockedPanelChrome(_panelRect, PanelWidth, wideMapPanel: true);
            if (ShouldApplyAutomaticPosition())
                UpdateScreenPosition();

            if (_scrollList?.Rect != null)
            {
                _scrollList.Rect.anchoredPosition = new Vector2(0f, -BodyContentTop);
                _scrollList.Rect.sizeDelta = new Vector2(-ContentHorizontalInset, BaUiListMetrics.ScrollViewportHeight(VisibleListRowCount));
            }
        }

        private static void SyncRows()
        {
            if (_scrollList?.Content == null)
                return;

            EnsureHistoryRecipe();

            var needed = VisitHistoryStore.Count;
            BaUiListRowPools.SyncHolders(
                Rows,
                _scrollList.Content,
                needed,
                _textScale,
                CurrentHistoryTemplate(),
                "Row",
                (i, ui) =>
                {
                    var row = new HistoryRow { Ui = ui };
                    ui.Bind(
                        onCenter: () => OnCenterClicked(row),
                        onSetDestination: () => OnSetDestinationClicked(row),
                        onAdd: () => OnAddBookmarkClicked(row),
                        onDrive: () => OnNavigateClicked(row));
                    return row;
                },
                r => r.Ui);
        }

        private static BaUiListRowTemplate CurrentHistoryTemplate() =>
            CityMapBookmarksPanel.IsVisible
                ? BaUiListRows.VisitHistoryMap()
                : BaUiListRows.VisitHistoryHud();

        private static void EnsureHistoryRecipe()
        {
            var mapMode = CityMapBookmarksPanel.IsVisible;
            if (mapMode == _historyMapMode && Rows.Count > 0)
                return;

            _historyMapMode = mapMode;
            ClearRows();
        }

        private static void ClearRows()
        {
            for (var i = 0; i < Rows.Count; i++)
            {
                if (Rows[i]?.Ui?.Root != null)
                    Object.Destroy(Rows[i].Ui.Root);
            }

            Rows.Clear();
        }

        private static void LayoutListContent()
        {
            if (_scrollList?.Content == null)
                return;

            var y = 0f;
            var activeCount = BaUiListRowPools.LayoutHoldersInScroll(
                _scrollList,
                Rows,
                r => r.Ui,
                r => r.Ui.Root.activeSelf,
                ref y);
            _scrollList.LayoutRows(activeCount);
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
            BaUiFocus.ReleaseForMovement();
            if (_root != null)
                _root.SetActive(false);

            BookmarkRouteDistanceService.Cancel(BookmarkDistanceConsumer.History);
            DistanceRefreshTracker.Reset();
        }

        internal static void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        /// <summary>
        /// The city map borrows the normal-game visibility when it opens, but any
        /// history toggle performed on the map remains local to that map session.
        /// </summary>
        internal static void OnCityMapToggled(bool open)
        {
            if (open)
            {
                if (_mapVisibilitySnapshotActive)
                    return;

                EnsureCreated();
                _normalVisibilityBeforeMap = IsOpen;
                _mapVisibilitySnapshotActive = true;

                if (_normalVisibilityBeforeMap)
                    Open();
                else
                    Close();

                return;
            }

            if (!_mapVisibilitySnapshotActive)
                return;

            var restoreNormalVisibility = _normalVisibilityBeforeMap;
            _mapVisibilitySnapshotActive = false;
            _normalVisibilityBeforeMap = false;
            _ignoreEscapeFrame = Time.frameCount;

            if (restoreNormalVisibility)
                Open();
            else
                Close();
        }

        internal static void Tick()
        {
            if (_root == null)
                return;

            UpdateVisibility();

            if (!IsOpen)
                return;

            var mapMode = CityMapBookmarksPanel.IsVisible;
            if (mapMode != _historyMapMode)
                RefreshList(fullDistanceRefresh: false);

            if (CityMapBookmarksPanel.IsVisible && ShouldApplyAutomaticPosition())
                UpdateScreenPosition();

            if (!CityMapBookmarksPanel.BlocksMapInput)
                BaUiFocus.ReleaseForMovement();

            TickDistanceResults();
            TickLiveDistanceRefresh();
        }

        internal static void TickOverlay()
        {
            if (!IsOpen)
                return;

            if (_mapVisibilitySnapshotActive || Time.frameCount == _ignoreEscapeFrame)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
                Close();
        }

        private static void UpdateVisibility()
        {
            if (_root == null)
                return;

            if (!ModConfig.DisplayOutsideEnabled)
            {
                if (IsOpen)
                    Close();

                return;
            }

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
            RefreshLocalizedText();
            RefreshDistances(fullDistanceRefresh);
        }

        private static bool ShouldApplyAutomaticPosition() =>
            _dragState == null || (!_dragState.HasSavedPosition && !_dragState.IsDragging);

        private static void RefreshRows()
        {
            for (var i = 0; i < Rows.Count; i++)
            {
                var row = Rows[i];
                row.HistoryIndex = i;
                var bookmark = VisitHistoryStore.GetAt(i);
                if (bookmark == null)
                {
                    row.Ui.SetActive(false);
                    continue;
                }

                row.Ui.SetActive(true);
                row.Ui.NameLabel.text = BookmarkLabelResolver.GetDisplayName(bookmark);
                var canNavigate = bookmark.TryGetNavigationTarget(out _);
                if (row.Ui.SetDestButton != null)
                    row.Ui.SetDestButton.interactable = canNavigate;
                if (row.Ui.DriveButton != null)
                    row.Ui.DriveButton.interactable = canNavigate;
                if (row.Ui.AddButton != null)
                    row.Ui.AddButton.interactable = BookmarkStore.CanAdd();
                if (row.Ui.CenterButton != null)
                    row.Ui.CenterButton.interactable = true;
                row.Ui.NameLabel.color = BaUi.Colors.Body;
                ApplyRowDistanceLabel(row, bookmark);
                RefreshRowTypeIcon(row, bookmark);
            }
        }

        private static void RefreshRowTypeIcon(HistoryRow row, BookmarkEntry bookmark)
        {
            var ui = row?.Ui;
            if (ui?.TypeIconRoot == null)
                return;

            var hasIcon = bookmark != null &&
                          BookmarkRowIconResolver.TryGetForBookmark(bookmark, out var rowIcon) &&
                          ApplyRowTypeIcon(ui, rowIcon);

            if (!hasIcon)
                ui.TypeIconRoot.SetActive(false);
        }

        private static bool ApplyRowTypeIcon(BaUiListRow ui, BookmarkRowIcon rowIcon)
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
                RequestDistanceRefresh(rows => BookmarkRouteDistanceService.RequestRefresh(
                    BookmarkDistanceConsumer.History,
                    rows));
                DistanceRefreshTracker.RememberCurrentOrigin();
                return;
            }

            RequestDistanceRefresh(rows => BookmarkRouteDistanceService.RequestCompute(
                BookmarkDistanceConsumer.History,
                rows));
        }

        private static void ApplyRowDistanceLabel(HistoryRow row, BookmarkEntry bookmark)
        {
            var ui = row?.Ui;
            if (ui?.DistanceLabel == null)
                return;

            if (bookmark == null || !ui.Root.activeSelf)
            {
                ui.DistanceLabel.text = "—";
                return;
            }

            var key = ToDistanceRowKey(row);
            if (DistanceCache.TryGetValue(key, out var cached))
                ui.DistanceLabel.text = cached;
            else if (BookmarkRouteDistanceService.IsKeyPending(BookmarkDistanceConsumer.History, key))
                ui.DistanceLabel.text = "…";
            else
                ui.DistanceLabel.text = "…";
        }

        private static void RequestDistanceRefresh(
            System.Action<IReadOnlyList<(BookmarkDistanceRowKey Key, BookmarkEntry Bookmark)>> dispatch,
            bool includeCached = false,
            bool visibleRowsOnly = false)
        {
            DistanceRequests.Clear();

            for (var i = 0; i < Rows.Count; i++)
            {
                var row = Rows[i];
                if (row?.Ui?.Root == null || !row.Ui.Root.activeSelf || row.HistoryIndex < 0)
                    continue;
                if (visibleRowsOnly && !IsVisibleInScroll(row.Ui))
                    continue;

                var bookmark = VisitHistoryStore.GetAt(row.HistoryIndex);
                if (bookmark == null)
                    continue;

                var key = ToDistanceRowKey(row);
                if (BookmarkRouteDistanceService.IsKeyPending(BookmarkDistanceConsumer.History, key) ||
                    (!includeCached && DistanceCache.ContainsKey(key)))
                    continue;

                if (!includeCached)
                    row.Ui.DistanceLabel.text = "…";
                DistanceRequests.Add((key, bookmark));
            }

            if (DistanceRequests.Count == 0)
                return;

            dispatch(DistanceRequests);
            DistanceRequests.Clear();
        }

        private static void TickLiveDistanceRefresh()
        {
            if (!DistanceRefreshTracker.ShouldRefresh())
                return;

            RequestDistanceRefresh(
                rows => BookmarkRouteDistanceService.RequestRefresh(
                    BookmarkDistanceConsumer.History,
                    rows),
                includeCached: true,
                visibleRowsOnly: true);
        }

        private static bool IsVisibleInScroll(BaUiListRow ui)
        {
            try
            {
                var viewport = _scrollList?.Scroll?.viewport;
                if (viewport == null || ui?.Rect == null)
                    return true;

                var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, ui.Rect);
                var viewportRect = viewport.rect;
                return bounds.max.y >= viewportRect.yMin && bounds.min.y <= viewportRect.yMax;
            }
            catch
            {
                return true;
            }
        }

        private static void TickDistanceResults()
        {
            while (BookmarkRouteDistanceService.TryDequeueCompleted(
                       BookmarkDistanceConsumer.History,
                       out var result))
                ApplyDistanceResult(result);
        }

        private static void ApplyDistanceResult(BookmarkDistanceResult result)
        {
            var text = result.Success
                ? BookmarkRouteDistance.FormatDistance(result.Meters)
                : "—";
            DistanceCache[result.Key] = text;

            if (!TryFindRow(result.Key, out var row) || row?.Ui?.DistanceLabel == null)
                return;

            row.Ui.DistanceLabel.text = text;
        }

        private static bool TryFindRow(BookmarkDistanceRowKey key, out HistoryRow row)
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

        private static BookmarkDistanceRowKey ToDistanceRowKey(HistoryRow row) =>
            new BookmarkDistanceRowKey
            {
                Kind = BookmarkDistanceRowKind.History,
                BookmarkIndex = row.HistoryIndex
            };

        private static void OnCenterClicked(HistoryRow row)
        {
            var bookmark = VisitHistoryStore.GetAt(row?.HistoryIndex ?? -1);
            if (bookmark == null)
                return;

            CityMapBookmarkFocusService.TryFocusBookmark(bookmark);
        }

        private static void OnSetDestinationClicked(HistoryRow row)
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

        private static void OnAddBookmarkClicked(HistoryRow row)
        {
            var bookmark = VisitHistoryStore.GetAt(row?.HistoryIndex ?? -1);
            if (bookmark == null)
                return;

            BookmarkPickService.TryOpenDialogFromBookmark(bookmark);
        }

        private static void OnNavigateClicked(HistoryRow row)
        {
            var bookmark = VisitHistoryStore.GetAt(row?.HistoryIndex ?? -1);
            if (bookmark == null)
                return;

            BookmarkQuickNavService.NavigateFromBookmark(bookmark);
        }

        private static void OnHistoryChanged() => RefreshList();

        internal static void RefreshLocalizedText()
        {
            if (_titleLabel != null)
                _titleLabel.text = ModUiText.VisitHistoryTitle;

            for (var i = 0; i < Rows.Count; i++)
            {
                if (Rows[i].Ui.SetDestLabel != null)
                    Rows[i].Ui.SetDestLabel.text = ModUiText.BookmarksSetDestination;
                if (Rows[i].Ui.DriveLabel != null)
                    Rows[i].Ui.DriveLabel.text = ModUiText.BookmarksDrive;
                if (Rows[i].Ui.AddLabel != null)
                    Rows[i].Ui.AddLabel.text = ModUiText.VisitHistoryAdd;
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
            _dragState = null;
            _titleLabel = null;
            _scrollList = null;
            _panelHeight = 0f;
            _restoreAfterUnblock = false;
            _mapVisibilitySnapshotActive = false;
            _normalVisibilityBeforeMap = false;
            _ignoreEscapeFrame = -1;
            _historyMapMode = false;
            DistanceRefreshTracker.Reset();
        }
    }
}
