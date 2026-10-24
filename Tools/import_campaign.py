"""Normalize the selected main campaign into bounded, lazy-loaded JSON chunks."""
import argparse
from collections import Counter
import hashlib
import gzip
import json
from pathlib import Path

FUNNEL = '3db5681ee587345ad9a3cf8400a692d1'
KINDS = {'keys':'Key','gates':'Gate','pixelPipes':'PixelPipe','snakes':'Snake','walls':'Wall','eggBoxes':'EggBox',
         'pixelIceBlocks':'Ice','pixelWoodBlocks':'Wood','pixelColorDoors':'ColorDoor','biscuits':'Biscuit',
         'splitObjects':'Split','ufos':'Ufo','pumpkins':'Pumpkin','curtains':'Curtain','shooterCages':'ShooterCage',
         'multiSnakes':'MultiSnake','beanBoxes':'BeanBox','musicToys':'MusicToy','crossbows':'Crossbow',
         'accordions':'Accordion','coloredShooterCages':'ColoredCage','matryoshkas':'Matryoshka',
         'spaceships':'Spaceship','pixelMixers':'Mixer'}
GROUPS = {'SurpriseShooters':'Shooters','ConnectedShooters':'Connections','Locks':'Shooters','ShooterPipes':'Pipes',
          'Hammers':'Shooters','ShooterIceBlocks':'IceBlocks','ChainedShooters':'Chains','BullTotems':'Totems',
          'MusicToyMallets':'Mallets','SlotCages':'Cages','AstronautShooters':'Shooters','ConveyorDoors':'Doors'}


def digest(data): return hashlib.sha256(data).hexdigest()


def points(value):
    if isinstance(value, list):
        for item in value: yield from points(item)
    elif isinstance(value, dict):
        if 'X' in value and 'Y' in value: yield {'x': value['X'], 'y': value['Y']}
        else:
            for key in ['GridPoints','MainGridPoints','SnakeGridPoints','TailGridPoints']:
                if key in value: yield from points(value[key])


def layer(color=-1, health=1): return {'color':int(color), 'health':max(1,int(health))}


def normalize(number, raw, source_hash, title=None):
    image=raw['PixelImageData']; features=[]
    for key in raw:
        if key in GROUPS and raw[key].get(GROUPS[key]): features.append(key)
        elif key not in GROUPS and key not in {'Difficulty','HasTimeLimit','TimeLimit','SlotCount','ConveyorLimit','QueueGroup','PixelImageData','contentHash','isValid','validationErrors'}:
            raise ValueError(f'Unknown level field {number}/{key}')
    health={(p['x'],p['y']):p['health'] for p in image.get('pixelHealths',[])}
    hidden={(p['X'],p['Y']) for p in image.get('surprisePixels',{}).get('Pixels',[])}
    pixels=[]
    for p in image['pixels']:
        value=dict(x=p['x'],y=p['y'],color=p['material'])
        if p.get('areaX',1)!=1:value['spanX']=p['areaX']
        if p.get('areaY',1)!=1:value['spanY']=p['areaY']
        if health.get((p['x'],p['y']),1)!=1:value['health']=health[(p['x'],p['y'])]
        if (p['x'],p['y']) in hidden:value['hidden']=True
        pixels.append(value)
    if health: features.append('HardPixels')
    if hidden: features.append('SurprisePixels')
    pipes={p['ShooterId']:p['Queue']['shooters'] for p in raw.get('ShooterPipes',{}).get('Pipes',[])}
    anchor_map={}; states={}
    locks=set(raw.get('Locks',{}).get('Shooters',[])); hammers=set(raw.get('Hammers',{}).get('Shooters',[]))
    surprise=set(raw.get('SurpriseShooters',{}).get('Shooters',[])); astronauts=set(raw.get('AstronautShooters',{}).get('Shooters',[]))
    ice={x['ShooterId']:x['Health'] for x in raw.get('ShooterIceBlocks',{}).get('IceBlocks',[])}
    totems={x['ShooterId']:x['Count'] for x in raw.get('BullTotems',{}).get('Totems',[])}
    mallets={x['ShooterId']:x['Material'] for x in raw.get('MusicToyMallets',{}).get('Mallets',[])}
    def shooter(p, pipe_id=-1):
        ident=p['id']; kind='Lock' if ident in locks else 'Hammer' if ident in hammers else 'Totem' if ident in totems else 'Mallet' if ident in mallets else 'Normal'
        result=dict(id=ident,ammo=p['ammo'] if kind=='Normal' else 0,color=p['material'],kind=kind,surprise=ident in surprise,astronaut=ident in astronauts,ice=ice.get(ident,0),counter=totems.get(ident,0),featureColor=mallets.get(ident,-1),pipeId=pipe_id)
        if ident in states: raise ValueError(f'Duplicate shooter {number}/{ident}')
        states[ident]=result;return result
    def expand(p, origin=-1, chain=()):
        ident=p['id']
        if ident in pipes:
            if ident in chain: raise ValueError('Cyclic shooter pipe')
            result=[item for child in pipes[ident] for item in expand(child,ident,chain+(ident,))]
            if not result: raise ValueError('Empty shooter pipe')
            anchor_map[ident]=result[0]['id']; return result
        return [shooter(p,origin)]
    queues=[{'shooters':[item for p in q['shooters'] for item in expand(p)]} for q in raw['QueueGroup']['shooterQueues']]
    objects=[]
    for field,value in image.items():
        if field in {'pixels','pixelHealths','surprisePixels','Preview','contentHash','width','height','physicalWidth','physicalHeight'}: continue
        items=next(iter(value.values()),[]) if isinstance(value,dict) else value
        if not items: continue
        if field not in KINDS: raise ValueError(f'Unknown populated board field {number}/{field}')
        kind=KINDS[field];features.append(kind)
        for item in items:
            cells=list(points(item));unique={(p['x'],p['y']):p for p in cells};cells=list(unique.values())
            if not cells: raise ValueError(f'Missing object cells {number}/{kind}')
            obj=dict(id=len(objects),kind=kind,cells=cells,goal=False,layers=[layer()],direction=item.get('Direction',0),count=1,featureColor=item.get('Material',-1))
            if kind in {'Gate','Snake'}: obj.update(goal=True,layers=[layer(item['Material'],item['Count'])],count=item.get('Length',item['Count']))
            elif kind=='PixelPipe': obj.update(goal=True,layers=[layer(v['Material'],v['Count']) for v in item['Queue']])
            elif kind=='EggBox': obj.update(goal=True,layers=[layer(v['Material'],v['Count']) for v in item['Eggs']])
            elif kind in {'Ice','Wood','ColorDoor','Mixer'}: obj['layers']=[layer(item.get('Material',-1),item['Health'])]
            elif kind=='Biscuit': obj['layers']=[layer(-1,item['LayerHealth']) for _ in range(item['LayerCount'])]
            elif kind=='Split': obj.update(layers=[layer(-1,len(item['GridPoints']))],count=item.get('SegmentDecreasePerHit',1))
            elif kind=='Ufo': obj['layers']=[layer(v['Material']) for v in item['Seats']]
            elif kind=='Pumpkin': obj.update(goal=True,layers=[layer(v['MaterialId'],v['Health']) for v in item['Pumpkins']])
            elif kind in {'Curtain','MusicToy'}: obj['layers']=[layer(v['Material'],v['Health']) for v in item['Segments']]
            elif kind=='MultiSnake': obj.update(goal=True,layers=[layer(v['Material'],v['Count']) for v in item['Segments']])
            elif kind=='BeanBox': obj.update(goal=True,layers=[layer(v['MaterialId'],v['TotalHealth']) for v in item['Groups']],count=item['BeanCount'])
            elif kind=='Crossbow': obj.update(goal=True,layers=[layer(item['Material'],item['Health'])],count=item['Area'])
            elif kind=='Accordion': obj['layers']=[layer(item['Material'],item['HpPerShrink']+item['LastShrinkBonus'])]
            elif kind=='Matryoshka': obj.update(goal=True,layers=[layer(v['MaterialId'],v['Health']) for v in item['Layers']])
            elif kind=='Spaceship': obj['layers']=[layer(item['Material'])]
            elif kind=='ColoredCage': obj.update(goal=True,layers=[layer(item['Material'],item['Health'])])
            if 'CagedShooter' in item: obj['release']=shooter(item['CagedShooter'])
            objects.append(obj)
    links=[]
    for field,chain in [('ConnectedShooters',False),('ChainedShooters',True)]:
        entries=raw.get(field,{}).get('Chains' if chain else 'Connections',[])
        for g in entries:
            members=list(dict.fromkeys(anchor_map.get(i,i) for i in g['Shooters']))
            if len(members)<2 or not all(i in states for i in members): raise ValueError(f'Invalid link {number}/{g}')
            links.append(dict(id=g['Id'],chain=chain,members=members))
    result=dict(sourceNumber=number,sourceHash=source_hash,title=title or f'Flow {number}',difficulty=raw['Difficulty'],width=image['width'],height=image['height'],slotCount=raw['SlotCount'],conveyorLimit=raw['ConveyorLimit'],timeLimit=raw['TimeLimit'] if raw.get('HasTimeLimit') else 0,pixels=pixels,queues=queues,links=links,objects=objects,slotCages=[dict(slot=x['SlotIndex'],color=x['Material'],health=x['Health']) for x in raw.get('SlotCages',{}).get('Cages',[])],conveyorDoors=[dict(progress=x['Percent'],color=x['Material'],health=x['Health']) for x in raw.get('ConveyorDoors',{}).get('Doors',[])])
    return result,list(dict.fromkeys(features))


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('source',type=Path);args=parser.parse_args()
    project=Path(__file__).resolve().parents[1]; root=args.source.resolve()/FUNNEL
    config_path=project/'Assets/_Project/PixelFlow/Resources/PixelFlow/pixelFlowCampaign.json'
    current=json.loads(config_path.read_text()); original_titles={l['sourceNumber']:l['title'] for l in current['levels']}
    chunks=project/'Assets/_Project/PixelFlow/Resources/PixelFlow/Levels';chunks.mkdir(exist_ok=True)
    catalog=[];batch=[];receipts=[];sources=[];counts=Counter();first=[]
    for number in range(1,2601):
        path=root/f'{number}.json';data=path.read_bytes();definition,features=normalize(number,json.loads(data),digest(data),original_titles.get(number))
        chunk=(number-1)//100
        catalog.append(dict(sourceNumber=number,title=definition['title'],difficulty=definition['difficulty'],chunk=f'Levels/campaign-{chunk:02}',features=features,sourceHash=definition['sourceHash']))
        counts.update(features);batch.append(definition);sources.append(dict(level=number,sha256=definition['sourceHash']))
        if number<=7:first.append(definition)
        if number%100==0:
            output=chunks/f'campaign-{chunk:02}.bytes';decoded=(json.dumps({'levels':batch},separators=(',',':'))+'\n').encode();encoded=gzip.compress(decoded,mtime=0)
            output.write_bytes(encoded);receipts.append(dict(path=output.relative_to(project).as_posix(),sha256=digest(encoded),decodedBytes=len(decoded),levels=len(batch)));batch=[]
    current.update(schemaVersion=2,levels=first,entries=catalog)
    payload=(json.dumps(current,separators=(',',':'))+'\n').encode();config_path.write_bytes(payload)
    report=dict(schemaVersion=2,scope='Main campaign normalized with explicit typed features; native semantics/tuning equivalence is not established.',levels=2600,chunks=receipts,featureCounts=dict(counts),sourceLevels=sources,campaignSha256=digest(payload),sourceManifestSha256=digest((args.source/'manifest.json').read_bytes()))
    (project/'Docs/Evidence/campaign-import.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps({'levels':2600,'chunks':len(receipts),'featureFamilies':len(counts),'normalizedBytes':sum((project/x['path']).stat().st_size for x in receipts)}))


if __name__=='__main__':main()
