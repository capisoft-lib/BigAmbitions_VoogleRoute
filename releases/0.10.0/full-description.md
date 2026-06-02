# Voogle Route v0.10.0

**Voogle Route** extends **Voogle Maps** in the game world: once you set a destination on the city map, you get visible guidance on screen and on the ground—in the same spirit as BizPhone apps (BizMan, Voogle Maps, EconoView), without replacing the vanilla map app.

**Localization:** UI and turn instructions follow the game’s current language (all **22** Big Ambitions interface languages).

**Source code & updates:** [GitHub — capisoft-lib/BigAmbitions_VoogleRoute](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute)  
**Latest version manifest:** [latest.json](https://raw.githubusercontent.com/capisoft-lib/BigAmbitions_VoogleRoute/main/latest.json)

## Features

- **Route line** on the ground (customizable color and width) — toggle with **ROUTE ON / ROUTE OFF**
- **Turn HUD** while driving: distance and instruction (e.g. “120 m — Turn left”)
- **Ground arrows** at upcoming intersections
- **Auto-walk** on foot toward the destination (**AUTO WALK / WALK ON**)
- **VOOGLE ROUTE** panel (bottom-left, game-style UI)
- Automatically hidden in the **subway** and when navigation context is unavailable

## Installation

1. Install [MelonLoader](https://melonwiki.xyz/) for Big Ambitions.
2. Copy **`VoogleRoute.dll`** into `Big Ambitions/Mods/`.
3. Remove the old **`OnMapGps.dll`** if present (official rename).
4. Launch the game — MelonLoader should log: `Voogle Route v0.10.0 loaded.`

## How to use

1. Open the map (**Voogle Maps**) and set a destination as usual.
2. Use the **VOOGLE ROUTE** panel:
   - **ROUTE ON** — show the route line (full path if enabled in settings)
   - **ROUTE OFF** — hide the line (turn HUD and arrows may still show depending on settings)
   - **AUTO WALK / WALK ON** — automatic on-foot movement along the NavMesh path
3. When driving, follow the top banner and intersection arrows.

## Settings (MelonLoader)

Category **Voogle Route** in MelonLoader preferences:

| Option | Description |
|--------|-------------|
| RouteLineEnabled | Show route line |
| AutoWalkEnabled | Auto-walk on foot |
| ShowTurnGuidance | Turn distance + instruction HUD |
| ShowIntersectionArrows | Ground arrows at intersections |
| ShowFullRouteLine | Full line to destination |
| FootLineWidth / VehicleLineWidth | Line width on foot / in vehicle |
| LineColor R/G/B/A | Line color |
| MinTurnAngleDegrees | Minimum angle to count as a turn |
| HudButtonScale / NavHudOffsetY | Panel size and position |

## Compatibility

- **Game:** Big Ambitions EA **0.10** (MelonLoader Il2Cpp build tested)
- **Dependencies:** MelonLoader only (BAUI not required)
- **Saves:** Read-only use of `customDestination`; does not alter progression

## Known limitations

- Routes follow **NavMesh** walkable areas, not exact traffic lane centers.
- In vehicles, paths are **indicative** (same NavMesh basis as on foot in v1).
- No guidance during **subway** rides.

## Changelog 0.10.0

- Renamed from **On-Map GPS** to **Voogle Route** (aligned with Voogle Maps)
- UI: **VOOGLE ROUTE** panel, **ROUTE ON/OFF** buttons
- Full in-game localization for all supported game languages
- New MelonLoader preference category: `VoogleRoute`

## Credits

Community mod by **capisoft-lib** — not affiliated with Hovgaard Games.
