# Current state

Stage: source/Editor review checkpoint after presentation and reset fixes. All further player builds stopped at the owner's request. The interactive Unity Editor is closed.

Project: D:/Game/PixelFlowClone. Template baseline: f6486d0e8fa7e877360d7b775c96ad419474fe0f. Editor: C:/Program Files/Unity/Hub/Editor/6000.4.12f1/Editor/Unity.exe.

Implemented: seven original early levels (2,560 pixels), original 34-color mapping and selected art/audio; front queue selection, bounded conveyor, occluded color hits, ammo, five waiting slots/relaunch, win/loss, pause, restart, next/Home, progression and sound setting. Static campaign uses Dreamy DataConfig; persistent data uses Dreamy Datasave. Runtime state is a separate tested model.

Validation: `Evidence/editmode-final.xml` passed 20/20. The final existing Windows candidate completed 12 pointer-driven scenario checks and exited 0 (`Evidence/player-final10/smoke.json`). Normal visible Home/window-close also exited 0 (`Evidence/player-close-quitprobe10.json`). Opened actual Home/game/win captures for review.

Latest source validation (2026-10-24): `Evidence/editmode-tests-20261024-105312.xml` passed 20/20 rule/data/save cases with no graphics device. `Evidence/editmode-review-20261024-105022.xml` passed 2/2 Editor scene fixtures with a graphics device and isolated saves; route receipt has 9 checks. Both were Editor-only, not player builds. Preview/input details are in `Evidence/editor-review/verified-source-review-20261024/{captures,route}.json`: synthetic MouseState events, explicit InputSystem/EventSystem pumping and UI render warmup; these are not physical-device or normal player-scheduling acceptance.

Presentation source now uses larger pigs (scale0.90, row spacing1.15), separates waiting/queue/ammo spacing, and uses owned rounded nine-slices for progress/modal instead of stretched button art. Retired board objects deactivate immediately before deferred Destroy; ClearSession stops session audio and resets transient timers. Opened latest level 1 previews at 720x1280,1080x1920,1080x2340, plus pause and Home-progress. These are offscreen URP previews, not Game View or simulated cutout safe areas. Full original-game layout/timing/audio fidelity is still pending.

Capture gate remains incomplete on that existing artifact: its runner correctly failed because the last `home-progress.png` asynchronous write was cut off by process exit. Source-only corrections now persist capture pixels synchronously and turn the pig mesh toward the camera (original face/eye UV geometry faces +Z, camera is at -Z). They have not been rebuilt/retested in a player because the owner said no more builds. Puzzle rule code is unchanged; review the updated presentation in Editor.

Windows compatibility: Unity 6000.4.12f1 native cleanup faults were traced with matching symbols to PlatformAccessibilityManager teardown. On this exact Windows player revision only, the app flushes its synchronous progress save, then terminates its own process through Windows. This verified path bypasses Unity shutdown callbacks; review/remove it when upgrading Editor or adding shutdown-dependent services. No SDK DLL patch, module exclusion, accessibility override or global OS setting remains. Android and Editor use normal Unity lifecycle.

Android: build was explicitly cancelled by the owner. No successful APK/device/store claim. No further build jobs should be launched without a new request.

Input audit 2026-10-24: the owner correctly flagged insufficient detail compared with the Oldcraft build-kit. Added TASK, BEHAVIOR_MATRIX, STYLE_BIBLE, refs/manifest and asset-roles, TODO, KICKOFF and WORKFLOW_AUDIT.vi. These are working specifications, not retrospective owner approval or new game evidence. Seven levels remain an agent-chosen checkpoint, not agreed full-clone scope. C01–C16 now separate requirements from evidence and gaps.

Source inventory: `Evidence/source-feature-profile.json` matches3117 decoded definitions across 22 funnels; main campaign 2600 has 12 special-feature groups. `FEATURE_MAP.vi.md` records counts, samples and expansion order. Three selected embedded demo clips were inspected through frame sheets; they support limited visual/state observations, not same-level live runtime equivalence.

Next: close C04 loss/retry UI coverage and C10/C11 runtime listening/normal-speed feel review; obtain suitable original runtime references for B01/B03–B07/B11 when available. Keep unknowns explicit. Then pilot SurpriseShooters at source level 8 and ConnectedShooters at 14 from their contracts, without stripping special fields or importing all levels blindly. Use `Tools/run-unity.ps1 -Action Tests` for 20 non-graphics cases and `-Action Review` for 2 GPU Editor fixtures, sequentially with no interactive project instance open. Do not build. Advanced mechanics, exact fidelity, physical touch/device performance and owner acceptance remain pending.

Evidence and full iteration history: `Evidence/iteration-notes.md`, `Evidence/native-exit-diagnosis.json`, `Evidence/asset-intake.json`, `Evidence/project-profile.json`. No background build is intended to remain running.
