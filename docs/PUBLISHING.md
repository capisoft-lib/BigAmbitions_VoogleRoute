# Publishing Voogle Route

## Steam Workshop (primary — Big Ambitions EA 0.11 and 1.0 experimental)

### Dependencies: LIB_BaPlayerLocation and LIB_BaUnifiedUI

Voogle Route **requires** **LIB BA Player Location 1.0.0+** and **LIB BA Unified UI 1.0.0+** at runtime. They are separate Workshop items and must both be enabled.

Publish one Voogle Route 1.0.0 Workshop item and one package only. Its DLL selects the compatible game APIs at runtime; do not create a separate EA 0.11 build. The packaged route graph is the 1.0 graph, with 1.0-only Hamptons behavior disabled automatically on EA 0.11.

1. Publish **LIB_BaPlayerLocation 1.0.0+** and **LIB_BaUnifiedUI 1.0.0+** first (`bigambitions` → Mod Builder → Build & Install → Mod Creator upload).
2. Publish **VoogleRoute** and state both dependencies in the Workshop description (`releases/<version>/full-description.md`).
3. Players must subscribe to and enable all three mods. Also add both libraries to Steam's **Required Items** list once their Workshop item IDs exist.

Legacy dev mods `BaPlayerLocation-Subscriber` / `BaPlayerLocation-WebSocket` are **not** the shipped dependency; they were an earlier split layout used during local development.

### Runtime paths (do not hardcode ModsLocal for bundled assets)

| Kind | Location |
|------|----------|
| Everything (`Data/`, `config.json`, `Logs/`, etc.) | `ModContext.ModRootPath` only |

Steam Workshop: all paths are relative to the subscribed mod folder. The mod code does not create a parallel `ModsLocal/<ModId>/` tree unless the game installs the mod there (then `ModRootPath` already points at that folder).

### Voogle Route upload

1. Build and install **LIB_BaUnifiedUI** and **LIB_BaPlayerLocation** as separate mods in Mod Builder.
2. Build **VoogleRoute** via Mod Builder (`Output/VoogleRoute/`). Confirm `Output/VoogleRoute/Data/` and `Dependencies/VoogleRoute.Pathfinding.dll` exist, and confirm no `LIB_BaUnifiedUI*.dll` is present in its package.
3. Upload through the game's Workshop tools / Hovgaard publishing flow.
4. Copy workshop text from `releases/<version>/`:
   - `short-description.txt` — summary field
   - `full-description.md` — BBCode body (use `[b]`, `[list]`, `[url]` only — no `[size=]` tags)

## GitHub releases

1. Bump `VERSION`, `latest.json`, `ModManifest.asset`, and `CHANGELOG.md`.
2. Update `releases/<version>/` workshop copy if needed.
3. Commit on `main`, then tag: `git tag v1.0.0 && git push origin v1.0.0`
4. CI attaches `latest.json` and release copy files. Built DLL is **not** produced in CI (requires Unity + game assemblies).

## Version checklist

| File | Field |
|------|--------|
| `VERSION` | `1.0.0` |
| `latest.json` | `version`, `gameVersion` |
| `ModManifest.asset` | `Version:` |
| `CHANGELOG.md` | `## [1.0.0]` section |
