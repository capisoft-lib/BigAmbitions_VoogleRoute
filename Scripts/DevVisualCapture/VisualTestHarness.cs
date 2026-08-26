using System;
using System.Collections;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace VoogleRoute
{
    internal static class VisualTestHarness
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        private static bool _scheduled;
        private static bool _running;

        internal static void TrySchedule(MonoBehaviour host)
        {
            if (_scheduled || _running || host == null)
                return;

            if (!File.Exists(VisualTestPaths.RequestPath))
                return;

            _scheduled = true;
            host.StartCoroutine(RunCaptureRoutine());
        }

        private static IEnumerator RunCaptureRoutine()
        {
            _running = true;
            VisualTestResult result = null;

            try
            {
                var request = LoadRequest();
                if (request == null)
                {
                    result = Fail(null, "request.json missing or invalid.");
                    yield break;
                }

                var scenario = LoadScenario(request);
                if (scenario == null)
                {
                    result = Fail(request.ScenarioId, "Scenario not found in manifest.json.");
                    yield break;
                }

                ModLog.Info(
                    "[VisualTest] Starting scenario=" + request.ScenarioId +
                    " save=" + request.SaveName);

                var timeoutAt = Time.unscaledTime + 60f;
                while (!GameState.IsWorldReady() && Time.unscaledTime < timeoutAt)
                    yield return null;

                if (!GameState.IsWorldReady())
                {
                    result = Fail(request.ScenarioId, "World not ready after 60s.");
                    yield break;
                }

                if (request.CaptureDelaySeconds > 0f)
                    yield return new WaitForSecondsRealtime((float)request.CaptureDelaySeconds);

                yield return VisualTestScenarioRunner.RunPostLoadSteps(scenario.PostLoad);

                var fullScreen = string.Equals(
                    request.CaptureMode,
                    "fullScreen",
                    StringComparison.OrdinalIgnoreCase);

                var captureTarget = scenario.CaptureTarget;
                VisualTestScreenBounds bounds = default;
                if (!fullScreen &&
                    !VisualTestUiTargets.TryResolveScreenBounds(
                        captureTarget,
                        request.MarginPixels,
                        out bounds))
                {
                    result = Fail(
                        request.ScenarioId,
                        "Capture target not available: " + captureTarget +
                        ". Check save state and postLoad steps.");
                    yield break;
                }

                Texture2D texture = null;
                yield return VisualTestCapture.CaptureAfterFrames(
                    bounds,
                    fullScreen,
                    captured => texture = captured);

                if (texture == null)
                {
                    result = Fail(request.ScenarioId, "Screenshot capture returned null.");
                    yield break;
                }

                var outputPath = ResolveOutputPath(request);
                try
                {
                    VisualTestCapture.SavePng(texture, outputPath);
                    result = new VisualTestResult
                    {
                        Success = true,
                        ScenarioId = request.ScenarioId,
                        OutputPath = outputPath,
                        CaptureMode = request.CaptureMode,
                        Width = texture.width,
                        Height = texture.height,
                        CompletedAtUtc = DateTime.UtcNow.ToString("o")
                    };
                    ModLog.Info("[VisualTest] Saved " + outputPath);
                }
                finally
                {
                    UnityEngine.Object.Destroy(texture);
                }
            }
            finally
            {
                if (result != null)
                    WriteResult(result);

                ArchiveRequest();
                _running = false;
                _scheduled = false;
            }
        }

        private static VisualTestRequest LoadRequest()
        {
            try
            {
                VisualTestPaths.EnsureRoot();
                if (!File.Exists(VisualTestPaths.RequestPath))
                    return null;

                var json = File.ReadAllText(VisualTestPaths.RequestPath);
                return JsonConvert.DeserializeObject<VisualTestRequest>(json, JsonSettings);
            }
            catch (Exception ex)
            {
                ModLog.Info("[VisualTest] Failed to read request: " + ex.Message);
                return null;
            }
        }

        private static VisualTestScenarioDefinition LoadScenario(VisualTestRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ScenarioId))
                return null;

            if (!File.Exists(VisualTestPaths.ManifestPath))
                return null;

            try
            {
                var json = File.ReadAllText(VisualTestPaths.ManifestPath);
                var manifest = JsonConvert.DeserializeObject<VisualTestManifest>(json, JsonSettings);
                return manifest?.Scenarios?
                    .FirstOrDefault(s => string.Equals(s.Id, request.ScenarioId, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                ModLog.Info("[VisualTest] Failed to read manifest: " + ex.Message);
                return null;
            }
        }

        private static string ResolveOutputPath(VisualTestRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.OutputPath))
                return request.OutputPath;

            VisualTestPaths.EnsureRoot();
            return Path.Combine(VisualTestPaths.RootDirectory, "output", request.ScenarioId + ".png");
        }

        private static VisualTestResult Fail(string scenarioId, string error) =>
            new VisualTestResult
            {
                Success = false,
                ScenarioId = scenarioId,
                Error = error,
                CompletedAtUtc = DateTime.UtcNow.ToString("o")
            };

        private static void WriteResult(VisualTestResult result)
        {
            try
            {
                VisualTestPaths.EnsureRoot();
                var json = JsonConvert.SerializeObject(result, JsonSettings);
                File.WriteAllText(VisualTestPaths.ResultPath, json);
            }
            catch (Exception ex)
            {
                ModLog.Info("[VisualTest] Failed to write result: " + ex.Message);
            }
        }

        private static void ArchiveRequest()
        {
            try
            {
                if (!File.Exists(VisualTestPaths.RequestPath))
                    return;

                if (File.Exists(VisualTestPaths.ProcessedRequestPath))
                    File.Delete(VisualTestPaths.ProcessedRequestPath);

                File.Move(VisualTestPaths.RequestPath, VisualTestPaths.ProcessedRequestPath);
            }
            catch (Exception ex)
            {
                ModLog.Info("[VisualTest] Failed to archive request: " + ex.Message);
            }
        }
    }
}
