using System.Collections.Generic;
using Capisoft.Lib.BaUnifiedUI.Controls;
using Capisoft.Lib.BaUnifiedUI.Layout;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VoogleRoute.Navigation;

namespace VoogleRoute.UI
{
    /// <summary>Business rows share the bookmarks viewport but have no bookmark persistence or distance jobs.</summary>
    internal sealed class CityMapBusinessRows
    {
        private sealed class Row
        {
            internal BaUiListRow Ui;
            internal TextMeshProUGUI Address;
            internal BookmarkEntry Entry;
        }

        private readonly List<Row> _rows = new List<Row>();
        internal int VisibleCount { get; private set; }

        internal void Refresh(BaUiScrollList scroll, float scale, bool visible, string filter)
        {
            if (scroll?.Content == null) return;
            BaUiListRowPools.SyncHolders(_rows, scroll.Content, PlayerBusinessBookmarkStore.All.Count,
                scale, BaUiListRows.MapActions(), "BusinessRow", CreateRow, row => row.Ui);

            VisibleCount = 0;
            for (var i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                row.Entry = PlayerBusinessBookmarkStore.All[i];
                var show = visible && row.Entry.MatchesFilter(filter);
                row.Ui.SetActive(show);
                if (!show) continue;
                VisibleCount++;
                row.Ui.NameLabel.text = row.Entry.DisplayName;
                row.Address.text = row.Entry.LocationLabel;
                row.Ui.SetDestLabel.text = ModUiText.BookmarksSetDestination;
                row.Ui.DriveLabel.text = ModUiText.BookmarksDrive;
            }
        }

        private static Row CreateRow(int index, BaUiListRow ui)
        {
            var row = new Row { Ui = ui };
            ui.DistanceLabel.gameObject.SetActive(false);
            var nameRect = ui.NameLabel.rectTransform;
            nameRect.offsetMin = new Vector2(0f, 0f);
            nameRect.offsetMax += new Vector2(BaUiListMetrics.RowDistanceWidth +
                BaUiListMetrics.RowNameToDistanceGap, 0f);

            // Two independently clipped lines keep long names and addresses inside the row.
            row.Address = Object.Instantiate(ui.NameLabel, ui.NameLabel.transform.parent);
            row.Address.name = "BusinessAddress";
            row.Address.fontSize = ui.NameLabel.fontSize * 0.8f;
            row.Address.color = new Color(0.65f, 0.68f, 0.72f, 1f);
            row.Address.richText = false;
            row.Address.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            ui.NameLabel.richText = false;
            nameRect.anchorMin = new Vector2(0f, 0.5f);

            var focus = ui.Root.AddComponent<Button>();
            focus.targetGraphic = ui.NameLabel;
            focus.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            focus.onClick.AddListener(() => CityMapBookmarkFocusService.TryFocusBookmark(row.Entry));
            ui.NameLabel.raycastTarget = true;
            row.Address.raycastTarget = true;
            ui.Bind(
                onCenter: () => CityMapBookmarkFocusService.TryFocusBookmark(row.Entry),
                onSetDestination: () => BookmarkDestinationService.TrySetFromBookmark(row.Entry),
                onDrive: () => BookmarkQuickNavService.NavigateFromBookmark(row.Entry));
            return row;
        }

        internal int Layout(BaUiScrollList scroll, ref float y) =>
            BaUiListRowPools.LayoutHoldersInScroll(scroll, _rows, row => row.Ui,
                row => row.Ui.Root.activeSelf, ref y);

        internal void Clear()
        {
            _rows.Clear();
            VisibleCount = 0;
        }
    }
}
