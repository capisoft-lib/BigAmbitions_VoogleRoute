# Voogle Route

**Voogle Route** extends **Voogle Maps** in Big Ambitions: set a destination on the city map, then follow a glowing **on-ground route line** on foot or in a vehicle, with optional **auto-walk** and **auto-drive fast travel** (time skip) when driving.

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
4. Set a destination on **Voogle Maps** (or click the city map **M**) → use the **VOOGLE ROUTE** panel (bottom-left).

Workshop description copy per release: [`releases/<version>/`](releases/).

## Features

- **Route line on the ground** — neon path to your map destination
- **Road-aware driving routes** — via [PathFinding](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute.PathFinding)
- **Auto-walk** — optional automatic walking along the route
- **Auto-drive fast travel** — in a vehicle, tap **AUTO-DRIVE** to confirm a time skip and arrive near your destination (not auto-steering — fast travel only)
- **Confirmation popup** — estimated travel time, arrival time (`HH:mm`), and distance; **ESC** closes the dialog
- **Base taxi multiplier** — mod option (1–10×, live value next to slider) or `base_taxi_multiplier` in `config.json`; adjusts skip-travel duration
- **Indoor navigation** — optional route line and auto-walk to the building exit (**WAY OUT** / **GET OUT**)
- **City map overlay** — route line on the city map (**M**); click to set a destination
- **VOOGLE ROUTE panel** — route toggle, auto-walk / auto-drive, custom line color (gear icon)
- Hidden in the **subway** and when navigation is unavailable

### Auto-drive fast travel (vehicle)

**AUTO-DRIVE** is fast travel only — it does not steer the car along the route line.

1. Set a **Voogle Maps** destination (map GPS target).
2. Enter a vehicle — the panel shows **AUTO-DRIVE** instead of auto-walk.
3. Tap **AUTO-DRIVE** → review travel time, arrival time, and distance → confirm **DRIVE**.
4. The game fades, teleports you near the destination on the road network, and advances time.
5. Adjust skip duration under **Mods → Voogle Route → Base taxi multiplier** (or `base_taxi_multiplier` in `config.json`).

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

Optional `config.json` lives next to the mod content (`ModContext.ModRootPath`). For a local SDK build that installs to `ModsLocal`, copy `config.json.example` to `ModsLocal/VoogleRoute/config.json`. Steam Workshop installs use the subscribed mod folder automatically.

Notable keys: `route_line_color`, `indoor_route`, `indoor_autowalk`, `base_taxi_multiplier` (default `2`, range 1–10).

## Changelog / licence

- [CHANGELOG.md](CHANGELOG.md)
- [LICENSE](LICENSE)

---

# Voogle Route (français)

Mod **Steam Workshop** pour Big Ambitions EA **0.11** : ligne d'itinéraire au sol, marche auto, **voyage rapide auto** en véhicule (saut temporel — pas de conduite automatique le long de la ligne), navigation intérieure, carte ville (**M**), couleur personnalisable pour les destinations **Voogle Maps**.

En véhicule : **AUTO-DRIVE** = voyage rapide uniquement. Confirmation avec temps estimé, heure d'arrivée et distance ; **Échap** ferme le dialogue. Multiplicateur taxi dans **Mods** ou `base_taxi_multiplier` dans `config.json`.

**Routage véhicule :** voir [BigAmbitions_VoogleRoute.PathFinding](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute.PathFinding).

Builds MelonLoader 0.10 : [legacy/README.md](legacy/README.md).
