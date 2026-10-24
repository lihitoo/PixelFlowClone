"""Import a selected, hash-receipted Pixel Flow sample; leaves the source extraction intact."""
import argparse
import hashlib
import json
import shutil
from pathlib import Path

PALETTE = ['Blue', 'DarkBlue', 'Black', 'Green', 'Orange', 'Pink', 'Purple', 'Red', 'Turquoise', 'Yellow', 'White', 'Brown', 'DarkGreen', 'Emerald', 'Lilac', 'Sage', 'Salmon', 'Lavender', 'Terracotta', 'Parmesan', 'Brick', 'Gray', 'DarkGray', 'DarkPurple', 'GypsyPink', 'PowderPink', 'PastelOrange', 'Burgundy', 'Olive', 'DustyTeal', 'MilkyBrown', 'Toffee', 'NeonPurple', 'SharkGray']


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--extracted-root', type=Path, required=True)
    args = parser.parse_args()
    source = args.extracted_root.resolve(strict=True)
    project = Path(__file__).resolve().parents[1]
    root = project / 'Assets/_Project/PixelFlow'
    root.mkdir(parents=True, exist_ok=True)
    receipts = []

    def copy(group, pattern, target):
        matches = list((source / 'exports' / group).rglob(pattern))
        if len(matches) != 1:
            raise ValueError(f'Expected one {group}/{pattern}, got {len(matches)}')
        original = matches[0]
        destination = root / target
        if destination.exists() and sha(destination) != sha(original):
            raise ValueError(f'Refusing to overwrite modified asset: {destination}')
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(original, destination)
        receipts.append({'source': original.relative_to(source).as_posix(), 'output': destination.relative_to(project).as_posix(), 'bytes': destination.stat().st_size, 'sha256': sha(destination)})

    for group, pattern, target in [
        ('Mesh', 'Cube.011__1296.obj', 'Art/Pig.obj'),
        ('Texture2D', 'Texture_Shooter__1049.png', 'Art/Pig.png'),
        ('Sprite', 'PixelFlow Logo__*.png', 'Art/Logo.png'),
        ('Sprite', 'Button_Green__1205.png', 'Art/PlayButton.png'),
        ('Sprite', 'Button_Blue__843.png', 'Art/BlueButton.png'),
        ('Sprite', 'PiggyNoOutline__2245.png', 'Art/HeroPig.png'),
        ('Font', 'Roboto-Bold__659.ttf', 'Art/RobotoBold.ttf'),
        ('AudioClip', 'Button_Tap_Pop__1787__0.wav', 'Audio/Tap.wav'),
        ('AudioClip', 'bean_hit_thock__1761__0.wav', 'Audio/Pop.wav'),
        ('AudioClip', 'pig2_trimmed__1806__0.wav', 'Audio/Launch.wav'),
        ('AudioClip', 'endgame_win__619__0.wav', 'Audio/Win.wav'),
    ]:
        copy(group, pattern, target)

    colors = []
    for index, name in enumerate(PALETTE):
        matches = list((source / 'exports/Material/resources.assets').glob(name + '__*.json'))
        if len(matches) != 1:
            raise ValueError(f'Ambiguous material {name}')
        material = json.loads(matches[0].read_text(encoding='utf-8'))
        properties = dict(material['m_SavedProperties']['m_Colors'])
        color = properties['_BaseColor']
        colors.append({'id': index, 'name': name, 'r': color['r'], 'g': color['g'], 'b': color['b']})
        receipts.append({'source': matches[0].relative_to(source).as_posix(), 'sha256': sha(matches[0]), 'purpose': f'palette {index}: observed Main Pixel Materials pointer order and PixelMaterial index/name'})

    levels = []
    titles = ['First Flow', 'Sweet Spot', 'Bright Idea', 'Keep It Moving', 'Color Garden', 'Perfect Timing', 'Find Your Flow']
    for number in range(1, 8):
        path = source / f'exports/Levels/3db5681ee587345ad9a3cf8400a692d1/{number}.json'
        data = json.loads(path.read_text(encoding='utf-8'))
        image = data['PixelImageData']
        for key, value in data.items():
            if isinstance(value, dict) and key not in {'QueueGroup', 'PixelImageData'} and any(value.values()):
                raise ValueError(f'Unsupported shooter mechanic in level {number}: {key}')
        for key, value in image.items():
            if isinstance(value, list) and value and key != 'pixels':
                raise ValueError(f'Unsupported board mechanic in level {number}: {key}')
        pixels = [{'x': p['x'], 'y': p['y'], 'color': p['material']} for p in image['pixels']]
        if any(p.get('areaX', 1) != 1 or p.get('areaY', 1) != 1 for p in image['pixels']):
            raise ValueError('Unsupported multi-cell pixel')
        queues = [{'shooters': [{'id': p['id'], 'ammo': p['ammo'], 'color': p['material']} for p in q['shooters']]} for q in data['QueueGroup']['shooterQueues']]
        levels.append({'sourceNumber': number, 'title': titles[number - 1], 'width': image['width'], 'height': image['height'], 'slotCount': data['SlotCount'], 'conveyorLimit': data['ConveyorLimit'], 'pixels': pixels, 'queues': queues})
        receipts.append({'source': path.relative_to(source).as_posix(), 'sha256': sha(path), 'purpose': f'normalized level {number}; pixels, material IDs, shooter IDs/ammo/order and capacities preserved'})

    config = {'schemaVersion': 1, 'loopSeconds': 8.8, 'shotInterval': 0.045, 'colors': colors, 'levels': levels}
    destination = root / 'Resources/PixelFlow/pixelFlowCampaign.json'
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps(config, separators=(',', ':')) + '\n', encoding='utf-8')
    report = {'sourcePackageSha256': 'ad7b8a8f7659ec86a583976e466458b69fd58f08f2667f6f17925fa570e9bad2', 'sourcePreserved': True, 'selectedLevels': 7, 'palette': '34 original color IDs/names; _BaseColor values from exported materials', 'estimated': ['belt duration', 'shot interval', 'targeting tie breaks', 'camera and animation timing'], 'receipts': receipts, 'campaignSha256': sha(destination)}
    (project / 'Docs/Evidence/asset-intake.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(f'Imported {len(receipts)} receipted sources; seven levels, {sum(len(x["pixels"]) for x in levels)} pixels.')


if __name__ == '__main__':
    main()
