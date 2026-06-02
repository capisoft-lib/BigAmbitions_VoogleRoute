using System.Net.Http;
using MelonLoader;
using UnityEngine.Events;

namespace VoogleRoute.Update;

/// <summary>Version check, download, staged install on quit, and in-game update dialogs.</summary>
internal static class UpdateService
{
    private enum Phase
    {
        Idle,
        Checking,
        PromptUpdate,
        PromptBackground,
        Downloading,
        Restarting
    }

    private static Phase _phase = Phase.Idle;
    private static UpdateManifest? _checkResultManifest;
    private static UpdateManifest? _downloadManifest;
    private static string? _checkError;
    private static bool _quitHooked;
    private static volatile bool _checkCompleted;
    private static bool _manualCheckRequested;

    private static volatile bool _downloadFinished;
    private static UpdateDownloader.DownloadResult _downloadResult;
    private static bool _downloadRestartWhenDone;

    internal static void Initialize()
    {
        HookQuit();
        UpdateDialogUi.EnsureCreated();

        if (PendingUpdateState.TryLoad(out var pending) && pending != null)
        {
            MelonLogger.Msg(
                $"[Voogle Route] Update v{pending.Version} is ready — will install when you exit the game.");
        }

        if (!ModConfig.CheckForUpdates.Value)
            return;

        StartVersionCheck();
    }

    internal static void Tick()
    {
        if (_phase == Phase.Checking && _checkCompleted)
        {
            _checkCompleted = false;
            if (_checkResultManifest != null)
                ShowUpdatePrompt(_checkResultManifest);
            else if (_checkError != null)
            {
                MelonLogger.Warning($"[Voogle Route] Update check failed: {_checkError}");
                _checkError = null;
                _phase = Phase.Idle;
            }
            else
            {
                if (_manualCheckRequested)
                    MelonLogger.Msg($"[Voogle Route] You are on the latest version (v{ModInfo.Version}).");
                _phase = Phase.Idle;
            }

            _manualCheckRequested = false;
        }

        if (_downloadFinished)
        {
            _downloadFinished = false;
            HandleDownloadFinished();
        }
    }

    internal static void Shutdown()
    {
        UpdateInstaller.TryApplyPendingInstall();
        UpdateDialogUi.Destroy();
    }

    internal static void RequestVersionCheck()
    {
        if (!ModConfig.CheckForUpdates.Value)
        {
            MelonLogger.Msg("[Voogle Route] Enable update checks in Voogle Route Settings first.");
            return;
        }

        if (_phase is Phase.Downloading or Phase.Restarting)
            return;

        _manualCheckRequested = true;
        StartVersionCheck();
    }

    private static void HookQuit()
    {
        if (_quitHooked)
            return;

        _quitHooked = true;
        MelonEvents.OnApplicationDefiniteQuit.Subscribe(OnApplicationDefiniteQuit);
    }

    private static void OnApplicationDefiniteQuit() => UpdateInstaller.OnGameQuitting();

    private static void StartVersionCheck()
    {
        if (_phase == Phase.Downloading || _phase == Phase.Restarting)
            return;

        if (_phase == Phase.Checking)
            return;

        _phase = Phase.Checking;
        _checkCompleted = false;
        _checkResultManifest = null;
        _checkError = null;

        var manifestUrl = ModInfo.LatestManifestUrl;
        var localVersion = ModInfo.Version;

        Task.Run(async () =>
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
                var json = await client.GetStringAsync(manifestUrl).ConfigureAwait(false);
                var manifest = UpdateManifest.TryParse(json);
                if (manifest == null)
                {
                    _checkError = "Invalid latest.json manifest.";
                    return;
                }

                if (!UpdateVersionComparer.IsRemoteNewer(manifest.Version, localVersion))
                    return;

                _checkResultManifest = manifest;
            }
            catch (Exception ex)
            {
                _checkError = ex.Message;
            }
            finally
            {
                _checkCompleted = true;
            }
        });
    }

    private static void ShowUpdatePrompt(UpdateManifest manifest)
    {
        _checkResultManifest = null;

        if (ModConfig.AutoDownloadUpdates.Value)
        {
            MelonLogger.Msg($"[Voogle Route] Update v{manifest.Version} found — downloading automatically.");
            BeginDownload(manifest, restartWhenDone: false);
            return;
        }

        _phase = Phase.PromptUpdate;

        var message =
            $"Update available (v{ModInfo.Version} → v{manifest.Version}).\n\n" +
            "Would you like to install and restart the game?";

        UpdateDialogUi.ShowPrimary(
            message,
            (UnityAction)(() => OnLaterClicked(manifest)),
            (UnityAction)(() => OnNowClicked(manifest)));
    }

    private static void OnLaterClicked(UpdateManifest manifest)
    {
        if (_phase != Phase.PromptUpdate)
            return;

        if (ModConfig.AutoDownloadUpdates.Value)
        {
            BeginDownload(manifest, restartWhenDone: false);
            return;
        }

        _phase = Phase.PromptBackground;
        UpdateDialogUi.ShowBackgroundPrompt(
            "Would you like to download in background for next restart of the game?",
            (UnityAction)OnBackgroundNo,
            (UnityAction)(() => OnBackgroundYes(manifest)));
    }

    private static void OnNowClicked(UpdateManifest manifest) => BeginDownload(manifest, restartWhenDone: true);

    private static void OnBackgroundNo()
    {
        UpdateDialogUi.Hide();
        _phase = Phase.Idle;
    }

    private static void OnBackgroundYes(UpdateManifest manifest) => BeginDownload(manifest, restartWhenDone: false);

    private static void BeginDownload(UpdateManifest manifest, bool restartWhenDone)
    {
        _phase = Phase.Downloading;
        _downloadManifest = manifest;
        _downloadRestartWhenDone = restartWhenDone;

        if (restartWhenDone)
        {
            UpdateDialogUi.EnsureCreated();
            UpdateDialogUi.SetStatus("Downloading update…");
            UpdateDialogUi.SetButtonsEnabled(false);
        }
        else
        {
            UpdateDialogUi.Hide();
            MelonLogger.Msg($"[Voogle Route] Downloading v{manifest.Version} in background…");
        }

        var url = string.IsNullOrWhiteSpace(manifest.LatestDownloadUrl)
            ? manifest.DownloadUrl
            : manifest.LatestDownloadUrl;

        Task.Run(async () =>
        {
            _downloadResult = await UpdateDownloader
                .DownloadAsync(url, UpdatePaths.StagingFilePath, manifest.Sha256)
                .ConfigureAwait(false);
            _downloadFinished = true;
        });
    }

    private static void HandleDownloadFinished()
    {
        var manifest = _downloadManifest;
        _downloadManifest = null;
        var restartWhenDone = _downloadRestartWhenDone;

        if (!_downloadResult.Success)
        {
            MelonLogger.Error($"[Voogle Route] Update download failed: {_downloadResult.Error}");
            UpdateDialogUi.SetStatus("");
            UpdateDialogUi.SetButtonsEnabled(true);
            UpdateDialogUi.Hide();
            _phase = Phase.Idle;
            return;
        }

        if (manifest == null)
        {
            _phase = Phase.Idle;
            return;
        }

        UpdateInstaller.ScheduleInstall(UpdatePaths.StagingFilePath, manifest);

        if (restartWhenDone)
        {
            _phase = Phase.Restarting;
            UpdateDialogUi.SetStatus("Update ready. Restarting…");
            UpdateInstaller.QuitAndRestartAfterInstall();
            return;
        }

        MelonLogger.Msg($"[Voogle Route] v{manifest.Version} downloaded — install on next game exit.");

        if (ModConfig.PromptInstallUpdate.Value)
        {
            _phase = Phase.PromptUpdate;
            UpdateDialogUi.ShowPrimary(
                $"Update v{manifest.Version} is ready.\n\nInstall and restart the game now?",
                (UnityAction)OnPostDownloadInstallLater,
                (UnityAction)OnPostDownloadInstallNow);
            return;
        }

        _phase = Phase.Idle;
    }

    private static void OnPostDownloadInstallLater()
    {
        UpdateDialogUi.Hide();
        _phase = Phase.Idle;
    }

    private static void OnPostDownloadInstallNow()
    {
        _phase = Phase.Restarting;
        UpdateDialogUi.SetStatus("Restarting…");
        UpdateInstaller.QuitAndRestartAfterInstall();
    }
}
