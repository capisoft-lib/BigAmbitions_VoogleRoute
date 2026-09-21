using System;
using BigAmbitions.ModsInternal;

namespace VoogleRoute.Phone
{
    /// <summary>
    /// Same activation test as Gagazon and LIB_BaElectricCar: only an enabled
    /// Aluna's Phone Pages entry counts. Downloaded files or loaded assemblies do not.
    /// </summary>
    internal static class PhonePagesPresence
    {
        internal const string SteamModId = "3739104140";
        internal const string DisplayName = "Aluna's Phone Pages";

        internal static bool IsActive()
        {
            var activeMods = ModLifecycleLoader.GetActiveMods();
            if (activeMods == null)
                return false;

            foreach (var mod in activeMods)
            {
                if (mod.Item1 == SteamModId
                    || string.Equals(mod.Item2, DisplayName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
