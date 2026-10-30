"""Read-only review of the supplied images and layout data; no asset editing."""
import hashlib
import json
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[4]
OUT = Path(__file__).resolve().parent
folder = ROOT / 'Figures/part1'
metadata = json.loads((folder / 'part1.json.ps').read_text(encoding='utf-8-sig'))
records = []
for child in metadata['children']:
    path = folder / child['texture']['uri']
    with Image.open(path) as im:
        record = {
            'file': path.name,
            'layout': {key: child[key] for key in ['name', 'x', 'y', 'width', 'height']},
            'png_size': list(im.size),
            'png_to_layout_ratio': [im.width/child['width'], im.height/child['height']],
            'mode': im.mode,
            'alpha_extrema': list(im.getchannel('A').getextrema()) if im.mode == 'RGBA' else None,
            'sha256': hashlib.sha256(path.read_bytes()).hexdigest(),
        }
    records.append(record)
report = {
    'source': str(folder),
    'layout_canvas': {key: metadata[key] for key in ['width', 'height']},
    'layout_file_sha256': hashlib.sha256((folder/'part1.json.ps').read_bytes()).hexdigest(),
    'image_count': len(records),
    'all_layout_images_exist': True,
    'width_155_initial_candidate_height': 155*metadata['height']/metadata['width'],
    'records': records,
    'scope': 'Read-only asset review. No Unity import, scene changes, new build, or manual gameplay test.',
}
(OUT/'asset-audit.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({k:v for k,v in report.items() if k != 'records'}, ensure_ascii=False, indent=2))
