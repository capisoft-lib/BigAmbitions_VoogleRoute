using UnityEngine;
using VoogleRoute.Phone;

namespace VoogleRoute
{
    [DefaultExecutionOrder(-200)]
    internal sealed class VoogleRouteDriver : MonoBehaviour
    {
        internal static VoogleRouteDriver Instance { get; private set; }

        private void Awake() => Instance = this;

        private void OnDisable()
        {
            if (Instance == this) Navigation.Driving.PhysicalDriveService.Stop();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Navigation.Driving.PhysicalDriveService.Stop();
                Instance = null;
            }
        }

        private void Update()
        {
            Navigation.Driving.PhysicalDriveService.TickInput();
            RouteActionShortcuts.Tick();
            VoogleRouteLoop.Tick();
        }

        private void LateUpdate()
        {
            Navigation.Driving.PhysicalDriveService.LateTick();
            PhoneGpsPanel.LateTick();
        }

        private void FixedUpdate() => Navigation.Driving.PhysicalDriveService.FixedTick();
    }
}
