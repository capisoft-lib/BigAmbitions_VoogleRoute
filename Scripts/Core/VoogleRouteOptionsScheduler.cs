using UnityEngine;

namespace VoogleRoute
{
    /// <summary>Defers mod-options re-registration so OptionsService is not torn down mid-UI init.</summary>
    internal sealed class VoogleRouteOptionsScheduler : MonoBehaviour
    {
        private static VoogleRouteOptionsScheduler _instance;
        private bool _refreshPending;

        internal static void RequestRefresh()
        {
            EnsureRunning();
            _instance._refreshPending = true;
        }

        internal static void Shutdown()
        {
            if (_instance == null)
                return;

            var host = _instance.gameObject;
            _instance = null;
            Object.Destroy(host);
        }

        internal static void EnsureRunning()
        {
            if (_instance != null)
                return;

            var host = new GameObject("VoogleRoute_OptionsScheduler");
            host.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(host);
            _instance = host.AddComponent<VoogleRouteOptionsScheduler>();
        }

        private void Update()
        {
            if (!_refreshPending)
                return;

            _refreshPending = false;
            ModConfig.RefreshOptionsRegistered();
        }
    }
}
