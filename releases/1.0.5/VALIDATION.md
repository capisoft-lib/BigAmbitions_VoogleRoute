# Voogle Route 1.0.5 validation and publication

Published on 2026-09-13 after explicit user authorization.

## Change and checks

- Retains ordinary building GPS after vehicle arrival outside missions; defers
  completion/auto-entry to foot arrival and does not start Auto Walk automatically.
- Production behavior change confined to NavigationArrivalService.cs. Existing
  delivery, Auto Walk and shared-library sources were preserved.
- 34 arrival scenarios passed against production navigation files with simulated
  game/Unity services, including delivery shifts and return-to-depot handling.
- Negative control against pre-change sources reproduces the vehicle GPS clearing.
- Full player build via `bigambitions/scripts/compile-install-voogle-route.ps1
  -NoInstall` passed, along with Mono player-profile verification. Assembly 1.0.5.0.
- Three existing TMP deprecation warnings in unchanged UI files.
- No in-game acceptance test. Publication does not establish gameplay validation.

## Package

- Source: `bigambitions/Output/VoogleRoute`, 31 files, 4,537,274 bytes.
- Prepared copy: `%USERPROFILE%/AppData/LocalLow/Capisoft/WorkshopReleases/VoogleRoute-1.0.5/VoogleRoute`.
- One embedded local CodeView PDB path sanitized using the existing packaging
  script. Sanitized DLL copied back to Output for Steam's direct content handoff.
- Only VoogleRoute.dll differs from the prior Output package; other files unchanged.
- DLL SHA-256: `2E9F9AE95FE40AB1ADA3D534777EA38DCB824177B323A8E61FF1D0A0C8DC995E`.
- PathFinding SHA-256 unchanged: `2AD8FBEF31FED3CC9CE740E716FA23996568BB2F38056C04EF6C72F738B52597`.
- Uploader fingerprint: `2874EAC95132F703D3887D292DCBBE7B8A06DDE0CA56E20B97A09B48569B3A1F`.
- Prior Output backed up under `%LOCALAPPDATA%/Capisoft/BigAmbitions/WorkshopBackups/VoogleRoute-before-1.0.5-20260913-132924`.
- No ModsLocal installation, Git commit or push performed.

## Steam evidence

- validate, stage-check and plan passed: UPDATE 3740623471, AppID 1331550,
  target build 3675, game metadata modVersion 0, visibility Unchanged.
- whoami verified the Steam session for AppID 1331550.
- One upload, exit 0: `Workshop update succeeded: 3740623471`.
- English and French descriptions submitted; English change note submitted.
- Required Items attached: 3741773276 (Player Location), 3790426259 (Unified UI).
- Direct package restored and fingerprint verified after submission.
- Independent public Steam API readback: result 1, app 1331550, visibility 0,
  banned 0, time_updated 1789299050, description contains Voogle Route 1.0.5
  and the new parking text.
- Two Steam shutdown `pipes.cpp` assertions appeared after the confirmed success;
  command still exited 0 and subsequent public readback confirmed the update.

[Workshop item](https://steamcommunity.com/sharedfiles/filedetails/?id=3740623471)
