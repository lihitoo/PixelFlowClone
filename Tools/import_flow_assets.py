"""Narrow, reproducible intake of selected UI/audio; source export is read-only."""
import hashlib, json, pathlib, shutil, subprocess
ROOT = pathlib.Path(__file__).resolve().parents[1]
SOURCE = pathlib.Path(r'D:\Downloads\PixelFlow_0.36.3_extracted\exports')
DEST = ROOT / 'Assets/_Project/PixelFlow/Resources/PixelFlow'
FFMPEG = pathlib.Path(json.loads((ROOT / 'Docs/Evidence/source-video-review/ffmpeg-tool.json').read_text())['path'])
MAPPING = {
    'Main Menu Music (Cover) 2__617__0.wav': 'Audio/MenuMusic.ogg',
    'Normal Level Music (Cover) 1__637__0.wav': 'Audio/LevelMusic.ogg',
    'Unlock__1759__0.wav': 'Audio/Feature.wav',
    'TrayIcon__680.png': 'UI/Tray.png',
    'IconPowerup_Hand_Colored__1954.png': 'UI/Hand.png',
    'IconPowerup_Shuffle_Colored__2234.png': 'UI/Shuffle.png',
    'IconPowerup_SuperShooter_Colored__2024.png': 'UI/Super.png',
    'Icon_SuperUnicorn_Active__860.png': 'UI/Unicorn.png',
}
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def main():
    receipts = []
    for name, target in MAPPING.items():
        found = list(SOURCE.rglob(name))
        if len(found) != 1: raise ValueError(f'{name}: expected one source, found {found}')
        src = found[0]; dst = DEST / target; dst.parent.mkdir(parents=True, exist_ok=True)
        if dst.suffix == '.ogg':
            subprocess.run([str(FFMPEG), '-nostdin', '-v', 'error', '-y', '-i', str(src), '-c:a', 'libvorbis', '-q:a', '4', str(dst)], check=True)
        else: shutil.copyfile(src, dst)
        receipts.append(dict(source=str(src), sourceSha256=sha(src), destination=str(dst.relative_to(ROOT)), sha256=sha(dst), bytes=dst.stat().st_size, conversion='vorbis q4' if dst.suffix == '.ogg' else 'copy'))
    (ROOT / 'Docs/Evidence/flow-asset-intake.json').write_text(json.dumps(dict(assets=receipts), indent=2), encoding='utf-8')
    print(json.dumps(dict(assets=len(receipts), bytes=sum(a['bytes'] for a in receipts))))
if __name__ == '__main__': main()
