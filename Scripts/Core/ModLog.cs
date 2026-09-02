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
        private static readonly object WriteGate = new object();
        private static ModContext _context;
        private static StreamWriter _fileWriter;
        private static string _logFilePath;
        private static bool _fileLoggingEnabled;
        private static ModLogLevel _minLevel = ModLogLevel.Error;
        private static int _pendingFileLines;
        private static DateTime _lastFileFlushUtc;
        private const int BufferedLinesBeforeFlush = 32;
        private static readonly TimeSpan BufferedFlushInterval = TimeSpan.FromSeconds(2);

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
                _fileWriter = new StreamWriter(_logFilePath, append: false) { AutoFlush = false };
                _fileWriter.WriteLine(
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) +
                    " [INFO] Voogle Route session started.");
                _fileWriter.Flush();
                _pendingFileLines = 0;
                _lastFileFlushUtc = DateTime.UtcNow;
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
            lock (WriteGate)
            {
                if (_fileWriter != null)
                {
                    try
                    {
                        _fileWriter.WriteLine(
                            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) +
                            " [INFO] Voogle Route session ended.");
                        _fileWriter.Flush();
                        _fileWriter.Dispose();
                    }
                    catch
                    {
                        // Non-fatal on unload.
                    }

                    _fileWriter = null;
                }

                _pendingFileLines = 0;
            }

            _logFilePath = null;
            _context = null;
        }

        internal static void Debug(string message) => Write(ModLogLevel.Debug, message);

        internal static void Debug(Func<string> messageFactory) => Write(ModLogLevel.Debug, messageFactory);

        internal static void Info(string message) => Write(ModLogLevel.Info, message);

        internal static void Info(Func<string> messageFactory) => Write(ModLogLevel.Info, messageFactory);

        internal static void Error(string message) => Write(ModLogLevel.Error, message);

        internal static void Error(string message, Exception exception) =>
            Write(ModLogLevel.Error, message + ": " + exception.Message + Environment.NewLine + exception.StackTrace);

        internal static bool IsEnabled(ModLogLevel level) => level >= _minLevel;

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

            var match = Regex.Match(
                json,
                "\"" + Regex.Escape(key) + "\"\\s*:\\s*(true|false)",
                RegexOptions.IgnoreCase,
                TimeSpan.FromMilliseconds(250));
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

            var match = Regex.Match(
                json,
                "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]*)\"",
                RegexOptions.IgnoreCase,
                TimeSpan.FromMilliseconds(250));
            if (!match.Success)
                return false;

            value = match.Groups[1].Value;
            return true;
        }

        private static void Write(ModLogLevel level, string message)
        {
            if (!IsEnabled(level))
                return;

            lock (WriteGate)
            {
                if (_fileLoggingEnabled && _fileWriter != null)
                {
                    try
                    {
                        var line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) +
                                   " [" + level.ToString().ToUpperInvariant() + "] " + message;
                        _fileWriter.WriteLine(line);
                        _pendingFileLines++;
                        var nowUtc = DateTime.UtcNow;
                        if (level == ModLogLevel.Error ||
                            _pendingFileLines >= BufferedLinesBeforeFlush ||
                            nowUtc - _lastFileFlushUtc >= BufferedFlushInterval)
                        {
                            _fileWriter.Flush();
                            _pendingFileLines = 0;
                            _lastFileFlushUtc = nowUtc;
                        }
                    }
                    catch
                    {
                        // Ignore disk failures after startup.
                    }
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

        private static void Write(ModLogLevel level, Func<string> messageFactory)
        {
            if (!IsEnabled(level) || messageFactory == null)
                return;
            Write(level, messageFactory());
        }
    }
}
