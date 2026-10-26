# Current state

## Results / direct target client checkpoint verified — 2026-10-26

[Review](ExactClone/Results/review.html) and [validation](ExactClone/Results/validation.json) are finalized. Win/fail/continue/Hand/Super use authored GameplayPanel overlays and source art/video/fonts. Continue handles save rollback/Retry and Mini Shop return to the same failed run; result close abandons the committed failed snapshot without another life charge. Win reopening grants nothing. Hold-to-preview and video texture cleanup are verified within their recorded scope. Rewarded ×2 remains unavailable.

144 distinct cases have latest passing results: full run141/144 plus the3 failed cases rerun3/3 after fixture wait/target-input repairs. This is aggregate coverage, not a single144/144 run; [case ledger](ExactClone/Results/coverage.json) and earlier failures/interruptions remain available. Static126 imports pass, final prefab audit has one SafeArea/no missing scripts/no zero-border Sliced, and12 actual Game View images cover four states at three aspects. FoundationDemo's retained MainScene instance is inactive; source prefab/scene GUIDs stay intact. Duplicate-named blue/purple inset choice was corrected with the import GUID preserved.

MCP recovered successfully. Final Editor: stopped on clean BootstrapScene, compile idle, Console0errors, isolated save/review scope restored, Play entry BootstrapScene and build order Bootstrap→Main. [Final state](ExactClone/Results/editor-final-state.json). Toolkit game-vibe/extract references updated;80/80 tests and final skill/context validation pass. No player builds.

Next whole-game work remains tutorial/unlock/native timing and advanced target layouts, then rank/pass/events/account with their appropriate source art and real provider seams. Current reward50/5, continue100 and extra-waiting-slot revive remain estimated; fail-offer/streak loss and production ads/backend are incomplete. This checkpoint does not accept an identical finished whole-game clone.

## Final regression repair — 2026-10-26

Full Results regression `editor-verified.xml` completed141/144. Failures: Mini Shop→Lives refill fixture waited before the return callback opened Lives; SaveFailure fixture still clicked the removed target list; Shop bundle/restore fixture hit its240s timeout without an assertion trace. Original XML retained. Fixture now waits for Shop/Lives transitions together and uses direct world color pick plus Back for target cancellation; economic assertions and timeouts are unchanged.

Active rerun `b96c0289d30e4025b7ff38cb29743c51`, output ExactClone/Results/editor-regression-retry.xml, folder results-regression-retry-20261026, exactly those3 cases. Runtime implementation and12 Game View images are unchanged by these fixture fixes. After completion, save honest aggregate coverage, normalize Unity blank-scalar whitespace, refresh static report and final Editor state. No player builds.

## Result/Game View final regression — 2026-10-26

[Latest comparison](ExactClone/Results/review.html) now contains12 actual Game View captures (Hand/Super/fail/win ×1080x1920/1080x2340/720x1280). Win/fail captures use explicitly labeled presentation fixtures; transactional behavior is separate. Duplicate-named blue/purple sprite selection was corrected without changing the imported GUID. The retained FoundationDemo root instance in MainScene is now inactive, removing its visible ToggleButton; its source prefab and scene GUID remain untouched. The existing scene-authoring command retains that local integration patch.

Recovery checks [4/4 pass](ExactClone/Results/editor-recovery.xml). Active full regression `58519a665261466a9259fcbc6c3f29a9`, output `ExactClone/Results/editor-verified.xml`, folder `results-verified-20261026`. This supersedes interrupted jobs; do not claim the full run until XML completes. Static126 imports pass. Native costs/revive behavior, fail offer and production providers remain gaps. No player builds.

## MCP recovered; result validation resumed — 2026-10-26

MCP is connected again to PixelFlowClone@fdf023ebb2b9f1bc, Editor PID5904. The previous full run `fa0c4eba2d20434fa0a9e1c20b5a0efd` was interrupted when the old Editor ended; editor-final.xml was never written. Partial receipts in results-final-20261026 are retained and do not establish an overall pass. No temporary Editor validation runner was created.

BootstrapScene is clean, compile idle and Console0errors on reconnection. Fixed result close to abandon the failed snapshot and show Home within one guarded transaction (avoids nesting a second Guard). Shop return now waits for actual Hide completion. Native video aspect and fail pattern have been corrected in the same prefab. Active focused recovery job `f83ebed944eb4dd489f3c614a90ba038`, output ExactClone/Results/editor-recovery.xml;4 cases expected (2 terminal UI flows plus2 blocking-layer rule cases). Finish it, then recapture final Game View and rerun affected regression. No player builds.

## Result / direct target migration in progress — 2026-10-26

Continuing [Results plan](ExactClone/Results/PLAN.vi.md). GameplayPanel prefab now contains authored win/fail/target content and uses8 additional selected original files, including out_of_space_offer video. Legacy result constructor and target list were removed; Hand selects queue pigs in world, Super selects board pixels/object cells through PhysicsRaycaster and the existing transactional model. Coin continue now opens Mini Shop when insufficient and returns to the same failed attempt. Hold-to-preview is authored; rewarded win remains unavailable. Existing estimated prices/rewards/revive semantics are not certified against0.36.3.

Initial actual Game View baseline captures are in ExactClone/Results, with view-only result setup explicitly distinguished from functional terminal tests. Corrections to target spacing and plain win body are saved; fresh captures and validation are pending. Focused3-test run `dabf32b79d2a416bac365ef2e93fc94a`, output `ExactClone/Results/editor-focused.xml`, evidence folder `results-focused-20261026`. Static126 selected imports pass. Editor compilation has no errors. Task-created interrupted REA cache was moved intact fromC toD; no cache data deleted. C now has enough space to run review. No player builds.

## Gameplay/reference checkpoint and REA continuation - 2026-10-26

Final Unity regression [140/140](ExactClone/Gameplay/editor-final.xml) passed; static verification passes118 imported files and Runtime/Editor boundaries. [Gameplay/Lives comparison](ExactClone/Gameplay/review.html) links the source frames and isolated URP previews at1080x1920,1080x2340,720x1280. Four-booster HUD, direct Settings, wallet Mini Shop return and authored booster purchase with save failure/Retry are implemented. Lives purple/orange layout is now authored. Earlier failed runs are retained in validation-history.json.

[Short whole-game completion plan](ExactClone/COMPLETION_PLAN.vi.md): result/continue/target popups, tutorial/booster parity, rank/pass/events/account, then full-journey comparison. Whole-game parity is not complete. New reference coverage remains version-scoped.

[REA static investigation](ExactClone/Research/REA20261026/RESULT.vi.md) ran CLI6.8.0 on Node24.22.1 against original0.36.3 metadata31:388images/33,230types/303,157methods, with complete Evidence and selected gameplay members. Hand/Shuffle and fail-offer/Mini-Shop coordinator names are observed; native bodies/config and live semantics remain unknown. No MCP registration/global tool changes or player builds.

The additional interactive review failed to write its isolated save because driveC is full. Editor was stopped on clean BootstrapScene, compilation idle and owned review scope restored; final saved-prefab re-audit also could not compile its MCP snippet due the same disk-full temp-path failure. New actual Game View captures/audit remain not-run, not passed. Existing140/140 and URP preview evidence predates these environment failures. Do not claim current Console0errors: the disk-full errors are retained.

Toolkit game-vibe/extract references now retain version-scoped video evidence, UI/model clock separation, wrapper/XML recovery, capture-method boundaries and required nonempty REA Evidence. Toolkit validation and80/80 tests pass; this is workflow validation, not whole-game acceptance.

## Reference expansion implementation record - 2026-10-26

Added [reference gallery](ExactClone/Research/Expansion20261025/review.html):10 source records,10 bounded public-video clips,24 selected runtime frames plus article montages. URLs/dates/timecodes/hashes/evidence roles are retained in its ledger/findings. MGFTUE footer visibly identifies0.32.1(263); no new runtime reference is certified0.36.3. Captures cover Settings/profile/username confirmation, purchase cancel, fail/continue/lives/win and four booster interactions. Source extraction is untouched.

Gameplay HUD now uses an authored BaseFeaturePanel variant (one SafeArea, original art/Lilita) with four boosters, gear -> existing Settings directly and wallet -> Mini Shop -> same run. Procedural HUD, extra back/II/progress prose and standalone Pause renderer were removed. Booster stock purchase now uses an authored popup inside GameplayPanel; existing transaction/rollback service remains authority. Lives is being aligned with newly observed purple/orange source composition, with unavailable rewarded-ad state and no fake reward. [Plan](ExactClone/Gameplay/PLAN.vi.md). Focused gameplay/settings/mini-shop/save checks passed6/6 before the visual Lives alignment; combined regression is running. No player builds, billing/ads SDK or invented online success.

## Shop, Settings and Lives prefab/client checkpoint - 2026-10-25

Shop now has all11 observed products (6Gold,NoAds,NoAdsBundle,Small/Medium/Ultra). Restore was removed from the actual Shop prefab, its events and footer space. Original NoAds/Gold2x assets keep their imported GUIDs. Fake gateway remains the only payment provider. [Shop review](ExactClone/Shop/Final/review.html), [27/27 tests](ExactClone/Shop/Final/editor-tests.xml), [capture/asset receipts](ExactClone/Shop/Final/validation.json). Unlock thresholds and web-reference version matching remain unverified.

Per the owner's onward instruction, Settings is being migrated directly into the existing imported sample SettingsPanel.prefab, preserving script/prefab GUIDs and using the same PanelManager/root DI/ProgressStore. Both procedural Settings constructors were removed. Source-art slider/toggles, conditional Restart/GiveUp confirmation and Settings restore are implemented; focused failure/lifecycle checks passed12/12 and actual Game View captures are recorded at1080x1920,1080x2340,720x1280. [Settings evidence and gaps](ExactClone/Settings/review.html). Lives/refill now uses an authored BaseFeaturePanel variant (inherited SafeArea/tween), root-injected LivesPresenter and the existing progress owner; procedural Lives UI was removed. Combined [regression138/138](ExactClone/Lives/editor-verified.xml) passes; the strengthened already-failed/unlimited abandonment case also [passes1/1](ExactClone/Lives/abandon-loss-verified.xml). Earlier137/138 runs retain Editor input/resolution failures; the fixture now keeps Simulator closed until RunFinished and verifies actual1080x1920 before queuing input. No player builds.

[Lives/Game View review](ExactClone/Lives/review.html) records full/empty/unlimited at1080x1920 and responsive1080x2340/720x1280. Final saved Settings/Lives prefab audit: one SafeArea each, no missing scripts or zero-border Sliced images. Editor stopped on clean BootstrapScene, compilation idle and review save scope restored. Console0errors. [Final state](ExactClone/Lives/editor-final-state.json).

Remaining work: migrate gameplay HUD/pause and shared result/booster/error popups; complete source rank/pass/event/account providers and exact-version screenshot coverage. Settings source actions currently report unavailable providers; notification is a saved preference only. Lives max/interval/refill price remain estimated because exported source config fields are stripped. This is a client checkpoint, not completion of the identical whole-game clone.

## Latest Home/Shop prefab checkpoint - 2026-10-25

Owner's prefab/mobile/template steering is implemented for the two main panels. Home now loads the original template HomePanel prefab through PanelManager. Shop directly uses the imported sample ShopPanel script/prefab and shared ShopOfferItem; the parallel root variant, view adapter/interface and six per-SKU Gold prefabs were removed. GUIDs of the three retained template/sample prefabs are unchanged. ShopScreenPresenter receives dependencies from the existing root and wraps the installed purchase presenter; ProgressStore remains the single wallet/save authority. Template and game Shop address constants now match the actual Addressables entry. See [ownership/local patches](Architecture/HOME_SHOP_PREFABS.vi.md) and [short plan](Architecture/HOME_SHOP_PREFAB_PLAN.vi.md).

UI uses portrait1080x1920, Scale With Screen Size, Match Width Or Height0.5. Home and Shop each own one Dreamy SafeArea container; item prefabs inherit it. Original Lilita One SDF/atlas and a saved outline/shadow preset replace Roboto on Home/Shop. Original sprite bodies were retained; icons/node/Play stay Simple, inspected continuous frames/caps use Sliced, pattern is Tiled. Difficulty pills fit their text through prefab LayoutGroup/ContentSizeFitter. Header/dock edge anchors and tab column anchors repair clipping at long portrait aspects. [Font receipt](Architecture/home-shop-font-intake.json), [saved-prefab audit](Architecture/home-shop-prefab-audit.json).

Verification: [121/121 Unity regression](Architecture/home-shop-verified.xml); after responsive anchor changes, [5/5 affected UI fixtures](Architecture/home-shop-responsive.xml); final difficulty-label authoring [1/1 Home route](Architecture/home-label-verified.xml). Checks cover template startup/Main, Home/settings/lives/tabs, Shop scroll, fake purchase success/cancel/failure/Retry/deferred/repeat, atomic bundle delivery, owned hiding, restore/recovery and mini-shop return, plus existing gameplay/save regressions. Test Runner XML is authoritative: MCP wrappers can become orphaned after EnterPlayMode domain reload. Earlier119/121 and capture-sampling failures are retained; sparse error screens needed denser image sampling, without lowering the nonblank threshold. [Validation summary](Architecture/home-shop-validation-summary.json).

[Source/clone review](Architecture/home-shop-review.html) includes actual Game View RenderTexture readbacks at1080x1920,1080x2340 and720x1280. Captures seed level36 and2000coins only in isolated HomeReview saves; this is not earned progression. GPU row order is flipped on export; production transforms/canvas modes are unchanged. Separate URP offscreen previews remain labeled. Long-aspect Home header/tabs/docks were checked inside the full-rect Editor safe area with one inset owner each. Cutout/device behavior remains unverified.

Toolkit source skills unity-ui, dreamy-ui, unity-ui-reskin and game-vibe-workflow's reuse reference now retain the owner's prefab-first, safe-area, scaler, justified-slicing, extracted-font, baseline-first comparison, template-style/MVP/DI and generic-naming rules. Toolkit npm checks passed80/80. Project AGENTS.md also retains these preferences. The two one-off Python rewrite files and the temporary Editor migration class were removed; existing asset/import/verification tools remain.

Final Editor check: compilation successful, Console0errors, stopped on clean BootstrapScene; Play entry/build order remain Bootstrap then Main. No player build or billing SDK was run/installed. Shared legacy modals and gameplay HUD still use the existing renderer and are not claimed as fully prefab-authored. Exact0.36.3 screen-state/version matching, native unlock/spacing/icon/animation tuning and real rank/pass/event/account/billing services remain explicit whole-game gaps. This is a Home/Shop authoring and client-flow checkpoint, not acceptance of an identical complete clone.

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
