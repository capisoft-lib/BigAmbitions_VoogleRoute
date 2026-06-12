using System;
using System.IO;
using BAModAPI;
using UnityEngine;

namespace VoogleRoute
{
    /// <summary>
    /// All mod files (config, logs, data) live under <see cref="ModContext.ModRootPath"/>.
    /// ModsLocal is never touched unless the game itself installs the mod there (then ModRootPath points at it).
    /// </summary>
    internal static class ModStoragePaths
    {
        internal const string ModId = "VoogleRoute";
        internal const string ModsLocalFolder = "ModsLocal";
        internal const string LogsFolder = "Logs";
        internal const string ConfigFileName = "config.json";
        internal const string BookmarksFileName = "bookmarks.json";
        internal const string EnhancedRoutesCsv = "Data/big_ambitions_enhanced_routes.csv";

        private static string _modRoot;
        private static bool _migrationDone;

        internal static string ModRootDirectory
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_modRoot))
                    return _modRoot;

                return FallbackModsLocalRoot();
            }
        }

        internal static void Initialize(ModContext context)
        {
            _modRoot = string.IsNullOrWhiteSpace(context?.ModRootPath)
                ? null
                : context.ModRootPath;
            EnsureMigrated();
        }

        internal static void Shutdown() => _modRoot = null;

        internal static string PathInModRoot(string relativePath) =>
            CombineRelative(ModRootDirectory, relativePath);

        internal static string FileInModRoot(string fileName) =>
            PathInModRoot(fileName);

        private static string FallbackModsLocalRoot()
        {
            var path = Path.Combine(Application.persistentDataPath, ModsLocalFolder, ModId);
            Directory.CreateDirectory(path);
            return path;
        }

        private static string CombineRelative(string root, string relativePath)
        {
            if (Path.IsPathRooted(relativePath))
                throw new ArgumentException("Path must be relative to the mod root.", nameof(relativePath));

            return Path.Combine(root, relativePath);
        }

        private static void EnsureMigrated()
        {
            if (_migrationDone)
                return;

            _migrationDone = true;

            var persistent = Application.persistentDataPath;
            if (string.IsNullOrWhiteSpace(persistent))
                return;

            var root = ModRootDirectory;
            Directory.CreateDirectory(root);

            var legacyRoot = Path.Combine(persistent, ModId);
            if (Directory.Exists(legacyRoot))
            {
                CopyTreeIfMissing(legacyRoot, root);
                TryDeleteLegacyPath(legacyRoot);
            }

            var legacyLineColor = Path.Combine(persistent, "VoogleRoute_line_color.txt");
            var newLineColor = Path.Combine(root, "line_color.txt");
            if (CopyFileIfMissing(legacyLineColor, newLineColor))
                TryDeleteLegacyPath(legacyLineColor);
        }

        private static bool CopyFileIfMissing(string source, string destination)
        {
            if (!File.Exists(source) || File.Exists(destination))
                return false;

            var dir = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.Copy(source, destination);
            return true;
        }

        private static void TryDeleteLegacyPath(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
                else if (Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
            }
            catch
            {
                // Non-fatal if another process still holds the old path.
            }
        }

        private static void CopyTreeIfMissing(string sourceDir, string destinationDir)
        {
            foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                var relative = file.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var target = Path.Combine(destinationDir, relative);
                if (File.Exists(target))
                    continue;

                var targetDir = Path.GetDirectoryName(target);
                if (!string.IsNullOrEmpty(targetDir))
                    Directory.CreateDirectory(targetDir);

                File.Copy(file, target);
            }
        }
    }
}
