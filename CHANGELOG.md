# Changelog

All notable changes to **Voogle Route** are documented here.

## Unreleased

## [1.0.0] - 2026-08-26

### Added

- **Big Ambitions 1.0 experimental routing data** — refreshed the city road graph from the current game waypoints and added bidirectional regression coverage for all 18 Hamptons mansion addresses
- **Hamptons WAY OUT / GET OUT routing** — dedicated entrance-first exit resolution for open-world mansion plots, with bounded perimeter fallback and exact terminal-waypoint handoff
- **Configurable action shortcuts** — route-line toggle and auto-walk/auto-drive can be rebound under **Options → Mods**; defaults are **Ctrl+Shift+Y** and **Ctrl+Shift+X**
- **Draggable windows** — the action panel, settings, bookmarks, history and popups use native dragging, screen clamping and saved positions supplied by BA Unified UI

### Changed

- **Single cross-version package** — one Voogle Route 1.0.0 DLL now adapts at runtime to Big Ambitions EA 0.11 and 1.0 experimental; no alternate legacy build is required
- **Stable location dependency** — now requires **LIB BA Player Location 1.0.0+**, published as the same cross-version library package for EA 0.11 and 1.0 experimental
- **Standalone UI dependency** — now requires **LIB BA Unified UI 1.0.0+** as a separate enabled Workshop item; Voogle Route no longer bundles a private BAUI DLL
- **Route rendering on 1.0 experimental** — hybrid LineRenderer/ribbon-mesh strokes preserve a thin, deterministic route width when stripped player APIs are unavailable
- **Hamptons vehicle approaches** — improved mansion entrances, curb-side lane arrivals and cul-de-sac turns in both directions
- **History map sessions** — city-map History visibility is isolated from the normal HUD state and restored exactly when the map closes
- **Options integration** — every BAUI canvas hides and becomes non-interactive while the vanilla Options screen is open, then returns to its prior state
- **Shortcut safety** — complete key chords are checked against vanilla and participating mod bindings, while menus, text fields and modal windows block route actions

### Fixed

- **EA 0.11 compatibility** — isolated 1.0-only Hamptons types and adapted taxi state, city-map taxi mode, fades, time skips and entrance lookup to both game APIs
- **Taxi arrival compatibility** — added a post-taxi guard so VoogleRoute does not immediately auto-enter the destination building after a vanilla taxi warp
- **Route thickness regression** — restored visible foot, vehicle, indoor, subway and city-map route strokes on Big Ambitions 1.0 experimental
- **History recalculation feedback** — opening History no longer shows or controls the active-route recalculation banner while row distances load in the background
- **Hamptons exit performance** — entrance candidates are prioritized and perimeter work is bounded to avoid NavMesh retries and frame-rate collapse near mansion gates

## [0.11.11] - 2026-07-25

### Fixed

- **Flatbed & hand truck mode** — pushed delivery equipment is normalized to on-foot navigation even when an older location library reports it as a car
- **Auto-drive safety** — cargo tools are rejected before route planning, confirmation, and teleport, preventing lost products and randomly moved carts
- **Walking route origin** — fallback compatibility uses the player position instead of the cargo vehicle position
- **Last parked vehicle** — flatbeds and hand trucks no longer replace the saved motor-vehicle shortcut

### Changed

- Requires **LIB BA Player Location** `0.11.2+`, which reports `spawnInPlayerObject` cargo tools as walking

## [0.11.10] - 2026-06-19

### Fixed

- **Flatbed & hand truck** — pushing delivery cargo (`spawnInPlayerObject` vehicles) is treated as on-foot navigation; GPS panel shows **AUTO-WALK** instead of **AUTO-DRIVE**
- **Delivery job routing** — improved sync with vanilla delivery missions (stop targets, return-to-depot, deferred arrival handling); auto-walk triggers door interact at delivery stops from a flatbed or hand truck
- **Route colors** — foot, vehicle, and indoor line colors persist correctly after save/reload (IL2CPP-safe mod-data encoding; `System.Text.Json` write failure in-game)

## [0.11.9] - 2026-06-17

### Added

- **LIB_BaUnifiedUI integration** — GPS panel, settings, bookmarks, history, and popups use the shared vanilla-style UI library (bundled in `Dependencies/LIB_BaUnifiedUI.dll`; no separate Workshop mod required)
- **City-map DRIVE / WALK shortcuts** — beside the vanilla set-destination button when a building is selected on the map (**M**)
- **Auto-enter on arrival** — optional mod toggle to enter a vehicle or building once navigation reaches the destination
- **Delivery job routing** — follows vanilla job guider targets when no map GPS is set
- **Auto-drive fuel estimate** — confirmation popup shows approximate fuel use for the trip
- **Display toggles** — **Display VoogleRoute Outside** and **Display VoogleRoute Inside** under **ESC → Options → Mod** (read at city load)
- **Mod locale lookup** — panel strings load from the mod `Locales/` folder for all **22** languages
- **Arrival toast** — localized “You have arrived at your destination” message
- **Subway hints** — localized board-station and ride-complete strings on the GPS panel

### Changed

- **Auto-walk** — refactored movement pipeline; improved subway boarding and foot-leg handoff
- **Visit history** — richer row metadata and persistence improvements
- **PathFinding** — elevation-aware vehicle arrival and refined foot subway planning

### Fixed

- **Auto-drive under bridge decks** — teleport placement respects in-game entrance height instead of snapping under elevated road geometry
- **Stuck screen fade** — recovers from a black overlay when no travel fade or subway ride is in progress
- **Mod options at city load** — outside/inside display prefs read from PlayerPrefs even before opening **ESC → Options → Mod**

## [0.11.8] - 2026-06-13

### Added

- **Subway routing on foot** — when walkable NavMesh cannot reach the destination, plan **walk → subway → walk** via city stations (including Manhattan Bridge crossings)
- **Auto-walk subway boarding** — at the board station, auto-walk triggers the vanilla ride and selects the planned exit station
- **Use subway** mod option — **ESC → Options → Mod → Voogle Route** (default **on**); disable for foot-only routing with no subway fallback
- **Subway route line** — dashed **yellow** segment on the ground and on the city map (**M**); foot legs remain separate (no blue chord across the subway section)
- **Locales** — `voogle_route_options_use_subway` in all **22** supported languages

### Changed

- **Subway trace elevation** — subway segment projected to the same ground height as the on-foot path on the city map

### Fixed

- **Auto-walk at subway** — no longer stops at the station without boarding when the route uses the metro
- **Subway map UI** — Voogle Route bookmarks/history/recalc banner hide during subway mode so they do not block station interaction
- **Subway destination selection** — city-map **BOOKMARKS** panel no longer prevented choosing a subway station (blocked clicks / focus)
- **Async route display** — cached subway segments preserved while a foot route recalc is in progress

## [0.11.7] - 2026-06-13

### Added

- **Visit History panel** — last **50 buildings visited** (any type), most recent first; scrollable list with route distances
- **History shortcuts** — clock icon on the **VOOGLE ROUTE** GPS header and on the city-map **BOOKMARKS** panel header
- **Row actions** — **CENTER**, green **ADD** (opens bookmark dialog with building coordinates), and **SET** destination, matching bookmark row layout
- **Persistence** — visit history stored in `bookmarks.json` under `visit_history` (deduplicated by address/position)
- **Locales** — History title and ADD button strings (`en`, `fr`; other languages fall back to English)

### Changed

- **GPS header** — fourth toolbar icon (history) added alongside bookmark, last-car, and settings buttons
- **Map panel chrome** — shared header layout helpers (`NavPanelLayout`); 420 px bookmarks/history headers aligned with the body frame

### Fixed

- **Bookmarks on map** — panel stays visible when selecting a building; hides only when opening BizMan or other map overlays (not on building click alone)
- **Business creation** — company name field keeps focus while typing on the city map
- **Visit History** — panel opens from the bookmarks header while the city map is open
- **History header** — close button inset and title alignment match the bookmarks panel

## [0.11.6] - 2026-06-13

### Added

- **Bookmarks panel** on the city map (**M**) — searchable list with **SET** destination, **CENTER** map, and route distance per row
- **Custom bookmarks** — save named places from the panel or the HUD **+** button at your current position; persisted in `bookmarks.json` (auto-migrated from legacy `config.json` bookmark fields)
- **Quick shortcuts** — **Last Car**, **Last Home**, **Last Shop** auto-updated when you park, enter your home, or enter a business
- **Owned vehicles** — parked motor vehicles listed automatically (outside warehouses)
- **HUD shortcuts** — blue **+** saves a bookmark at your position; green **car** sets your route to the last parked vehicle
- **Building-type icons** on bookmark rows
- **Async distance labels** — route distances computed on a background thread while the bookmarks panel is open
- **Locales** — bookmark UI strings in all **22** supported languages

### Changed

- **UI visual parity** — GPS panel, settings, popups, and bookmarks use vanilla-style frames, fonts, and button treatment (`GameStylePanelChrome`)
- **Input handling** — mod UI blocks game hotkeys while typing in bookmark search or name fields
- Default log level **error** when file logging is enabled (`config.json`)

### Removed

- Leftover experimental **auto-steering** sources (`VehiclePathFollower`, `VehicleDriveController`, etc.) and auto-drive diagnostic logs

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

[1.0.0]: https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/releases/tag/v1.0.0
[0.11.11]: https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/releases/tag/v0.11.11
[0.11.10]: https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/releases/tag/v0.11.10
[0.11.9]: https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/releases/tag/v0.11.9
[0.11.8]: https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/releases/tag/v0.11.8
[0.11.7]: https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/releases/tag/v0.11.7
[0.11.6]: https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/releases/tag/v0.11.6
[0.11.5]: https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/releases/tag/v0.11.5
[0.11.4]: https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/releases/tag/v0.11.4
[0.11.3]: https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/releases/tag/v0.11.3
[0.11.0]: https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/releases/tag/v0.11.0
[0.10.1]: https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/releases/tag/v0.10.1
[0.10.0]: https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/releases/tag/v0.10.0
