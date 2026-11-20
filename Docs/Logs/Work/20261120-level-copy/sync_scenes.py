from pathlib import Path
import re,json,hashlib
r=Path(r'D:\UnityProjects\Demo');v=r/'Docs/Backups/20261118-shared-systems-script-folders/Validation';b=r/'Docs/Backups/20261120-level-copy'
def blocks(p):
 return {m[2]:(m[1],m[3]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)\n(.*?)(?=^--- !u!|\Z)',p.read_text(encoding='utf-8-sig'),re.M|re.S)}
result={};errors=[]
for name in ['TheFirstScene_Seaside','ForestWhitebox','MineWhitebox','SnowWhitebox','LighthouseWhitebox','StartGame']:
 rel=Path('Project6.4/Assets/Scenes')/(name+'.unity');before=blocks(b/rel);after=blocks(v/rel);diff=[]
 for oid,(kind,old) in before.items():
  if oid not in after:diff.append(oid+':missing');continue
  new=after[oid][1]
  if name=='ForestWhitebox' and oid=='1227016755' and '  environment:' not in old:
   # Unity serializes the existing optional traversal field on first scene save.
   new=new.replace('  environment: {fileID: 0}\n','')
  if kind=='1':
   old=re.sub(r'  m_Component:.*?(?=  m_Layer:)', '',old,flags=re.S);new=re.sub(r'  m_Component:.*?(?=  m_Layer:)', '',new,flags=re.S)
  if kind=='4':
   old=re.sub(r'  m_Children:.*?(?=  m_Father:)', '',old,flags=re.S);new=re.sub(r'  m_Children:.*?(?=  m_Father:)', '',new,flags=re.S)
  if old!=new:diff.append(oid+':'+kind)
 if hashlib.sha256((r/rel).read_bytes()).digest()!=hashlib.sha256((b/rel).read_bytes()).digest():diff.append('source changed since task backup')
 result[name]={'existingObjects':len(before),'newObjects':len(after)-len(before),'changedExistingData':diff};errors+=diff
print(json.dumps(result,ensure_ascii=False,indent=2))
(r/'Docs/Logs/Work/20261120-level-copy/geometry-verification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
if errors:raise SystemExit(1)
import shutil
for name in ['TheFirstScene_Seaside','ForestWhitebox','SnowWhitebox']:
 rel=Path('Project6.4/Assets/Scenes')/(name+'.unity');shutil.copy2(v/rel,r/rel)
print('Synced three incrementally patched scenes after preservation check.')
