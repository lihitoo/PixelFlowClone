"""Read selected exported Unity YAML as evidence; never mutate/import it."""
import collections,hashlib,json,pathlib,re
ROOT=pathlib.Path(__file__).resolve().parents[1]
SOURCE=pathlib.Path(r'D:\Downloads\PixelFlow_0.36.3_extracted')
ASSETS=SOURCE/'UnityProject/ExportedProject/Assets'
OUT=ROOT/'Docs/ExactClone'
NAMES=['Settings View Variant','Privacy Settings View','Shop Page Variant','UI Shop','Shop Gold View','Shop No Ads View',
       'Shop Bundle View No Ads Variant','Shop Bundle View 2 Starter Variant','Shop Bundle View Fail Offer Variant',
       'Leaderboard Page Variant','Leaderboard','Leaderboard View Entry','Pixel Arena Leaderboard View Entry',
       'Battle Pass Info View','Battle Pass Golden Ticket View']

def main():
 catalog=json.loads((ROOT/'Docs/AssetCatalog/assets.json').read_text(encoding='utf8'))['records']
 by_guid={r['guid']:r for r in catalog if r['guid']}; by_path={r['path']:r for r in catalog}
 paths=[ASSETS/'GameObject'/f'{name}.prefab' for name in NAMES]
 paths += [ASSETS/'Core/Systems/Load/Scenes/Boot.unity',ASSETS/'Game/Scenes/Home.unity',ASSETS/'Game/Scenes/Gameplay.unity']
 paths += [p for p in (ASSETS/'MonoBehaviour').glob('*.asset') if any(t in p.stem.lower() for t in ['leaderboard_config','heart_generator_settings_json','battle_pass_golden_ticket_heart_generator','splash sprite'])]
 results=[]
 for path in paths:
  if not path.is_file(): continue
  data=path.read_text(encoding='utf8'); relative=path.relative_to(SOURCE).as_posix()
  names=re.findall(r'^  m_Name: (.+)$',data,re.M)
  texts=re.findall(r'^  (?:m_text|m_Text|text|_text|m_textValue): (.+)$',data,re.M)
  dependencies=[]
  for guid in sorted(set(re.findall(r'guid: ([a-f0-9]{32})',data))):
   asset=by_guid.get(guid)
   builtin=guid in {'0000000000000000e000000000000000','0000000000000000f000000000000000'}
   dependencies.append(dict(guid=guid,assetId=asset['id'] if asset else None,path=asset['path'] if asset else None,uri=asset['uri'] if asset else None,kind=asset['kind'] if asset else None,resolution='exported-file' if asset else 'unity-builtin-reference' if builtin else 'unresolved'))
  docs=[]
  for block in re.split(r'^--- ',data,flags=re.M)[1:]:
   head=block.splitlines()[0]; name=re.search(r'^  m_Name: (.*)$',block,re.M)
   # Capture geometry and presentation references only, not invented behavior.
   fields={}
   for key in ['m_AnchorMin','m_AnchorMax','m_AnchoredPosition','m_SizeDelta','m_Pivot','m_Color','m_FontSize','m_fontSize','m_Text','m_text','m_textValue','m_Sprite','m_fontAsset','m_FontAsset']:
    match=re.search(r'^  '+key+r': (.*)$',block,re.M)
    if match: fields[key]=match.group(1)
   if fields: docs.append(dict(header=head,name=name.group(1) if name else None,fields=fields))
  results.append(dict(assetId=by_path.get(relative,{}).get('id'),path=relative,uri=path.resolve().as_uri(),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),nodeNames=names,literalTexts=texts,dependencies=dependencies,presentationComponents=docs,interpretation='Serialized structure/assets only. Scene/prefab presence does not prove unlock thresholds, active remote config, native callbacks or source runtime layout.'))
 OUT.mkdir(parents=True,exist_ok=True)
 (OUT/'source-ui-structure.json').write_text(json.dumps(dict(schemaVersion=1,sourceVersion='0.36.3',records=results),ensure_ascii=False,indent=2)+'\n',encoding='utf8')
 print(json.dumps(dict(files=len(results),referencedAssets=len({d['guid'] for r in results for d in r['dependencies']}),missingGuidReferences=sum(d['resolution']=='unresolved' for r in results for d in r['dependencies']),builtinReferenceOccurrences=sum(d['resolution']=='unity-builtin-reference' for r in results for d in r['dependencies']),nodes=sum(len(r['nodeNames']) for r in results))))
if __name__=='__main__': main()
