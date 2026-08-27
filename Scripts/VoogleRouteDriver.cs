using UnityEngine;

namespace VoogleRoute
{
    internal sealed class VoogleRouteDriver : MonoBehaviour
    {
        internal static VoogleRouteDriver Instance { get; private set; }

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            VoogleRouteLoop.Tick();
            RouteActionShortcuts.Tick();
        }
    }
}
