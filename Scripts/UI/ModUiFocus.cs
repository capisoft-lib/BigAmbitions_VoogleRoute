using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace VoogleRoute.UI
{
    /// <summary>
    /// Clears Voogle Route UI selection so WASD/arrows and click-to-move work again.
    /// GameManager.HasInputSelected blocks CityMapCam and MouseController while a UI button stays selected.
    /// Only mod-owned roots (<c>VoogleRoute_*</c>) are cleared — never vanilla game UI.
    /// </summary>
    internal static class ModUiFocus
    {
        private const string ModUiRootPrefix = "VoogleRoute_";

        internal static void ReleaseForMovement()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return;

            var selected = eventSystem.currentSelectedGameObject;
            if (selected == null || !IsUnderModUiRoot(selected))
                return;

            eventSystem.SetSelectedGameObject(null);
        }

        internal static bool IsUnderModUiRoot(GameObject go)
        {
            if (go == null)
                return false;

            for (var t = go.transform; t != null; t = t.parent)
            {
                if (t.name.StartsWith(ModUiRootPrefix, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        internal static UnityAction Wrap(UnityAction action)
        {
            if (action == null)
                return null;

            return () =>
            {
                action();
                ReleaseForMovement();
            };
        }
    }
}
