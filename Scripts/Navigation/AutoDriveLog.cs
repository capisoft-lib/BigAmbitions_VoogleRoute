using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace VoogleRoute.Navigation
{
    /// <summary>Always-on auto-drive log (does not depend on config.json logging flag).</summary>
    internal static class AutoDriveLog
    {
        private static StreamWriter _writer;
        private static string _logFilePath;
        private static bool _openFailed;

        internal static string LogFilePath => _logFilePath;

        internal static void Write(string message)
        {
            var line = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " " + message;
            try
            {
                EnsureOpen();
                _writer?.WriteLine(line);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[VoogleRoute][AutoDrive] file write failed: " + ex.Message);
            }

            Debug.Log("[VoogleRoute][AutoDrive] " + message);
        }

        internal static void Shutdown()
        {
            try
            {
                if (_writer != null)
                {
                    _writer.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) +
                                      " session ended");
                    _writer.Dispose();
                }
            }
            catch
            {
                // ignore
            }

            _writer = null;
        }

        private static void EnsureOpen()
        {
            if (_writer != null || _openFailed)
                return;

            try
            {
                var logsDir = ModStoragePaths.PathInModRoot(ModStoragePaths.LogsFolder);
                Directory.CreateDirectory(logsDir);
                _logFilePath = Path.Combine(logsDir, "autodrive.log");
                _writer = new StreamWriter(_logFilePath, append: true) { AutoFlush = true };
                _writer.WriteLine(
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) +
                    " --- autodrive session modRoot=" + ModStoragePaths.ModRootDirectory +
                    " log=" + _logFilePath);
            }
            catch (Exception ex)
            {
                _openFailed = true;
                Debug.LogWarning("[VoogleRoute][AutoDrive] cannot open log file: " + ex.Message);
            }
        }
    }
}
