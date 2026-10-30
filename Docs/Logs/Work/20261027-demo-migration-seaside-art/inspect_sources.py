"""Read-only source audit and map diagram. Original assets are never rewritten."""
import json
from collections import Counter
from pathlib import Path
import openpyxl
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[4]
OUT = Path(__file__).resolve().parent
data = json.loads((ROOT / 'Project/Assets/Resources/SeasideMap.json').read_text(encoding='utf-8-sig'))
report = {'json': {k: v for k, v in data.items() if k != 'cells'}}
report['json']['kinds'] = dict(Counter(c['kind'] for c in data['cells']))
books = []
for path in sorted((ROOT / 'maps').glob('*.xlsx')):
    if path.name.startswith('~$'):
        continue
    try:
        wb = openpyxl.load_workbook(path, data_only=False)
    except Exception as error:
        books.append({'name': path.name, 'error': str(error), 'bytes': path.stat().st_size})
        continue
    sheets = []
    for ws in wb:
        fills = Counter()
        for row in ws:
            for cell in row:
                if cell.fill.patternType == 'solid':
                    color = cell.fill.fgColor
                    fills[str(color.rgb if color.type == 'rgb' else f'{color.type}:{color.index}')] += 1
        sheets.append({'name': ws.title, 'range': ws.calculate_dimension(), 'solid_fills': dict(fills)})
    if path.name == '海边地图.xlsx':
        ws = wb['Sheet1']
        differences = []
        for c in data['cells']:
            color = ws[c['address']].fill.fgColor
            key = color.rgb if color.type == 'rgb' else f'{color.type}:{color.index}'
            expected = {'ground': 'theme:1', 'route': 'theme:4', 'checkpoint': 'theme:6', 'spike': 'FFFF0000'}[c['kind']]
            if key != expected:
                differences.append({'address': c['address'], 'json_kind': c['kind'], 'xlsx_color': str(key)})
        report['json_to_seaside_workbook'] = {'checked': len(data['cells']), 'different': len(differences), 'examples': differences[:15]}
    books.append({'name': path.name, 'sheets': sheets})
report['workbooks'] = books
art = Image.open(ROOT / 'Figures/Seaside.png')
report['art'] = {'size': art.size, 'mode': art.mode}
scale = 10
im = Image.new('RGB', (data['width']*scale, data['height']*scale), '#e6f0ef')
draw = ImageDraw.Draw(im)
colors = {'ground': '#183a46', 'spike': '#ec4a52', 'checkpoint': '#e2ab15', 'route': '#4e8eee'}
for c in data['cells']:
    x, y = c['x']*scale, (data['height']-c['y']-1)*scale
    draw.rectangle((x, y, x+scale-1, y+scale-1), fill=colors.get(c['kind'], '#d2a8ff'))
for x in range(0, data['width'], 10):
    draw.line((x*scale, 0, x*scale, im.height), fill='#85a6a4', width=1)
    draw.text((x*scale+2, im.height-15), str(x), fill='#36565a')
for y in range(0, data['height'], 10):
    yy=(data['height']-y)*scale
    draw.line((0, yy, im.width, yy), fill='#85a6a4', width=1)
    draw.text((2, yy-13), str(y), fill='#36565a')
im.save(OUT / 'map-reference.png')
(OUT / 'source-audit.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(report, ensure_ascii=False, indent=2))
