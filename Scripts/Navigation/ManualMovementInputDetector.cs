using System.Reflection;
using Helpers;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VoogleRoute.Navigation
{
    /// <summary>Détecte une prise de contrôle manuelle (WASD / clic sol) pendant l'auto-walk.</summary>
    internal static class ManualMovementInputDetector
    {
        private const float MoveCancelThresholdSq = 0.04f;
        private const float SuppressSecondsAfterAutoIssue = 0.4f;

        private static readonly BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.Instance;
        private static MethodInfo _readVector2Method;

        private static float _suppressUntil = -1f;

        internal static void SuppressBriefly() =>
            _suppressUntil = Time.unscaledTime + SuppressSecondsAfterAutoIssue;

        internal static bool HasManualMovementInput()
        {
            if (Time.unscaledTime < _suppressUntil)
                return false;

            if (GameState.IsOverlayBlockingNavigation())
                return false;

            if (!TryGetPlayerAction("Move", out var moveAction))
                return false;

            try
            {
                if (IsActionPressed(moveAction) && ReadVector2SqrMagnitude(moveAction) >= MoveCancelThresholdSq)
                    return true;

                if (TryGetPlayerAction("Click", out var clickAction) &&
                    WasPressedThisFrame(clickAction) &&
                    !IsPointerOverUi())
                    return true;
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static bool TryGetPlayerAction(string actionName, out object action)
        {
            action = null;

            try
            {
                if (!InputHelper.IsInitialized())
                    return false;

                var input = InputHelper.playerInput;
                if (input == null)
                    return false;

                var playerActions = input.GetType().GetProperty("Player", InstanceFlags)?.GetValue(input);
                if (playerActions == null)
                    return false;

                var enabled = playerActions.GetType().GetProperty("enabled", InstanceFlags)?.GetValue(playerActions);
                if (enabled is bool isEnabled && !isEnabled)
                    return false;

                action = playerActions.GetType().GetProperty(actionName, InstanceFlags)?.GetValue(playerActions);
                return action != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsActionPressed(object action) =>
            action.GetType().GetProperty("IsPressed", InstanceFlags)?.GetValue(action) is true;

        private static bool WasPressedThisFrame(object action) =>
            action.GetType().GetProperty("WasPressedThisFrame", InstanceFlags)?.GetValue(action) is true;

        private static float ReadVector2SqrMagnitude(object action)
        {
            if (_readVector2Method == null)
            {
                foreach (var method in action.GetType().GetMethods(InstanceFlags))
                {
                    if (method.Name != "ReadValue" || !method.IsGenericMethodDefinition || method.GetParameters().Length != 0)
                        continue;

                    _readVector2Method = method.MakeGenericMethod(typeof(Vector2));
                    break;
                }
            }

            if (_readVector2Method == null)
                return 0f;

            if (_readVector2Method.Invoke(action, null) is Vector2 value)
                return value.sqrMagnitude;

            return 0f;
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
