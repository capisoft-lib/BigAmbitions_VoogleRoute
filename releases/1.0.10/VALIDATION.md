# Voogle Route 1.0.10 validation

Date: 2026-10-11. Workshop item: [3740623471](https://steamcommunity.com/sharedfiles/filedetails/?id=3740623471).

## Change

Vehicle-exit cleanup runs from the player-location bridge before UI/navigation observers. It retains the last available effective vehicle mode across missing GPS poses and Hamptons live-mode changes. Failed native resets can retry at 0.1-second intervals for up to two seconds. Native navigation blockers are preserved; successful cleanup stops before later manual movement.

## Automated evidence

- Vehicle exit: **17/17** scenarios, compiling production `PlayerLocationSession`, `MovementModeDetector`, `PlayerLocationSnapshotMapper`, `PlayerNavigationRelease`, `VehicleExitNavigationRelease` and the library snapshot types against simulated Unity/game services.
- Negative control: the previous `PlayerLocationSession` fails the hidden-navigation exit scenario (`expected 1, got 0`).
- Existing arrival harness: **34/34** scenarios.
- PathFinding: **203/203** tests.
- Official player-mode compile: success with `-NoInstall -NoDebugSymbols`, Unity 2022.3.62f2 and installed game Build 3682. Four pre-existing TMP wrapping warnings; no errors.
- Assembly and package manifest: 1.0.10. English/French release descriptions fit Steam's 8000-byte limit (5718/6659 UTF-8 bytes).
- Workshop validate, stage-check and plan: passed. 32 files, 4,579,637 bytes, only `Dependencies/VoogleRoute.Pathfinding.dll` as packaged dependency.
- Vehicle-exit runner cleaned its temporary projects. Automatic approval review rejected cleanup of the separate arrival-harness directory and preparation script (`blocked by policy`); those two temporary artifacts remain outside the package.

Commands from the workspace root:

```powershell
powershell -NoProfile -File bigambitions/Assets/Mods/VoogleRoute/tools/vehicle-exit-tests/test-vehicle-exit.ps1
powershell -NoProfile -File bigambitions/Assets/Mods/VoogleRoute/tools/arrival-tests/test-arrivals.ps1
dotnet test bigambitions/Assets/Mods/VoogleRoute/PathFinding/Tests/VoogleRoute.Pathfinding.Tests.csproj --configuration Release --no-restore
powershell -NoProfile -File bigambitions/scripts/compile-install-voogle-route.ps1 -NoInstall -NoDebugSymbols
Copy-Item bigambitions/Assets/Mods/VoogleRoute/ModManifest.asset bigambitions/Output/VoogleRoute/ModManifest.asset
```

The compile script does not copy the manifest sidecar; preserve the explicit copy when reconstructing this package.

## Publication evidence

- UPDATE 3740623471 completed with exit code 0 and `Workshop update succeeded` after Steam was restarted and the user authorized a second attempt. First attempt failed before submission with `NoSteamClient`.
- English and French localizations submitted; visibility preserved, Required Items 3741773276 and 3790426259 attached. No legal-agreement warning.
- Independent public Steam API readback: result=1, updated=2026-10-11 10:39:18Z, visibility=0, banned=0, size=4,579,637, English description identifies 1.0.10 and the vehicle-exit fix.
- Public French page also identifies 1.0.10 and the new walking-control and pedestrian-transition sections.
- DLL SHA-256: `C1515BCC94FAB6AB8B42555B5BABF9E29A1738F9515A7247EE37D71F53BF6BC4`.
- Package fingerprint: `78904E850BAA6C6B5AE8B2097AE2FBDCA9BD82B70F1EC6F5FDF2E3CB24B5DB15`.

## Limits

The original character-stuck report has not been reproduced in a live save. The automated scenarios prove the skipped-cleanup code paths and bounded recovery behavior; they do not prove native movement, NavMesh placement or every other cause of an immobilized character. A full game restart and a real AUTO-DRIVE/vehicle-exit test remain necessary for gameplay confirmation.
