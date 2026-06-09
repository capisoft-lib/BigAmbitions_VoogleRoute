# Voogle Route

**Voogle Route** extends **Voogle Maps** in Big Ambitions: set a destination on the city map, then follow a glowing **on-ground route line** on foot or in a vehicle, with optional **auto-walk**.

| | |
|---|---|
| **Game** | Big Ambitions EA **0.11 Experimental** |
| **Distribution** | **[Steam Workshop](https://steamcommunity.com/app/2977660/workshop/)** — primary install method |
| **Languages** | All **22** Big Ambitions interface languages |
| **Requires** | [`LIB_BaPlayerLocation`](https://github.com/capisoft-lib/BigAmbitions_LIB_BaPlayerLocation) |
| **Vehicle routing** | [`VoogleRoute.Pathfinding`](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute.PathFinding) (git submodule) |
| **Author** | [capisoft-lib](https://github.com/capisoft-lib) — community mod, not affiliated with Hovgaard Games |

## Steam Workshop

1. Open the game's **Workshop** browser (or the mod's Steam Workshop page).
2. Click **Subscribe**.
3. Launch Big Ambitions → **Mods** menu → enable **Voogle Route** and **LIB BA Player Location**.
4. Set a destination on **Voogle Maps** → use the **VOOGLE ROUTE** panel (bottom-left).

Workshop description copy per release: [`releases/<version>/`](releases/).

## Features

- **Route line on the ground** — neon path to your map destination
- **Road-aware driving routes** — via [PathFinding](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute.PathFinding)
- **Auto-walk** — optional automatic walking along the route
- **VOOGLE ROUTE panel** — route toggle, auto-walk, custom line color
- Hidden in the **subway** and when navigation is unavailable

Routing algorithm, graph data, and generator tools live in the **PathFinding** repository — not here.

## Repository layout

This repository **is** the mod (flat layout — copy the repo root into `Assets/Mods/VoogleRoute/`).

```text
Scripts/ Locales/ Data/ Dependencies/    Unity mod
PathFinding/                              git submodule
ModManifest.asset  VoogleRoute.asmdef
tools/build-pathfinding.ps1               DLL + CSV sync from submodule
releases/ legacy/ docs/                   publishing & changelog (not loaded by the game)
```

## Development

Requires [Big Ambitions Modding SDK](https://github.com/HovgaardGames/BigAmbitionsModding) (Unity **2022.3.62f2**).

```bash
git clone --recurse-submodules https://github.com/capisoft-lib/BigAmbitions_VoogleRoute.git
cd BigAmbitions_VoogleRoute
```

1. Copy this repo into your SDK at `Assets/Mods/VoogleRoute/` (and install [`LIB_BaPlayerLocation`](https://github.com/capisoft-lib/BigAmbitions_LIB_BaPlayerLocation)).
2. Build PathFinding artifacts:

   ```powershell
   .\tools\build-pathfinding.ps1
   ```

3. **Mod Builder → Build + Install** for `LIB_BaPlayerLocation`, then `VoogleRoute`.

Output: `%LocalLow%\Hovgaard Games\Big Ambitions\ModsLocal\VoogleRoute\`

## Configuration

Copy `config.json.example` to `ModsLocal/VoogleRoute/config.json` (see file for keys).

## Changelog / licence

- [CHANGELOG.md](CHANGELOG.md)
- [LICENSE](LICENSE)

---

# Voogle Route (français)

Mod **Steam Workshop** pour Big Ambitions EA **0.11** : ligne d'itinéraire au sol, marche auto, couleur personnalisable pour les destinations **Voogle Maps**.

**Routage véhicule :** voir [BigAmbitions_VoogleRoute.PathFinding](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute.PathFinding).

Builds MelonLoader 0.10 : [legacy/README.md](legacy/README.md).
