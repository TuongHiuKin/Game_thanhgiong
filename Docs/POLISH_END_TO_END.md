# Thanh Giong isometric polish - end-to-end notes

Date: 2026-10-08

## Installed skill repositories

- `awesome-gamedev-agent-skills`: cloned at `E:/Game/AgentSkillRepos/awesome-gamedev-agent-skills`, commit `d4b0e35550c55ae70bdfcab4ef5a0e94610438a9`.
- `Unity-Skills`: cloned at `E:/Game/AgentSkillRepos/Unity-Skills`, commit `12a40e49d790845c325efb7919a8d9928c6d30c4`.
- Installed Codex skills are under `C:/Users/ndai0/.codex/skills`.
- Unity package `com.besty.unity-skills` is embedded under `E:/Game/Game_thanhgiong/Packages/com.besty.unity-skills`.

## Gameplay and presentation pass

- Converted the campaign toward a 2.5D/isometric presentation with fixed orthographic framing, painterly URP grading, film grain, ambient motes, sunshafts, animated water ripples and soft route composition.
- Added bounded landscape dressing across the maps: clustered grass, reeds, flowers and stones with route preservation and no extra collision blockers.
- Improved mounted combat feel: dodge invulnerability, wall-stop behavior, bamboo/iron attack timing, combo beats, hoof contact dust and pause-stable rig animation.
- Smoothed enemy procedural animation: locomotion speed blending, softer turn banking, footstep bob tied to filtered velocity, attack anticipation blend, stuck/recover/stun easing and less abrupt hit recoil.
- Added five seed skills with charge, mana, cooldown, placement validation, Lumen root behavior, healing lotus, bloom visuals and distinct audio cues.
- Added checkpoint snapshot restore for player, seed state, enemy state and collectibles, with a more robust enemy restore fallback for runtime-created scene objects.
- Improved UI/HUD behavior: timed popups, safe-area sizing across viewport ratios, hotbar cooldown feedback and pause menu navigation.
- Improved audio layering: adaptive exploration/combat mix, event ducking, spatial river/bank ambience and master/music/effects mix validation.
- Hardened Player build shaders for speed ribbons, golden dissolve and checkpoint markers so stripped shader fallbacks do not crash standalone builds.

## Verification evidence

- Editor console after compile: `0` error entries.
- Editor Play verifier:
  - `E:/Game/Game_thanhgiong/PortVerification/runtime_results.txt`: `GODOT_PORT_VERIFY PASS failures=0`.
  - `E:/Game/Game_thanhgiong/IsometricVerification/runtime_results.txt`: `ISOMETRIC_VERIFY PASS failures=0`.
- Final Windows build:
  - Executable: `E:/Game/Game_thanhgiong/Builds/Windows/ThanhGiong.exe`.
  - Build job: `build-f05ece464a`.
  - Result: succeeded, `0` errors, `669` warnings, `184.25 MB`.
- Standalone verifier launched with:
  - `ThanhGiong.exe --verify-polish -screen-width 1280 -screen-height 720 -screen-fullscreen 0`.
  - `E:/Game/Game_thanhgiong/Builds/Windows/standalone_results.txt`: `STANDALONE_VERIFY PASS runtime_errors=0`.
  - `E:/Game/Game_thanhgiong/Builds/Windows/PortVerification/runtime_results.txt`: `GODOT_PORT_VERIFY PASS failures=0`.
  - `E:/Game/Game_thanhgiong/Builds/Windows/IsometricVerification/runtime_results.txt`: `ISOMETRIC_VERIFY PASS failures=0`.
- Game View screenshot:
  - `E:/Game/Game_thanhgiong/BuildRoot/VerificationShots/editor_gameview_lang_giong.png`.

## Notes

- The build still reports asset/import warnings, mostly from legacy KayKit FBX/material metadata, but no build errors and no standalone runtime errors were present in the final verification.
- The verifier intentionally checks campaign maps, equipment gestures, mounted rig, UI timing, seeds, checkpoint replay, terrain support, adaptive audio, pause behavior and standalone player stability.
