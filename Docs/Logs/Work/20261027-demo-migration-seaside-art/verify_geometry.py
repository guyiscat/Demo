"""Compare selected serialized gameplay fields, ignoring engine serialization upgrades."""
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
OUT = Path(__file__).resolve().parent
SOURCE = ROOT.parent / 'game1/Seaside-PlaytestFix-v3-Project/Project'


def scalar(value):
    if value.startswith('{'):
        return {key: scalar(item.strip()) for key, item in re.findall(r'(\w+):\s*([^,}]+)', value)}
    if re.fullmatch(r'-?\d+', value):
        return int(value)
    if re.fullmatch(r'-?\d*\.\d+(?:[eE][-+]?\d+)?', value):
        return float(value)
    return value


def signature(path):
    text = path.read_text(encoding='utf-8-sig')
    objects = {}
    for match in re.finditer(r'^--- !u!(\d+) &(\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)', text, re.M | re.S):
        # Only Unity's top-level scalar/inline-vector fields are needed; no general YAML parser.
        obj = {key: scalar(value.strip()) for key, value in re.findall(r'^  (\w+):[ \t]*(.*)$', match.group(3), re.M)}
        objects[match.group(2)] = (int(match.group(1)), obj)
    by_go = {}
    for oid, (kind, obj) in objects.items():
        go = str(obj.get('m_GameObject', {}).get('fileID', ''))
        by_go.setdefault(go, []).append((kind, obj))
    cells = {}
    cell_guid = re.search(r'guid: (\w+)', (SOURCE / 'Assets/Scripts/SeasideCell.cs.meta').read_text()).group(1)
    for go_id, entries in by_go.items():
        cell = next((o for k, o in entries if k == 114 and o.get('m_Script', {}).get('guid') == cell_guid), None)
        if cell is None:
            continue
        go = objects[go_id][1]
        transform = next(o for k, o in entries if k == 4)
        shape = next((o for k, o in entries if k == 61), None)
        ancestors = []
        parent = str(transform['m_Father']['fileID'])
        while parent != '0':
            ancestor = objects[parent][1]
            ancestors.append({key: ancestor.get(key) for key in ['m_LocalPosition', 'm_LocalScale', 'm_LocalRotation']})
            parent = str(ancestor['m_Father']['fileID'])
        anchor_id = str(cell.get('respawnAnchor', {}).get('fileID', 0))
        anchor = objects.get(anchor_id)
        cells[go_id] = {
            'name': go['m_Name'], 'layer': go['m_Layer'], 'active': go['m_IsActive'],
            'transform': {key: transform.get(key) for key in ['m_LocalPosition', 'm_LocalScale', 'm_LocalRotation']},
            'ancestors': ancestors,
            'cell': {key: cell.get(key) for key in ['excelAddress', 'kind', 'attachable', 'respawn', 'checkpointId']},
            'anchor': None if anchor is None else {key: anchor[1].get(key) for key in ['m_LocalPosition', 'm_LocalScale']},
            'collider': None if shape is None else {key: shape.get(key) for key in ['m_Size', 'm_Offset', 'm_IsTrigger', 'm_Enabled', 'm_Material']},
        }
    digest = hashlib.sha256(json.dumps(cells, sort_keys=True).encode()).hexdigest()
    return cells, digest


original, original_sha = signature(SOURCE / 'Assets/Scenes/Seaside01.unity')
report = {'source_cells': len(original), 'source_geometry_sha256': original_sha, 'comparisons': []}
for name in ['Seaside01', 'SeasideArtPreview']:
    cells, sha = signature(ROOT / f'Project/Assets/Scenes/{name}.unity')
    diff = [key for key in original.keys() | cells.keys() if original.get(key) != cells.get(key)]
    report['comparisons'].append({'scene': name, 'cells': len(cells), 'geometry_sha256': sha, 'different_objects': len(diff), 'examples': diff[:8]})
report['json_byte_identical'] = (SOURCE / 'Assets/Resources/SeasideMap.json').read_bytes() == (ROOT / 'Project/Assets/Resources/SeasideMap.json').read_bytes()
report['image_byte_identical'] = (ROOT / 'Figures/Seaside.png').read_bytes() == (ROOT / 'Project/Assets/Art/Backgrounds/Seaside.png').read_bytes()
(OUT / 'geometry-verification.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(report, ensure_ascii=False, indent=2))
assert report['json_byte_identical'] and report['image_byte_identical']
assert all(item['different_objects'] == 0 for item in report['comparisons'])
