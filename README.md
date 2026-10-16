# Voogle Route

**Voogle Route** extends **Voogle Maps** in Big Ambitions: set a destination on the city map, then follow a glowing **on-ground route line** on foot or in a vehicle, with optional **auto-walk**, **visible auto-drive (beta)** or **fast travel** (time skip), and a **city map bookmarks** panel for saved places.

> ☕ If Voogle Route has saved you from getting lost—or from remembering where you parked—you can support its development by [buying me a coffee](https://buymeacoffee.com/capitaine). Coffee keeps the developer awake, so auto-walk can keep doing the walking.

| | |
|---|---|
| **Game** | Big Ambitions **1.0 Build 3682** — Voogle Route **1.0.12** |
| **Distribution** | **[Steam Workshop](https://steamcommunity.com/app/1331550/workshop/)** — primary install method |
| **Languages** | All **22** Big Ambitions interface languages |
| **Requires** | [`LIB_BaPlayerLocation 1.0.0+`](https://github.com/capisoft-lib/BigAmbitions_LIB_BaPlayerLocation) and [`LIB_BaUnifiedUI 1.0.0+`](https://github.com/capisoft-lib/BigAmbitions_LIB_BaUnifiedUI), both installed and enabled separately |
| **Vehicle routing** | [`VoogleRoute.Pathfinding`](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute.PathFinding) (git submodule) |
| **Author** | [capisoft-lib](https://github.com/capisoft-lib) — community mod, not affiliated with Hovgaard Games |

## Steam Workshop

1. Open the game's **Workshop** browser (or the mod's Steam Workshop page).
2. Click **Subscribe**.
3. Launch Big Ambitions → **Mods** menu → enable **Voogle Route**, **LIB BA Player Location**, and **LIB BA Unified UI**.
4. Set a destination on **Voogle Maps** (or click the city map **M**) → use the **VOOGLE ROUTE** panel (bottom-left).

If Voogle Route disappeared after an update, its panel does not open, or the
game reports mods missing from a save, follow the bilingual
[LIB BA Unified UI troubleshooting guide](docs/BA_UNIFIED_UI_TROUBLESHOOTING.md).

Current Workshop description copy: [`releases/1.0.12/`](releases/1.0.12/), in English and French. Earlier release texts remain in [`releases/`](releases/).

## What's new in 1.0.12

- **Choose each trip** — WATCH THE DRIVE follows the streets with your car; FAST TRAVEL keeps the time skip. Set the default selection in mod options.
- **Visible driving (beta)** — native upshifts/downshifts, road-speed cruising and controls for pedestrian stopping, speeding and red lights. Corner speeds, braking and cruise regulation have been revised.
- **Live availability** — a dialog opened while driving updates after you stop or reposition. Unchanged stationary refusals stay cached.
- Start stopped on a traffic lane, facing its direction. No reverse, U-turns, automatic parking or elevated/underground driving routes; choose fast travel when visible driving is unavailable.
- Built against Big Ambitions 1.0 Build 3682. Legacy API adapters remain in the source; EA 0.11 was not revalidated for 1.0.12.

## What's new in 1.0.11

- **UI scaling slider** — set Voogle Route from 50% to 150% under **Options → Mods**; changes apply immediately and are saved per save.

## What's new in 1.0.10

- Release the previous navigation goal when leaving a motor vehicle, including Hamptons plots and parking interiors, even when navigation panels are hidden or another screen is open.
- Keep vehicle-exit detection across a brief GPS interruption and retry safely while pedestrian navigation is still settling. A successful release does not cancel later manual movement.
- Keep the AUTO-DRIVE fade and time-skip recovery introduced in 1.0.9.

## Features

- **Route line on the ground** — neon path to your map destination
- **Visible auto-drive (beta)** — street-following control of the occupied car, with native gears and per-save driving options
- **Subway fallback on foot** — when NavMesh cannot reach the destination, optional **walk → subway → walk** routing (toggle **Use subway** in mod options)
- **Road-aware driving routes** — via [PathFinding](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute.PathFinding), with bounded reachable-arrival fallback when the nearest one-way lanes cannot be entered
- **Big Ambitions 1.0 road coverage** — refreshed graph with bidirectional routing for all 18 Hamptons mansion addresses
- **Auto-walk** — optional automatic walking along the route (includes automatic subway boarding when the route uses the metro)
- **Auto-enter on arrival** — optional toggle to enter a vehicle or building when navigation completes
- **Delivery job routing** — follows vanilla job guider targets when no map GPS is set
- **City map bookmarks** — on the city map (**M**): searchable list, **SET** / **CENTER**, route distance per row, **ADD BOOKMARK** by clicking the map
- **City-map DRIVE / WALK** — shortcuts beside the vanilla set-destination button when a building is selected
- **Quick shortcuts** — **Last Car**, **Last Home**, **Last Shop** (auto-tracked); owned parked vehicles listed automatically
- **HUD shortcuts** — blue **+** saves a bookmark at your position; green **car** routes to your last parked vehicle
- **Travel mode choice** — in a vehicle, tap **AUTO-DRIVE** and select **WATCH THE DRIVE** or **FAST TRAVEL** before confirming
- **Confirmation popup** — estimated travel time, arrival time (`HH:mm`), distance, and fuel for auto-drive; **ESC** closes the dialog
- **Base taxi multiplier** — mod option (1–10×, live value next to slider) or `base_taxi_multiplier` in `config.json`
- **UI** — vanilla-style panels supplied by the separate **LIB BA Unified UI** Workshop dependency, with a per-save 50–150% scale slider in **Options → Mods**
- **Draggable UI** — all seven interactive Voogle Route windows remember their positions
- **Configurable shortcuts** — route line, auto-move and Hide/Show UI can be rebound under **Options → Mods**; defaults are **Ctrl+Shift+Y**, **Ctrl+Shift+X** and **Ctrl+Shift+C**
- **Configurable route colors** — on-foot, indoor and vehicle lines each have a native color picker under **Options → Mods**
- **Indoor navigation** — optional route line and auto-walk to the building exit (**WAY OUT** / **GET OUT**)
- **Hamptons exits** — dedicated entrance-first routing for open-world mansion plots and their property gates
- **City map overlay** — route line on the city map (**M**); click to set a destination
- **VOOGLE ROUTE panel** — route toggle, auto-walk / auto-drive, bookmark, History and last-car buttons
- **Hidden while using the computer** — the panel and open History hide throughout computer-game sessions, including the launcher, pause and game-over screens; route shortcuts are blocked, and the previous visibility returns when you leave
- Hidden in the **subway** and when navigation is unavailable

### Auto-drive (vehicle)

**AUTO-DRIVE** offers visible driving and fast travel for each trip.

1. Set a **Voogle Maps** destination (map GPS target).
2. Enter a vehicle — the panel shows **AUTO-DRIVE** instead of auto-walk.
3. Tap **AUTO-DRIVE** and choose **WATCH THE DRIVE** or **FAST TRAVEL**, then review the details and confirm.
4. Visible driving needs a stopped car aligned on its lane. Movement keys, ESC or STOP DRIVING return control; park manually at arrival. The dialog refreshes automatically after stopping or repositioning.
5. Fast travel fades, moves the car near the destination and advances time. Its dialog shows travel time, arrival, distance and fuel. Adjust skip duration with **Base taxi multiplier** in mod options.
6. Driving options offer automatic cruise or a 20–80 km/h manual cap, stopping for pedestrians, speeding up to 80 km/h and red-light overrides. Speeding and red-light overrides default off; collision and fine systems remain active.

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

1. Copy this repo into your SDK at `Assets/Mods/VoogleRoute/` and install [`LIB_BaPlayerLocation`](https://github.com/capisoft-lib/BigAmbitions_LIB_BaPlayerLocation) plus [`LIB_BaUnifiedUI`](https://github.com/capisoft-lib/BigAmbitions_LIB_BaUnifiedUI).
2. Build the PathFinding artifacts:

   ```powershell
   .\tools\build-pathfinding.ps1
   ```

   This copies only `VoogleRoute.Pathfinding.dll` into `Dependencies/`. `LIB_BaUnifiedUI` remains a separate mod and must never be copied into the Voogle Route package.

3. **Mod Builder → Build + Install** for `LIB_BaPlayerLocation`, then `LIB_BaUnifiedUI`, then `VoogleRoute`.

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

Mod **Steam Workshop** pour Big Ambitions **1.0 Build 3682** : Voogle Route **1.0.12** fournit la ligne d'itinéraire au sol, la marche auto, la **conduite visible (bêta)** ou le **voyage rapide** en véhicule, les favoris sur la carte ville, la navigation intérieure et les itinéraires Hamptons.

Le graphe routier 1.0 est inclus. Les adaptateurs legacy restent dans le code ; EA 0.11 n'a pas été revalidée pour cette mise à jour.

## Nouveautés de la version 1.0.12

- **Choix par trajet** — Voir conduire ou Voyage rapide dans la confirmation AUTO-DRIVE ; mode présélectionné configurable.
- **Conduite visible (bêta)** — passages de rapports natifs, vitesse de croisière et options piétons, dépassement des limitations et feux rouges. Allure en virage, freinage et régulation révisés.
- **Dialogue réactif** — la disponibilité se réévalue après l'arrêt ou un repositionnement, sans fermer la fenêtre et sans recalcul répété sur un refus inchangé.
- Départ à l'arrêt, aligné sur une voie. Pas de marche arrière, demi-tour, stationnement automatique ni conduite sur voie élevée/souterraine ; le voyage rapide reste disponible.

## Nouveautés de la version 1.0.11

- **Échelle de l’interface** — réglez Voogle Route de 50 % à 150 % dans **Options → Mods** ; le changement s’applique immédiatement et est enregistré par sauvegarde.

## Nouveautés de la version 1.0.10

- Libère l'ancien objectif de déplacement à la sortie d'un véhicule motorisé, y compris aux Hamptons et dans les parkings, même si les panneaux de navigation sont masqués ou qu'un autre écran est ouvert.
- Conserve la détection de sortie pendant une brève interruption GPS et réessaie si la navigation piétonne n'est pas encore prête. Une fois la libération réussie, les nouveaux déplacements manuels sont préservés.
- Conserve la récupération du fondu et du saut temporel AUTO-DRIVE introduite en 1.0.9.

Sur la carte (**M**) : panneau **FAVORIS** (recherche, **FIXER** destination, **CENTRER**, distances). Raccourcis **Dernière voiture / domicile / magasin** ; véhicules garés listés automatiquement. Sur le panneau GPS : **+** enregistre la position actuelle ; icône **voiture** = retour à la dernière voiture garée.

En véhicule : **AUTO-DRIVE** propose **Voir conduire** ou **Voyage rapide**. Le voyage rapide conserve ses estimations de durée, arrivée, distance et carburant. Pendant la conduite visible, les touches de déplacement, Échap ou Arrêter rendent le volant ; le stationnement reste manuel. Les options du mod définissent le mode par défaut, la vitesse automatique ou un plafond de 20–80 km/h, l'arrêt aux piétons, le dépassement et les feux rouges. Ces deux dernières dérogations sont désactivées par défaut ; collisions et amendes restent actives. Multiplicateur taxi, raccourcis et couleurs sont dans **Options → Mods**. Favoris dans `bookmarks.json`.

**Routage véhicule :** voir [BigAmbitions_VoogleRoute.PathFinding](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute.PathFinding).

Builds MelonLoader 0.10 : [legacy/README.md](legacy/README.md).
