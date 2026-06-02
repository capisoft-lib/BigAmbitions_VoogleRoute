namespace VoogleRoute.Update;

/// <summary>
/// Placeholder for a future in-game or MelonLoader update notifier.
/// Compare <see cref="ModInfo.Version"/> against <see cref="UpdateManifest.Version"/> from
/// <see cref="ModInfo.LatestManifestUrl"/>.
/// </summary>
internal static class UpdateChecker
{
    // Future: HttpClient GET ModInfo.LatestManifestUrl, parse UpdateManifest, compare versions.
    // Prefer GitHub release DLL (LatestReleaseDllUrl) or Nexus (NexusUrl) based on user preference.
}
