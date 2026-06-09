# Voogle Route

Big Ambitions mod (EA 0.11+) — on-ground GPS route line to your **Voogle Maps** destination, optional auto-walk on foot.

| Property | Value |
|----------|-------|
| **Game** | Big Ambitions EA **0.11 Experimental** |
| **Unity** | **2022.3.62f2** with [Big Ambitions Modding SDK](https://github.com/hovgaardgames/bigambitions) |
| **Mod ID** | `VoogleRoute` |
| **Requires** | [`LIB_BaPlayerLocation`](https://github.com/capisoft-lib/BigAmbitions_LIB_BaPlayerLocation) |
| **Vehicle routing** | [`VoogleRoute.Pathfinding`](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute.PathFinding) (git submodule) |

## Features

- **Route line on the ground** — neon path to your map destination; separate styling on foot vs. in a vehicle
- **Road-aware driving routes** — vehicle paths use the shared PathFinding library (see linked repo)
- **Auto-walk** — walk the route automatically; stops if you take manual control
- **VOOGLE ROUTE panel** — bottom-left UI (`ROUTE ON / ROUTE OFF`, color picker)
- Hidden in the **subway** and when navigation is unavailable

Routing algorithm, enhanced traffic graph, and CSV format are documented in the **[PathFinding repository](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute.PathFinding)** — not here.

## Clone (developers)

This folder is `VoogleRoute/` inside [BigAmbitions_VoogleRoute](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute). Clone with submodules:

```bash
git clone --recurse-submodules https://github.com/capisoft-lib/BigAmbitions_VoogleRoute.git
cd BigAmbitions_VoogleRoute/VoogleRoute
```

| Repository | Role |
|------------|------|
| [BigAmbitions_VoogleRoute](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute) | Mod sources, locales, shipped CSV data, graph generator tools |
| [BigAmbitions_VoogleRoute.PathFinding](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute.PathFinding) | `PathFinding/` submodule — routing DLL sources |
| [BigAmbitions_LIB_BaPlayerLocation](https://github.com/capisoft-lib/BigAmbitions_LIB_BaPlayerLocation) | Player position / movement mode (required at runtime) |

## Install into your SDK

1. Copy **both** mods into your SDK:

   ```text
   <YourSdk>/Assets/Mods/LIB_BaPlayerLocation/
   <YourSdk>/Assets/Mods/VoogleRoute/
   ```

   Or run `tools/install-into-sdk.ps1` from each mod folder (after `git submodule update --init`).

2. Build the PathFinding DLL into `Dependencies/`:

   ```powershell
   .\tools\build-pathfinding.ps1
   ```

3. Open the SDK in Unity, import game DLLs if prompted.
4. **Mod Builder → Build & Install** for `LIB_BaPlayerLocation`, then `VoogleRoute`.
5. Enable **LIB BA Player Location** and **Voogle Route** in the in-game mod menu.

To change routing logic, edit the **PathFinding** submodule and rebuild — see its README.

## Configuration

Copy `config.json.example` to:

```text
%USERPROFILE%\AppData\LocalLow\...\BigAmbitions\ModsLocal\VoogleRoute\config.json
```

| Key | Meaning |
|-----|---------|
| `logging` | Verbose mod logs |
| `log_level` | `error`, `warn`, `info` |
| `show_line_detection` | Debug overlay (uses PathFinding corridor helpers) |
| `route_line_color` | RGBA line color |

In-game toggles (route line, auto-walk) are in the mod options panel.

## Repository layout

```text
PathFinding/                git submodule → PathFinding repo
Dependencies/               VoogleRoute.Pathfinding.dll (built) + System.Text.Json
Data/                       big_ambitions_enhanced_routes.csv (input to PathFinding)
Scripts/                    Unity glue — NavMesh on foot, DLL + CSV for vehicles
Locales/
tools/build-pathfinding.ps1 build submodule → Dependencies/
```

## License

MIT
