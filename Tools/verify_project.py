"""Read-only ownership, source-receipt and local artifact checks; no gameplay claim."""
import hashlib
import json
import sys
from pathlib import Path


def sha(path):
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(chunk)
    return digest.hexdigest()


root = Path(__file__).resolve().parents[1]
owned = root / 'Assets/_Project/PixelFlow'
failures = []
for path in (owned / 'Runtime').rglob('*.cs'):
    if 'using UnityEditor' in path.read_text(encoding='utf-8'):
        failures.append(f'Runtime imports Editor: {path.relative_to(root)}')
for path in owned.rglob('*.asmdef'):
    record = json.loads(path.read_text(encoding='utf-8'))
    if record['name'] in {'PixelFlow.Runtime', 'PixelFlow.Rules'} and any('Editor' in reference for reference in record.get('references', [])):
        failures.append('Runtime references Editor assembly')
    if not path.with_name(path.name + '.meta').is_file(): failures.append('Missing assembly meta: ' + path.name)
intake = json.loads((root / 'Docs/Evidence/asset-intake.json').read_text(encoding='utf-8'))
verified = 0
for receipt in intake['receipts']:
    if 'output' not in receipt: continue
    path = root / receipt['output']
    if not path.is_file() or sha(path) != receipt['sha256']: failures.append('Changed imported source: ' + receipt['output'])
    else: verified += 1
campaign = owned / 'Resources/PixelFlow/pixelFlowCampaign.json'
for ui_area in ('Home', 'Shop'):
    ui_receipt = root / f'Docs/ExactClone/{ui_area}/{ui_area.lower()}-asset-intake.json'
    if not ui_receipt.exists(): continue
    for receipt in json.loads(ui_receipt.read_text(encoding='utf-8'))['receipts']:
        path = root / receipt['output']
        if not path.is_file() or sha(path) != receipt['sha256']:
            failures.append(f'Changed {ui_area} asset: ' + receipt['output'])
            continue
        meta = path.with_name(path.name + '.meta')
        guid = receipt.get('importedGuid')
        if not guid or not meta.is_file() or ('guid: ' + guid) not in meta.read_text(encoding='utf8'):
            failures.append(f'Unresolved {ui_area} asset GUID: ' + receipt['role'])
        else:
            verified += 1
flow_receipt = root / 'Docs/Evidence/flow-asset-intake.json'
if flow_receipt.exists():
    for receipt in json.loads(flow_receipt.read_text(encoding='utf-8'))['assets']:
        path = root / receipt['destination']
        if not path.is_file() or sha(path) != receipt['sha256']: failures.append('Changed flow asset: ' + receipt['destination'])
        else: verified += 1
expanded = root / 'Docs/Evidence/campaign-import.json'
if expanded.exists():
    catalog = json.loads(expanded.read_text(encoding='utf-8'))
    if sha(campaign) != catalog['campaignSha256']: failures.append('Campaign differs from full import receipt')
    for chunk in catalog['chunks']:
        path = root / chunk['path']
        if not path.is_file() or sha(path) != chunk['sha256']: failures.append('Changed campaign chunk: ' + chunk['path'])
elif sha(campaign) != intake['campaignSha256']: failures.append('Campaign differs from intake receipt')
sources = [{'path': p.relative_to(root).as_posix(), 'sha256': sha(p)} for p in sorted(owned.rglob('*')) if p.is_file() and p.suffix in {'.cs', '.asmdef', '.json', '.bytes', '.unity', '.mat', '.asset'}]
report = {'status': 'pass' if not failures else 'fail', 'importedFilesVerified': verified, 'failures': failures,
          'sourceFingerprint': hashlib.sha256(json.dumps(sources, sort_keys=True).encode()).hexdigest(), 'sources': sources,
          'scope': 'Owned runtime/editor boundaries and selected source/output identity; Unity tests, rendering and player interaction are separate reports.'}
print(json.dumps(report, indent=2))
sys.exit(bool(failures))
