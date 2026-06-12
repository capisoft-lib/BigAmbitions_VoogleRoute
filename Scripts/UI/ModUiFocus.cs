using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace VoogleRoute.UI
{
    /// <summary>
    /// Clears UI EventSystem selection so WASD/arrows and click-to-move work again.
    /// GameManager.HasInputSelected blocks CityMapCam and MouseController while a UI button stays selected.
    /// </summary>
    internal static class ModUiFocus
    {
        internal static void ReleaseForMovement()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null || eventSystem.currentSelectedGameObject == null)
                return;

            eventSystem.SetSelectedGameObject(null);
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
