using UnityEngine;

namespace VoogleRoute.Navigation
{
    internal static class VehicleEntranceHelper
    {
        internal static bool TryGetDriverEntrancePosition(VehicleController controller, out Vector3 position)
        {
            position = default;
            if (controller == null)
                return false;

            try
            {
                var targets = controller.NavMeshTargetsPositions;
                if (targets != null && targets.Length > 0 && targets[0].sqrMagnitude > 0.01f)
                {
                    position = targets[0];
                    return true;
                }

                position = controller.transform.position;
                return position.sqrMagnitude > 0.01f;
            }
            catch
            {
                return false;
            }
        }
    }
}
