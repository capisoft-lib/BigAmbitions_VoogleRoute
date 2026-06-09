using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using BAModAPI;
using UnityEngine;

namespace VoogleRoute
{
    internal static class ModLog
    {
        private static ModContext _context;
        private static StreamWriter _fileWriter;
        private static string _logFilePath;
        private static bool _fileLoggingEnabled;
        private static ModLogLevel _minLevel = ModLogLevel.Error;

        internal static string LogFilePath => _logFilePath;

        internal static bool FileLoggingEnabled => _fileLoggingEnabled;
        internal static ModLogLevel MinLevel => _minLevel;

        internal static void Configure(bool fileLoggingEnabled, ModLogLevel minLevel)
        {
            _fileLoggingEnabled = fileLoggingEnabled;
            _minLevel = minLevel;
        }

        internal static void Initialize(ModContext context)
        {
            _context = context;
            _logFilePath = null;

            if (!_fileLoggingEnabled)
            {
                Info("ModLog initialized (file logging disabled, console only).");
                return;
            }

            try
            {
                var logsDir = ModStoragePaths.PathInModRoot(ModStoragePaths.LogsFolder);
                Directory.CreateDirectory(logsDir);

                var fileName = "voogle-route_" +
                    DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture) + ".log";
                _logFilePath = Path.Combine(logsDir, fileName);
                _fileWriter = new StreamWriter(_logFilePath, append: false) { AutoFlush = true };
                _fileWriter.WriteLine(
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) +
                    " [INFO] Voogle Route session started.");
                Info("ModLog initialized | file=" + _logFilePath);
            }
            catch (Exception ex)
            {
                _fileLoggingEnabled = false;
                _logFilePath = null;
                _context?.Logger.Info("[VoogleRoute] File logging disabled: " + ex.Message);
            }
        }

        internal static void Shutdown()
        {
            if (_fileWriter != null)
            {
                try
                {
                    _fileWriter.WriteLine(
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) +
                        " [INFO] Voogle Route session ended.");
                    _fileWriter.Dispose();
                }
                catch
                {
                    // Non-fatal on unload.
                }

                _fileWriter = null;
            }

            _logFilePath = null;
            _context = null;
        }

        internal static void Debug(string message) => Write(ModLogLevel.Debug, message);

        internal static void Info(string message) => Write(ModLogLevel.Info, message);

        internal static void Error(string message) => Write(ModLogLevel.Error, message);

        internal static void Error(string message, Exception exception) =>
            Write(ModLogLevel.Error, message + ": " + exception.Message + Environment.NewLine + exception.StackTrace);

        internal static ModLogLevel ParseLevel(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return ModLogLevel.Error;

            switch (value.Trim().ToLowerInvariant())
            {
                case "debug":
                    return ModLogLevel.Debug;
                case "info":
                    return ModLogLevel.Info;
                case "error":
                    return ModLogLevel.Error;
                default:
                    return ModLogLevel.Error;
            }
        }

        internal static bool TryReadBool(string json, string key, out bool value)
        {
            value = false;
            if (string.IsNullOrWhiteSpace(json))
                return false;

            var match = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
            if (!match.Success)
                return false;

            value = string.Equals(match.Groups[1].Value, "true", StringComparison.OrdinalIgnoreCase);
            return true;
        }

        internal static bool TryReadString(string json, string key, out string value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(json))
                return false;

            var match = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]*)\"", RegexOptions.IgnoreCase);
            if (!match.Success)
                return false;

            value = match.Groups[1].Value;
            return true;
        }

        private static void Write(ModLogLevel level, string message)
        {
            if (level < _minLevel)
                return;

            var line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) +
                       " [" + level.ToString().ToUpperInvariant() + "] " + message;

            if (_fileLoggingEnabled && _fileWriter != null)
            {
                try
                {
                    _fileWriter.WriteLine(line);
                }
                catch
                {
                    // Ignore disk failures after startup.
                }
            }

            if (_context == null)
                return;

            var gameMessage = level == ModLogLevel.Debug
                ? "[DEBUG] " + message
                : level == ModLogLevel.Error
                    ? "[ERROR] " + message
                    : message;

            _context.Logger.Info(gameMessage);
        }
    }
}
