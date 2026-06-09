using UnityEngine;

namespace VoogleRoute.UI
{
    internal static class LegacyTurnHudCleanup
    {
        private static readonly string[] ObjectNames =
        {
            "VoogleRoute_TurnHudRoot",
            "TurnPanel",
            "IntersectionSchematic",
        };

        internal static void DestroyAll()
        {
            for (var n = 0; n < ObjectNames.Length; n++)
            {
                var name = ObjectNames[n];
                GameObject go;
                while ((go = GameObject.Find(name)) != null)
                    Object.Destroy(go);
            }
        }
    }
}
