# Voogle Route 1.0.12 validation

Date: 2026-10-16. Workshop item: [3740623471](https://steamcommunity.com/sharedfiles/filedetails/?id=3740623471).

## Release scope

- Visible auto-drive beta and per-trip choice between WATCH THE DRIVE and FAST TRAVEL.
- Native gears, speed preferences, pedestrian/speeding/red-light switches, revised cornering and cruise control.
- Live readiness refresh after stopping/repositioning, with cached stationary refusals and expanded timing probes.
- Release metadata: ModManifest/latest.json 1.0.12, assembly/file version 1.0.12.0, informational version 1.0.12.
- Current target is Big Ambitions 1.0 Build 3682. EA 0.11 was not revalidated; legacy adapters were preserved in source.

## Build and package

- Player compilation succeeded for 153 C# sources against Unity 2022.3.62f2 and installed Big Ambitions Build 3682 (Steam build 25482473).
- No debug symbols; player-runtime assembly checks passed. Four existing TextMeshPro `enableWordWrapping` warnings, no errors.
- Package: 33 files, 4,696,721 bytes, 22 runtime locales. The only packaged dependency is `Dependencies/VoogleRoute.Pathfinding.dll`.
- Workshop validation passed, including the package file rules and embedded local user-path scan. Staging copy creation/verification/removal passed.
- Required Items: LIB BA Player Location `3741773276`, LIB BA Unified UI `3790426259`; neither library is bundled in this package.
- Package fingerprint: `5CB1B9D4FFFF7493618C905A172C20254227009188F3FD38A1BAEA286DFDF26E`.
- Release DLL SHA-256: `5914507B8840325FE8E9BA9C44E887E429C11DDF0389A6BF7096B34603266357`.
- The same release was installed in ModsLocal with every payload hash compared; user config stayed at SHA-256 `59505653560F93D6E4A8273782B463001FB89049415019B37A877A04BE7EF07D`.

## Publication

- Private uploader manifest outside the repository kept the existing item, visibility Unchanged, target build 3682 and numeric modVersion 0. Numeric metadata was preserved; the semantic release version comes from the mod package.
- Non-mutating validate, stage-check and plan all exited 0; plan resolved to UPDATE `3740623471`.
- Steam identity check confirmed AppID 1331550.
- One authorized upload attempt exited 0 and reported `Workshop update succeeded: 3740623471`.
- English and French description submissions and both Required Items were confirmed. Package fingerprint was checked again after handoff. No legal-agreement warning was returned.
- Two Steam shutdown assertion lines (`!m_bOutstandingCallback`) printed after the successful outcome; exit code remained 0 and independent public readback confirmed the result.
- Independent Steam public API readback: result 1; version 1.0.12 in the description; updated `2026-10-16 14:57:04Z`; visibility 0 (Public), banned 0; file size 4,696,721 bytes. The new visible-driving and live-dialog sections are present.

## Gameplay evidence and limits

The user drove the development revisions, including revision 6, with native gear changes recorded in Player.log. The final revision-7 readiness refresh was compiled/deployed, then included unchanged in this release apart from version metadata. No additional test suite or live-game interaction was run for release preparation.

Earlier logs measured preparation at about 216 ms and departure at 137 ms; finer probes were added, but these pauses are not claimed resolved. The new readiness behavior still needs observation in game. Visible driving remains beta: stopped/aligned lane start, manual parking, no reverse/U-turn/elevated/underground driving routes. Fast travel stays available. Restart the game after Workshop updates.
