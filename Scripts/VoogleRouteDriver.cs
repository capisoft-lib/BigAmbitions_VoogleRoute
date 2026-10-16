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
            if (Instance == this)
            {
                Navigation.Driving.PhysicalDriveService.Stop(reason: "driver_disabled");
                Navigation.Driving.DrivingPerformance.Flush(force: true);
            }
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
            Navigation.Driving.DrivingPerformance.Frame();
            var started = Navigation.Driving.DrivingPerformance.Begin();
            try
            {
                Navigation.Driving.PhysicalDriveService.TickInput();
                RouteActionShortcuts.Tick();
                var navigationStarted = Navigation.Driving.DrivingPerformance.Begin();
                try { VoogleRouteLoop.Tick(); }
                finally { Navigation.Driving.DrivingPerformance.End(Navigation.Driving.DrivingStage.Navigation, navigationStarted); }
            }
            finally { Navigation.Driving.DrivingPerformance.End(Navigation.Driving.DrivingStage.Update, started); }
        }

        private void LateUpdate()
        {
            Navigation.Driving.PhysicalDriveService.LateTick();
            var started = Navigation.Driving.DrivingPerformance.Begin();
            try { PhoneGpsPanel.LateTick(); }
            finally { Navigation.Driving.DrivingPerformance.End(Navigation.Driving.DrivingStage.PhoneUi, started); }
            Navigation.Driving.DrivingPerformance.Flush();
        }

        private void FixedUpdate()
        {
            if (!Navigation.Driving.PhysicalDriveService.IsActive) return;
            var started = Navigation.Driving.DrivingPerformance.Begin();
            try { Navigation.Driving.PhysicalDriveService.FixedTick(); }
            finally { Navigation.Driving.DrivingPerformance.End(Navigation.Driving.DrivingStage.Fixed, started); }
        }
    }
}
