using System;
using System.Reflection;
using UnityEngine;

namespace VoogleRoute.Phone
{
    /// <summary>
    /// Aluna's Phone Pages polls the mouse wheel in Update to change home-screen
    /// pages. Keep that blocked while the GPS app is open so the wheel zooms the map.
    /// </summary>
    internal static class PhonePagesInputGuard
    {
        private static Type _runnerType;
        private static PropertyInfo _instanceProperty;
        private static FieldInfo _wheelCooldown;
        private static bool _resolved;

        internal static void SuppressPageScroll()
        {
            SetWheelCooldown(1f);
        }

        internal static void Release()
        {
            SetWheelCooldown(0f);
        }

        private static void SetWheelCooldown(float value)
        {
            if (!TryResolve())
                return;

            try
            {
                var instance = _instanceProperty.GetValue(null);
                if (instance == null)
                    return;
                _wheelCooldown.SetValue(instance, value);
            }
            catch
            {
                // Optional Aluna internals; GPS zoom still works from Input.mouseScrollDelta.
            }
        }

        private static bool TryResolve()
        {
            if (_resolved)
                return _runnerType != null && _instanceProperty != null && _wheelCooldown != null;

            _resolved = true;
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (var i = 0; i < assemblies.Length; i++)
                {
                    var name = assemblies[i].GetName().Name;
                    if (name == null || name.IndexOf("AlunaPhonePages", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    _runnerType = assemblies[i].GetType("AlunaPhonePages.PhonePagesRunner");
                    break;
                }

                if (_runnerType == null)
                    return false;

                _instanceProperty = _runnerType.GetProperty(
                    "Instance",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                _wheelCooldown = _runnerType.GetField(
                    "_wheelCooldown",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }
            catch
            {
                _runnerType = null;
            }

            return _runnerType != null && _instanceProperty != null && _wheelCooldown != null;
        }
    }
}
