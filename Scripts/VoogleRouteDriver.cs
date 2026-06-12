using UnityEngine;
using VoogleRoute.Navigation;

namespace VoogleRoute
{
    internal sealed class VoogleRouteDriver : MonoBehaviour
    {
        private void Update() => VoogleRouteLoop.Tick();

        private void FixedUpdate() => AutoDriveService.PhysicsTick();
    }
}
