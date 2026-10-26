"""Selected original Shop art only; metadata first, no dummy scripts/whole atlas."""
import argparse,hashlib,json,pathlib,re,shutil
ROOT=pathlib.Path(__file__).resolve().parents[1]
SOURCE=pathlib.Path('D:/Downloads/PixelFlow_0.36.3_extracted')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 parser=argparse.ArgumentParser(description=__doc__)
 parser.add_argument('--upgrade-resolution',action='store_true',help='Replace only outputs that still match their recorded source receipt.')
 options=parser.parse_args()
 catalog=json.loads((ROOT/'Docs/AssetCatalog/assets.json').read_text(encoding='utf8'))['records']
 names={'Roof':'Shop_Roof','Section':'Shop_Section_Title','GoldCard':'Frame_Item_Card_Yellow','Price':'Button_Green_Rounder_XS',
 'BundleBlue':'Frame_Bundle_Blue','ContentBlue':'Frame_Bundle_Content_Blue','BundlePurple':'Frame_Bundle_Purple','ContentPurple':'Frame_Bundle_Content_Purple',
 'BundleYellow':'Frame_Bundle_Yellow','ContentYellow':'Frame_Bundle_Content_Yellow','SectionBlue':'Frame_Bundle_Content_Section_Blue','SectionPurple':'Frame_Bundle_Content_Section_Purple','SectionYellow':'Frame_Bundle_Content_Section_Yellow',
 'Popular':'Label_Badge01_Green','NoAds':'Icon_NoAds_2x','Tray':'IconPowerup_ExtraSlot_Colored','Hand':'IconPowerup_Hand_Colored','Shuffle':'IconPowerup_Shuffle_Colored','Super':'IconPowerup_SuperShooter_Colored'}
 names.update({f'Gold{i}':f'Golds_{i}_2x' for i in range(6)})
 receipt_path=ROOT/'Docs/ExactClone/Shop/shop-asset-intake.json'
 previous={r['role']:r for r in json.loads(receipt_path.read_text(encoding='utf8'))['receipts']} if receipt_path.exists() else {}
 dest=ROOT/'Assets/_Project/PixelFlow/Resources/PixelFlow/UI/Shop';dest.mkdir(parents=True,exist_ok=True);receipts=[]
 for role,name in names.items():
  asset=next(x for x in catalog if x['path'].startswith('exports/Sprite/') and x['name']==name)
  src=SOURCE/asset['path'];dst=dest/(role+'.png')
  old=previous.get(role)
  replacing=dst.exists() and sha(dst)!=sha(src)
  if replacing:
   if not options.upgrade_resolution or old is None or sha(dst)!=old['sha256']:raise ValueError('Changed/unapproved derivative: '+str(dst))
  if not dst.exists() or replacing:shutil.copyfile(src,dst)
  sprite=SOURCE/'UnityProject/ExportedProject/Assets/Sprite'/(name+'.asset');border=[0]*4
  if sprite.exists():
   m=re.search(r'm_Border: \{x: ([\d.]+), y: ([\d.]+), z: ([\d.]+), w: ([\d.]+)\}',sprite.read_text(encoding='utf8'))
   if m:border=list(map(float,m.groups()))
  meta=dst.with_name(dst.name+'.meta');guid=re.search(r'^guid: (\w+)',meta.read_text(),re.M)[1] if meta.exists() else None
  receipts.append(dict(role=role,assetId=asset['id'],source=asset['path'],sourceUri=src.as_uri(),sourceSha256=sha(src),output=dst.relative_to(ROOT).as_posix(),sha256=sha(dst),bytes=dst.stat().st_size,sourceBorder=border,importedGuid=guid))
  if replacing:receipts[-1]['previousSource']={k:old[k] for k in ('assetId','source','sha256')}
  elif old and 'previousSource' in old:receipts[-1]['previousSource']=old['previousSource']
 out=ROOT/'Docs/ExactClone/Shop';out.mkdir(parents=True,exist_ok=True)
 (out/'shop-asset-intake.json').write_text(json.dumps(dict(schemaVersion=1,targetVersion='0.36.3',receipts=receipts),indent=2),encoding='utf8')
 print(json.dumps({'sprites':len(receipts),'bytes':sum(x['bytes'] for x in receipts)}))
if __name__=='__main__':main()
