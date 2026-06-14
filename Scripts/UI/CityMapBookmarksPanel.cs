using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VoogleRoute.Navigation;

using Capisoft.Lib.BaUnifiedUI.Core;
using Capisoft.Lib.BaUnifiedUI.Fluent;
using Capisoft.Lib.BaUnifiedUI.Controls;
using Capisoft.Lib.BaUnifiedUI.Layout;
namespace VoogleRoute.UI
{
    internal static class CityMapBookmarksPanel
    {
        private const string RootName = "VoogleRoute_BookmarksPanel_v38";
        private const int VisibleListRowCount = 8;
        private const int CanvasSortOrder = 11000;
        /// <summary>Wider than the action panel (370) — header/frame via <see cref="BaUi"/> fluent API.</summary>
        private const float PanelWidth = 420f;
        private const float ScreenMarginX = 16f;
        private const float ScreenBottomMargin = BaUi.Layout.ScreenMarginMinY;

        private static GameObject _root;
        private static RectTransform _panelRect;
        private static TextMeshProUGUI _titleLabel;
        private static TMP_InputField _searchField;
        private static TextMeshProUGUI _searchPlaceholder;
        private static TextMeshProUGUI _pickHintLabel;
        private static TextMeshProUGUI _addButtonLabel;
        private static TextMeshProUGUI _clearButtonLabel;
        private static BaUiScrollList _scrollList;
        private static float _textScale = 1f;
        private static float _panelHeight;

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
        private static bool _loggedBookmarksChrome;
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
            internal readonly BaUiListRow Ui;
            internal GameObject Root => Ui.Root;
            internal GameObject TypeIconRoot => Ui.TypeIconRoot;
            internal Image TypeIcon => Ui.TypeIcon;
            internal TextMeshProUGUI NameLabel => Ui.NameLabel;
            internal TextMeshProUGUI DistanceLabel => Ui.DistanceLabel;
            internal Button CenterButton => Ui.CenterButton;
            internal Image CenterButtonImage => Ui.CenterButtonImage;
            internal TextMeshProUGUI CenterFallbackLabel => Ui.CenterFallbackLabel;
            internal Button SetDestButton => Ui.SetDestButton;
            internal TextMeshProUGUI SetDestLabel => Ui.SetDestLabel;
            internal Button DriveButton => Ui.DriveButton;
            internal TextMeshProUGUI DriveLabel => Ui.DriveLabel;
            internal Button DeleteButton => Ui.DeleteButton;
            internal RowKind Kind = RowKind.Bookmark;
            internal int BookmarkIndex = -1;
            internal int VehicleIndex = -1;

            internal RowUi(BaUiListRow ui) => Ui = ui;
        }

        internal static bool IsVisible => _root != null && _root.activeSelf;
        internal static bool IsSearchFocused => _searchField != null && _searchField.isFocused;
        internal static bool IsPickMode => _pickMode;
        internal static bool BlocksMapInput =>
            IsSearchFocused || CityMapBookmarkAddDialog.IsOpen || CityMapBookmarkAddDialog.IsNameFocused;

        private static readonly RowKind[] QuickRowKinds =
        {
            RowKind.LastCar,
            RowKind.LastHome,
            RowKind.LastShop
        };

        private static float ContentHorizontalInset => BaUi.Layout.ContentInset * 2f;

        private static float FooterButtonWidth =>
            (PanelWidth - ContentHorizontalInset - BaUi.Layout.ButtonGap) * 0.5f;

        private static BaUiListRowTemplate MapActionsOnPanelTemplate =>
            BaUiListRows.Template(BaUiListRowRecipe.MapActions).OnPanel(ContentHorizontalInset).Build();

        private static BaUiListRowTemplate MapBookmarkScrollTemplate => BaUiListRows.MapBookmark();

        private static BaUiListRowTemplate MapActionsScrollTemplate => BaUiListRows.MapActions();

        internal static void EnsureCreated()
        {
            VoogleRoutePanelLifecycle.DestroyIfStale(ref _root, RootName, Destroy);
            if (_root != null)
            {
                BaUi.ApplyLayer(_root);
                if (_panelRect != null)
                {
                    AnchorBottomLeft(_panelRect);
                    ApplyPanelLayout();
                    LayoutListContent();
                }
                return;
            }

            BaUi.EnsureReady();
            BookmarkStore.Changed += OnBookmarksChanged;
            QuickBookmarkStore.Changed += OnQuickBookmarksChanged;

            QuickRows.Clear();
            BaUiSearchField search = null;

            var built = BaUi.Overlay(RootName, CanvasSortOrder)
                .Dock(BaDock.BottomLeft, marginX: ScreenMarginX, marginY: ScreenBottomMargin)
                .Panel(BaPanelRecipe.WideMapPanel, PanelWidth)
                .Header(h => h
                    .TitleLeft(ModUiText.BookmarksTitle)
                    .Icon(BaIcons.History, () => VisitHistoryPanel.Toggle(), "\u23F1"))
                .Content(c => c
                    .QuickRows(QuickBookmarkStore.SlotCount, MapActionsOnPanelTemplate, OnQuickRowCreated, out _)
                    .Search(ModUiText.BookmarksSearchPlaceholder, OnSearchChanged, out search, OnSearchFieldSelected)
                    .PickHint(out _pickHintLabel)
                    .ScrollList(VisibleListRowCount, out _scrollList)
                    .Footer(BaUi.Layout.ButtonHeight, h => h
                        .Button(ModUiText.BookmarksAdd, BaButtonStyle.Blue, OnAddBookmarkClicked, FooterButtonWidth, "AddButton")
                        .Gap(BaUi.Layout.ButtonGap)
                        .Button(ModUiText.BookmarksClearAll, BaButtonStyle.Red, OnClearAllClicked, FooterButtonWidth, "ClearButton")))
                .Build();

            _root = built.Root;
            _textScale = Mathf.Clamp(built.Scale, 0.85f, 1.15f);
            _panelRect = built.Panel;
            _panelHeight = built.PanelHeight;
            _titleLabel = built.Header.Find("Title")?.GetComponent<TextMeshProUGUI>();
            _searchField = search?.Field;
            _searchPlaceholder = search?.Placeholder;
            _addButtonLabel = built.Panel.Find("AddButton/Label")?.GetComponent<TextMeshProUGUI>();
            _clearButtonLabel = built.Panel.Find("ClearButton/Label")?.GetComponent<TextMeshProUGUI>();

            BaUi.ApplyLayer(_root);
            _root.SetActive(false);
            _lastBookmarkCount = BookmarkStore.All.Count;
            RefreshLocalizedText();
            RefreshList();
        }

        private static void OnQuickRowCreated(int index, BaUiListRow ui)
        {
            var row = new RowUi(ui) { Kind = QuickRowKinds[index] };
            WireMapActionsRow(row);
            QuickRows.Add(row);
        }

        private static void WireMapActionsRow(RowUi row)
        {
            row.Ui.Bind(
                onCenter: () => OnCenterClicked(row),
                onSetDestination: () => OnSetDestinationClicked(row),
                onDrive: () => OnNavigateClicked(row));
            if (row.DriveLabel != null)
                row.DriveLabel.text = ResolveMapActionLabel();
        }

        private static void WireMapBookmarkRow(RowUi row)
        {
            row.Ui.Bind(
                onCenter: () => OnCenterClicked(row),
                onSetDestination: () => OnSetDestinationClicked(row),
                onDrive: () => OnNavigateClicked(row),
                onDelete: () => OnDeleteClicked(row));
            if (row.DriveLabel != null)
                row.DriveLabel.text = ResolveMapActionLabel();
        }

        private static void SyncBookmarkRows()
        {
            if (_scrollList?.Content == null)
                return;

            BaUiListRowPools.SyncHolders(
                Rows,
                _scrollList.Content,
                BookmarkStore.All.Count,
                _textScale,
                MapBookmarkScrollTemplate,
                "Row",
                (i, ui) =>
                {
                    var row = new RowUi(ui) { Kind = RowKind.Bookmark, BookmarkIndex = i };
                    WireMapBookmarkRow(row);
                    return row;
                },
                r => r.Ui);
        }

        private static void SyncVehicleRows()
        {
            if (_scrollList?.Content == null)
                return;

            PlayerVehicleBookmarkStore.Refresh();
            BaUiListRowPools.SyncHolders(
                VehicleRows,
                _scrollList.Content,
                PlayerVehicleBookmarkStore.Count,
                _textScale,
                MapActionsScrollTemplate,
                "VehicleRow",
                (i, ui) =>
                {
                    var row = new RowUi(ui) { Kind = RowKind.Vehicle, VehicleIndex = i };
                    WireMapActionsRow(row);
                    return row;
                },
                r => r.Ui);
        }

        private static void LayoutListContent()
        {
            if (_scrollList?.Content == null)
                return;

            var y = 0f;
            var activeCount = 0;
            activeCount += BaUiListRowPools.LayoutHoldersInScroll(
                _scrollList,
                VehicleRows,
                r => r.Ui,
                r => r.Root.activeSelf,
                ref y);
            activeCount += BaUiListRowPools.LayoutHoldersInScroll(
                _scrollList,
                Rows,
                r => r.Ui,
                r => r.Root.activeSelf,
                ref y);
            _scrollList.LayoutRows(activeCount);
        }

        private static void ApplyPanelLayout()
        {
            if (_panelRect == null)
                return;

            SyncVehicleRows();
            SyncBookmarkRows();
            _panelRect.sizeDelta = new Vector2(PanelWidth, _panelHeight);
            BaUiWidgets.RestoreDockedPanelChrome(_panelRect, PanelWidth, wideMapPanel: true);
            if (!_loggedBookmarksChrome)
            {
                _loggedBookmarksChrome = true;
                VoogleRouteUiDiagnostics.LogPanelChrome(
                    "bookmarks-layout",
                    _panelRect,
                    PanelWidth,
                    BaUi.Layout.ComputeWideMapPanelHeaderWidenTrim(PanelWidth));
            }
        }

        private static void AnchorBottomLeft(RectTransform panel)
        {
            panel.anchorMin = panel.anchorMax = Vector2.zero;
            panel.pivot = Vector2.zero;
            panel.anchoredPosition = new Vector2(ScreenMarginX, ScreenBottomMargin);
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
                ui.NameLabel.color = BaUi.Colors.Body;
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
                ui.NameLabel.color = hasData ? BaUi.Colors.Body : mutedName;
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
            BaUiFocus.ReleaseForMovement();
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

            BaUiFocus.ReleaseForMovement();
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

        internal static void SuppressForSubwayNavigation()
        {
            CancelPickMode();
            CityMapBookmarkAddDialog.Close();
            BookmarkRouteDistanceService.Cancel();
            if (_root != null)
                _root.SetActive(false);
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
            _scrollList = null;
            _panelHeight = 0f;
            _titleLabel = null;
            _searchField = null;
            _searchPlaceholder = null;
            _pickHintLabel = null;
            _addButtonLabel = null;
            _clearButtonLabel = null;
        }
    }
}

