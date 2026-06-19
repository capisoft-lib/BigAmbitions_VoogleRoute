using BigAmbitions.InputSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using VoogleRoute;

namespace VoogleRoute.Navigation
{
    /// <summary>Détecte une prise de contrôle manuelle (WASD / clic sol) pendant l'auto-walk.</summary>
    internal static class ManualMovementInputDetector
    {
        private const float MoveCancelThresholdSq = 0.04f;
        private const float SuppressSecondsAfterAutoIssue = 0.75f;

        private static float _suppressUntil = -1f;

        internal static void SuppressBriefly() =>
            _suppressUntil = Time.unscaledTime + SuppressSecondsAfterAutoIssue;

        internal static bool HasManualMovementInput()
        {
            if (Time.unscaledTime < _suppressUntil)
                return false;

            if (GameState.IsOverlayBlockingNavigation())
                return false;

            try
            {
                if (GameManager.ShouldBlockKeyboardShortcuts())
                    return false;

                if (PlayerAction.Move.Pressing() &&
                    PlayerAction.Move.Vector().sqrMagnitude >= MoveCancelThresholdSq)
                    return true;

                if (PlayerAction.Click.Pressed() && !IsPointerOverUi())
                    return true;
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static bool IsPointerOverUi()
        {
            try
            {
                var eventSystem = EventSystem.current;
                return eventSystem != null && eventSystem.IsPointerOverGameObject();
            }
            catch
            {
                return false;
            }
        }
    }
}
