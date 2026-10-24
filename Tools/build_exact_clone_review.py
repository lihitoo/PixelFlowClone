"""Build a local evidence browser and explicit source/candidate comparisons."""
import hashlib,html,json,pathlib
ROOT=pathlib.Path(__file__).resolve().parents[1]; OUT=ROOT/'Docs/ExactClone'
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def main():
 data=json.loads((OUT/'screen-contracts.json').read_text(encoding='utf8'))
 visual=json.loads((OUT/'visual-evidence.json').read_text(encoding='utf8'))
 visual['images']=[image for image in visual['images'] if not image['id'].startswith('video-frame-')]
 for screen in data['screens']: screen['visualReferenceIds']=[item for item in screen['visualReferenceIds'] if not item.startswith('video-frame-')]
 sources=json.loads((OUT/'Research/sources.json').read_text(encoding='utf8'))
 frames=[]
 for number,state,screens in [(2,'source gameplay level1 already in progress',['PLAY01','PLAY02','PLAY03','PLAY04']),
                              (12,'source reward/transition survey; state must be visually checked',['WIN01']),
                              (30,'source loading splash',['SYS01','SYS02'])]:
  path=OUT/'Research/video-frames-480'/f'survey-{number:03}.png'
  item=dict(id='video-frame-'+str(number),path=path.relative_to(ROOT).as_posix(),sourceId='video-early',url='https://www.youtube.com/watch?v=ukM7_qSP1Fo',sha256=sha(path),review='usable-reference' if number!=12 else 'survey-only',observation=state,targetVersionVerified=False,
            sample=dict(method='Sequential decode with ffmpeg fps=1/5; no input fast-seek on MPEG-TS timestamps.',index=number,nominalBinSeconds=[(number-1)*5,number*5],timingMeasurement=False),screens=screens)
  frames.append(item)
  if number!=12:
   visual['images'].append(item)
   for screen in data['screens']:
    if screen['id'] in screens:
     screen['visualReferenceIds'].append(item['id']); screen['visualCoverage']='source-image-partial'
 visual['excludedFrameSet']=dict(path='Research/video-frames',reason='Earlier fast-seek frames are not timestamp-reliable for MPEG-TS input; names such as source-loading did not match decoded state. Use only sequential video-frames-480 entries with reviewed states.')
 (OUT/'visual-evidence.json').write_text(json.dumps(visual,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
 (OUT/'screen-contracts.json').write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
 video=OUT/'Research/early-review-480.mp4'
 (OUT/'Research/video-evidence.json').write_text(json.dumps(dict(url='https://www.youtube.com/watch?v=ukM7_qSP1Fo',metadata='video-early-metadata.json',path=video.relative_to(ROOT).as_posix(),sha256=sha(video),bytes=video.stat().st_size,role='Third-party public gameplay video, not known0.36.3',selectedFrames=frames,limits='Landscape capture and active board state differ from portrait clone previews. Do not compare raw pixel error or infer native timing from5-second survey. Survey frames were inspected, not all671 seconds at normal speed.'),indent=2)+'\n',encoding='utf8')
 # Clone pictures are historical evidence scoped to their source revision. This
 # work only prepares a new brief; it does not recertify game source/UI parity.
 baseline='Docs/Evidence/editor-review/review-20261024-131418/'
 pairs=[
  dict(id='compare-home',screen='HOME01',original='Docs/ExactClone/Research/images/ui-triptych.jpg',clone=baseline+'home-720x1280.png',finding='Source: vertical level path, lives/coin header and bottom tabs. Clone: logo/Hero and menu grid. Large composition/flow mismatch; replace shell with source assets.',limit='Original is a montage with different progression and unspecified version; composition-only comparison.'),
  dict(id='compare-shop',screen='SHOP01',original='Docs/ExactClone/Research/images/shop-analysis-01.png',clone=baseline+'shop-720x1280.png',finding='Source: scrollable priced coin packs/No Ads/bundles. Clone: four coin-for-booster rows. Different products, navigation, layout and purchase flow.',limit='Source prices are screenshot locale/time examples, not configured production catalog.'),
  dict(id='compare-game',screen='PLAY01',original='Docs/ExactClone/Research/images/event-backend-02.jpg',clone=baseline+'level-1-720x1280.png',finding='Source rounded narrow conveyor, compact gear/level/coins, waiting and booster composition. Clone has square treads, added counters/progress and different scaling.',limit='Different puzzle and progression. Inspect layout/art roles; no target/hit-order or raw pixel comparison.'),
  dict(id='compare-load',screen='SYS02',original='Docs/ExactClone/Research/video-frames-480/survey-030.png',clone=None,finding='Source loading splash is visible in public video; current clone has no corresponding loading screen capture or implementation.',limit='Landscape video, source version unspecified; target0.36.3 Boot YAML corroborates loading/splash structure.'),
  dict(id='compare-fail',screen='LOSE01',original='Docs/ExactClone/Research/images/ui-triptych.jpg',clone=baseline+'loss-720x1280.png',finding='Source Out of Space has board preview, coin action and fail offer. Clone generic retry/100-coin revive differs in presentation and rule.',limit='Failure triggers/progression differ; source900 price is state-specific.'),
  dict(id='compare-settings',screen='SET01',original=None,clone=baseline+'settings-720x1280.png',finding='Original prefab explicitly has separate Sound/Music sliders and many ancillary controls. Clone switches/master volume do not match. Native screenshot remains missing.',limit='Serialized controls establish structure; not a rendered source screenshot.'),
  dict(id='compare-events',screen='EVT02',original='Docs/ExactClone/Research/images/event-backend-03.webp',clone=None,finding='Published Fire Quest/Arena UI exists. Clone has neither event screen; requires complete event/claim/service flows.',limit='Vendor image has two event panels, not monthly leaderboard proof.'),
  dict(id='compare-rank',screen='RANK01',original=None,clone=None,finding='Source leaderboard prefab has podium, entries, loading/error/retry. Clone lacks rank; exact rendered monthly-state screenshots still needed.',limit='Do not relabel Arena image as monthly ranking.')]
 for pair in pairs:
  for key in ['original','clone']:
   if pair[key]: pair[key+'Sha256']=sha(ROOT/pair[key])
 (OUT/'comparisons.json').write_text(json.dumps(dict(schemaVersion=1,role='Prepared visual gap comparison; not visual pass.',pairs=pairs),ensure_ascii=False,indent=2)+'\n',encoding='utf8')
 sources['sources']=[s for s in sources['sources'] if s['id']!='video-early']+[dict(id='video-early',url='https://www.youtube.com/watch?v=ukM7_qSP1Fo',kind='third-party-runtime-video',title='TapCheat: Pixel Flow Gameplay Walkthrough Part1 Level1-15',status='downloaded-and-surveyed',version='unspecified; uploaded2025-11-16',metadata='video-early-metadata.json')]
 (OUT/'Research/sources.json').write_text(json.dumps(sources,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
 payload=json.dumps(dict(screens=data['screens'],images=visual['images'],pairs=pairs,sources=sources['sources']),ensure_ascii=False).replace('<','\\u003c')
 template='''<!doctype html><html lang="vi"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Pixel Flow — exact clone evidence review</title>
<style>:root{color-scheme:dark;font:15px/1.6 system-ui;background:#111724;color:#e9eef8}body{max-width:1400px;margin:auto;padding:24px}a{color:#88caff}h1{font-size:28px}h2{font-size:23px}small,.muted{color:#b3bfd4}.banner{background:#40321a;border:1px solid #94733e;padding:16px;border-radius:12px}.toolbar{display:flex;gap:12px;position:sticky;top:0;background:#111724;padding:16px 0;z-index:3;flex-wrap:wrap}input,select,button{font:inherit;padding:10px;background:#222e45;color:white;border:1px solid #536682;border-radius:7px}input{flex:1}article{background:#1b2539;padding:20px;border-radius:12px;margin:20px 0}.pair{display:grid;grid-template-columns:1fr 1fr;gap:16px}.frame{background:#0b101c;text-align:center;min-height:160px;padding:12px}.frame img{max-height:570px;max-width:100%;object-fit:contain}.missing{color:#ffce86;padding:50px 12px}.tag{display:inline-block;background:#344763;padding:2px 9px;border-radius:16px;margin:3px}.links{overflow-wrap:anywhere}details{margin:14px 0;border-top:1px solid #40516c;padding-top:9px}.source-images{display:flex;flex-wrap:wrap;gap:10px}.source-images img{max-width:280px;max-height:270px;object-fit:contain}.count{color:#9fd9b8}@media(max-width:700px){.pair{grid-template-columns:1fr}body{padding:12px}}</style>
<h1>Pixel Flow — bộ brief và ảnh đối chiếu toàn game</h1><p>Target0.36.3 · không build · JSON từng asset · <a href="BRIEF.vi.md">Brief</a> · <a href="screen-contracts.json">65 contracts</a> · <a href="../AssetCatalog/manifest.json">24.588 file asset</a></p>
<div class="banner"><b>Chưa phải exact clone.</b> Ảnh nguồn được tách runtime / montage / marketing / serialized structure. Các cặp khác level/version chỉ dùng so composition. Source settings/rank và một số state chưa có screenshot: giữ gap, không thay bằng mock hoặc ảnh help-page.</div>
<div class="toolbar"><input id="search" placeholder="Tìm loading, shop, settings, rank, booster…"><select id="mode"><option value="pairs">Ảnh đối chiếu và sai lệch</option><option value="screens">Tất cả65 màn/luồng</option><option value="missing">Màn thiếu screenshot cụ thể</option></select><span id="count" class="count"></span></div><main id="content"></main>
<script type="application/json" id="data">PAYLOAD</script><script>
const d=JSON.parse(document.getElementById('data').textContent),$=x=>document.getElementById(x),esc=x=>String(x??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const relative=p=>p.startsWith('Docs/ExactClone/')?p.slice(16):'../../'+p;
function frame(p,label){return '<div class="frame"><b>'+label+'</b>'+(p?'<br><a href="'+esc(relative(p))+'"><img src="'+esc(relative(p))+'"></a>':'<div class="missing">Chưa có ảnh phù hợp — không thay bằng ảnh giả</div>')+'</div>'}
function render(){let q=$('search').value.toLowerCase(),mode=$('mode').value,rows=(mode==='pairs'?d.pairs:d.screens).filter(x=>JSON.stringify(x).toLowerCase().includes(q));if(mode==='missing')rows=rows.filter(x=>!x.visualReferenceIds.length);$('count').textContent=rows.length+' mục';$('content').innerHTML=rows.map(x=>mode==='pairs'?'<article><h2>'+esc(x.id)+' · '+esc(x.screen)+'</h2><div class="pair">'+frame(x.original,'NGUỒN GỐC')+frame(x.clone,'CLONE HIỆN TẠI')+'</div><p>'+esc(x.finding)+'</p><small>'+esc(x.limit)+'</small></article>':'<article><h2>'+esc(x.id)+' · '+esc(x.title)+'</h2><span class="tag">'+esc(x.visualCoverage)+'</span><span class="tag">'+esc(x.status)+'</span><p><b>Entry:</b> '+esc(x.entry)+'</p><p><b>Luồng phải có:</b> '+esc(x.requiredBehavior)+'</p><p><b>State/edge:</b> '+esc(x.requiredStates.join('; '))+'</p><p><b>Sai lệch hiện tại:</b> '+esc(x.currentCloneGap)+'</p><small>'+esc(x.sourceVersion)+'</small><details><summary>Ảnh và nguồn</summary><div class="source-images">'+x.visualReferenceIds.map(id=>d.images.find(y=>y.id===id)).filter(Boolean).map(y=>'<a href="'+esc(relative(y.path))+'"><img src="'+esc(relative(y.path))+'" title="'+esc(y.observation)+'"></a>').join('')+'</div>'+x.sourceIds.map(id=>{let s=d.sources.find(y=>y.id===id);return s?'<p><a href="'+esc(s.url)+'">'+esc(id+': '+s.title)+'</a> · '+esc(s.kind)+'</p>':'<p>'+esc(id)+' → <a href="source-ui-structure.json">source serialized UI</a></p>'}).join('')+'</details><details class="links"><summary>'+x.assetCandidates.length+' asset ứng viên — cần verify binding</summary>'+x.assetCandidates.map(a=>'<p><a href="'+esc(a.uri)+'">'+esc(a.path)+'</a><br><small>'+esc(a.id)+'</small></p>').join('')+'</details></article>').join('')}
$('search').oninput=render;$('mode').onchange=render;render();</script></html>'''
 (OUT/'review.html').write_text(template.replace('PAYLOAD',payload),encoding='utf8')
 print(json.dumps(dict(comparisons=len(pairs),screens=len(data['screens']),visualGaps=sum(not s['visualReferenceIds'] for s in data['screens']),videoFramesReviewed=2)))
if __name__=='__main__': main()
