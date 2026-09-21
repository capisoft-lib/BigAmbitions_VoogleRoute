# Voogle Route 1.0.6 validation and publication

Published on 2026-09-21 after explicit user authorization.

## Change and checks

- Phone GPS on Aluna's Phone Pages: heading-up vanilla map, bookmark search, city-map shortcut.
- Resume in-phone GPS after closing BizMan or the city map (M) with preserved map framing.
- Block ground click-to-move while panning the phone map.
- Long-press on a still point drops a draft pin; **Set destination** confirms navigation.
- Pathfinding tests: 203/203 pass (unchanged routing DLL).
- Player build via `compile-install-voogle-route.ps1 -NoInstall -NoDebugSymbols`; assembly 1.0.6.0.
- Four existing TMP deprecation warnings in unchanged UI files.
- No automated in-game acceptance test for the phone GPS flows.

## Package

- Source: `bigambitions/Output/VoogleRoute`, 30 Steam content files, 3,552,576 bytes (preview excluded).
- Workshop forbidden `Dependencies/LIB_BaUnifiedUI.PlayerMode.dll` removed from Output before validation (dev-only compile artifact).
- Release DLL built with `/debug-` (no embedded CodeView PDB path; sanitize count 0).
- Prepared copy: `%USERPROFILE%/AppData/LocalLow/Hovgaard Games/Big Ambitions/ModsLocal/VoogleRoute`.
- Prior ModsLocal backed up under `%LOCALAPPDATA%/Capisoft/BigAmbitions/WorkshopBackups/VoogleRoute-20260921-203444-751`.
- VoogleRoute.dll SHA-256: `9F39E19D98BC63050D00D5BD9B8EAF8B9BBA2D3D68D46C3C9A1D42388A28EE26`.
- Pathfinding SHA-256 unchanged: `2AD8FBEF31FED3CC9CE740E716FA23996568BB2F38056C04EF6C72F738B52597`.
- Package fingerprint (prepare): `2F36A84AA77EF73C2C72CA3DB77F10F9ED7E6D9F201A5111F4AC74A14CB7A514`.
- Uploader fingerprint: `9B8E85F449A6A681DE58C35A57078B1CB810C8FFA62537281EC722C7C3240297`.

## Steam evidence

- `validate`, `plan`, and `whoami` passed: UPDATE 3740623471, AppID 1331550, target build 3680, visibility Unchanged.
- One upload, exit 0: `Workshop update succeeded: 3740623471`.
- English and French descriptions and change notes submitted from `releases/1.0.6/`.
- Required Items attached: 3741773276 (Player Location), 3790426259 (Unified UI).
- Logs: `%LOCALAPPDATA%/Capisoft/BigAmbitionsWorkshopUploader/evidence/voogle-route-1.0.6/`.

[Workshop item](https://steamcommunity.com/sharedfiles/filedetails/?id=3740623471)
