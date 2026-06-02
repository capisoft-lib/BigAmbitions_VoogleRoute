using System.Diagnostics;
using MelonLoader;
using UnityEngine;

namespace VoogleRoute.Update;

internal static class UpdateInstaller
{
    private static bool _restartAfterInstall;

    internal static string GetModDllPath()
    {
        var location = typeof(Plugin).Assembly.Location;
        if (!string.IsNullOrWhiteSpace(location) && File.Exists(location))
            return location;

        return Path.Combine(GetModsDirectory(), "VoogleRoute.dll");
    }

    internal static string GetModsDirectory()
    {
        var dll = GetModDllPath();
        return Path.GetDirectoryName(dll) ?? throw new InvalidOperationException("Cannot resolve Mods folder.");
    }

    internal static void ScheduleInstall(string stagingPath, UpdateManifest manifest)
    {
        var target = GetModDllPath();
        new PendingUpdateState
        {
            Version = manifest.Version,
            StagingPath = stagingPath,
            TargetPath = target,
            DownloadUrl = manifest.LatestDownloadUrl
        }.Save();

        MelonLogger.Msg($"[Voogle Route] Update v{manifest.Version} staged; will apply when the game exits.");
    }

    internal static bool TryApplyPendingInstall()
    {
        if (!PendingUpdateState.TryLoad(out var pending) || pending == null)
            return false;

        try
        {
            var target = pending.TargetPath;
            var staging = pending.StagingPath;

            if (!File.Exists(staging))
            {
                PendingUpdateState.Clear();
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);

            var backup = target + ".bak";
            if (File.Exists(target))
            {
                try
                {
                    if (File.Exists(backup))
                        File.Delete(backup);
                    File.Copy(target, backup, overwrite: true);
                }
                catch
                {
                    // Best-effort backup; continue with install.
                }
            }

            File.Copy(staging, target, overwrite: true);
            PendingUpdateState.Clear();

            try
            {
                if (File.Exists(staging))
                    File.Delete(staging);
            }
            catch
            {
                // ignored
            }

            MelonLogger.Msg($"[Voogle Route] Applied update v{pending.Version} to Mods folder.");
            return true;
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[Voogle Route] Failed to apply pending update: {ex.Message}");
            return false;
        }
    }

    /// <summary>Quit now; apply pending DLL on exit, then relaunch the game executable.</summary>
    internal static void QuitAndRestartAfterInstall()
    {
        _restartAfterInstall = true;
        MelonLogger.Msg("[Voogle Route] Installing update and restarting…");
        Application.Quit();
    }

    internal static void OnGameQuitting()
    {
        TryApplyPendingInstall();

        if (!_restartAfterInstall)
            return;

        _restartAfterInstall = false;
        LaunchGameExecutable();
    }

    private static void LaunchGameExecutable()
    {
        var exe = ResolveGameExecutable();
        if (string.IsNullOrEmpty(exe) || !File.Exists(exe))
        {
            MelonLogger.Error("[Voogle Route] Could not find Big Ambitions executable to restart.");
            return;
        }

        var workDir = Path.GetDirectoryName(exe)!;
        Process.Start(new ProcessStartInfo
        {
            FileName = exe,
            WorkingDirectory = workDir,
            UseShellExecute = true
        });
    }

    private static string? ResolveGameExecutable()
    {
        var dataDir = Application.dataPath;
        if (string.IsNullOrEmpty(dataDir))
            return null;

        var root = Directory.GetParent(dataDir)?.FullName;
        if (string.IsNullOrEmpty(root))
            return null;

        var preferred = Path.Combine(root, "Big Ambitions.exe");
        if (File.Exists(preferred))
            return preferred;

        foreach (var exe in Directory.EnumerateFiles(root, "*.exe"))
        {
            var name = Path.GetFileName(exe);
            if (name.Contains("Unity", StringComparison.OrdinalIgnoreCase))
                continue;
            if (name.Contains("Crash", StringComparison.OrdinalIgnoreCase))
                continue;
            return exe;
        }

        return null;
    }
}
