# Voogle Route 1.0.4 validation

Published on 2026-09-12 after explicit authorization to retry using Output/VoogleRoute.
The uploader exited 0 with Workshop update succeeded: 3740623471, confirmed
English/French submissions and both Required Items. The public Steam API returned
result 1, description version 1.0.4, visibility 0 (public), banned 0 and update
timestamp 1789238211. Earlier failed attempts are retained below as history.
Not installed into the player's ModsLocal folder and not verified in a live
QuickRid session.

## Change and regression boundaries

- Arrival memory survives clearing the local tracker and temporary missing GPS
  samples. Reimporting the completed target cannot restart route computation,
  auto-enter or notifications while the player remains in the arrival area.
- Job targets, and map targets associated with a mission or job guider, retain
  the external destination and guider. Ordinary map GPS and Voogle Route's own
  world/parked-vehicle guider still clear as before.
- The memory rearms for a different target, a changed mission object, an explicit
  destination selection, session shutdown, or leaving the original arrival
  radius plus 5 m. It keeps the original radius when changing from vehicle to
  foot, and keeps the effective foot endpoint after the route cache is cleared.
- Native DeliveryDriverMission handling remains deferred, including the return
  to depot; native auto-walk delivery interaction is retained.
- Arrival distance thresholds remain 7 m on foot and 25 m in vehicles.
- No shared library, route data, pathfinding source or packaged dependency change.

An external phase change with the same mission object and identical coordinates
is indistinguishable from GPS resynchronization. It intentionally remains quiet
until the player leaves the area or explicitly requests navigation again. A new
mission object at the same coordinates does rearm. Do not infer or complete a
third-party mission phase from GPS proximity.

## Automated evidence

Run from the mod repository:

```powershell
& ./tools/arrival-tests/test-arrivals.ps1
dotnet test ./PathFinding/Tests/VoogleRoute.Pathfinding.Tests.csproj -c Release --nologo
```

- 24 arrival regression scenarios passed. The harness compiles seven production
  navigation files, including actual AutoWalkService, against simulated game and
  Unity services. It checks notification counts, route requests, external GPS
  preservation, cleanup, explicit reselection, mode changes, effective endpoints,
  transient gaps, new targets/missions, and native delivery behavior.
- The harness calls the explicit vanilla-button rearm callback; native Unity
  button event delivery still requires the in-game check below.
- 203 PathFinding tests passed, zero failed or skipped.
- Negative control: pre-fix code emitted 25 notifications in 100 syncs. Reproduce
  with the command below; an exception stating expected 1, got 25 is intentional:

```powershell
& ./tools/arrival-tests/test-arrivals.ps1 -Baseline -BaselineRef 4f7be6aef242230311dec09207787e6b297dd891
```

Full player-mode build succeeded via `compile-install-voogle-route.ps1 -NoInstall`.
Both assemblies passed the installed Mono player profile check, using Unity
2022.3.62f2. Three existing TMP word-wrapping deprecation warnings remain in
unchanged UI files. Assembly version read back from the prepared DLL: 1.0.4.0.

## Prepared release

- Folder: `%USERPROFILE%/AppData/LocalLow/Capisoft/WorkshopReleases/VoogleRoute-1.0.4/VoogleRoute`.
- After comparison with the successful BetterFines upload, the active manifest
  now uses `%BIG_AMBITIONS_REPO%/bigambitions/Output/VoogleRoute`. Its files are
  identical to the prepared folder, including the sanitized DLL. Validation,
  staging and plan passed with the same fingerprint; the subsequently authorized
  Steam upload from this path succeeded.
- 31 files, 4,537,274 bytes. Compared with the prepared 1.0.3 package, only
  `VoogleRoute.dll` differs; the other 30 files match byte for byte.
- Release DLL SHA-256: `32748D010E05DD23D11C74101742DF275F38DFF8990D236DE8D79B932991C661`.
- Unchanged PathFinding dependency SHA-256:
  `2AD8FBEF31FED3CC9CE740E716FA23996568BB2F38056C04EF6C72F738B52597`.
- One embedded local CodeView PDB path was removed by the standard package
  preparation script; no player data or shared libraries are bundled.
- `validate`, `stage-check`, and `plan` all passed for UPDATE 3740623471,
  visibility Unchanged, English and French, dependencies 3741773276 and
  3790426259. Target build 3675 is taken from the local game's latest Player.log
  startup entry. Workshop integer modVersion remains 0, distinct from the DLL
  semantic version.
- Uploader fingerprint:
  `AB0423E33C1645613CFC6013995408BCD32A448E5EE00059961E6C59328956BA`.
- Manifest: `%LOCALAPPDATA%/Capisoft/BigAmbitionsWorkshopUploader/manifests/VoogleRoute.json`.
- Following explicit publication authorization, Steam identity verification passed
  and one upload was attempted. Base content/English submission returned Fail (2).
  A subsequent read-only plan retained the previous published fingerprint. No
  automatic retry, Git commit or push was performed.
- A second explicitly authorized attempt also failed with Fail (2). Steam's
  depot build log reports the release parent mapping as nonexistent and finds
  zero files. The local package still has 31 files and the verified DLL hash.
  File discovery by Steam is failing; the underlying path/access cause remains
  unresolved. Publication is still not confirmed.

## Live acceptance checks still pending

Use a restarted game with the candidate build and record the actual QuickRid
version and game build. These checks are necessary before claiming confirmed
QuickRid compatibility or visual/UI regression coverage:

1. Reach a QuickRid pickup while its mission remains active; wait at least 30 s.
   Expect one arrival, no reappearing Voogle Route line or repeated auto-enter,
   and unchanged passenger/mission behavior. Repeat at drop-off, manually and
   with Auto Drive.
2. Continue to the next stop, then test a second ride and a stop at the same
   address. Check the identical-phase limitation described above.
3. Exit the car while waiting, move around the radius boundary, then leave by
   more than 30 m and return. No spam while waiting; navigation can resume after
   leaving. Test auto-walk too, with auto-enter enabled and disabled.
4. Set a normal GPS destination with no mission, reach it, and select the same
   destination again through vanilla SET DESTINATION, map AUTO-WALK/AUTO-DRIVE,
   bookmarks and History. Verify a fresh route/arrival and working native buttons.
5. Navigate to a parked vehicle or a world bookmark during a mission; Voogle
   Route's own guider must clear while the job guider remains operational.
6. Run a native delivery on foot, with pushed cargo and in a vehicle/scooter,
   including return to depot. Check delivery interaction and no extra toasts.
7. Verify an ordinary subway walk and building auto-enter, then reload a save
   and confirm the same destination can be used without stale suppression.

Automated tests reduce regression risk; they cannot prove the behavior of an
unobserved third-party mission implementation or a rendered game session.
