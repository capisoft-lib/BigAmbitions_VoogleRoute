using UnityEngine;
using VoogleRoute.Phone;

namespace VoogleRoute
{
    [DefaultExecutionOrder(-200)]
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
            RouteActionShortcuts.Tick();
            VoogleRouteLoop.Tick();
        }

        private void LateUpdate()
        {
            PhoneGpsPanel.LateTick();
        }
    }
}
