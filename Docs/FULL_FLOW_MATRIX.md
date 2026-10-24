# Offline gameplay and UI coverage

Updated 2026-10-24 for the owner's request to implement full gameplay/UI flows. This supersedes the seven-level implementation checkpoint, but does not declare exact equivalence to the original native game. No player build is authorized or produced in this iteration.

## Implemented contract

| Player flow | Implementation | Verification |
|---|---|---|
| Boot / load / retry error | `PixelFlowApp.InitializeAsync`, DataConfig and Datasave; error Retry preserves existing profile | Actual scene bootstrap in each Editor fixture; future-schema rejection in ProgressFlowTests |
| Fresh start / onboarding / Help | Four short steps, explicit Back/Next/Done, persisted basics marker; a first-seen guide per source feature | Pointer onboarding in scene fixtures; advanced source feature guides |
| Home / choose / locked level / paging | 2,600 catalog entries; 20 reused buttons/page; completed/locked/difficulty labels; one decoded chunk cache | All entries validated; locked/paged selection and advanced level selection through UI |
| Start another level with a saved run | Explicit replacement confirmation; no overwrite on cancel; checkpoint before board replacement | Advanced scene fixture starts multiple levels with saved runs |
| Queue / linked launch / belt / shooting / return | Pure `PuzzleSession`; atomic connected/chain group; color and line of sight; health/footprints; waiting/cages/doors | Original seven source solves, targeted rule fixtures, source1 pointer solve, capacity/double-click/modal tests |
| Special source mechanics | `BoardMechanics`, `LevelDefinition`, importer; typed mutable object state kept outside config | All 2,600 load; 33 representative snapshot round trips; specific family event/layer tests |
| Tray / Shuffle | Real capacity increase capped at8; eligible queue permutation preserving special constraints | Rule tests and pointer Tray use; failed save restores old capacity |
| Hand / Super / cancel | Paged eligible queue targets or visible colors; actual launch/clear; consume only after effect and successful save | Rule effects and pointer apply/cancel/recovery routes |
| Unicorn | Activation at10 wins, hit charge100, waiting pigs' ammo/colors; no item purchase; loss resets activation | Pure rule waiting-ammo/charge test; profile reward/loss ownership. Normal device interaction not measured |
| Pause / Resume / Back | Freeze model; modal blocks world; Escape follows current screen | Pointer pause blocks world; resume and button navigation |
| Restart | New run ID, pristine queue/board/capacity/revive state; free | Same-frame retirement plus pointer Retry after pause/loss/continue |
| Home / Continue / autosave / app pause | Snapshot preserves run ID, queues, active/waiting pigs, object layers, health, timing, booster capacity | Snapshot round trips; pointer Home/Continue resumes same run; app-pause hook is source inspected, not OS lifecycle tested |
| Win / reward / Next | First-clear and replay rewards, run-ID dedupe, next unlock, terminal persistence before navigation | Source1 pointer win/reload; profile idempotency; explicit one-level campaign-end fixture |
| Loss / coin continue / retry | Reason, up to2 revives, max8 waiting slots; coin continue atomic; free Retry | Timer loss/revive rule and pointer scene flow. Timer trigger is an explicit fixture, not a measured source timer |
| Shop | Earned coins buy four booster types; insufficient balance/full inventory rejected | Copy-on-write failure tests, pointer purchase and inventory/coin assertions |
| Daily | Persisted UTC-day claim, streak cycle; repeated claim/clock rollback rejected | Store idempotency tests and pointer claim/disabled claimed button |
| Settings | Sound/music/haptics/volume persisted; SFX and looping music owners separated | Pointer toggles/reload and SFX cleanup; hearing/device vibration not verified |
| Save failure / retry / cancellation | Model snapshot rollback plus unchanged published wallet/inventory; Retry repeats action; cancel returns to the appropriate page | Failing service injected into an isolated Editor fixture; normal production controller/UI executes |
| Incompatible saved run | Discard only current run after explicit confirmation, preserve wallet/completions; retry bootstrap for profile errors | Source path inspected; corrupt snapshot and future schema tests. Not every filesystem failure can be reproduced by fixtures |

## Source mechanics and approximations

All populated main-funnel fields are normalized with source hashes. Shooter families: Surprise, Connected, Locks, Pipes, Hammers, Ice, Chains, Totems, Mallets, SlotCages, Astronauts, ConveyorDoors. Hard/hidden pixels retain health, dimensions and reveal state. The 24 populated board kinds have typed footprints, health/layers, counters and release state. Goal layers are distinguished from protective overlays so their ammunition is not counted twice.

Layer targets include gate/pipe/snake/eggs/pumpkin/beans/crossbow/colored cage/doll; walls occlude; keys and uncolored cages expose-trigger; ice/biscuit/split respond to hits; colored shields/curtains/accordions/mixers respond to matching hits; wood/musical segments use support nodes; UFOs use finished-pig seat colors; spaceships use astronaut launch. These contracts are described to players in `FeatureGuide`, with source-derived type/order/health and inferred event semantics. Crossbows currently use colored target health: no claim of recovered native projectile behavior. Native direction-specific animations, exact shrink geometry, support timing and puzzle balancing still require source-runtime comparison.

The importer preserves 517 side-funnel definitions outside the main catalog. It does not turn them into invented campaign continuations. No accounts, ads SDK, real-money purchase, remote event service or leaderboard has been enabled. Shop/daily/progression are local complete loops for the offline implementation.

## Data, resources and ownership

- Static: `Resources/PixelFlow/pixelFlowCampaign.json` indexes2,600;26 compressed chunks,100 levels each. Seven early definitions stay inline for compatibility. Decoded chunk stream is bounded32MB, only one decoded chunk cached.
- Rules: Unity-independent assembly; a session owns pixels/HP, blockers, queues, stored pigs, links/counters, timer and snapshots. No Editor references.
- Save: stable key `pixel-flow-progress`, schema2; legacy unlocks/completions/sound preserved; wallet/inventory/settings/active run use save-before-publish copy-on-write. A win reward and booster transaction cannot be silently split by a failed disk write.
- Art/audio: original selected assets plus procedural geometry/UI;8 extra selected derivatives total2,340,478 bytes, including two streamed OGG tracks. Receipts: `Evidence/asset-intake.json`, `Evidence/flow-asset-intake.json`, `Evidence/campaign-import.json`. Original export and XAPK untouched.
- Input/rendering: uGUI and Input System; no business mutation in widgets. Model freeze follows app screen. Board disposal disables old colliders immediately before deferred destruction.

## Evidence boundary

Use CURRENT_STATE for the latest passing XML and capture folder. Editor graphics fixtures pump synthetic mouse events and render URP offscreen previews with isolated saves. Some fixtures explicitly inject a failing save service, alter the timer, grant test-only unlocks or use a one-level catalog; their receipts name those setup steps. They do not claim physical input, real filesystem outage, completed original level2600, full campaign solvability, original runtime equivalence, mobile frame rate or owner acceptance.
