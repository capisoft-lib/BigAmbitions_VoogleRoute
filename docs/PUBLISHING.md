# Publishing Voogle Route

## Steam Workshop (primary — EA 0.11)

1. Build the mod locally via the Big Ambitions SDK Mod Builder (`Output/VoogleRoute/`).
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
