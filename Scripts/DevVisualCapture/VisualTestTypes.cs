using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace VoogleRoute
{
    internal sealed class VisualTestManifest
    {
        [JsonProperty("gameVersion")]
        public string GameVersion { get; set; }

        [JsonProperty("characterId")]
        public string CharacterId { get; set; }

        [JsonProperty("captureDefaults")]
        public VisualTestCaptureDefaults CaptureDefaults { get; set; } = new VisualTestCaptureDefaults();

        [JsonProperty("scenarios")]
        public List<VisualTestScenarioDefinition> Scenarios { get; set; } = new List<VisualTestScenarioDefinition>();
    }

    internal sealed class VisualTestCaptureDefaults
    {
        [JsonProperty("delaySeconds")]
        public double DelaySeconds { get; set; } = 2.0;

        [JsonProperty("mode")]
        public string Mode { get; set; } = "panel";

        [JsonProperty("marginPixels")]
        public int MarginPixels { get; set; } = 4;
    }

    internal sealed class VisualTestScenarioDefinition
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("saveName")]
        public string SaveName { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("captureTarget")]
        public string CaptureTarget { get; set; }

        [JsonProperty("postLoad")]
        public List<string> PostLoad { get; set; } = new List<string>();
    }

    internal sealed class VisualTestRequest
    {
        [JsonProperty("scenarioId")]
        public string ScenarioId { get; set; }

        [JsonProperty("saveName")]
        public string SaveName { get; set; }

        [JsonProperty("captureDelaySeconds")]
        public double CaptureDelaySeconds { get; set; } = 2.0;

        [JsonProperty("captureMode")]
        public string CaptureMode { get; set; } = "panel";

        [JsonProperty("marginPixels")]
        public int MarginPixels { get; set; } = 4;

        [JsonProperty("outputPath")]
        public string OutputPath { get; set; }
    }

    internal sealed class VisualTestResult
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("scenarioId")]
        public string ScenarioId { get; set; }

        [JsonProperty("outputPath")]
        public string OutputPath { get; set; }

        [JsonProperty("captureMode")]
        public string CaptureMode { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("completedAtUtc")]
        public string CompletedAtUtc { get; set; }
    }
}
