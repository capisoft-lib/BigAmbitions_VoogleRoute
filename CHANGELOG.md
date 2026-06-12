# Changelog

All notable changes to **Voogle Route** are documented here.

## [0.11.5] - 2026-06-12

### Added

- **Auto-drive fast travel** — in a vehicle, tap **AUTO-DRIVE** on the panel to open a confirmation popup, then skip time and teleport near your map destination (vanilla `TimeMachine` + `UiFader` flow, road-network placement)
- **Confirmation popup** — Voogle-style modal with estimated travel time, arrival time (`HH:mm`), and distance; **ESC**, dimmer click, or **CANCEL** closes without travelling
- **Base taxi multiplier** — mod option slider (1–10×) with live `{value}x` next to the control; persisted as `base_taxi_multiplier` in `config.json`
- **Locales** — auto-drive HUD, popup, errors, and multiplier value strings in all **22** supported languages

### Changed

- **VOOGLE ROUTE panel** — second button shows **AUTO-DRIVE** in vehicle mode (fast travel) instead of auto-walk

### Removed

- **In-vehicle auto-steering** — experimental path-following / Pure Pursuit driving was dropped; **0.11.5** ships fast travel only

## [0.11.4] - 2026-06-12

### Added

- **Indoor navigation** — route line and optional auto-walk to the building exit when you are inside (separate Mods toggles; panel shows **WAY OUT** / **GET OUT**)
- **City map overlay** — route line on the in-game city map (**M**); click the map to set a Voogle destination with confirm/cancel popup
- **Bridge and center-deck routing** — updated enhanced route graph for reliable vehicle paths over bridges and the downtown deck

### Fixed

- **Locales** — UTF-8 fixes for all 22 languages; new strings for indoor nav, map destination, and recalc banner

## [0.11.3] - 2026-06-09

### Fixed

- **Steam Workshop paths**: all runtime files (`Data/`, `config.json`, `Logs/`) use `ModContext.ModRootPath` only — no parallel `ModsLocal/<ModId>/` folder
- **LIB_BaPlayerLocation**: same content vs user path split for `subscriber_config.json`
- Mod Builder now copies `Data/` into `Output/<ModId>/` for Workshop uploads
- Async vehicle recalc banner no longer stuck when a pending recalc is cancelled

## [0.11.2] - 2026-06-09

### Changed

- Runtime file paths centralized in `ModStoragePaths`
- Requires **LIB BA Player Location** `0.11.1+`

### Fixed

- Reliable file logging when `config.json` has `"logging": true` (ModsLocal dev install only until 0.11.3)

## [0.11.1] - 2026-06-08

### Changed

- Requires **LIB_BaPlayerLocation** as the sole player-position dependency (legacy BaPlayerLocation dev mods removed)

## [0.11.0] - 2026-06-07

### Added

- **SDK rewrite** for Big Ambitions EA **0.11 Experimental** (official modding API, no MelonLoader)
- **Steam Workshop** distribution — subscribe and enable in the **Mods** menu
- **Road-aware vehicle routes** via enhanced traffic waypoint graph (`Data/big_ambitions_enhanced_routes.csv`)
- **JSON locales** (`Locales/*.json`) for all **22** game languages
- In-game **Mods** options for route line and auto-walk; gear icon for route color presets + native color picker

### Changed

- Install path: `%LocalLow%/.../ModsLocal/VoogleRoute/` (folder with DLL + `Locales/` + `Data/`) instead of `Big Ambitions/Mods/VoogleRoute.dll`
- Repository now contains the **SDK mod folder** only — not the full Big Ambitions monorepo

### Removed

- MelonLoader entry point, in-game auto-update, turn-by-turn driving HUD, and intersection arrows (0.10 MelonLoader features; see git tags `v0.10.0` / `v0.10.1` for legacy source)

## [0.10.1] - 2026-06-02 (MelonLoader — legacy)

### Fixed

- Duplicate destination route line on the map while mod route is active

### Changed

- Removed **ShowFullRouteLine** setting

## [0.10.0] - 2026-06-02 (MelonLoader — legacy)

### Added

- On-ground route line, turn HUD, intersection arrows, auto-walk, 22-language UI
- `latest.json` manifest for MelonLoader auto-update

[0.11.5]: https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/releases/tag/v0.11.5
[0.11.4]: https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/releases/tag/v0.11.4
[0.11.3]: https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/releases/tag/v0.11.3
[0.11.0]: https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/releases/tag/v0.11.0
[0.10.1]: https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/releases/tag/v0.10.1
[0.10.0]: https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/releases/tag/v0.10.0
