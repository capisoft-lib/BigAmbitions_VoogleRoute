using System.Reflection;
using UnityEngine;

namespace VoogleRoute.Navigation
{
    /// <summary>Detects manual throttle, brake, or steering during auto-drive.</summary>
    internal static class ManualVehicleInputDetector
    {
        private const float ThrottleBrakeThreshold = 0.08f;
        private const float SteeringThreshold = 0.12f;
        private const float SuppressSecondsAfterAutoApply = 0.35f;

        private static readonly BindingFlags StaticFlags = BindingFlags.Public | BindingFlags.Static;
        private static readonly BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.Instance;

        private static float _suppressUntil = -1f;
        private static Assembly _externalPluginsAssembly;

        internal static void SuppressBriefly() =>
            _suppressUntil = Time.unscaledTime + SuppressSecondsAfterAutoApply;

        internal static bool HasManualVehicleInput()
        {
            if (Time.unscaledTime < _suppressUntil)
                return false;

            if (GameState.IsOverlayBlockingNavigation())
                return false;

            try
            {
                if (TryReadVehicleAction("Throttle", out var throttle) && throttle > ThrottleBrakeThreshold)
                {
                AutoDriveLog.Write("manual throttle=" + throttle.ToString("F2"));
                    return true;
                }

                if (TryReadVehicleAction("Brakes", out var brakes) && brakes > ThrottleBrakeThreshold)
                {
                AutoDriveLog.Write("manual brakes=" + brakes.ToString("F2"));
                    return true;
                }

                if (TryReadVehicleAction("Brake", out brakes) && brakes > ThrottleBrakeThreshold)
                {
                AutoDriveLog.Write("manual brake=" + brakes.ToString("F2"));
                    return true;
                }

                if (TryReadVehicleAction("Steering", out var steering) &&
                    Mathf.Abs(steering) > SteeringThreshold)
                {
                AutoDriveLog.Write("manual steering=" + steering.ToString("F2"));
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                AutoDriveDiagnostics.LogBlockedOnce("manual input read failed: " + ex.Message);
                return false;
            }

            return false;
        }

        private static bool TryReadVehicleAction(string actionName, out float value)
        {
            value = 0f;

            var inputSystemType = ResolveInputSystemProviderType();
            if (inputSystemType == null)
                return false;

            var actionsField = inputSystemType.GetField("vehicleInputActions", StaticFlags);
            var asset = actionsField?.GetValue(null);
            if (asset == null)
                return false;

            var findAction = asset.GetType().GetMethod("FindAction", InstanceFlags, null, new[] { typeof(string) }, null);
            if (findAction == null)
                return false;

            var action = findAction.Invoke(asset, new object[] { actionName });
            if (action == null)
                return false;

            var enabledProp = action.GetType().GetProperty("enabled", InstanceFlags);
            if (enabledProp?.GetValue(action) is bool isEnabled && !isEnabled)
                action.GetType().GetMethod("Enable", InstanceFlags)?.Invoke(action, null);

            foreach (var method in action.GetType().GetMethods(InstanceFlags))
            {
                if (method.Name != "ReadValue" || !method.IsGenericMethodDefinition || method.GetParameters().Length != 0)
                    continue;

                value = (float)method.MakeGenericMethod(typeof(float)).Invoke(action, null);
                return true;
            }

            return false;
        }

        private static System.Type ResolveInputSystemProviderType()
        {
            try
            {
                _externalPluginsAssembly ??= System.Reflection.Assembly.Load("ExternalPlugins");
                return _externalPluginsAssembly.GetType("NWH.VehiclePhysics2.Input.InputSystemVehicleInputProvider");
            }
            catch
            {
                return null;
            }
        }
    }
}
