using Helpers;

namespace VoogleRoute.Navigation
{
    /// <summary>Releases mod-issued navmesh control so vanilla exit zones and click-to-move work.</summary>
    internal static class PlayerNavigationRelease
    {
        internal static void Release()
        {
            try
            {
                PlayerHelper.PlayerController?.ResetNavigation();
            }
            catch
            {
                // ignore
            }
        }
    }
}
