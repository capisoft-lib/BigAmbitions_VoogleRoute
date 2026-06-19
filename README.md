# Voogle Route

**Voogle Route** extends **Voogle Maps** in Big Ambitions: set a destination on the city map, then follow a glowing **on-ground route line** on foot or in a vehicle, with optional **auto-walk**, **auto-drive fast travel** (time skip), and a **city map bookmarks** panel for saved places.

| | |
|---|---|
| **Game** | Big Ambitions EA **0.11 Experimental** |
| **Distribution** | **[Steam Workshop](https://steamcommunity.com/app/2977660/workshop/)** — primary install method |
| **Languages** | All **22** Big Ambitions interface languages |
| **Requires** | [`LIB_BaPlayerLocation`](https://github.com/capisoft-lib/BigAmbitions_LIB_BaPlayerLocation) ( [`LIB_BaUnifiedUI`](https://github.com/capisoft-lib/BigAmbitions_LIB_BaUnifiedUI) is bundled in `Dependencies/` ) |
| **Vehicle routing** | [`VoogleRoute.Pathfinding`](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute.PathFinding) (git submodule) |
| **Author** | [capisoft-lib](https://github.com/capisoft-lib) — community mod, not affiliated with Hovgaard Games |

## Steam Workshop

1. Open the game's **Workshop** browser (or the mod's Steam Workshop page).
2. Click **Subscribe**.
3. Launch Big Ambitions → **Mods** menu → enable **Voogle Route** and **LIB BA Player Location**.
4. Set a destination on **Voogle Maps** (or click the city map **M**) → use the **VOOGLE ROUTE** panel (bottom-left).

Workshop description copy per release: [`releases/<version>/`](releases/).

## Features

- **Route line on the ground** — neon path to your map destination
- **Subway fallback on foot** — when NavMesh cannot reach the destination, optional **walk → subway → walk** routing (toggle **Use subway** in mod options)
- **Road-aware driving routes** — via [PathFinding](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute.PathFinding)
- **Auto-walk** — optional automatic walking along the route (includes automatic subway boarding when the route uses the metro)
- **Auto-enter on arrival** — optional toggle to enter a vehicle or building when navigation completes
- **Delivery job routing** — follows vanilla job guider targets when no map GPS is set
- **City map bookmarks** — on the city map (**M**): searchable list, **SET** / **CENTER**, route distance per row, **ADD BOOKMARK** by clicking the map
- **City-map DRIVE / WALK** — shortcuts beside the vanilla set-destination button when a building is selected
- **Quick shortcuts** — **Last Car**, **Last Home**, **Last Shop** (auto-tracked); owned parked vehicles listed automatically
- **HUD shortcuts** — blue **+** saves a bookmark at your position; green **car** routes to your last parked vehicle
- **Auto-drive fast travel** — in a vehicle, tap **AUTO-DRIVE** to confirm a time skip and arrive near your destination (not auto-steering — fast travel only); fuel estimate shown in the confirmation popup
- **Confirmation popup** — estimated travel time, arrival time (`HH:mm`), distance, and fuel for auto-drive; **ESC** closes the dialog
- **Base taxi multiplier** — mod option (1–10×, live value next to slider) or `base_taxi_multiplier` in `config.json`
- **UI** — vanilla-style panels via bundled **LIB_BaUnifiedUI** (no separate Workshop mod)
- **Indoor navigation** — optional route line and auto-walk to the building exit (**WAY OUT** / **GET OUT**)
- **City map overlay** — route line on the city map (**M**); click to set a destination
- **VOOGLE ROUTE panel** — route toggle, auto-walk / auto-drive, bookmark and last-car buttons, custom line color (gear icon)
- Hidden in the **subway** and when navigation is unavailable

### Auto-drive fast travel (vehicle)

**AUTO-DRIVE** is fast travel only — it does not steer the car along the route line.

1. Set a **Voogle Maps** destination (map GPS target).
2. Enter a vehicle — the panel shows **AUTO-DRIVE** instead of auto-walk.
3. Tap **AUTO-DRIVE** → review travel time, arrival time, and distance → confirm **DRIVE**.
4. The game fades, teleports you near the destination on the road network, and advances time.
5. Adjust skip duration under **Mods → Voogle Route → Base taxi multiplier** (or `base_taxi_multiplier` in `config.json`).

### City map bookmarks

Open the city map (**M**) to show the **BOOKMARKS** panel:

1. **Custom bookmarks** — **ADD BOOKMARK**, then click the map, or tap **+** on the GPS panel to save your current position.
2. **Quick shortcuts** — **Last Car**, **Last Home**, **Last Shop** update as you park, enter home, or enter a business.
3. **Owned vehicles** — parked motor vehicles appear in the list (not in warehouses).
4. **SET** sets a Voogle destination; **CENTER** focuses the map; route **distance** is computed in the background.

Bookmarks are stored in `bookmarks.json` next to the mod (auto-migrated from legacy `config.json` bookmark fields).

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
2. Build PathFinding artifacts and sync bundled dependencies:

   ```powershell
   .\tools\build-pathfinding.ps1
   ```

   This copies `VoogleRoute.Pathfinding.dll` and `LIB_BaUnifiedUI.dll` into `Dependencies/` so Unity Mod Builder includes them at deploy time. After rebuilding **LIB_BaUnifiedUI** in Mod Builder, run `.\tools\sync-dependencies.ps1` (or use menu **Big Ambitions → Mods → Voogle Route → Sync bundled dependencies**).

3. **Mod Builder → Build + Install** for `LIB_BaPlayerLocation`, then `VoogleRoute`.

Output: `%LocalLow%\Hovgaard Games\Big Ambitions\ModsLocal\VoogleRoute\`

## Configuration

Optional files live next to the mod content (`ModContext.ModRootPath`). For a local SDK build that installs to `ModsLocal`, copy the `.example` files to `ModsLocal/VoogleRoute/`. Steam Workshop installs use the subscribed mod folder automatically.

| File | Purpose |
|------|---------|
| `config.json` | Route color, indoor options, logging, `base_taxi_multiplier` |
| `bookmarks.json` | Custom bookmarks and quick shortcuts (`Last Car` / `Home` / `Shop`) |

Notable `config.json` keys: `route_line_color`, `indoor_route`, `indoor_autowalk`, `base_taxi_multiplier` (default `2`, range 1–10), `log_level` (default `error` when logging is enabled).

## Changelog / licence

- [CHANGELOG.md](CHANGELOG.md)
- [LICENSE](LICENSE)

---

# Voogle Route (français)

Mod **Steam Workshop** pour Big Ambitions EA **0.11** : ligne d'itinéraire au sol, marche auto, **voyage rapide auto** en véhicule (saut temporel), **favoris sur la carte ville** (**M**), navigation intérieure, couleur personnalisable pour les destinations **Voogle Maps**.

Sur la carte (**M**) : panneau **FAVORIS** (recherche, **FIXER** destination, **CENTRER**, distances). Raccourcis **Dernière voiture / domicile / magasin** ; véhicules garés listés automatiquement. Sur le panneau GPS : **+** enregistre la position actuelle ; icône **voiture** = retour à la dernière voiture garée.

En véhicule : **AUTO-DRIVE** = voyage rapide uniquement. Confirmation avec temps estimé, heure d'arrivée et distance ; **Échap** ferme le dialogue. Multiplicateur taxi dans **Mods** ou `base_taxi_multiplier` dans `config.json`. Favoris dans `bookmarks.json`.

**Routage véhicule :** voir [BigAmbitions_VoogleRoute.PathFinding](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute.PathFinding).

Builds MelonLoader 0.10 : [legacy/README.md](legacy/README.md).
