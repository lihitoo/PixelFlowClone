"""Index every exported file and serialized object without loading heavy assets.

The output is a local reference catalog, not a Unity import list. GUIDs identify
exported-project files; source object IDs and hashes are reused from receipts.
"""
import argparse
import collections
import hashlib
import json
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
DEFAULT_SOURCE = pathlib.Path(r'D:\Downloads\PixelFlow_0.36.3_extracted')
RULES = [
    ('loading', r'loading|splash'), ('shop', r'shop|bundle|offer|no.?ads|coin.?pack|gold.?pack'),
    ('home', r'home|main.?menu|bottom.?bar|navigation'), ('settings', r'setting|toggle|sound|haptic'),
    ('rank', r'leader.?board|ranking|rank_|league|arena|tournament'), ('teams', r'team|clan|chat'),
    ('pass', r'battle.?pass|pixel.?pass|season.?pass|premium'), ('lives', r'lives|life|heart|refill'),
    ('profile', r'profile|avatar|account|nickname'), ('daily', r'daily|calendar|reward'),
    ('events', r'event|fire.?quest|cosmic|temple|medal|race|forest|journey'),
    ('gameplay', r'pig|shooter|pixel|conveyor|booster|powerup|snake|gate|cage|mallet|totem|wood|hammer|spaceship'),
    ('result', r'victory|win_|lose|fail|continue|retry|confetti')]

def roles(name):
    return [role for role, pattern in RULES if re.search(pattern, name, re.I)]

def stable(prefix, path):
    return prefix + hashlib.sha256(path.encode()).hexdigest()[:20]

def write(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, separators=(',', ':')) + '\n', encoding='utf-8')

def build(source, output):
    source = source.resolve(); output = output.resolve()
    report = source / 'reports/unity-objects.jsonl'
    metadata = {}; objects = []; bytes_read = report.stat().st_size
    for line in report.open(encoding='utf-8'):
        item = json.loads(line)
        key = f"{item['asset_file']}:{item['path_id']}"
        record = dict(id=stable('obj-', key), sourceFile=item['asset_file'], pathId=item['path_id'], type=item['type'],
                      name=item.get('name'), extractionStatus=item['status'], files=[], rawSourceUri=None)
        for leaf in item.get('outputs', []):
            path = leaf['path'].replace('\\', '/'); metadata[path] = dict(item, receiptOutput=leaf)
            record['files'].append(stable('asset-', path))
        candidates = sorted((source/'raw').rglob(item['asset_file'])) if item['asset_file'] not in raw_cache else raw_cache[item['asset_file']]
        raw_cache[item['asset_file']] = candidates
        if len(candidates) == 1: record['rawSourceUri'] = candidates[0].resolve().as_uri()
        if item.get('error'): record['error'] = item['error']
        objects.append(record)
    files = []; guids = {}; role_map = collections.defaultdict(list); kind_counts = collections.Counter()
    imported = {}
    for receipt_path, field, source_field in [('asset-intake.json','receipts','source'), ('flow-asset-intake.json','assets','source')]:
        receipt_file = ROOT / 'Docs/Evidence' / receipt_path
        if not receipt_file.exists(): continue
        data = json.loads(receipt_file.read_text(encoding='utf-8'))
        for receipt in data.get(field, []):
            dest = receipt.get('destination') or receipt.get('output')
            original = receipt.get(source_field) or receipt.get('sourcePath')
            if original and dest:
                p = pathlib.Path(original)
                if not p.is_absolute(): p = source / p
                imported[str(p.resolve()).casefold()] = dict(path=dest, uri=(ROOT / dest).resolve().as_uri(), receipt=f'Docs/Evidence/{receipt_path}')
    for folder in [source/'exports', source/'UnityProject/ExportedProject/Assets']:
        for path in sorted(folder.rglob('*')):
            if not path.is_file() or path.suffix == '.meta': continue
            relative = path.relative_to(source).as_posix(); meta = metadata.get(relative, {})
            receipt = meta.get('receiptOutput', {}); tags = roles(path.stem)
            kind = meta.get('type') or (path.relative_to(folder).parts[0] if len(path.relative_to(folder).parts)>1 else path.suffix.lstrip('.'))
            asset_id = stable('asset-', relative); guid = None
            meta_file = path.with_name(path.name + '.meta')
            if meta_file.is_file():
                header = meta_file.open(encoding='utf-8-sig', errors='replace').read(1024); bytes_read += len(header.encode())
                match = re.search(r'^guid: ([a-f0-9]{32})$', header, re.M)
                if match: guid = match.group(1); guids[guid] = asset_id
            obj_key = f"{meta.get('asset_file')}:{meta.get('path_id')}"
            entry = dict(id=asset_id, name=meta.get('name',path.stem), path=relative, uri=path.resolve().as_uri(), bytes=path.stat().st_size,
                         kind=kind, guid=guid, roleCandidates=tags, roleConfidence='filename-only' if tags else 'unclassified',
                         sourceObjectId=stable('obj-',obj_key) if meta else None,
                         sha256=receipt.get('sha256'), hashStatus='existing-extraction-receipt' if receipt.get('sha256') else 'not-read-heavy-asset',
                         imported=imported.get(str(path.resolve()).casefold()), selectedForRuntime=str(path.resolve()).casefold() in imported)
            if meta.get('image_size'): entry['imageSize']=meta['image_size']
            if meta.get('duration_seconds') is not None: entry['durationSeconds']=meta['duration_seconds']
            # Namespaced scripts are structure stubs, not recovered behavior.
            entry['recoveredGameplayLogic'] = False
            files.append(entry); kind_counts[kind] += 1
            for tag in tags: role_map[tag].append(asset_id)
    write(output / 'assets.json', dict(schemaVersion=1, sourceRoot=str(source), records=files))
    write(output / 'objects.json', dict(schemaVersion=1, sourceReceipt=report.as_uri(), records=objects))
    write(output / 'guid-index.json', dict(schemaVersion=1, guids=guids))
    write(output / 'role-index.json', dict(schemaVersion=1, confidence='Candidate tags inferred from filenames; inspect before binding.', roles=role_map))
    manifest = dict(schemaVersion=1, sourceVersion='0.36.3', sourcePackageSha256='ad7b8a8f7659ec86a583976e466458b69fd58f08f2667f6f17925fa570e9bad2',
                    sourceRoot=str(source), files=len(files), serializedObjects=len(objects), guidCount=len(guids), kinds=dict(kind_counts),
                    roleCandidateCounts={k:len(v) for k,v in role_map.items()}, metadataBytesRead=bytes_read, heavyAssetBodiesRead=0,
                    catalogs={name:dict(path=name,uri=(output/name).as_uri(),sha256=hashlib.sha256((output/name).read_bytes()).hexdigest()) for name in ['assets.json','objects.json','guid-index.json','role-index.json']},
                    meaning='One link per exported asset file, source-object records including unexported/failing objects, GUID lookup and candidate roles. Metadata/file existence is not runtime integration or exact-clone completion.')
    write(output / 'manifest.json',manifest)
    print(json.dumps({k:manifest[k] for k in ['files','serializedObjects','guidCount','roleCandidateCounts','heavyAssetBodiesRead']}))

def verify(output):
    output=output.resolve(); manifest=json.loads((output/'manifest.json').read_text()); assets=json.loads((output/'assets.json').read_text())['records']
    ids={x['id'] for x in assets}; errors=[]
    if len(ids)!=len(assets): errors.append('duplicate asset IDs')
    from urllib.request import url2pathname
    from urllib.parse import urlparse
    for record in assets:
        if not pathlib.Path(url2pathname(urlparse(record['uri']).path)).is_file(): errors.append(record['path'])
        if record['imported'] and not (ROOT/record['imported']['path']).is_file(): errors.append('missing imported '+record['imported']['path'])
    for name, item in manifest['catalogs'].items():
        if hashlib.sha256((output/name).read_bytes()).hexdigest()!=item['sha256']: errors.append('changed catalog '+name)
    objects=json.loads((output/'objects.json').read_text())['records']
    for obj in objects:
        if any(x not in ids for x in obj['files']): errors.append('unresolved object '+obj['id'])
    result=dict(status='pass' if not errors else 'fail', files=len(assets),objects=len(objects),errors=errors)
    write(output/'validation.json', result); print(json.dumps(result)); return bool(errors)

raw_cache={}
if __name__=='__main__':
    parser=argparse.ArgumentParser(); parser.add_argument('--source',type=pathlib.Path,default=DEFAULT_SOURCE)
    parser.add_argument('--output',type=pathlib.Path,default=ROOT/'Docs/AssetCatalog'); parser.add_argument('--verify',action='store_true')
    args=parser.parse_args()
    if args.verify: raise SystemExit(verify(args.output))
    build(args.source,args.output); raise SystemExit(verify(args.output))
