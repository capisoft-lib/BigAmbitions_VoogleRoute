using System.IO;
using VoogleRoute.Pathfinding.Graph;

namespace VoogleRoute.Navigation
{
    /// <summary>
    /// Graphe routier CSV — même source et même loader que le debugger Blazor.
    /// </summary>
    internal static class RouteGraphStore
    {
        private static RouteGraph _graph;

        internal static bool IsReady => _graph != null;

        internal static RouteGraph Graph
        {
            get
            {
                if (_graph == null)
                    TryEnsureLoaded();
                return _graph;
            }
        }

        internal static void WarmUp()
        {
            if (_graph != null)
                return;

            TryEnsureLoaded();
        }

        internal static bool TryEnsureLoaded()
        {
            if (_graph != null)
                return true;

            var path = ModStoragePaths.PathInModRoot(ModStoragePaths.EnhancedRoutesCsv);
            if (!File.Exists(path))
            {
                ModLog.Error(
                    "Route CSV not found | tried=" + path +
                    " mod_root=" + ModStoragePaths.ModRootDirectory);
                return false;
            }

            try
            {
                _graph = CsvRouteGraphLoader.LoadFromEnhancedCsv(path);
                ModLog.Info("CSV route graph loaded (" + _graph.Size + " nodes) from " + path);
                return true;
            }
            catch (System.Exception ex)
            {
                ModLog.Error("Failed to load CSV route graph", ex);
                _graph = null;
                return false;
            }
        }

        internal static void Invalidate()
        {
            _graph = null;
        }
    }
}
