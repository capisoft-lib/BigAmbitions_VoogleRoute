using System;
using Capisoft.Lib.BaUnifiedUI.Core;
using Capisoft.Lib.BaUnifiedUI.Fluent;
using UnityEngine;

namespace VoogleRoute.UI
{
    /// <summary>Every VoogleRoute panel uses the same recreate rules when chrome or root version changes.</summary>
    internal static class VoogleRoutePanelLifecycle
    {
        private static readonly string[] RemovedTurnHudNames =
        {
            "VoogleRoute_TurnHudRoot",
            "TurnPanel",
            "IntersectionSchematic",
        };

        private static readonly string[] OrphanUiRootPrefixes =
        {
            "VoogleRoute_ActionPanel",
            "VoogleRoute_HudRoot",
            "VoogleRoute_BookmarksPanel",
            "VoogleRoute_VisitHistory",
            "VoogleRoute_Settings",
            "VoogleRoute_MapDestPopup",
            "VoogleRoute_MapBuildingNav",
            "VoogleRoute_AutoDrivePopup",
            "VoogleRoute_BookmarkAddDialog",
        };

        internal static bool ShouldRecreate(GameObject root, string rootName) =>
            BaUiPanelHost.ShouldRecreate(root, rootName);

        internal static void DestroyIfStale(ref GameObject root, string rootName, Action destroy)
        {
            if (root == null)
                return;

            var reason = BaUiPanelHost.DescribeRecreateReason(root, rootName);
            VoogleRouteUiDiagnostics.LogStaleCheck(root, rootName, reason);
            BaUiPanelHost.DestroyIfStale(ref root, rootName, destroy);
        }

        /// <summary>Once per city load — purge removed turn HUD and stale versioned panel roots.</summary>
        internal static void PurgeLegacyUiOnCityLoad()
        {
            BaUiPanelHost.PurgeNamedRoots(RemovedTurnHudNames);
            BaUiPanelHost.PurgeOrphanRoots(OrphanUiRootPrefixes);
        }
    }
}