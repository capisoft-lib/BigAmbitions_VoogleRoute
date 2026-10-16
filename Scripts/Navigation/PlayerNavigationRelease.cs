using Helpers;

namespace VoogleRoute.Navigation
{
    /// <summary>Releases mod-issued navmesh control so vanilla exit zones and click-to-move work.</summary>
    internal static class PlayerNavigationRelease
    {
        internal static void Release() => TryRelease();

        internal static bool TryRelease()
        {
            try
            {
                var player = PlayerHelper.PlayerController;
                if (player == null)
                    return false;

                player.ResetNavigation();
                return true;
            }
            catch
            {
                // The pedestrian agent can still be changing state during vehicle exit.
                return false;
            }
        }
    }
}
