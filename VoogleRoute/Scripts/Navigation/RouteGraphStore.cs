using System.IO;
using VoogleRoute.Pathfinding.Graph;

namespace VoogleRoute.Navigation
{
    /// <summary>
    /// Graphe routier CSV — même source et même loader que le debugger Blazor.
    /// </summary>
    internal static class RouteGraphStore
    {
        private const string RelativeCsvPath = "Data/big_ambitions_enhanced_routes.csv";

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

        internal static bool TryEnsureLoaded()
        {
            if (_graph != null)
                return true;

            var path = Path.Combine(ModStoragePaths.ModRootDirectory, RelativeCsvPath);
            if (!File.Exists(path))
            {
                ModLog.Error("Route CSV not found: " + path);
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
