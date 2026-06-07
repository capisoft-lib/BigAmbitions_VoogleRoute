# Voogle Route

**Voogle Route** extends **Voogle Maps** in Big Ambitions: set a destination on the city map, then follow a glowing **on-ground route line** on foot or in a vehicle, with optional **auto-walk**.

Built on the official **Big Ambitions Modding SDK** (EA 0.11 Experimental). No MelonLoader.

| | |
|---|---|
| **Game** | Big Ambitions EA **0.11 Experimental** |
| **Distribution** | [Steam Workshop](https://steamcommunity.com/app/2977660/workshop/) — subscribe, then enable in the in-game **Mods** menu |
| **Languages** | All **22** Big Ambitions interface languages |
| **Author** | [capisoft-lib](https://github.com/capisoft-lib) — community mod, not affiliated with Hovgaard Games |

## Features

- **Route line on the ground** — neon path to your map destination; separate styling on foot vs. in a vehicle
- **Road-aware driving routes** — vehicle paths follow the city's road network
- **Auto-walk** — walk the route automatically (`WALK ON`); stops if you take manual control
- **VOOGLE ROUTE panel** — bottom-left BizPhone-style UI (`ROUTE ON / ROUTE OFF`)
- **Custom line color** — presets or the in-game color picker (gear icon)
- Hidden in the **subway** and when navigation is unavailable

## Repository layout

```
VoogleRoute/          ← mod sources (drop into SDK Assets/Mods/VoogleRoute/)
  Scripts/
  Locales/
  Data/
  ModManifest.asset
  Thumbnail.png
releases/             ← Steam Workshop copy per version
tools/                ← locale generator
```

## Player install

Subscribe on **Steam Workshop**, then enable **Voogle Route** in the game's **Mods** menu.

## Development

Requires the [Big Ambitions Modding SDK](https://github.com/HovgaardGames/BigAmbitionsModding) (Unity **2022.3.62f2**) and a local game install for imported assemblies.

1. Clone this repository.
2. Copy or symlink the `VoogleRoute/` folder into your SDK project at `Assets/Mods/VoogleRoute/`.
3. Import game DLLs via the SDK setup flow.
4. Build with **Big Ambitions → Mod Builder → Build + Install** on `VoogleRoute`, or run your project's batch build script.

Output installs to:

`%LocalLow%\Hovgaard Games\Big Ambitions\ModsLocal\VoogleRoute\`

## Migrating from MelonLoader (0.10)

The MelonLoader builds (`v0.10.0`, `v0.10.1`) are **legacy**. Remove MelonLoader and `VoogleRoute.dll` from `Big Ambitions/Mods/`, then use the Workshop / SDK build for 0.11. See [legacy/README.md](legacy/README.md).

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

## Licence

See [LICENSE](LICENSE).

---

# Voogle Route (français)

Mod **SDK Big Ambitions** : ligne d'itinéraire au sol, marche auto et couleur personnalisable pour les destinations **Voogle Maps**.

| | |
|---|---|
| **Jeu** | Big Ambitions EA **0.11 Experimental** |
| **Installation** | **Steam Workshop** — s'abonner, puis activer dans le menu **Mods** |
| **Langues** | Les **22** langues d'interface du jeu |

### Développement

Copier le dossier `VoogleRoute/` dans `Assets/Mods/VoogleRoute/` d'un projet SDK, puis builder via le Mod Builder Unity.

Les versions MelonLoader 0.10 sont archivées : voir [legacy/README.md](legacy/README.md).
