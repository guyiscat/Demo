import re,json,pathlib
root=pathlib.Path(r'D:\UnityProjects\Demo\Project6.4')
scripts={re.search(r'guid: (\w+)',p.read_text()).group(1):p.stem[:-3] for p in (root/'Assets/Scripts').rglob('*.cs.meta')}
for scene in ['TheFirstScene_Seaside','ForestWhitebox','SnowWhitebox']:
 raw=(root/f'Assets/Scenes/{scene}.unity').read_text(encoding='utf-8-sig')
 blocks={m.group(2):(m.group(1),m.group(3)) for m in re.finditer(r'^--- !u!(\d+) &(\d+)\n(.*?)(?=^--- !u!|\Z)',raw,re.M|re.S)}
 gos={i:re.search(r'  m_Name: (.*)',b).group(1) for i,(t,b) in blocks.items() if t=='1'}
 trans={}; comps={}
 for i,(t,b) in blocks.items():
  g=re.search(r'm_GameObject: \{fileID: (\d+)\}',b)
  if not g:continue
  gid=g.group(1)
  if t=='4':
   pos=[float(v) for v in re.search(r'm_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)',b).groups()];parent=re.search(r'm_Father: \{fileID: (\d+)\}',b).group(1)
   trans[i]=(gid,parent,pos)
  if t=='114':
   script=re.search(r'm_Script: \{fileID: \d+, guid: (\w+)',b)
   if script:comps.setdefault(gid,[]).append((scripts.get(script.group(1),script.group(1)),b))
 def world(i):
  gid,pa,p=trans[i];return [a+b for a,b in zip(p,world(pa))] if pa!='0' else p
 def path(i):
  gid,pa,p=trans[i];return (path(pa)+'/' if pa!='0' else '')+gos[gid]
 print('\nSCENE',scene)
 for tid,(gid,pa,pos) in trans.items():
  name=gos[gid];cs=comps.get(gid,[])
  relevant=any((s in ['SeasideSession','ClimbPoint','ForestLevel','SnowLevel','ThermalSurface'] or s=='SeasideCell' and re.search(r'kind: [24]',b)) for s,b in cs)
  if scene=='TheFirstScene_Seaside' and (re.search('light|end|start|seaside_terrain_modules|海|灯',name,re.I) or relevant):
   print(path(tid),'world=',world(tid),'comps=',[(s,re.sub(r'.*?m_EditorClassIdentifier:[^\n]*\n','',b,flags=re.S).strip()) for s,b in cs if s=='SeasideSession' or s=='SeasideCell'])
  elif scene!='TheFirstScene_Seaside' and relevant:print(path(tid),'world=',world(tid),'comps=',[(s,re.sub(r'.*?m_EditorClassIdentifier:[^\n]*\n','',b,flags=re.S).strip()[:400]) for s,b in cs])
