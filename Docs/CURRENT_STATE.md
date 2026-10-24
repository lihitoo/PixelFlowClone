# Current state

## Active architecture migration ? 2026-10-24

Owner requires template startup and Foundation reuse before custom infrastructure. Current entry is template `BootstrapScene` (Init) ? GameInstaller.Ready ? GameInit ? SceneLoader/loading ? `MainScene`; scene assets/GUIDs retained. Main holds PixelFlow controller and template UI layers, with sample HomeBootstrap disabled. Persistent root uses PixelFlowInstallerProfile; controller binds/releases after scene load, no serialized cross-scene controller reference.

Shop now has authored ShopPanel/ShopOfferItem/CoinHolder prefab variants, Addressables key, UI tween and a wrapper around the package presenter. Progress remains the single save authority, exposed through an Economy wallet adapter. Audio uses Dreamy profile/library/service. The focused Shop regression passes through template startup/loading/Main and fake purchase success/cancel/failure/pending/repeat ([XML](Architecture/editor-shop-startup-pass.xml)). Full architecture regressions are running; native parity and release are **not accepted yet**. Earlier checkpoints below describe historical state. Follow [reuse plan](Architecture/DREAMY_REUSE_PLAN.md). Toolkit npm checks pass80/80 after the startup/reuse skill update. No player build; real IAP remains deferred behind the authorized fake gateway.

## Latest checkpoint — original-asset Home, 2026-10-24

The active Home task now replaces the invented logo/Hero/menu grid with the source-art level path, current/difficulty nodes, Play, gear, coin/lives header, Start/Shop/rank tabs and gated event/offer docks. See [Home brief](ExactClone/Home/TASK.vi.md) and [source/Game View comparison](ExactClone/Home/review.html). This is an implementation checkpoint, **not acceptance of identical0.36.3 Home or a complete whole-game clone**.

Imported47 selected original PNGs,2,648,170bytes, with native sprite borders; [per-asset receipt](ExactClone/Home/home-asset-intake.json) links asset IDs/source URIs/hashes to project outputs and imported GUIDs. Source extraction is untouched. Home includes persistent lives/regeneration/refill, separate audio sliders and haptic/notification-preference/smooth-motion toggles. Remote rank/pass/events/No Ads entry/error/Retry/close routes do not fabricate scores, purchases or rewards. Their actual providers and full native content remain missing; Shop still uses the prior coin-for-booster implementation.

Dreamy DataConfig/Datasave remain integrated; Dreamy UI now owns safe-area calculation. Inspected actual manifest/lock/package source. Owner authorizes discovering/importing additional packages from Dreamy-Game-Foundation when needed; inspected22 organization repos, but Home needs no additional package yet. Existing UI/Audio/Assets/Core/Economy/Settings/Shop pins stay in place. [Reuse decisions and remaining integration](ExactClone/Home/DREAMY_REUSE.vi.md) distinguish installed packages from actual consumers; PixelFlowApp remains the composition root and ProgressStore the single transactional progress owner.

Verification: [Unity XML](ExactClone/Home/editor-tests-final.xml) **137/137 passed**:96 PixelFlow rule/data/save tests,7 real-scene Editor UI routes,34 template tests. New checks cover life regeneration/cap/unlimited/migration/atomic refill/audio failure; pointer Home/lives/refill/rank locks/unavailable Retry/docks/tabs/settings; existing gameplay, resume, boosters, save failure and campaign-end regressions. Explicit test-only unlock/coin/timer/selection fixtures are labeled in receipts. [Summary](ExactClone/Home/test-summary.json). The MCP wrapper lost its job association after domain reload; the completed Unity XML and route receipts are the result authority, not that orphaned wrapper status. Earlier failed XMLs are retained separately.

Actual visible [Game View Home36](ExactClone/Home/game-view-visible-Home-36.png) and [capture receipt](ExactClone/Home/game-view-visible-Home-36.json) are separate from [offscreen URP previews](Evidence/editor-review/home-20261024-090539/captures.json) at720x1280/1080x1920/1080x2340. Visible desktop capture is682x1212 at approximately0.95 Game View display scale, with isolated level36/2000coin fixture; it is not a native device screenshot. The end-of-frame capture coroutine did not finish in this Editor configuration, so that temporary capture helper was removed and the actual visible Game View was captured instead.

After removing that unused Editor-only capture helper, compilation finished, [Console errors0](ExactClone/Home/console-final.json), [scene clean/rootCount3](ExactClone/Home/scene-final.json), and [Editor stopped/idle](ExactClone/Home/editor-final.json). Unity remains open; no new instance or player build was launched. Static verification checks66 selected source assets and Runtime/Editor boundaries; [report](ExactClone/Home/static-validation.json).

Still pending: exact target-version runtime screenshots, full native loading/shop/settings/rank/pass/event/account functions, source unlock/heart tuning and depletion policy, full map scrolling/endgame behavior, and matching typography/spacing/pattern/dock appearance. Notification toggle currently persists a preference; delivery is not integrated. Do not equate local client routing with native service parity. The full-game scope below remains required.

Next: close the recorded source/version/tuning gaps and finish these native surfaces using the same brief → source-state capture → asset binding → implementation → pointer checks → comparison loop. Do not promote this Home iteration as a finished exact clone or build a player without a renewed request.

## Latest owner steering — exact whole-game preparation

Owner corrected the target after the offline implementation checkpoint: an identical complete game needs full brief and public gameplay/non-core function research before implementation, original screenshots for comparison, JSON links to every extracted asset, suitable source-asset integration and notification before an interactive Unity/MCP switch. Do not continue using the old offline scope as a reason to omit loading/Home/shop/rank/settings/meta/services.

Applied to this game before toolkit promotion: [ExactClone/BRIEF.vi.md](ExactClone/BRIEF.vi.md), [65 surface/state contracts](ExactClone/screen-contracts.json), [interactive review](ExactClone/review.html), [8 source/clone gap comparisons](ExactClone/comparisons.json), [per-file asset catalog](AssetCatalog/manifest.json). Metadata-only catalog links24,588 exported/Unity-project files and77,970 serialized source objects, with17,480 GUID entries. The reusable generic helper also passed on6,758 files under the plain exports folder. Heavy asset bodies were not read to build those indexes.

Research includes53 source records (official help/store, video, author/vendor observations),9 captured web pages, published source images and sequentially decoded video frames. Website captures, marketing images, related-game pictures and actual original-game screenshots are explicitly distinguished. Source0.36.3 UI structure was inspected in23 selected prefab/scene/config files, yielding1,314 named nodes and352 distinct GUID references.10 occurrences refer to2 Unity built-in GUIDs, which resolve through the engine rather than exported files; no other selected GUID is unresolved. No source art/extraction was copied into the toolkit.

**Exact clone is NOT complete; exact-parity brief readiness remains pending for uncovered evidence.** The audit found major existing deviations: invented logo/Hero Home instead of the source map; four coin-for-booster shop rows instead of packs/No Ads/bundles; simplified settings; no loading/lives/rank/pass/event/account flows; inferred advanced rules.44 detailed surface records lack suitable original state screenshots, and captured web/video versions are not automatically0.36.3. These are retained tasks/dependencies, not scope exclusions. See [preparation validation](ExactClone/validation.json); integrity pass is separate from full-clone completion.

This preparation did not change runtime C# or run/build Unity. Earlier91/6 results below remain scoped to the offline contract/revision. No Unity MCP tool is exposed in this session; no interactive Editor was opened. Before actual Game View/binding work needs interactive Unity/MCP, notify the owner with reason, project/scene/connection state and planned actions. Continue honoring **no player builds**.

Next: close source-version/settings/rank/pass/failure/lives evidence gaps, then implement faithful loading/Home/settings shell and full remaining functions using the existing assets according to the new brief. Read ExactClone/BRIEF before more feature code. The offline checkpoint below is historical implementation/evidence, not acceptance of an identical whole-game clone.

## Prior expanded offline checkpoint

Stage: expanded offline gameplay/UI implemented; final source/Editor validation passed. No new player builds. Interactive Unity stays closed; all owned batch Editor runs have exited.

Project: D:/Game/PixelFlowClone. Template baseline f6486d0e8fa7e877360d7b775c96ad419474fe0f. Unity6000.4.12f1 at C:/Program Files/Unity/Hub/Editor/6000.4.12f1/Editor/Unity.exe. Package pins preserved; Runtime has no Editor dependency. This iteration changes the game project, not the sibling toolkit.

## Implemented now

- Main campaign2,600 source definitions with34-color palette,26 compressed chunks and a paged20-button selector. One decoded chunk cache, bounded32MB decompression. The7 early inline levels remain compatible; they are no longer the entire scope.
- All12 populated shooter feature families,24 board-object kinds, hard/multi-cell pixel health and hidden/reveal state. Typed health/layers, support nodes, links/chain launch, keys/locks, pipe payloads, cage release, frozen pigs, totems, mallets, sealed waiting slots, UFO seats, astronauts/spaceships and conveyor doors. Configuration stays separate from mutable session state.
- Home;4-step onboarding/Help; first-seen feature guides; locked/completed/paged levels; gameplay HUD;5 boosters and target/cancel; pause/resume; free retry; win/next/campaign end; loss/coin continue; local coin shop; UTC daily gift; sound/music/haptics/volume settings; replacement confirmation and save-error Retry/cancel.
- Save schema2 keeps legacy unlock/completed/sound. Profile owns coins, booster inventory, streak, tutorials and active-run snapshot. Home/Continue restores the same run. Checkpoints run every5 seconds and on pause/Home/app-pause/quit. Copy-on-write save-before-publish plus model rollback prevents failed booster/continue writes from spending items or coins. Result reward is committed after a successful booster transaction.
- Original selected art/SFX plus8 selected UI/audio derivatives (2,340,478 bytes), including two streamed OGG music tracks. Procedural board/obstacle geometry exposes HP, layer, hidden, frozen, support, link, slot and door states. Fixed overlapping cage captions after viewing actual advanced-level previews.

Contracts and code-to-test map: [FULL_GAMEPLAY_PLAN.md](FULL_GAMEPLAY_PLAN.md), [FULL_FLOW_MATRIX.md](FULL_FLOW_MATRIX.md). Source intake: [campaign-import.json](Evidence/campaign-import.json), [flow-asset-intake.json](Evidence/flow-asset-intake.json).

## Verification

Latest rule/data/save run: [editmode-tests-20261024-130727.xml](Evidence/editmode-tests-20261024-130727.xml), **91/91 passed**. Includes all2,600 definitions loaded/validated,33 representative state round trips, original1–7 and source8/14 solved through normal launch rules, family-specific layer/event conditions, boosters/revive, invalid snapshot/result rejection, legacy migration and save transaction/idempotency.

Latest graphics run: [editmode-review-20261024-131418.xml](Evidence/editmode-review-20261024-131418.xml), **6/6 passed** after snapshot consistency, campaign-count and support-label sizing changes. The6 fixtures cover:
1. Home/onboarding/game/pause previews at3 portrait aspects.
2. Pointer capacity/double-click/pause blocking; same-frame board retirement; source1 win/reload; source2 retry; repeated Home/Play.
3. Shop/daily/settings/locked paging; Tray/Hand/Super apply/cancel; saved-run resume; timer-triggered loss, paid continue and free retry.
4. Source14,230,600,1001,1401,1701,2001,2401 selection/tutorial/render/pause/Home.
5. Injected disk failure rolls back Tray/Super; pointer Retry commits once; cancel returns through target picker.
6. Explicit one-level campaign fixture records reward once and routes the final Next to Home.

Captures and route receipts: [review-20261024-131418](Evidence/editor-review/review-20261024-131418). Opened Home/game/shop/loss/advanced-cage/mixer/save-error previews in the earlier passing130037 run; cage labels corrected and re-opened. Final music/mallet and campaign-end previews were also opened; mallet labels were bounded to avoid queue overlap. All26 final PNGs and6 route receipts are indexed in Evidence/full-gameplay-validation.json. Older failing receipts are preserved. Static verification: [full-gameplay-static-20261024.json](Evidence/full-gameplay-static-20261024.json), pass with19 selected assets and all26 chunks verified; source fingerprint e14a30446f6780e0f3b6df94fbe47b94586cd847f73c52591bca729770557646.

Editor fixtures use isolated saves, queued synthetic mouse events, explicit InputSystem/EventSystem pumping and URP offscreen rendering. Some explicitly alter the timer, unlock levels, inject a save failure or use a one-level catalog; these setup steps are labeled in each receipt. They are not physical input, normal-player scheduling, device audio/performance, original-runtime equivalence or a solved original level2600.

## Remaining evidence and boundaries

- The source supplies identities/order/colors/health/footprints, not native IL2CPP logic. Event rules for advanced blockers/layers/support/shrink are explicit inferred contracts. Crossbow is currently a colored-health board target, without a recovered native firing behavior. Native animations, direction-sensitive details, cadence, loss grace and full original puzzle balancing still need same-state runtime comparison.
- All2,600 load correctly; that is not a claim that every one has been solved under these contracts.517 side-funnel definitions remain outside the main campaign.
- No ads SDK, real-money billing, account, leaderboard or remote event service. Shop/daily/progression are complete local offline flows.
- Touch, cutout safe area, OS background/kill/relaunch, listening/mix/vibration, thermal/memory/frame time and owner visual acceptance remain unverified on devices.
- No new Windows/Android artifact. Old artifacts predate this source and are historical. Android was cancelled. The exact-version Windows6000.4.12f1 native-exit workaround is retained as previously verified debt, not revalidated or expanded here.
- Next useful work is original-runtime comparison and normal-speed/device review when that scope is available. Do not restart builds or reduce the scope back to7 levels.

Commands: Tools/run-unity.ps1 -Action Tests; then -Action Review, sequentially with no interactive project instance. Tools/verify_project.py checks19 selected asset identities,26 chunk identities and Runtime/Editor boundaries. Do not use Prepare/Windows/Android for ordinary continuation.

Prior checkpoint is archived at [current-state-pre-full-gameplay.md](Evidence/current-state-pre-full-gameplay.md). Baseline owned text sources were preserved before this iteration in Evidence/full-gameplay-source-baseline.zip. Existing template/user changes and source extraction were preserved; no commit/push.
