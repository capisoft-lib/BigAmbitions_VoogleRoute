# Voogle Route

**Voogle Route** extends **Voogle Maps** in Big Ambitions: set a destination on the city map, then follow a glowing **on-ground route line** on foot or in a vehicle, with optional **auto-walk**.

| | |
|---|---|
| **Game** | Big Ambitions EA **0.11 Experimental** |
| **Distribution** | **[Steam Workshop](https://steamcommunity.com/app/2977660/workshop/)** — primary install method |
| **Languages** | All **22** Big Ambitions interface languages |
| **Author** | [capisoft-lib](https://github.com/capisoft-lib) — community mod, not affiliated with Hovgaard Games |

## Steam Workshop

Voogle Route is published as a **native Steam Workshop mod** for Big Ambitions EA 0.11.

1. Open the game's **Workshop** browser (or the mod's Steam Workshop page).
2. Click **Subscribe**.
3. Launch Big Ambitions → **Mods** menu → enable **Voogle Route**.
4. In the city, use the **VOOGLE ROUTE** panel (bottom-left) after setting a destination on **Voogle Maps**.

No MelonLoader, no manual DLL copy, no `Mods/` folder. The game downloads and updates the mod like any other Workshop item.

Workshop description copy for each release lives in [`releases/<version>/`](releases/).

## Features

- **Route line on the ground** — neon path to your map destination; separate styling on foot vs. in a vehicle
- **Road-aware driving routes** — vehicle paths use the shared **[PathFinding](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute.PathFinding)** library (enhanced Gley graph + A*)
- **Auto-walk** — walk the route automatically (`WALK ON`); stops if you take manual control
- **VOOGLE ROUTE panel** — bottom-left BizPhone-style UI (`ROUTE ON / ROUTE OFF`)
- **Custom line color** — presets or the in-game color picker (gear icon)
- Hidden in the **subway** and when navigation is unavailable

## Vehicle routing (PathFinding)

Algorithm, enhanced traffic graph, CSV schema, and A* details live in the separate **[BigAmbitions_VoogleRoute.PathFinding](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute.PathFinding)** repository (git submodule at `VoogleRoute/PathFinding/`).

This repo ships the **CSV data** (`VoogleRoute/Data/`) and **generator tools** (`tools/generate_enhanced_route_graph.py`, `docs/*.svg`). The mod loads the CSV through the PathFinding DLL at runtime.

## Repository layout

```
VoogleRoute/              ← mod sources (drop into SDK Assets/Mods/VoogleRoute/)
  PathFinding/            ← git submodule → BigAmbitions_VoogleRoute.PathFinding
  Dependencies/           ← VoogleRoute.Pathfinding.dll + System.Text.Json
  Scripts/
  Locales/
  Data/                   ← big_ambitions_enhanced_routes.csv
  tools/build-pathfinding.ps1
docs/                     ← route graph SVG, publishing notes
releases/                 ← Steam Workshop copy per version
tools/                    ← locale + graph generators
legacy/                   ← MelonLoader 0.10 archive pointer
```

## Development

Requires the [Big Ambitions Modding SDK](https://github.com/HovgaardGames/BigAmbitionsModding) (Unity **2022.3.62f2**), [`LIB_BaPlayerLocation`](https://github.com/capisoft-lib/BigAmbitions_LIB_BaPlayerLocation), and a local game install for imported assemblies.

1. Clone with submodules:

   ```bash
   git clone --recurse-submodules https://github.com/capisoft-lib/BigAmbitions_VoogleRoute.git
   ```

2. Copy or symlink `VoogleRoute/` into your SDK at `Assets/Mods/VoogleRoute/` (and install `LIB_BaPlayerLocation` the same way).
3. Build the pathfinding DLL:

   ```powershell
   cd VoogleRoute
   .\tools\build-pathfinding.ps1
   ```

4. Import game DLLs via the SDK setup flow.
5. **Mod Builder → Build + Install** for `LIB_BaPlayerLocation`, then `VoogleRoute`.

Output installs to:

`%LocalLow%\Hovgaard Games\Big Ambitions\ModsLocal\VoogleRoute\`

## Migrating from MelonLoader (0.10)

The MelonLoader builds (`v0.10.0`, `v0.10.1`) are **legacy**. Remove MelonLoader and `VoogleRoute.dll` from `Big Ambitions/Mods/`, then subscribe on Workshop for 0.11. See [legacy/README.md](legacy/README.md).

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

## Licence

See [LICENSE](LICENSE).

---

# Voogle Route (français)

Mod **Steam Workshop** pour Big Ambitions EA **0.11 Experimental** : ligne d'itinéraire au sol, marche auto et couleur personnalisable pour les destinations **Voogle Maps**.

### Installation

1. **S'abonner** sur Steam Workshop  
2. Activer **Voogle Route** dans le menu **Mods**  
3. Définir une destination sur Voogle Maps → panneau **VOOGLE ROUTE**

### Routage véhicule

Voir le dépôt **[BigAmbitions_VoogleRoute.PathFinding](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute.PathFinding)** (graphe enrichi, A*, format CSV). Ce dépôt fournit les données (`VoogleRoute/Data/`) et les outils de génération (`tools/`).

Les builds MelonLoader 0.10 sont archivés : [legacy/README.md](legacy/README.md).
