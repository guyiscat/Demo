from pathlib import Path
import collections,hashlib,json,re
ROOT=Path('D:/UnityProjects/Demo');ASSETS=ROOT/'Project6.4/Assets';OUT=Path(__file__).parent
guid_paths={}; refs=collections.defaultdict(list)
for meta in ASSETS.rglob('*.meta'):
    match=re.search(r'^guid: (\w+)',meta.read_text(encoding='utf-8-sig'),re.M)
    if match:guid_paths[match.group(1)]=meta.with_suffix('')
for path in ASSETS.rglob('*'):
    if not path.is_file() or path.suffix in ['.meta','.png','.jpg','.otf','.psd']:continue
    try:raw=path.read_text(encoding='utf-8-sig')
    except UnicodeError:continue
    for guid in set(re.findall(r'guid: ([0-9a-f]{32})',raw)):refs[guid].append(path.relative_to(ROOT).as_posix())
scene_report=[]
for scene in sorted(ASSETS.rglob('*.unity')):
    raw=scene.read_bytes();meta=Path(str(scene)+'.meta');guid=re.search(r'guid: (\w+)',meta.read_text()).group(1)
    scene_report.append({'path':scene.relative_to(ROOT).as_posix(),'bytes':len(raw),'sha256':hashlib.sha256(raw).hexdigest(),'guid':guid,'referencedBy':refs[guid]})
unreferenced=[{'path':p.relative_to(ROOT).as_posix(),'bytes':p.stat().st_size} for g,p in guid_paths.items() if p.is_file() and not refs[g] and p.suffix not in ['.cs','.unity']]
report={'scenes':scene_report,'noSerializedIncomingReference':unreferenced,'notes':'No serialized reference alone does not prove unused: Resources.Load, editor asset paths, source art, and manual work must also be inspected.'}
(OUT/'asset-audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report,ensure_ascii=False,indent=2))
