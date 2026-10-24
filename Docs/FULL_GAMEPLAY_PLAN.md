# Full offline gameplay and UI implementation

Owner steering 2026-10-24: implement the remaining gameplay, UI and functional flows. Continue source/Editor work; do not build players. This expands the seven-level checkpoint. Preserve the supplied source/extraction, selected asset receipts, template history and existing save key.

## Concrete delivery

- Campaign catalog for all 2,600 main-funnel levels, paged selection and lazy chunk loading. Keep the 517 side-funnel definitions separate; they are not automatically campaign continuations.
- Rule support for source shooter features and every populated board-object schema. Configuration is immutable; a session owns all mutable health, queues, locks, capacities, reveal, timer and object state.
- Home, onboarding/help, level selection, gameplay HUD, booster target/cancel, pause, settings, win, loss, retry, coin continue, campaign end, shop, daily reward, confirmation/error recovery and saved-run resume.
- Local wallet/inventory/rewards/settings with migration and idempotent operations. Failed saves must not consume resources or grant a duplicate win/reward.
- Functional presentation for special shooters and board objects: visible state/health/color, locked/hidden/ready feedback and effects. Existing assets are preferred; procedural geometry/UI may implement missing roles.
- Editor rules/data/save and UI/scene integration tests, snapshots and review. No normal-player/device/performance/owner-approval claim from these tests.

## Source and design decisions

The source data supplies identities, order, health, colors, areas, connections, pipe payloads and object layers. It does not recover native update logic. These contracts define a coherent functional implementation; unmeasured native details remain estimates, not claims of exact recovered code.

Shooter contracts:

| Family | Runtime contract |
|---|---|
| Surprise | Conceal color/ammo behind the queue front; permanently reveal when it reaches the front or launches. |
| Connected/chain | Resolve the live linked group, validate every member and total capacity, then launch atomically. Preserve links on return; retired members stop reserving capacity. |
| Locks | Source lock entries are queue blockers rather than extra ammunition. Exposing a board key removes one lock blocker. |
| Shooter pipes | Expand the pipe's actual payload at its queue position, preserving child IDs/order and pipe origin; do not count the placeholder's dummy ammo. |
| Hammer | Front support node strikes wood overlays and exits without spending regular-pig ammo or occupying the belt. |
| Shooter ice | Frozen nodes cannot launch; successful board hits reduce their ice counter, then reveal the normal shooter. |
| Totem | Counter blocker clears as regular shooters finish. |
| Mallet | Front support node strikes the matching current music-toy segment, then exits. |
| Slot cage | Seals a waiting slot until matching-color hits remove its source health. |
| Astronaut | Launching an astronaut opens a matching spaceship overlay while preserving its normal ammunition. |
| Conveyor door | Matching passages reduce door health; mismatched passages return early while the door remains closed. |

Board contracts:

- Base pixels retain source health and multi-cell footprints; one successful hit spends one shot, damage never duplicates a shared footprint.
- Gates, pixel pipes, snakes/multisnakes, eggs, pumpkins, beans, crossbows, colored cages and nesting dolls expose source-defined colored health/layers or queues as shot targets.
- Walls occlude shots without becoming extra required ammo. Keys and uncolored shooter cages trigger when exposed; cages release their stored shooter.
- Ice, wood, color doors, biscuits, curtains, split obstacles, UFO seats, music toys, accordions, spaceships and mixers protect their source footprint and respond to matching hits, support nodes or completion events as appropriate. Distinguish these overlays from extra shot-health targets so source ammo is not silently doubled.
- Every populated source kind is retained. Unknown fields/kinds fail normalization/validation rather than disappearing.

These event/overlay details are implementation decisions inferred from data relationships and demo frames. They need source-runtime comparison later. Counts/types are in `Evidence/source-feature-profile.json` and `Evidence/source-board-profile.json`.

## Booster and economy contracts

Primary source descriptions: [power-ups](https://loomgames.helpshift.com/hc/en/3-pixel-flow/faq/13-new-to-power-ups-learn-what-each-boost-does/) and [Super Unicorn](https://loomgames.helpshift.com/hc/en/3-pixel-flow/faq/43-meet-super-unicorn-your-new-super-booster/), inspected 2026-10-24. These describe current product behavior, not necessarily every detail of package0.36.3.

Hand enables a rear-queue selection, Shuffle rearranges eligible options, Add Tray expands active capacity, and Super Shooter clears visible targets of the chosen color. Targeted actions consume inventory only after a valid effect; cancellation/failure consumes nothing. Unicorn activation follows ten wins and hit-based charge, uses waiting shooters' colors/ammunition, and deactivates on failure. Charge threshold, prices, rewards and continue limits are configurable clone tuning, not measured source values.

The shop exchanges earned local coins for inventory. Daily claims use a persisted UTC-day marker and cannot claim twice for the same day. Completion rewards are recorded once per run, with first-clear rewards separated from repeat wins. Settings, inventory and the active-run snapshot share the project progress owner. No spending occurs through UI widgets directly.

## Implementation order and verification

1. Normalize full data and validate all chunks without importing the original dummy code.
2. Extend pure rules and object/shooter state; prove atomic group launch, blocker events, snapshots, loss/revive and boosters with deterministic fixtures.
3. Extend progress storage/catalog and prove migration, reward/claim/purchase idempotency and failed-save behavior.
4. Wire complete screen/state flows and present source feature states. Preserve tested plain-level behavior.
5. Run focused rules tests, then GPU Editor scene fixtures for onboarding, menus, settings/shop, win/loss/continue/retry, save/resume and representative special levels. Fix actual failures before calling a flow done.
6. Record a requirement-to-code/test matrix. Separate implemented, Editor-verified, source-fidelity and device/build gates. A visible button without a completed action is not implemented.
