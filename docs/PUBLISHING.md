# Publishing Voogle Route

## Steam Workshop (primary — EA 0.11)

### Dependency: LIB_BaPlayerLocation

Voogle Route **requires** the library mod **LIB BA Player Location** at runtime.

1. Publish **LIB_BaPlayerLocation** first (`NEW/bigambitions` → Mod Builder → Build & Install → Mod Creator upload).
2. Publish **VoogleRoute** and state the dependency in the Workshop description (`releases/<version>/full-description.md`).
3. Players must **subscribe to and enable both mods**. The Big Ambitions SDK does not auto-install Workshop dependencies — document them in the description.

Legacy dev mods `BaPlayerLocation-Subscriber` / `BaPlayerLocation-WebSocket` are **not** the shipped dependency; they were an earlier split layout used during local development.

### Runtime paths (do not hardcode ModsLocal for bundled assets)

| Kind | Location |
|------|----------|
| Everything (`Data/`, `config.json`, `Logs/`, etc.) | `ModContext.ModRootPath` only |

Steam Workshop: all paths are relative to the subscribed mod folder. The mod code does not create a parallel `ModsLocal/<ModId>/` tree unless the game installs the mod there (then `ModRootPath` already points at that folder).

### Voogle Route upload

1. Build both mods locally via the Big Ambitions SDK Mod Builder (`Output/LIB_BaPlayerLocation/`, then `Output/VoogleRoute/`). Confirm `Output/VoogleRoute/Data/` exists before upload.
2. Upload through the game's Workshop tools / Hovgaard publishing flow.
3. Copy workshop text from `releases/<version>/`:
   - `short-description.txt` — summary field
   - `full-description.md` — BBCode body (use `[b]`, `[list]`, `[url]` only — no `[size=]` tags)

## GitHub releases

1. Bump `VERSION`, `latest.json`, `VoogleRoute/ModManifest.asset`, and `CHANGELOG.md`.
2. Update `releases/<version>/` workshop copy if needed.
3. Commit on `main`, then tag: `git tag v0.11.0 && git push origin v0.11.0`
4. CI attaches `latest.json` and release copy files. Built DLL is **not** produced in CI (requires Unity + game assemblies).

## Version checklist

| File | Field |
|------|--------|
| `VERSION` | `0.11.0` |
| `latest.json` | `version`, `gameVersion` |
| `VoogleRoute/ModManifest.asset` | `Version:` |
| `CHANGELOG.md` | `## [0.11.0]` section |
