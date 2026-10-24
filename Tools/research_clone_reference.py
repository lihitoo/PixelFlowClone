"""Bounded public-web reference intake; captures are labeled by actual source.

uv run --with requests --with beautifulsoup4 --with playwright Tools/research_clone_reference.py
No login, paywall bypass, owner browser profile or installed game mutation.
"""
import concurrent.futures
import datetime
import hashlib
import json
import pathlib
import re
import time
from urllib.parse import urljoin,urlparse
import requests
from bs4 import BeautifulSoup

ROOT=pathlib.Path(__file__).resolve().parents[1]
OUT=ROOT/'Docs/ExactClone/Research'
BASE='https://loomgames.helpshift.com/hc/en/3-pixel-flow/'
EXTRA=[
 ('store-google','https://play.google.com/store/apps/details?hl=en&id=com.loomgames.pixelflow','official-store-marketing'),
 ('store-apple','https://apps.apple.com/us/app/pixel-flow/id6751056652','official-store-marketing'),
 ('shop-analysis','https://www.linkedin.com/pulse/whats-wrong-pixel-flows-game-shop-michael-khripin-m0xpf','third-party-shop-analysis'),
 ('guide-early','https://pixelflow-guide.com/levels/10/','third-party-video-index'),
 ('guide-updated','https://novaspuzzleden.com/games/Pixel-Flow-Updated0526','third-party-video-index'),
 ('mechanics-gallery','https://pixel-flow.app/wiki/features','third-party-mechanic-gallery'),
 ('guide-first','https://www.pixel-flow-level.com/en/level/1','third-party-video-index'),
 ('first-hour','https://www.linkedin.com/pulse/first-hour-deconstruction-pixel-flow-loom-games-sungsu-kim-mgz6f/','third-party-first-hour-observation'),
 ('event-backend','https://heroiclabs.com/blog/pixel-flow-highlight-announcement/','backend-vendor-first-party'),
 ('arena-fail','https://www.gamigion.com/pixel-flows-lv36-trap-streaks-fomo-monetization-collide/','third-party-event-observation')]

def save(name,value):
 OUT.mkdir(parents=True,exist_ok=True); (OUT/name).write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def fetch(spec):
 key,url,kind=spec
 try:
  response=requests.get(url,timeout=25,headers={'User-Agent':'Mozilla/5.0'}); response.raise_for_status()
  soup=BeautifulSoup(response.text,'html.parser')
  for x in soup(['script','style','nav','footer']): x.decompose()
  main=soup.find('article') or soup.find('main') or soup
  title=(soup.find('h1') or soup.title); title=title.get_text(' ',strip=True) if title else key
  links=[dict(text=a.get_text(' ',strip=True)[:140],url=urljoin(response.url,a.get('href',''))) for a in soup.find_all('a',href=True)]
  images=[dict(alt=i.get('alt',''),url=urljoin(response.url,i.get('data-delayed-url') or i.get('data-src') or i.get('src','')),srcset=i.get('srcset')) for i in main.find_all('img')]
  # Summary is bounded; screenshots/links remain evidence, not a copied manual.
  text=main.get_text(' ',strip=True); excerpt=' '.join(text.split()[:80])
  embeds=[urljoin(response.url,i.get('src') or i.get('data-src','')) for i in soup.find_all('iframe')]
  embeds+=list(dict.fromkeys(re.findall(r'https?[^\s\"<>]*(?:youtube\.com/embed/|youtu\.be/)[^\s\"<>]+',response.text)))
  item=dict(id=key,url=url,finalUrl=response.url,kind=kind,title=title,status='fetched',retrievedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),version='current-web-unspecified; not assumed0.36.3',excerpt=excerpt,headings=[h.get_text(' ',strip=True) for h in main.find_all(re.compile('^h[1-4]$'))],links=links,images=images,embeds=embeds,pageSha256=hashlib.sha256(response.content).hexdigest(),gameScreenshotVerified=False)
 except Exception as error: item=dict(id=key,url=url,kind=kind,status='failed',error=str(error))
 return item

def crawl():
 home=fetch(('help-index',BASE,'official-help-index')); sources=[home]
 sections=sorted({x['url'] for x in home.get('links',[]) if '/section/' in x['url'] and x['url'].startswith(BASE)})
 with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
  results=list(pool.map(fetch,[(f'help-section-{i:02}',u,'official-help-section') for i,u in enumerate(sections)]))
 sources+=results
 faqs=sorted({x['url'] for s in sources for x in s.get('links',[]) if '/faq/' in x['url'] and x['url'].startswith(BASE)})
 specs=[('help-'+re.search(r'/faq/(\d+)',u).group(1),u,'official-help-description') for u in faqs]+EXTRA
 with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool: sources+=list(pool.map(fetch,specs))
 save('sources.json',dict(schemaVersion=1,sourceVersionTarget='0.36.3',limits='Web documentation, dates and videos can describe later versions. Source claims and screenshots must be mapped per screen; no blanket completeness claim.',sources=sources))
 print(json.dumps(dict(sources=len(sources),fetched=sum(s['status']=='fetched' for s in sources),failed=[s['id'] for s in sources if s['status']!='fetched'],videoCandidates=[dict(id=s['id'],embeds=s['embeds']) for s in sources if s.get('embeds')],officialArticles=[dict(id=s['id'],title=s.get('title')) for s in sources if s['kind']=='official-help-description'])))

def augment():
 data=json.loads((OUT/'sources.json').read_text(encoding='utf-8')); known={x['id'] for x in data['sources']}
 for spec in EXTRA:
  if spec[0] not in known: data['sources'].append(fetch(spec))
 save('sources.json',data); print(json.dumps(dict(sources=len(data['sources']))))

def captures(only=None):
 from playwright.sync_api import sync_playwright
 data=json.loads((OUT/'sources.json').read_text(encoding='utf-8')); sources=data['sources']; receipts=[]
 wanted=set(only.split(',')) if only else {'store-google','store-apple','shop-analysis','guide-early','mechanics-gallery','help-index'}
 prior=OUT/'capture-receipts.json'
 if only and prior.exists(): receipts=[r for r in json.loads(prior.read_text(encoding='utf-8'))['receipts'] if r.get('sourceId') not in wanted]
 (OUT/'screenshots').mkdir(exist_ok=True); (OUT/'images').mkdir(exist_ok=True)
 with sync_playwright() as p:
  browser=p.chromium.launch(executable_path=r'C:\Users\trinh\AppData\Local\ms-playwright\chromium-1243\chrome-win64\chrome.exe',headless=True)
  context=browser.new_context(viewport={'width':1440,'height':1000},device_scale_factor=1)
  for source in sources:
   if source['id'] not in wanted or source['status']!='fetched': continue
   page=context.new_page(); record=dict(sourceId=source['id'],url=source['url'],kind='web-page-screenshot',gameScreenshot=False)
   try:
    page.goto(source['url'],wait_until='domcontentloaded',timeout=30000); page.wait_for_timeout(1800)
    # Scroll to let public lazy images load. No cookie/account actions required.
    for y in range(0,min(9000,page.evaluate('document.body.scrollHeight')),800): page.evaluate('(y)=>window.scrollTo(0,y)',y); page.wait_for_timeout(80)
    page.evaluate('window.scrollTo(0,0)'); path=OUT/'screenshots'/f"{source['id']}.png"; page.screenshot(path=str(path),full_page=True,timeout=20000)
    record.update(status='captured',path=path.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),viewport={'width':1440,'height':1000},finalUrl=page.url,title=page.title())
    imgs=page.locator('img').evaluate_all('(xs)=>xs.map(x=>({alt:x.alt,url:x.currentSrc||x.src,width:x.naturalWidth,height:x.naturalHeight}))')
    save(source['id']+'-rendered-images.json',dict(sourceId=source['id'],images=imgs))
    # Preserve selected published screen images separately, clearly marked
    # store marketing or third-party embedded image, not our runtime captures.
    candidates=[x for x in imgs if x['width']>=200 and x['height']>=200 and x['url'].startswith('http')]
    if source['id'] in {'store-google','store-apple','shop-analysis','mechanics-gallery','first-hour','event-backend','arena-fail'}:
     for n,img in enumerate(candidates[:30]):
      try:
       r=requests.get(img['url'],timeout=20); r.raise_for_status()
       if not r.headers.get('content-type','').startswith('image/'): continue
       suffix='.png' if 'png' in r.headers['content-type'] else '.webp' if 'webp' in r.headers['content-type'] else '.jpg'
       dst=OUT/'images'/f"{source['id']}-{n:02}{suffix}"; dst.write_bytes(r.content)
       receipts.append(dict(sourceId=source['id'],url=img['url'],pageUrl=source['url'],kind='published-image',role=source['kind'],gameScreenshot='unreviewed',path=dst.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(r.content).hexdigest(),width=img['width'],height=img['height'],alt=img['alt']))
      except Exception as error: receipts.append(dict(sourceId=source['id'],url=img['url'],status='image-failed',error=str(error)))
   except Exception as error: record.update(status='failed',error=str(error))
   receipts.append(record); page.close()
  context.close(); browser.close()
 save('capture-receipts.json',dict(schemaVersion=1,receipts=receipts,limits='Page screenshots prove pages rendered; published images need visual inspection/classification. No screenshot here is automatically a version0.36.3 runtime capture.'))
 print(json.dumps(dict(captured=sum(r.get('status')=='captured' for r in receipts),publishedImages=sum(r['kind']=='published-image' for r in receipts if 'kind' in r),failures=[r.get('sourceId') for r in receipts if 'failed' in r.get('status','')])) )

if __name__=='__main__':
 import argparse
 parser=argparse.ArgumentParser(); parser.add_argument('--capture',action='store_true'); parser.add_argument('--augment',action='store_true'); parser.add_argument('--only'); args=parser.parse_args()
 if args.capture: captures(args.only)
 elif args.augment: augment()
 else: crawl()
