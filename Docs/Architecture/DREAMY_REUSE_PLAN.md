# Dreamy reuse and maintainable Pixel Flow integration

Owner steering,2026-10-24: use existing Dreamy/template/_Feature code and prefabs first; only implement missing game-specific capability after inspecting Foundation. Target is a maintainable, shippable game, not a playable-only clone. Continue the Shop clone and source fidelity requirements. Real IAP belongs to the later SDK; keep the authorized fake behind the gateway. No player builds.

## Baseline and concrete problems

- The PixelFlow scene bypasses template GameInstaller. PixelFlowApp creates its own config/save/audio/UI, and PixelFlowView builds/manages most screens procedurally. Installing packages or using SafeArea/gateway interfaces does not establish reuse of their lifecycle/integration.
- Shop preparation/code currently has25 original sprites,11offers and fake success/cancel/error/deferred/restore/recovery. Baseline151 tests:150pass; `PointerInputResetAndProgressionRoute` fails retired-board inactivity. Keep this regression visible and fix it. Existing Home137pass is historical evidence.
- Template already owns ordered install/duplicate-root guard/service restoration; PanelManager/UIPanel/presenter host, Assets loader, UI tween components; `_Feature` has ShopPanel/ShopOfferItem/SettingsPanel/CoinHolder/Feedback rigs and installers. Reuse these instead of introducing parallel counterparts.
- README/source drift exists: Shop README contains an old manual-presenter example inconsistent with the later factory-host section, and an older statement about no fake gateway inconsistent with current template billing callback. Prefer pinned installed source plus tests; repair local docs, do not copy stale instructions.

## Sample ownership and conventions

Owner steering,2026-10-24: prefer direct changes/removal in the currently imported sample scripts/prefabs instead of copying a parallel feature. Preserve GUID/serialized consumers; remove demo/dead code only after checking references. Follow repository naming, single responsibility and explicit dependencies. Retain a variant only for a distinct game skin, and a narrow adapter only for a real game-domain boundary. Record local sample patches for future package/sample merges.

## Ownership decisions

| Concern | Reuse | Game-specific boundary |
|---|---|---|
| Composition | Preserve template BootstrapScene (Init) ? GameInit/SceneLoader ? MainScene; GameInstaller only in Bootstrap | PixelFlow profile configures save scope/configs/adapters; MainScene controller binds/releases its scene lifetime |
| Save/config | Existing DataConfig/Datasave; preserve old keys/schema/scope and codec compatibility | Existing campaign/rules/profile remain authoritative; no second coin or entitlement store |
| Economy | Economy contracts and `_Feature` holders; one adapter over current progress aggregate | Preserve atomic multi-resource reward/result/checkpoint semantics while exposing wallet/balance events; no destructive wallet migration |
| Shop | `_Feature` ShopPanel/ShopOfferItem prefab variants, ShopFeatureInstaller/ShopPresenter contracts | Native skin/sections/unlocks, atomic batch purchase adapter, extra restore/recovery intents; fake gateway only until SDK |
| UI lifecycle | Scene PanelManager, stable Addressables keys, presenter factory/host, UIPanel layers/cache/back | PixelFlow layout and screen graph; prefabs/serialized references own skin geometry; avoid runtime UI builders as final architecture |
| Assets | Dreamy Assets/Addressables loading and ownership; source art via serialized dependencies/atlas | Source receipt IDs→output/GUID→prefab/address bindings; preserve extraction and original/template GUIDs |
| Tween/feedback | UITweenPlayer/effect components, existing Feedback rig/player and optional DOTween provider | Native timing/config; feedback never grants resources |
| Audio/settings | Dreamy Audio profile/libraries/service; Settings integration and host gateways | Source clips, profile-backed persistence adapter and native settings skin; avoid parallel AudioSource/PlayerPrefs authorities |
| Gameplay | Retain verified pure PixelFlow rules/session | These are game-specific; Foundation packages provide supporting services, not recovered puzzle mechanics |

## Execution and gates

1. Finish readme/source/prefab/asmdef/address inventory with pinned package identity and recorded reuse decisions. Before custom code, search template, imported `_Feature`, installed package source and Foundation repositories for the actual missing capability.
2. Extend template with an optional composition profile, keeping its existing default lifecycle/tests. Wire PixelFlow through the same root and remove redundant construction/registration. Preserve normal and isolated save paths and legacy loading. No two roots/wallets/settings services.
3. Implement the progress-backed Economy adapter and package-compatible Shop service/catalog presentation boundary. Reuse existing presenters/installers; retain atomic receipt delivery and restoration tests. Handle unavailable APIs with a narrow adapter, not a replacement framework.
4. Author native Shop as prefab variants using Unity Editor APIs; bind existing ShopOfferItem and holders, use stable Addressables keys and PanelManager. Configure UITweenPlayer/effects on prefabs. Remove the previous Shop runtime layout/navigation path after parity/function checks. Preserve native visual target rather than shipping sample art.
5. Route Home/common UI, settings and audio through the same package/asset lifecycle. Any residual game-specific view authoring must be documented with the missing Foundation capability and an owner; no silent fallback to duplicate infrastructure.
6. Update architecture/install/run/update docs, toolkit skill/reference and regression evidence. Toolkit contains reusable guidance/source-free receipts only, not game/source assets.
7. Verify actual installed APIs and final consumer: compile/Console, old-save migration, single-owner/teardown/reopen/cancel/address-load failure, transactions/restore, original UI routes, board retirement, actual Game View comparisons. Run toolkit npm test. No player/device/store-ready claim without the deferred build/device/SDK gates.

## Completion contract

Deliver real code/prefab/address/installer integration and evidence, not a dependency-list-only compliance claim. Describe each retained exception, remove superseded paths, and distinguish tested integration, native parity and release readiness. `No build` and fake IAP remain explicit deferred release gates, even when architecture/Editor checks pass.
