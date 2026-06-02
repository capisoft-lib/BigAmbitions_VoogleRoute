# Publishing Voogle Route

Public repository: **[capisoft-lib/BigAmbitions_VoogleRoute](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute)**

Versioning and auto-update URLs are driven by [`Directory.Build.props`](../Directory.Build.props) (`ModVersion`, `ModGitHubRepository`, Nexus fields). A Release build regenerates [`latest.json`](../latest.json).

## Bump a release

1. Set `<ModVersion>` and add a [`CHANGELOG.md`](../CHANGELOG.md) entry.
2. `dotnet build VoogleRoute.csproj -c Release`
3. Commit `latest.json`, tag `v0.10.0`, push:

```powershell
git add latest.json CHANGELOG.md Directory.Build.props
git commit -m "Release v0.10.0"
git tag v0.10.0
git push origin main --tags
```

The [`release.yml`](../.github/workflows/release.yml) workflow attaches `releases/<version>/VoogleRoute.dll` and `latest.json` when the tag is pushed. Build locally first so `releases/0.10.0/VoogleRoute.dll` exists.

Asset name on GitHub must be **`VoogleRoute.dll`**:

`https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/releases/latest/download/VoogleRoute.dll`

## Auto-update manifest

Clients fetch:

`https://raw.githubusercontent.com/capisoft-lib/BigAmbitions_VoogleRoute/main/latest.json`

| Field | Purpose |
|-------|---------|
| `version` | Compare to `ModInfo.Version` |
| `latestDownloadUrl` | GitHub latest release DLL |
| `nexusUrl` | Player download on Nexus |
| `sha256` | Optional integrity check when publishing |

C# schema: [`Update/UpdateManifest.cs`](../Update/UpdateManifest.cs) — stub: [`Update/UpdateChecker.cs`](../Update/UpdateChecker.cs).

## Nexus Mods

When the mod page is live, set `ModNexusUrl` and `ModNexusModId` in `Directory.Build.props`, then `dotnet build -c Release`.

Upload copy from:

- `releases/<version>/short-description.txt`
- `releases/<version>/full-description.md`

## Do not commit

`bin/`, `obj/`, local `*.local.props`, or game/MelonLoader DLLs.
