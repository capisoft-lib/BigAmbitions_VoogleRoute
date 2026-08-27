# Visual test save fixtures

Dedicated Big Ambitions saves used as deterministic starting points for VoogleRoute UI screenshot tests.

## Layout

```
tests/visual-saves/
  manifest.json              # scenario id → save name mapping
  EA-0.11.5/                 # match game version folder (see below)
    _VOOGLE_VIS_/            # dedicated test character
      visual-ui-route-panel.hsg
      visual-ui-route-panel.hsg.meta
      visual-ui-route-panel.jpg
      ...
```

Saves are **not** in the repo until you create and export them (binary `.hsg` files). Follow the creation checklist below, then copy the character folder here.

## One-time save creation (in game)

1. Install **VoogleRoute** and its required libraries.
2. Set the game to **1920×1080 windowed** (recommended in `manifest.json`).
3. Start a **new character** named `_VOOGLE_VIS_` (folder id must match `manifest.json`).
4. For each scenario in `manifest.json`, reach the described state, then **Save** with the exact `saveName` (without extension).

| Scenario id | Save name | Prepare and capture in game |
|-------------|-----------|-----------------|
| `route-action-370` | `visual-ui-route-panel` | Outside, foot or vehicle, set a map GPS destination, route line on, action panel visible. Close settings/history/modals. |
| `map-bookmarks-420` | `visual-ui-map-bookmarks` | Open city map, bookmarks panel visible, a few fixed bookmarks in the list. History closed. |
| `bookmarks-and-history` | `visual-ui-dual-panels` | Open the city map, Bookmarks and History manually before capturing. |
| `settings-modal` | `visual-ui-settings` | Outside, open the route settings manually before capturing. |

5. **Never overwrite** these saves during a test session — reload from the main menu instead of saving.
6. Copy the whole `_VOOGLE_VIS_` folder from the game into `tests/visual-saves/<version>/`.

### Game save location

```
%USERPROFILE%\AppData\LocalLow\Hovgaard Games\Big Ambitions\SaveGames\<version-folder>\_VOOGLE_VIS_\
```

The `<version-folder>` name comes from the installed game build (often similar to `EA_0_11_5`). Run `tools/install-visual-saves.ps1 -ListVersions` to see folders on your machine.

### Mod metadata

Create saves **with mods enabled** so `.meta` files have `hasEverUsedMods: true` and matching `activeModsAtLastSave`. Otherwise the game shows mod mismatch dialogs on every load.

## Install fixtures into the game

From the mod repo root:

```powershell
.\tools\install-visual-saves.ps1
```

## Run one scenario

From the main menu, load `visual-ui-route-panel`, prepare its UI state, and take the screenshot manually.

Between scenarios: return to main menu and load the next save (do not save in-game).

## Compare screenshots

```powershell
.\tools\compare-visual.ps1 -ScenarioId route-action-370
```

Baselines live in `tests/visual/baselines/`. Save manual captures under `tests/visual/actual/`.
