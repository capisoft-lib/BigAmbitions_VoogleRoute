using System.Text.Json;
using System.Text.Json.Serialization;

namespace VoogleRoute.Update;

internal sealed class PendingUpdateState
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("stagingPath")]
    public string StagingPath { get; set; } = "";

    [JsonPropertyName("targetPath")]
    public string TargetPath { get; set; } = "";

    [JsonPropertyName("downloadUrl")]
    public string DownloadUrl { get; set; } = "";

    public static bool TryLoad(out PendingUpdateState? state)
    {
        state = null;
        var path = UpdatePaths.PendingFilePath;
        if (!File.Exists(path))
            return false;

        try
        {
            var json = File.ReadAllText(path);
            state = JsonSerializer.Deserialize<PendingUpdateState>(json);
            return state != null
                   && !string.IsNullOrWhiteSpace(state.StagingPath)
                   && File.Exists(state.StagingPath)
                   && !string.IsNullOrWhiteSpace(state.TargetPath);
        }
        catch
        {
            return false;
        }
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(UpdatePaths.PendingFilePath, json);
    }

    public static void Clear()
    {
        try
        {
            if (File.Exists(UpdatePaths.PendingFilePath))
                File.Delete(UpdatePaths.PendingFilePath);
        }
        catch
        {
            // ignored
        }
    }
}
