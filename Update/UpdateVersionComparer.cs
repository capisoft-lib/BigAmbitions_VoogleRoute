namespace VoogleRoute.Update;

internal static class UpdateVersionComparer
{
    public static bool TryParse(string? value, out Version version)
    {
        version = new Version(0, 0);
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        if (trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[1..];

        return Version.TryParse(trimmed, out version);
    }

    public static bool IsRemoteNewer(string remoteVersion, string localVersion)
    {
        if (!TryParse(remoteVersion, out var remote) || !TryParse(localVersion, out var local))
            return false;

        return remote > local;
    }
}
