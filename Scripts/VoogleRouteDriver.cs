using UnityEngine;

namespace VoogleRoute
{
    
    internal sealed class VoogleRouteDriver : MonoBehaviour
    {
        private void Update() => VoogleRouteLoop.Tick();
    }
}
