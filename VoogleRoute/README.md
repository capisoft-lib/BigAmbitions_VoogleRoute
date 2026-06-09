# Voogle Route

Big Ambitions mod (EA 0.11+) — GPS route line on the map, vehicle pathfinding on the traffic graph, optional auto-walk on foot.

| Property | Value |
|----------|-------|
| **Game** | Big Ambitions EA **0.11 Experimental** |
| **Unity** | **2022.3.62f2** with [Big Ambitions Modding SDK](https://github.com/hovgaardgames/bigambitions) |
| **Mod ID** | `VoogleRoute` |
| **Requires** | [`LIB_BaPlayerLocation`](https://github.com/capisoft-lib/BigAmbitions_LIB_BaPlayerLocation) |

## Clone (developers)

This folder is the `VoogleRoute/` directory inside the [BigAmbitions_VoogleRoute](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute) repository. Clone with submodules:

```bash
git clone --recurse-submodules https://github.com/capisoft-lib/BigAmbitions_VoogleRoute.git
cd BigAmbitions_VoogleRoute/VoogleRoute
```

PathFinding sources live in the git submodule `PathFinding/` → [BigAmbitions_VoogleRoute.PathFinding](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute.PathFinding).

## Install into your SDK

1. Copy **both** mods into your SDK:

   ```text
   <YourSdk>/Assets/Mods/LIB_BaPlayerLocation/
   <YourSdk>/Assets/Mods/VoogleRoute/
   ```

   Or run `tools/install-into-sdk.ps1` from each mod folder (after `git submodule update --init`).

2. Build the pathfinding DLL:

   ```powershell
   .\tools\build-pathfinding.ps1
   ```

3. Open the SDK in Unity, import game DLLs if prompted.
4. **Mod Builder → Build & Install** for `LIB_BaPlayerLocation`, then `VoogleRoute`.
5. Enable **LIB BA Player Location** and **Voogle Route** in the in-game mod menu.

## Pathfinding

Vehicle routing calls `VoogleRoute.Pathfinding.dll` from `Dependencies/` (netstandard2.1).

```
Unity mod (VoogleRoute.dll)
  → RoutePathfinder / RouteGraphStore
  → Dependencies/VoogleRoute.Pathfinding.dll
  ← built from PathFinding/ submodule
```

| Layer | Role |
|-------|------|
| **PathFinding/** | git submodule — routing algorithm sources |
| **Dependencies/** | precompiled DLLs shipped with the mod |
| **Data/** | CSV traffic graph loaded at runtime |
| **Scripts/** | Unity glue (NavMesh on foot, CSV + DLL for vehicles) |

After editing `PathFinding/`:

```powershell
.\tools\build-pathfinding.ps1
# then Mod Builder → Build & Install
```

## Configuration

Copy `config.json.example` to:

```text
%USERPROFILE%\AppData\LocalLow\...\BigAmbitions\ModsLocal\VoogleRoute\config.json
```

## Repository layout

```text
PathFinding/                git submodule (algorithm sources)
Dependencies/               VoogleRoute.Pathfinding.dll + System.Text.Json
Data/                       traffic graph CSV
Scripts/                    Unity mod code
tools/build-pathfinding.ps1 dotnet build → Dependencies/
```

## License

MIT
