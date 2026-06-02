# Voogle Route

[![MelonLoader](https://img.shields.io/badge/MelonLoader-required-orange)](https://melonwiki.xyz/)
[![Game](https://img.shields.io/badge/Big%20Ambitions-EA%200.10-blue)](https://store.steampowered.com/app/1331550/Big_Ambitions/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

**Voogle Route** is a MelonLoader mod for [Big Ambitions](https://store.steampowered.com/app/1331550/Big_Ambitions/) that extends **Voogle Maps** with on-ground navigation: route lines, turn guidance while driving, intersection arrows, and optional auto-walk—all localized in the game’s **22** interface languages.

| Download | Link |
|----------|------|
| **Nexus Mods** (recommended for players) | [Voogle Route on Nexus](https://www.nexusmods.com/bigambitions/mods/0) *(update URL in `Directory.Build.props` when live)* |
| **GitHub Releases** | [Latest release](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/releases/latest) |
| **Auto-update manifest** | [`latest.json`](https://raw.githubusercontent.com/capisoft-lib/BigAmbitions_VoogleRoute/main/latest.json) |

> **Note:** Update the Nexus mod ID in [`Directory.Build.props`](Directory.Build.props) when your Nexus page is live.

---

## Features

- **Route line** on the ground (color/width configurable)
- **ROUTE ON / ROUTE OFF** toggle on the **VOOGLE ROUTE** panel
- **Turn HUD** in vehicles (distance + instruction)
- **Intersection arrows** on the path ahead
- **Auto-walk** on foot (**AUTO WALK / WALK ON**)
- Hides during **subway** rides and invalid navigation contexts
- UI strings follow the in-game language (same locale as vanilla)

---

## Requirements

- [Big Ambitions](https://store.steampowered.com/app/1331550/Big_Ambitions/) **EA 0.10** (other versions untested)
- [MelonLoader](https://melonwiki.xyz/) (Il2Cpp)
- **No** BAUI-Framework required

---

## Installation

### From Nexus Mods

1. Install [MelonLoader](https://melonwiki.xyz/) for Big Ambitions.
2. Download **Voogle Route** from [Nexus Mods](https://www.nexusmods.com/bigambitions/mods/0).
3. Extract **`VoogleRoute.dll`** into:

   ```
   <Steam>\steamapps\common\Big Ambitions\Mods\
   ```

4. Remove **`OnMapGps.dll`** if you still have the old mod.
5. Launch the game — console should show: `Voogle Route v0.10.0 loaded.`

### From GitHub

1. Open [Releases](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/releases/latest).
2. Download **`VoogleRoute.dll`** from the release assets.
3. Copy it to `Big Ambitions/Mods/` as above.

---

## Usage

1. Set a destination on **Voogle Maps** (city map) as usual.
2. Use the **VOOGLE ROUTE** panel (bottom-left):
   - **ROUTE ON** — show the ground route line
   - **ROUTE OFF** — hide the line (turn HUD/arrows may still show)
   - **AUTO WALK / WALK ON** — automatic walking along the NavMesh path
3. When driving, follow the top banner and ground arrows.

MelonLoader preferences: category **Voogle Route**.

---

## Building from source

```powershell
# Optional: custom game install path
$env:BIG_AMBITIONS_DIR = "C:\Program Files (x86)\Steam\steamapps\common\Big Ambitions"

dotnet build VoogleRoute.csproj -c Release
```

Release build also:

- Updates [`latest.json`](latest.json) at the repo root
- Copies artifacts to `releases/<version>/` (`VoogleRoute.dll` + `latest.json`)

Close the game before building if copy-to-`Mods/` is enabled and the DLL is locked.

---

## Auto-update (`latest.json`)

The root **[`latest.json`](latest.json)** file is the machine-readable **latest version manifest** for future update checkers (in-game notifier, external launcher, etc.).

| Field | Purpose |
|-------|---------|
| `version` | Mod semver (`0.10.0`) |
| `gameVersion` | Target Big Ambitions EA (`0.10`) |
| `manifestUrl` | Stable URL to this file on `main` |
| `latestDownloadUrl` | GitHub “latest release” DLL asset |
| `downloadUrl` | Version-pinned GitHub asset (`v0.10.0`) |
| `nexusUrl` / `nexusModId` | Primary player download on Nexus |
| `sha256` | Optional integrity hash (set when publishing) |

C# schema: [`Update/UpdateManifest.cs`](Update/UpdateManifest.cs)  
Stub checker: [`Update/UpdateChecker.cs`](Update/UpdateChecker.cs)

**Bump a release**

1. Set `<ModVersion>` in [`Directory.Build.props`](Directory.Build.props).
2. Add an entry to [`CHANGELOG.md`](CHANGELOG.md).
3. `dotnet build -c Release`
4. Commit `latest.json`, tag `v0.10.0`, create a GitHub Release with `VoogleRoute.dll`.
5. Upload the same DLL to Nexus Mods.

---

## Project layout

```
VoogleRoute/
├── latest.json              # Auto-update manifest (committed)
├── Directory.Build.props    # Version + GitHub/Nexus URLs
├── CHANGELOG.md
├── releases/                # Local release bundles (Nexus upload helpers)
├── src (project root)
│   ├── Plugin.cs
│   ├── Localization/        # 22-language UI tables
│   ├── Navigation/
│   ├── Rendering/
│   ├── UI/
│   └── Update/              # Manifest schema + future updater
└── docs/
    └── REVERSE_ENGINEERING.md
```

---

## Documentation

- [Reverse engineering notes](docs/REVERSE_ENGINEERING.md)
- [Publishing this repo](docs/PUBLISHING.md)

---

## Known limitations

- Routes use **NavMesh** walkable areas, not exact traffic lane centers.
- Vehicle paths are indicative (same NavMesh basis as on foot in v1).
- Not affiliated with Hovgaard Games.

---

## License

[MIT](LICENSE) — see [LICENSE](LICENSE).
