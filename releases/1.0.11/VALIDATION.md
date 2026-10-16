# Voogle Route 1.0.11 validation

Date: 2026-10-16. Workshop item: [3740623471](https://steamcommunity.com/sharedfiles/filedetails/?id=3740623471).

## Change

- Add a 50–150% UI scale slider to **Options → Mods**. Changes apply immediately and persist per save; the option label/value is present in all 22 runtime locales.

## Build and package

- Player-mode compilation succeeded for 143 C# source files with Unity 2022.3.62f2 and installed Big Ambitions Build 3682.
- Assembly version: **1.0.11.0**. The build used no debug symbols and passed the player-runtime assembly check; no local user-profile path was found in the release DLL.
- Four existing CS0618 warnings remain for obsolete TextMeshPro `enableWordWrapping` calls; there were no compile errors.
- Package: **32 files**, **4,596,936 bytes**, 22 locales. The only packaged dependency is `Dependencies/VoogleRoute.Pathfinding.dll`.
- Workshop `validate`, `stage-check` and `plan` passed. Effective action: UPDATE `3740623471`; visibility unchanged; English and French descriptions; Required Items `3741773276` and `3790426259`.
- Package fingerprint: `06845D70D836F6D22ABFC8DCAA969F6C0FEAC3141E877218AC3EF65FAC727156`.
- Release DLL SHA-256: `837405CB0AAD9CABC0B14555838D9C52C9FF042AC387AF73712629EB86AE3C7B`.

## Publication

- One authorized update attempt exited 0: `Workshop update succeeded: 3740623471`.
- Steam confirmed English and French localization submissions and both Required Items. No legal-agreement warning was returned.
- Independent public API readback: result 1, updated `2026-10-16 11:09:32Z`, visibility 0 (Public), banned 0, file size 4,596,936 bytes.
- An anonymous HTML page fetch returned Steam's generic hidden/permission error; the public API readback independently reports the item as Public.

## Limits

The release build and Workshop submission were verified. No post-release live-game UI interaction was captured; restart Big Ambitions and check **Options → Mods → UI scale** in game.
