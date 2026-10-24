"""Pixel Flow's explicit whole-game contract and traceability, not implementation.

Keep per-screen evidence, native-version uncertainty, asset availability and
current clone gaps separate. This runs before promoting the reusable workflow.
"""
import collections,hashlib,json,pathlib
ROOT=pathlib.Path(__file__).resolve().parents[1]
OUT=ROOT/'Docs/ExactClone'

# ID | surface | evidence IDs | candidate asset tags | entry | controls/outcomes |
# state/error cases | clone divergence. Requirements are not retroactive approval.
ROWS='''
SYS01|Cold boot / splash|video-early,source-ui|loading|Cold process start|Initialize config, profile, audio and UI; transition according to source onboarding/progress|Fresh save; existing save; background; missing config; corruption|No faithful splash yet; startup creates Home UI before config completes
SYS02|Loading / transition|video-early,source-ui|loading|Boot or level transition|Display original logo/background/loading bar; show actual stage progress; block duplicate input|Slow load; failed stage; retry; cancellation; no fake percent completion|No loading screen presentation in current app
SYS03|Connectivity / service retry|source-ui,help-24,help-25,help-26,help-27|loading,rank|Service request or interrupted network|Show spinner, error and Retry; preserve current session and pending rewards|Offline; timeout; retry spam; recover; stale response|Generic local save error exists; no remote service state
SYS04|Consent / privacy / permission|source-ui|settings,profile|First eligible launch or Privacy Settings|Render consent controls; dismiss/return; notification permission via platform|Consent accepted/declined; unavailable OS prompt; saved choice|No source privacy/notification UI integration
SYS05|Version / support diagnostics|source-ui,help-22,help-23|settings|Settings footer or load failure|Show installed version and player ID; support entry; preserve progress|Copy ID; no network; missing account; log request|Current error popup omits source diagnostic surfaces
FTUE01|Fresh-player routing|first-hour,video-early,source-ui|home,gameplay|No progression / fresh save|Follow original first-play sequence; route directly to tutorial when observed; reveal lobby at observed stage|Fresh/relaunch; tutorial interruption; migrated save|Clone always begins at designed logo/Hero Home
FTUE02|Interactive core tutorial|first-hour,video-early|gameplay|First applicable board|Focus legal pig, dim/block other controls, point/tap; advance on real rule action|Wrong tap; repeated tap; app pause; reload tutorial|Current four-page text tutorial differs from source guided overlay
FTUE03|Booster introductions / unlocks|first-hour,source-ui,help-13|gameplay|Verified progression threshold|Introduce Tray, Hand, Shuffle, Super separately; persist each unlock; show correct locked state|Locked; first grant; use/cancel; insufficient stock|Clone grants and exposes all four immediately; thresholds need source config confirmation
FTUE04|Feature introduction cards|video-early,source-ui|gameplay|First level with each source feature|Original artwork/name/short rule; dismiss; resume board; record seen|First seen; repeated; interrupted; localization|Current procedural text cards and inferred rules need matching original cards
HOME01|Lobby / vertical level path|first-hour,source-ui|home|Eligible progression returns to Home|Level path, current node, Hard/Very Hard labels, Play; preserve scroll/current selection|Early/late progression; level reward resolution; content end|Current logo/Hero menu and20-level grid are a different design
HOME02|Home currency / lives header|first-hour,shop-analysis,source-ui|home,lives,shop|Home, Shop and relevant pages|Heart count/timer; coin amount; plus refill/shop actions; settings|Full hearts; zero; unlimited hearts; rolling timer; wallet update|No lives system; Home coin text only
HOME03|Bottom navigation|first-hour,shop-analysis,source-ui|home,shop,rank|Home-root pages|Shop, Start/Home, locked or unlocked rank destination; active tab feedback|Lock explanation; retain page/scroll; back navigation|Clone uses a grid of separate text buttons
HOME04|Home event / offer dock|first-hour,event-backend,source-ui|home,events,pass|Eligible event/time/progression|Open live event, pass, no-ads and offer entry; countdown/badge/claim state|Locked; not active; active; completed; claim window; expired|No source event/offer dock
HOME05|Progress / campaign-end league|source-ui,help-39|home,rank|Main content completed|Show endgame league/round progress and score rules; retain rewards|Latest content; new content arrival; reset/season rollover|Clone final Next returns Home; no endgame league
PLAY01|Level entry / HUD|video-early,event-backend,source-ui|gameplay|Play and level load|Source level/difficulty, coin plus/settings, board/belt/front queue/waiting, booster bar|Normal/Hard/Very Hard; source-selected dimensions; no duplicate Play|Clone HUD composition/large belt differs; top progress/counts are agent additions
PLAY02|Front queue selection / ammo|help-9,video-early,source-ui|gameplay|Active level|Tap front pig; match color; one valid hit consumes one shot; preserve queue order|Rear blocked; moving pig tapped; modal open; linked group capacity|Public model and pointer paths covered, exact timing/geometry not measured
PLAY03|Belt movement / firing|video-early,source-ui|gameplay|Accepted launch|Entrance spacing, smooth corners, pig facing, line of sight, ballistic/HP feedback|Occlusion; different color; frame time; several active pigs|Rectangular track and procedural rails differ; native target cadence/direction needs measurement
PLAY04|Waiting / overflow|help-9,video-early|gameplay|Lap ends with ammo|Return to source-defined waiting slot; relaunch; resolve overflow prompt at correct time|5 occupied; launch just before return; returning link; cage slot|Clone behavior tested but native grace/return timing unmeasured
PLAY05|Source pixel / layered goals|source-ui,mechanics-gallery|gameplay|Populated source board|Retain all source colors/health/spans/hidden state; layer and object changes match source|Hard pixel; reveal; overlapping overlay; no double-count ammo|Typed24 kinds implemented with inferred semantics; no blanket native-equivalence claim
PLAY06|Special pigs / support|source-ui,mechanics-gallery|gameplay|Feature-bearing queue|Original mesh/icon/color; hidden/ice/lock/pipe/link/totem/hammer/mallet/astronaut rule|Linked front ordering; blocked states; support effect; group return|Text captions/procedural substitutes remain despite source asset availability
PLAY07|Tray booster|help-13,first-hour,source-ui|gameplay|Unlocked boost on active board|Add source amount of conveyor capacity; consume/grant exactly once|Max capacity; no stock; purchase; retry save|Implemented; capacity/cost/unlock tuning not verified native
PLAY08|Hand booster|help-13,first-hour,source-ui|gameplay|Unlocked boost on active board|Select any eligible queue pig through original targeting interaction; cancel safely|Hidden/linked/locked/frozen; capacity; no effect; no stock|Full-screen list differs from original direct board selection
PLAY09|Shuffle booster|help-13,first-hour,source-ui|gameplay|Unlocked boost on active board|Use original eligible-set/shuffle rule; animate queue changes; update counts|Support/pipe/group constraints; no effect; insufficient stock|Clone eligible-set permutation is inferred; no original animation
PLAY10|Super Shooter booster|help-13,first-hour,source-ui|gameplay|Unlocked boost on active board|Original color pick/shot sequence and target set; consume once after valid effect|Occluded/hidden/layers; cancel; win during effect|Clone clears exposed layer, but native object exclusions/cadence unverified
PLAY11|Super Unicorn|help-43,source-ui|gameplay|Unlock/activation and charge conditions|Show inactive/charging/ready/consumed; combine waiting pigs using actual source rule|Failure reset; insufficient waiting ammo; resume mid-charge|Current ten-wins/hit100 and merge behavior are clone tuning; web may be newer than0.36.3
PAUSE01|In-level settings / pause|source-ui,video-early|settings|Gear during gameplay|Original settings modal; freeze/restore appropriate model/input; close by X/back|Repeated open; background; result appearing; modal click-through|Clone has an additional Pause screen plus a separate settings page
PAUSE02|Restart / Give Up confirmation|source-ui|settings,result|Settings Restart/Give Up|Show source confirmation/life warning; reset/return according to source; preserve wallet|Cancel; no hearts; unlimited hearts; active event loss|Clone free retry/Home snapshot are different policies
WIN01|Clear celebration / reward|video-early,help-14,source-ui|result|Last required target clears|Source celebration, reward card, coin animation and Continue; resolve event/pass rewards|Repeated terminal callback; reward save failure; interrupted/restore|Current large LEVEL CLEAR modal and reward tuning differ
WIN02|Rewarded multiplier / level reward|help-15,first-hour,source-ui|result,shop|Eligible clear/reward button|Ad availability, watch/cancel/error; grant only on verified reward callback|No-fill; app suspend; duplicate callback; network failure|No ad adapter or source multiplier UI; cannot silently grant
LOSE01|Out of Space / fail prompt|source-ui,first-hour|result|Source overflow/loss event|Original prompt, price, clear/slot continue offer, Hold to See Game, Close|Continue unaffordable; no service; repeated click; full waiting|Clone generic TRY AGAIN with different continue mechanism/cost
LOSE02|Continue transaction|source-ui,help-30|result,shop|Fail prompt continue|Apply source action and correct cost, rollback on failure; preserve current run|Different continue counts/prices; wallet insufficient; interrupted|Clone adds waiting slot/clears blockers; this is not established native behavior
LOSE03|Fail offer / streak-loss summary|first-hour,event-backend,source-ui|result,events,shop|Fail with active offer/streak|Show lost streak/multiplier, timed offer and items; close/return through exact flow|Offer expired; purchase cancelled; unlimited hearts; offline|Absent in current clone
SHOP01|Shop shell / scrolling sections|shop-analysis,first-hour,source-ui|shop|Shop bottom tab or coin plus|Source roof/header and vertical ScrollRect; preserve section order and scroll|Early-only coins; boosters unlocked; no-ads visibility; active tab|Current four local coin-for-booster rows are a replacement design
SHOP02|Six coin packs|shop-analysis,source-ui|shop|Gold Packs section|3x2 cards, source coin art/amounts, localized store prices, purchase trigger|Prices loading/unavailable; currency/region; item unavailable|Absent; source images and prefabs available
SHOP03|No Ads / No Ads bundle|shop-analysis,help-16,source-ui|shop|Eligible shop/offer entry|Correct entitlement, bundle contents and source layout; hide owned offer; retain rewarded choice|Already owned; restore; cancel; service fail|Absent; no fake payment success is acceptable
SHOP04|Small / Medium / Ultra bundles|shop-analysis,source-ui|shop,lives|Eligible shop stage|Original blue/purple/orange cards; coins/boosters/unlimited hearts; real grant receipt|Timed availability; duplicate receipt; expired session|Absent despite exported bundle prefab/art
SHOP05|Purchase lifecycle|help-30,source-ui|shop|Tap priced card|Connecting, platform purchase, pending, cancelled, failure, verified receipt, grant and acknowledgement|Double tap; network loss; already owned; duplicate receipt; process restart|No billing provider configured; implementable UI/adapters and real external service readiness must be separate
SHOP06|Restore purchases|source-ui,help-29,help-30|settings,shop|Settings restore action|Restore durable entitlements and report success/none/error accurately|Offline; no entitlement; refund; platform mismatch|Absent
SHOP07|Offerwall / external rewards|help-37,source-ui|shop,events|Eligible offerwall button|Open supported external flow, return, pending/claim/error state; verified reward|Tracking unavailable; missing reward; cancellation|Absent; service credentials/availability not supplied
LIFE01|Heart regeneration|shop-analysis,first-hour,source-ui|lives|Life consumed and timer starts|Source max/interval; UTC persisted clock; refill per interval; heart-plus modal|Full; zero; clock shift; restart; accumulated refill|Absent; source settings exist, actual values require resolution
LIFE02|Refill / unlimited hearts|shop-analysis,source-ui|lives,shop|Heart plus or no lives|Coin/ad/IAP refill options when source-supported; unlimited-heart timer; resume play|Expiry while playing; save fail; purchase pending|Absent
SET01|Settings sound / music|source-ui|settings|Settings modal|Two separate source sliders with live preview and persisted gains|0/1/intermediate; close/reload; mute while voices active|Clone switches plus master step buttons differ from source sliders
SET02|Haptic / notification / smooth motion|source-ui|settings|Settings modal conditional rows|Source toggle art and state; OS permission if applicable; real smooth-motion behavior|Unsupported platform; permission denied; value restore|Haptic only present; notifications/smooth-motion absent
SET03|Account / save progress / name|help-17,help-18,help-19,help-20,source-ui|profile,settings|Settings account/save action|Source username edit, identity/link/provider and save-sync states|Guest; linked; stale cloud; conflict; duplicate display name|No account/profile/sync backend; no name editor
SET04|Privacy / delete / social / support|source-ui|settings,profile|Settings ancillary actions|Privacy preferences, feedback/support, social deep link, deletion confirmation, version/player ID|No handler; no network; cancel delete; restore UI|Absent; do not reset saves merely to imitate deletion
RANK01|World monthly leaderboard|help-39,source-ui|rank|Unlocked rank tab|Source podium top3, scroll entries rank/avatar/name/score/reward, self-row jump and season timer|Loading; empty; self outside top; error/retry; refresh; stale season|Absent; original loading/error/Retry nodes are present in prefab
RANK02|Master / month history / reward|help-39,source-ui|rank|Qualify/season rollover/history|Correct master threshold/tabs, month data, rewards and claimed state|Month rollover; no score; unclaimed past reward; duplicate claim|Absent; no invented player scores permitted
RANK03|Player profile / avatar|source-ui,help-19|rank,profile|Self/profile button|Original avatar frame/name edit/score identity; persist avatar selection|Locked avatar; current name; cancel; malformed input|Absent
TEAM01|Team ranking / social|help-39,event-backend|teams,rank|Version-specific team entry|Research actual0.36.3 presence before adding; if present mirror list/member/join/leave/chat states|Locked; full team; no network; moderation state|Current docs mention teams, package evidence weak; target-version presence unresolved, not auto-excluded
PASS01|Pixel Pass free/premium track|help-12,first-hour,source-ui|pass|Unlocked pass home entry|Season timer; milestone rows; separate free/premium rewards; progress and Claim|Locked; earned; claimed; premium-required; season expiry|Absent despite many exported pass prefabs
PASS02|Golden Ticket / premium entitlement|help-32,source-ui|pass,lives,shop|Pass premium CTA|Show real ticket benefits, purchase/owned/pending; gold-name/unlimited-heart entitlement|Restore; expiry; purchase failed; no price|Absent; premium cannot be silently granted
PASS03|Bonus chest / finish / claim window|help-12,help-31,source-ui|pass,daily|Pass threshold or season finish|Source bonus rows/chests, carryover and remaining claim policy|Reward pending; season end; next season; repeated claim|Absent
EVT01|Event hub / unlock / schedule|help-26,help-27,source-ui|events|Home event dock|Availability, requirement, UTC countdown, enter/info and claim states|Not unlocked; not scheduled; active; claim period; offline|Absent; schedules from current web are not automatically0.36.3 facts
EVT02|Fire Quest|help-35,event-backend,first-hour,source-ui|events|Active daily streak event|Source tower/route, consecutive first-attempt progress, reward chest and failure reset|First entry; streak lost; success; expiry; claim once|Absent
EVT03|Pixel Arena|help-33,event-backend,first-hour,source-ui|events,rank|Eligible win / active arena|Join/name onboarding, points/multiplier/ranking, info popup and end reward|Lose/reset; delayed sync; finish; retry; reward claim|Absent
EVT04|Pixel Race|help-34,source-ui|events,rank|Scheduled race|Grouped race track, stage/finish, advancement only per source winning condition|Not first; next stage; end; claim|Absent; version-specific source matching required
EVT05|Cosmic Journey|help-36,source-ui|events|Scheduled journey|Source milestone path, objectives/tokens, reward claims and time window|No token; completed; expired; claim period|Absent
EVT06|Temple Mission|help-40,source-ui|events|Scheduled mission event|Four missions per stage and five stages according to current documentation; precise target version must match config|Mission complete; stage claim; shared progress; end|Absent; may describe newer product version
EVT07|Medal Rush|help-41,source-ui|events,rank|Scheduled medal event|Five-player race, medals, stage timers/multipliers and first-place advancement per version|Round ends; placement; next stage; reward|Absent; may describe newer product version
EVT08|Fairy Forest|help-42,source-ui|events|Scheduled forest event|Win difficulty-based shots, reveal tile/gem board, stage/claim path|No shots; stage win; expiry; interrupted reveal|Absent; version presence not established by current docs alone
DAILY01|Daily / gift rewards|help-11,help-14,source-ui|daily|Actual source daily entry if present|Match source placement and reward calendar/claim interaction; one transaction per source day|Claimed; day rollover; rollback clock; cloud conflict|Clone standalone daily button/reward table is an invented layout; shop-analysis reports no in-shop daily gift in its version
SAVE01|Local save / migration / resume|source-ui,help-17,help-23|profile|Action commit, suspend or relaunch|Preserve source-observed run/level policy; no currency duplication; version migration|Corrupt/old/new schema; full storage; app kill; resume result|Clone transaction/snapshot tests cover local contract only; original resume/give-up semantics unconfirmed
SAVE02|Cloud sync / recovery|help-17,help-18,event-backend|profile|Sign-in/device switch|Authentication, owned progress conflict, retry and restore with user identity|Guest collision; old server state; disconnect; deletion|Absent; cannot substitute local JSON and call cross-device parity
SYS06|Localization / accessible input / aspect|source-ui|settings,home|Locale/aspect/device change|Original fonts/keys, RTL where present, safe area, modal focus/back and touch interactions|Missing glyph; long prices/name; notch; small screen; landscape|English-only clone, procedural font atlas and offscreen previews do not cover native localization/safe area
SYS07|Audio / feedback / transitions|video-early,source-ui|gameplay,home,result|Every visible action and screen transition|Use identified original clips/materials/animation; match loop, contact, camera and mix|Pause/mute; repeat hits; screen exit; interrupted tween|Only selected sounds/music wired; full source animation/feedback matching pending
'''

GROUP_ASSET_PATTERNS={
 'loading':['Boot.unity','Loading Bar','Splash'], 'home':['Home','Bottom','Navigation'],
 'shop':['Shop Page Variant','Shop Gold View','Shop Bundle','Shop No Ads','Coin'],
 'rank':['Leaderboard Page Variant','Leaderboard View Entry','Monthly','Master Month'],
 'settings':['Settings View Variant','Privacy Settings View','Settings Toggle'],
 'lives':['heart_generator_settings_json','Heart','Life'], 'pass':['Battle Pass','Golden Ticket'],
 'events':['Fire Quest','Pixel Arena','Race','Mission','Rush','Cosmic','Forest'],
 'profile':['Avatar','User','Profile'], 'teams':['Team','Clan'],
 'daily':['Daily','Reward'], 'gameplay':['Gameplay.unity','Shooter','Feature','Conveyor'],
 'result':['Win','Fail','Reward','Continue']}

def main():
 OUT.mkdir(parents=True,exist_ok=True)
 assets=json.loads((ROOT/'Docs/AssetCatalog/assets.json').read_text(encoding='utf8'))['records']
 sources=json.loads((OUT/'Research/sources.json').read_text(encoding='utf8'))['sources']; source_ids={x['id'] for x in sources}|{'source-ui','video-early'}
 receipts=json.loads((OUT/'Research/capture-receipts.json').read_text(encoding='utf8'))['receipts']
 # Explicit visual classifications after opening contact sheets and selected full
 # images. Related-article pictures (other games/authors) are rejected evidence.
 accepted={'first-hour-02.png':'Home/gameplay/shop/Fire Quest panel montage; old unspecified version',
           'first-hour-03.png':'Core tutorial spotlight sequence', 'first-hour-04.png':'No Ads, gameplay and Home difficulty path',
           'shop-analysis-01.png':'Four progressive shop states; no-ads, six coin packs, bundles',
           'event-backend-02.jpg':'Portrait gameplay with rounded belt, waiting row and booster bar',
           'event-backend-03.webp':'Fire Quest and Pixel Arena panels',
           'ui-triptych.jpg':'Home level path, gameplay and OUT OF SPACE fail offer; third-party unspecified version'}
 accepted.update({f'mechanics-gallery-{i:02}.jpg':'Feature illustration only, not runtime screenshot' for i in range(2,14)})
 accepted['mechanics-gallery-08.jpg']='Pumpkin feature illustration, not runtime screenshot'
 accepted['mechanics-gallery-09.jpg']='UFO feature illustration, not runtime screenshot'
 visuals=[]
 for receipt in receipts:
  if receipt.get('kind')!='published-image': continue
  filename=pathlib.Path(receipt['path']).name; usable=filename in accepted
  visuals.append(dict(id='img-'+pathlib.Path(filename).stem,path=receipt['path'],sourceId=receipt['sourceId'],url=receipt['url'],sha256=receipt['sha256'],
                      review='usable-reference' if usable else 'excluded-as-runtime-reference',
                      observation=accepted.get(filename,'Logo/marketing/illustration/table/related article; retained as research, not gameplay or screen evidence.'),
                      targetVersionVerified=False))
 screen_visuals={
 'HOME01':['img-first-hour-02','img-first-hour-04'], 'HOME02':['img-shop-analysis-01'], 'HOME03':['img-first-hour-02','img-shop-analysis-01'],
 'HOME04':['img-first-hour-02'], 'SHOP01':['img-shop-analysis-01'], 'SHOP02':['img-shop-analysis-01'], 'SHOP03':['img-shop-analysis-01'], 'SHOP04':['img-shop-analysis-01'],
 'FTUE02':['img-first-hour-03'], 'FTUE03':['img-first-hour-03'], 'PLAY01':['img-event-backend-02'],
 'EVT02':['img-event-backend-03','img-first-hour-02'], 'EVT03':['img-event-backend-03'],
 'LOSE01':['img-ui-triptych'], 'LOSE02':['img-ui-triptych'], 'LOSE03':['img-ui-triptych']}
 screens=[]
 for row in ROWS.strip().splitlines():
  fields=row.split('|'); assert len(fields)==8,(len(fields),row)
  key,title,evidence,tags,entry,actions,states,gap=fields; tags=tags.split(','); evidence=evidence.split(',')
  assert all(x in source_ids for x in evidence),(key,evidence)
  candidates=[]
  for tag in tags:
   patterns=GROUP_ASSET_PATTERNS[tag]
   selected=[a for a in assets if a['kind'] not in ['Scripts','Plugins'] and any(t.lower() in a['name'].lower() for t in patterns)]
   selected.sort(key=lambda a:(0 if a['path'].endswith('.prefab') else 1 if a['kind']=='Sprite' else 2,a['name']))
   for a in selected[:8]:
    if a['id'] not in [x['id'] for x in candidates]: candidates.append(dict(id=a['id'],path=a['path'],uri=a['uri'],selection='candidate; inspect binding before use'))
  screens.append(dict(id=key,title=title,entry=entry,requiredBehavior=actions,requiredStates=states.split('; '),sourceIds=evidence,
      sourceVersion='0.36.3 serialized presence where source-ui applies; web/video version unresolved',
      visualReferenceIds=screen_visuals.get(key,[]),visualCoverage='source-image-partial' if key in screen_visuals else 'needs-state-screenshot',
      assetCandidates=candidates,assetCoverage='candidates-found' if candidates else 'needs-asset-resolution',
      currentCloneGap=gap,status='gap-audited-not-parity-accepted',priority='P0-for-exact-clone',
      acceptance=dict(function='Entry, each control, disabled/error/cancel and return paths execute and persist the expected deltas.',
       visual='Compare original and clone at matching progression/aspect/state; attach image pair and divergence notes.',
       asset='Selected original asset IDs resolve to imported GUID/output; no placeholder when suitable source asset exists.',
       completion='No unresolved required state, fabricated service result or unexplained visual substitution.')))
 (OUT/'screen-contracts.json').write_text(json.dumps(dict(schemaVersion=1,target='exact-whole-game-clone',targetVersion='0.36.3',ownerRequest='Whole-game fidelity: all functions including loading/shop/menu/rank/settings/home; asset catalog and screenshot comparison before implementation.',
   buildPolicy='No player builds without new owner request.',fullCloneComplete=False,briefReadyForParityImplementation=False,
   preparationStatus='Whole surface mapped; missing target-version/state screenshots and backend contracts remain named blockers to exact parity, not reasons to remove features.',
   screens=screens),ensure_ascii=False,indent=2)+'\n',encoding='utf8')
 (OUT/'visual-evidence.json').write_text(json.dumps(dict(schemaVersion=1,images=visuals,sourceCaptureReceipts='Research/capture-receipts.json',warning='Published state montage and source video evidence are different from live0.36.3 captures. Never use unrelated images or help-page screenshots as evidence of an in-game screen.'),ensure_ascii=False,indent=2)+'\n',encoding='utf8')
 # Small focused asset index is convenient, while the complete catalog stays in
 # one canonical location and every file remains individually addressable.
 linked={a['id']:a for s in screens for a in s['assetCandidates']}
 (OUT/'screen-asset-links.json').write_text(json.dumps(dict(schemaVersion=1,canonicalCatalog='../AssetCatalog/manifest.json',assets=list(linked.values()),screens={s['id']:[a['id'] for a in s['assetCandidates']] for s in screens}),ensure_ascii=False,indent=2)+'\n',encoding='utf8')
 print(json.dumps(dict(screens=len(screens),withVisualReferences=sum(bool(s['visualReferenceIds']) for s in screens),namedAssetLinks=len(linked),fullCloneComplete=False)))

if __name__=='__main__': main()
