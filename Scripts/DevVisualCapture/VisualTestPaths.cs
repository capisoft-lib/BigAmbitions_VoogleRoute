using System.IO;

namespace VoogleRoute
{
    internal static class VisualTestPaths
    {
        internal const string FolderName = "visual-test";
        internal const string ManifestFileName = "manifest.json";
        internal const string RequestFileName = "request.json";
        internal const string ResultFileName = "last-result.json";
        internal const string ProcessedRequestFileName = "request.processed.json";

        internal static string RootDirectory =>
            ModStoragePaths.PathInModRoot(FolderName);

        internal static string ManifestPath =>
            Path.Combine(RootDirectory, ManifestFileName);

        internal static string RequestPath =>
            Path.Combine(RootDirectory, RequestFileName);

        internal static string ResultPath =>
            Path.Combine(RootDirectory, ResultFileName);

        internal static string ProcessedRequestPath =>
            Path.Combine(RootDirectory, ProcessedRequestFileName);

        internal static void EnsureRoot()
        {
            Directory.CreateDirectory(RootDirectory);
        }
    }
}
