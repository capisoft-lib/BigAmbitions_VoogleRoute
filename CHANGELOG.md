# Changelog

All notable changes to **Voogle Route** are documented here.

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

[0.11.0]: https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/releases/tag/v0.11.0
[0.10.1]: https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/releases/tag/v0.10.1
[0.10.0]: https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/releases/tag/v0.10.0
