using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VoogleRoute.VisualTests
{
    internal sealed class VisualTestManifest
    {
        [JsonPropertyName("gameVersion")]
        public string GameVersion { get; set; }

        [JsonPropertyName("characterId")]
        public string CharacterId { get; set; }

        [JsonPropertyName("captureDefaults")]
        public VisualTestCaptureDefaults CaptureDefaults { get; set; } = new VisualTestCaptureDefaults();

        [JsonPropertyName("scenarios")]
        public List<VisualTestScenarioDefinition> Scenarios { get; set; } = new List<VisualTestScenarioDefinition>();
    }

    internal sealed class VisualTestCaptureDefaults
    {
        [JsonPropertyName("delaySeconds")]
        public double DelaySeconds { get; set; } = 2.0;

        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "panel";

        [JsonPropertyName("marginPixels")]
        public int MarginPixels { get; set; } = 4;
    }

    internal sealed class VisualTestScenarioDefinition
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("saveName")]
        public string SaveName { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("captureTarget")]
        public string CaptureTarget { get; set; }

        [JsonPropertyName("postLoad")]
        public List<string> PostLoad { get; set; } = new List<string>();
    }

    internal sealed class VisualTestRequest
    {
        [JsonPropertyName("scenarioId")]
        public string ScenarioId { get; set; }

        [JsonPropertyName("saveName")]
        public string SaveName { get; set; }

        [JsonPropertyName("captureDelaySeconds")]
        public double CaptureDelaySeconds { get; set; } = 2.0;

        [JsonPropertyName("captureMode")]
        public string CaptureMode { get; set; } = "panel";

        [JsonPropertyName("marginPixels")]
        public int MarginPixels { get; set; } = 4;

        [JsonPropertyName("outputPath")]
        public string OutputPath { get; set; }
    }

    internal sealed class VisualTestResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("scenarioId")]
        public string ScenarioId { get; set; }

        [JsonPropertyName("outputPath")]
        public string OutputPath { get; set; }

        [JsonPropertyName("captureMode")]
        public string CaptureMode { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("completedAtUtc")]
        public string CompletedAtUtc { get; set; }
    }
}
