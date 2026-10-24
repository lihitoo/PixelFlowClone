# Foundation decision

Use the Dreamy template as a project starting point at commit `f6486d0e8fa7e877360d7b775c96ad419474fe0f`. The machine has the matching Unity 6000.4.12f1 Editor, Windows player support and Android support. Existing sibling projects contain user edits and were left intact; this is a fresh upstream clone.

The template provides working package declarations, renderer assets, input/UI infrastructure and assembly conventions. Its first batch import/compile completed with exit 0. Inherited Git dependencies were pinned to their actual lock revisions rather than resolving moving branches during this clone. Package versions/source fingerprints are in `Evidence/project-profile.json`.

## Current runtime composition (owner correction,2026-10-24)

Preserve the template's two-scene startup: BootstrapScene is Init, GameInstaller is the single persistent root, GameInit waits for Ready and SceneLoader presents MainScene. Scene asset GUIDs are retained; build settings and Editor Play entry begin at Bootstrap. MainScene reuses the template Canvas/PanelManager/layers and owns the PixelFlow controller. The old PixelFlow.unity is historical migration input, excluded from the runtime entry list.

PixelFlowInstallerProfile is an optional hook on the existing installer. DataConfig loads the campaign/catalog; Datasave preserves existing keys/schema/codec/scope. ProgressStore is the one atomic game-state authority and PixelFlowResourceWallet exposes it through Economy contracts. No second wallet is created. A later-scene controller explicitly binds/releases to the persistent profile instead of a cross-scene serialized reference.

Shop is a native-art ShopPanel variant with nested ShopOfferItem variants, imported CoinHolder, package ShopPresenter/feature installer and stable Addressables key. UITweenPlayer/UITweenFade drive the root CanvasGroup. Native sections/restore/header and atomic receipt delivery are narrow game adapters. Fake IAP is explicitly authorized until the separate SDK integration.

Dreamy Audio profile/library/service replace the former custom AudioSource pool. Original clips remain game-authored dependencies. GameInit's sample music is disabled for this profile; PixelFlow selects its own cue. Main consumes services after readiness and never starts a second foundation installer.

Home/HUD/common popups and native settings presentation still contain procedural game views; their further migration and parity checks are tracked in [reuse plan](Architecture/DREAMY_REUSE_PLAN.md). Package presence is not treated as completed reuse. Editor tests/rendering/device/SDK readiness remain separate gates. Do not describe this migration as release acceptance.

## Compatibility and limits

The toolkit registry snapshot was read before API use. The fresh template includes newer/different revisions than that historical registry. Used signatures were checked in the actual resolved package source and recorded with hashes; project tests/build evidence establish only this project's tested scope. The global toolkit compatibility registry is unchanged.

Original source records establish level geometry, queues/ammo/capacities and palette IDs. The 34-color order was recovered by inspecting the `Main Pixel Materials` serialized reference list (34 references) and matching `PixelMaterial INDEX - NAME` objects; RGB comes from the corresponding exported material's `_BaseColor`. Mesh `Cube.011` is referenced by the original Shooter Visual prefab. No dummy scripts or dummy shaders were imported.

Belt timing, shot cadence, target occlusion/tie breaks, camera, layout and transitions are rebuilt configurable approximations. Store screenshots are reference-only. Exact visual/gameplay equivalence and all late-game mechanics are unestablished.

## Verification route

Completed pure/session and source-campaign EditMode tests (20/20), an existing non-development Windows player with explicit opt-in verification define, and 12 scenario checks from normal Home entry. The smoke queues Input System pointer events and uses an isolated save scope. The final artifact's last asynchronous capture was lost at exit; source capture code was corrected without rebuilding, following the owner's no-more-builds instruction. Current evidence/gate distinctions are in CURRENT_STATE.md.

Native Windows cleanup in this exact Editor revision faults in PlatformAccessibilityManager teardown. The clone's Windows 6000.4.12f1 compatibility path saves owned progress synchronously, then terminates only its own process. This passed the normal window-close probe and scenario process-exit check. It bypasses Unity/SDK shutdown callbacks; review this choice before adding shutdown-dependent services or changing Editor. Other platforms and Editor retain normal lifecycle. SDK binaries and OS accessibility settings are unchanged.

Audio listening, human feel review, exact fidelity and Android/device acceptance remain separate unverified gates. The Android build was cancelled by the owner; no further build should be started without a new request.
