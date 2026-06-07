using System.IO;
using UnityEngine;

namespace VoogleRoute
{
    internal static class ModStoragePaths
    {
        internal const string ModId = "VoogleRoute";

        private static bool _migrationDone;

        internal static string ModRootDirectory
        {
            get
            {
                EnsureMigrated();
                var path = Path.Combine(Application.persistentDataPath, "ModsLocal", ModId);
                Directory.CreateDirectory(path);
                return path;
            }
        }

        internal static string FileInModRoot(string fileName) =>
            Path.Combine(ModRootDirectory, fileName);

        private static void EnsureMigrated()
        {
            if (_migrationDone)
                return;

            _migrationDone = true;

            var persistent = Application.persistentDataPath;
            if (string.IsNullOrWhiteSpace(persistent))
                return;

            var newRoot = Path.Combine(persistent, "ModsLocal", ModId);
            Directory.CreateDirectory(newRoot);

            var legacyRoot = Path.Combine(persistent, ModId);
            if (Directory.Exists(legacyRoot))
            {
                CopyTreeIfMissing(legacyRoot, newRoot);
                TryDeleteLegacyPath(legacyRoot);
            }

            var legacyLineColor = Path.Combine(persistent, "VoogleRoute_line_color.txt");
            var newLineColor = Path.Combine(newRoot, "line_color.txt");
            if (CopyFileIfMissing(legacyLineColor, newLineColor))
                TryDeleteLegacyPath(legacyLineColor);
        }

        private static bool CopyFileIfMissing(string source, string destination)
        {
            if (!File.Exists(source) || File.Exists(destination))
                return false;

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
