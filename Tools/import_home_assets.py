"""Select original Home sprites; never import dummy code or whole export."""
import hashlib,json,pathlib,shutil,re
ROOT=pathlib.Path(__file__).resolve().parents[1];SOURCE=pathlib.Path(r'D:\Downloads\PixelFlow_0.36.3_extracted')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 candidates=json.loads((ROOT/'Docs/ExactClone/Home/asset-candidates.json').read_text(encoding='utf8'))
 all_assets=json.loads((ROOT/'Docs/AssetCatalog/assets.json').read_text(encoding='utf8'))['records']
 indices={'BottomBar':2,'TabSelected':4,'Coin':5,'Heart':6,'HeartInfinite':7,'Lock':8,'CurrentBlue':9,'CurrentPurple':10,'CurrentRed':11,
          'LabelPurple':13,'LabelRed':15,'NodeBlue':19,'NodeOrange':20,'NodeRed':21,'NodePurple':22,'Column':24,'StartPig':25,
          'EventBlue':26,'EventGreen':27,'EventArena':28,'EventRed':29,'Play':33,'Background':39,'Gift':41,'Leaderboard':47,
          'NoAds':54,'Settings':57,'Shop':58,'CurrentBlack':59,'NodeHard':60,'LabelHard':61}
 selected={name:candidates[i] for name,i in indices.items()}
 exact={'Pattern':'PixelFlow_Pattern','Plus':'Plus_Yellow','SmallPlus':'Plus_Green','LabelDark':'Frame_Label_Rounded_Dark','BattlePass':'Icon_BattlePass_GoldenTicket','Arena':'Pixel Arena Icon','FireQuest':'Icon_LavaQuest_Button',
        'Popup':'Popup_Plain_Blue','PopupHeader':'Popup_With_Header_Blue','Close':'Button_Close_Popup','SliderBackground':'Slider_Bg','SliderFill':'Slider_Fill_Pink','SliderHandle':'Slider_Handle','ToggleBackground':'Toggle_Bg','ToggleHandle':'Toggle_Handle','ToggleOn':'Toggle_On_Inside'}
 for name,original in exact.items():selected[name]=next(a for a in all_assets if a['path'].startswith('exports/Sprite/') and a['name']==original)
 dest=ROOT/'Assets/_Project/PixelFlow/Resources/PixelFlow/UI/Home';dest.mkdir(parents=True,exist_ok=True);receipts=[]
 for name,asset in selected.items():
  src=SOURCE/asset['path'];dst=dest/(name+'.png')
  if dst.exists() and sha(dst)!=sha(src):raise ValueError('Refusing to overwrite changed original derivative: '+str(dst))
  if not dst.exists():shutil.copyfile(src,dst)
  source_meta=SOURCE/'UnityProject/ExportedProject/Assets/Sprite'/(asset['name']+'.asset');border=[0,0,0,0]
  if source_meta.is_file():
   match=re.search(r'm_Border: \{x: ([\d.]+), y: ([\d.]+), z: ([\d.]+), w: ([\d.]+)\}',source_meta.read_text(encoding='utf8'))
   if match:border=[float(x) for x in match.groups()]
  receipts.append(dict(role=name,assetId=asset['id'],source=asset['path'],sourceUri=src.as_uri(),sourceSha256=sha(src),output=dst.relative_to(ROOT).as_posix(),sha256=sha(dst),bytes=dst.stat().st_size,sourceBorder=border,route='original-selected',importedGuid=None))
 report=dict(schemaVersion=1,targetVersion='0.36.3',sourcePackageSha256='ad7b8a8f7659ec86a583976e466458b69fd58f08f2667f6f17925fa570e9bad2',receipts=receipts)
 (ROOT/'Docs/ExactClone/Home/home-asset-intake.json').write_text(json.dumps(report,indent=2),encoding='utf8')
 print(json.dumps(dict(sprites=len(receipts),bytes=sum(x['bytes'] for x in receipts))))
if __name__=='__main__':main()
