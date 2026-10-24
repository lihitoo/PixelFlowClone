"""Validate preparation integrity and report fidelity gates independently."""
import hashlib,json,pathlib
from urllib.parse import urlparse
from urllib.request import url2pathname
ROOT=pathlib.Path(__file__).resolve().parents[1]; OUT=ROOT/'Docs/ExactClone'
def main():
 contract=json.loads((OUT/'screen-contracts.json').read_text(encoding='utf8'))
 sources=json.loads((OUT/'Research/sources.json').read_text(encoding='utf8'))['sources']
 images=json.loads((OUT/'visual-evidence.json').read_text(encoding='utf8'))['images']
 pairs=json.loads((OUT/'comparisons.json').read_text(encoding='utf8'))['pairs']
 catalog=json.loads((ROOT/'Docs/AssetCatalog/assets.json').read_text(encoding='utf8'))['records']
 asset_ids={x['id'] for x in catalog}; image_ids={x['id'] for x in images}; source_ids={x['id'] for x in sources}|{'source-ui'}
 errors=[]; gaps=[]
 if len(image_ids)!=len(images): errors.append('duplicate image IDs')
 ids=[x['id'] for x in contract['screens']]
 if len(set(ids))!=len(ids): errors.append('duplicate screen IDs')
 for screen in contract['screens']:
  for field in ['entry','requiredBehavior','requiredStates','sourceIds','assetCandidates','currentCloneGap','acceptance']:
   if not screen.get(field): errors.append(f"{screen['id']}: missing {field}")
  for key in screen['sourceIds']:
   if key not in source_ids: errors.append(f"{screen['id']}: unknown source {key}")
  for key in screen['visualReferenceIds']:
   if key not in image_ids: errors.append(f"{screen['id']}: unknown image {key}")
  for asset in screen['assetCandidates']:
   if asset['id'] not in asset_ids: errors.append(f"{screen['id']}: unknown asset {asset['id']}")
   if not pathlib.Path(url2pathname(urlparse(asset['uri']).path)).is_file(): errors.append(f"{screen['id']}: broken asset link {asset['path']}")
  gaps.append(dict(id=screen['id'],missingScreenshot=not screen['visualReferenceIds'],targetVersionVerification='pending',implementation=screen['currentCloneGap']))
 for image in images:
  path=ROOT/image['path']
  if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest()!=image['sha256']: errors.append('broken/changed image '+image['id'])
 for pair in pairs:
  for field in ['original','clone']:
   if not pair[field]: continue
   path=ROOT/pair[field]
   if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest()!=pair[field+'Sha256']: errors.append('broken comparison '+pair['id'])
 structure=json.loads((OUT/'source-ui-structure.json').read_text(encoding='utf8'))['records']
 unresolved=sorted({d['guid'] for r in structure for d in r['dependencies'] if d.get('resolution')=='unresolved'})
 builtins=sorted({d['guid'] for r in structure for d in r['dependencies'] if d.get('resolution')=='unity-builtin-reference'})
 report=dict(schemaVersion=1,integrity='pass' if not errors else 'fail',errors=errors,surfaces=len(ids),sources=len(sources),
   assets=len(catalog),assetCatalogValidation=json.loads((ROOT/'Docs/AssetCatalog/validation.json').read_text(encoding='utf8')),
   sourceUiFiles=len(structure),unresolvedSourceGuids=unresolved,builtinSourceGuids=builtins,comparisonPairs=len(pairs),sourceScreenshotsMissing=sum(g['missingScreenshot'] for g in gaps),
   gates=dict(researchIntegrity=not errors,briefPrepared=True,exactParityReady=False,fullGameImplemented=False,fullCloneComplete=False),
   gateMeaning='Integrity is independent of completeness. Preparation ran on the real Pixel Flow case and found concrete gaps. Missing target-version screenshots/behavior/service evidence cannot be converted into a faithful/full clone claim.',
   neededEditorMode=dict(openedUnity=False,usedUnityMcp=False,reason='Metadata/web/previous clone capture audit only. Notify owner before switching to interactive Unity/MCP for binding and actual Game View comparison.',availableMcp='No Unity MCP tool exposed in this session.'),
   buildPolicy='No new player builds.',gaps=gaps)
 (OUT/'validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
 print(json.dumps({k:report[k] for k in ['integrity','errors','surfaces','sources','assets','sourceScreenshotsMissing','comparisonPairs','gates']})); return bool(errors)
if __name__=='__main__': raise SystemExit(main())
