# Pixel Flow Unity clone

Latest owner correction supersedes the offline-only checkpoint: prepare a complete identical-game brief and web/runtime-screen coverage first; include loading/Home/shop/rank/settings and every applicable function, catalog each extracted asset, finish flows using suitable source assets, and notify before interactive Unity/MCP. Canonical expanded input: [ExactClone/BRIEF.vi.md](ExactClone/BRIEF.vi.md), [screen contracts](ExactClone/screen-contracts.json), [review](ExactClone/review.html), [asset catalog](AssetCatalog/manifest.json). Whole-game parity is not yet achieved; no-build remains in force.

Owner request: create a new Unity clone of the supplied Pixel Flow 0.36.3 game under D:/Game, using game-vibe-workflow and evaluating Dreamy template/packages.

Latest owner steering: implement full gameplay, UI and functional flows; no further player builds. The earlier Android build was stopped. Current expanded scope and verification map: [FULL_GAMEPLAY_PLAN.md](FULL_GAMEPLAY_PLAN.md), [FULL_FLOW_MATRIX.md](FULL_FLOW_MATRIX.md). Continue source/Editor work and preserve honest not-run gates.

## Detailed input pack — revised 2026-10-24

The owner flagged that the initial input was thinner than the Oldcraft build-kit. The initial seven-level slice was an agent assumption, not owner acceptance of full clone scope. The following records now expand this brief; they document proposed targets and unknowns without retroactively approving the implementation:

- [TASK.md](TASK.md): player journey, screens/input, state edge cases, data scope and C01–C16 acceptance criteria.
- [BEHAVIOR_MATRIX.md](BEHAVIOR_MATRIX.md): source observations, implementation assumptions and measurement plans B01–B13.
- [STYLE_BIBLE.md](STYLE_BIBLE.md): presentation contracts, controlled capture states and asset routes.
- [FEATURE_MAP.vi.md](FEATURE_MAP.vi.md): observed source-feature inventory and a staged expansion roadmap beyond the seven-level checkpoint.
- [refs/manifest.json](refs/manifest.json) and [refs/asset-roles.json](refs/asset-roles.json): reference roles, approval limits, runtime asset roles and provenance.
- [TODO.md](TODO.md), [KICKOFF.md](KICKOFF.md) and [WORKFLOW_AUDIT.vi.md](WORKFLOW_AUDIT.vi.md): priorities, resume/review prompts and application gaps. CURRENT_STATE remains the sole current-state record.

## Working scope (implementation assumptions)

- Portrait, single-player, offline playable clone of the core puzzle loop; mouse and touch.
- Source: owner-selected XAPK and the existing validated extraction. Preserve both.
- Reuse selected original level data, art and audio locally; rebuild gameplay code. Dummy AssetRipper classes/shaders are not gameplay implementation.
- P0: normal menu launch, choose an unlocked level, front-of-column shooter selection, limited conveyor capacity, automatic color-matched hits, ammo depletion, five waiting slots and relaunch, blocked-capacity feedback, win, loss, restart, next level, local progress and sound settings.
- Historical plan: prove one source level through a Unity scene and early Windows player before expanding. Latest owner constraint supersedes early/final build gates: no further player builds, including Android; continue source/Editor evidence only. No store publication.
- The expanded offline scope includes all2,600 main-campaign definitions, typed special mechanics, tutorials, saved-run resume, boosters, local coin shop, daily rewards and settings. No ads SDK, real-money purchases, remote live events, leaderboard or account service. Data loading and fixture coverage do not certify that every original puzzle is solvable under inferred native rules; keep the advanced fidelity/balancing gate explicit.
- REA mode: auto. Use existing source/data and official descriptions first. No REA setup unless a concrete P0 behavior question needs it.
- No paid generation needed; the selected extracted assets and procedural presentation are the starting route.

## Observations and unknowns

Observed: source level fields carry SlotCount, ConveyorLimit, queue shooters/ammo/material and pixel coordinates/material. Official developer help describes front-queue launch, same-color hits, ammo exhaustion and returning shooters. The store description identifies five waiting slots.

Unknown/estimated until measured: exact belt speed, projectile cadence/range, targeting/occlusion tie-breakers, camera and animation timing, original loss grace period. Keep estimates configurable; do not claim exact behavioral equivalence.

## Template decision

Use Dreamy template cloned from upstream commit f6486d0e8fa7e877360d7b775c96ad419474fe0f. The installed Editor is 6000.4.12f1, matching the template. Preserve original template content and history; put clone code under Assets/_Project/PixelFlow. Pin inherited Git dependencies to the template lock hashes before import.

Evaluate actual resolved package APIs before integrating them. Static levels are designer data, runtime puzzle state belongs to the puzzle session, persistent progress belongs to the save owner, and UI/feedback must not own puzzle authority. Keep Runtime/Editor/test assemblies separate.

## Evidence and gates

M0 includes the detailed input pack and unresolved fidelity questions, as well as template/package inspection. M1: deterministic puzzle rules plus input/reset. M2: representative art/level integration, using Editor while builds are stopped. M3: supported P0 batches. M4: functional, visual, motion/feel and audio review with relevant evidence. M5: source/Editor checkpoint now; final player gate is deferred-by-owner, not passed by older builds.

Owner acceptance remains pending. Automated scenario verification is not human playtesting or exact clone-fidelity acceptance.

Sources: https://loomgames.helpshift.com/hc/en/3-pixel-flow/faq/9-new-to-pixel-flow-learn-how-to-play-in-minutes/ and https://play.google.com/store/apps/details?id=com.loomgames.pixelflow .
