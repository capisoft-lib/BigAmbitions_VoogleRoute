using System.Text.Json;
using System.Text.Json.Serialization;

namespace VoogleRoute.Update;

/// <summary>
/// Schema for <c>latest.json</c> at the repository root (auto-update ready).
/// Consumers should fetch <see cref="ModInfo.LatestManifestUrl"/> and deserialize this type.
/// </summary>
internal sealed class UpdateManifest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("gameVersion")]
    public string GameVersion { get; set; } = "";

    [JsonPropertyName("author")]
    public string Author { get; set; } = "";

    [JsonPropertyName("dllName")]
    public string DllName { get; set; } = "";

    [JsonPropertyName("nexusUrl")]
    public string NexusUrl { get; set; } = "";

    [JsonPropertyName("nexusModId")]
    public int NexusModId { get; set; }

    [JsonPropertyName("downloadUrl")]
    public string DownloadUrl { get; set; } = "";

    [JsonPropertyName("latestDownloadUrl")]
    public string LatestDownloadUrl { get; set; } = "";

    [JsonPropertyName("manifestUrl")]
    public string ManifestUrl { get; set; } = "";

    [JsonPropertyName("changelogUrl")]
    public string ChangelogUrl { get; set; } = "";

  /// <summary>SHA-256 hex of the release DLL (optional; set when publishing).</summary>
    [JsonPropertyName("sha256")]
    public string? Sha256 { get; set; }

    [JsonPropertyName("releaseNotes")]
    public string ReleaseNotes { get; set; } = "";

    public static UpdateManifest? TryParse(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<UpdateManifest>(json);
        }
        catch
        {
            return null;
        }
    }
}
