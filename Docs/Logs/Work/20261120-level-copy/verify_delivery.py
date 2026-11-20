from pathlib import Path
from hashlib import sha256
import re,json,zipfile,sys
R=Path(r'D:\UnityProjects\Demo');W=Path(__file__).parent;V=R/'Docs/Backups/20261118-shared-systems-script-folders/Validation';B=R/'Docs/Backups/20261120-level-copy'
checks={};failures=[];hashes={}
def h(p):
 p=Path(p)
 if p not in hashes:
  s=sha256()
  with p.open('rb') as f:
   for c in iter(lambda:f.read(1024*1024),b''):s.update(c)
  hashes[p]=s.hexdigest()
 return hashes[p]
def check(k,ok,detail):
 checks[k]=detail
 if not ok:failures.append(k)
def tree(a,b,skip=False):
 aa={p.relative_to(a).as_posix():p for p in a.rglob('*') if p.is_file()};bb={p.relative_to(b).as_posix():p for p in b.rglob('*') if p.is_file()};errors=[]
 for k in sorted(set(aa)|set(bb)):
  if skip and k.startswith('Docs/'):continue
  if k not in aa or k not in bb or h(aa[k])!=h(bb[k]):errors.append(k)
 return {'files':len(aa),'errors':errors}
for key,rel,skip in [('builtSources','Project6.4/Assets',False),('projectSettings','Project6.4/ProjectSettings',False),('buildTools','Project6.4/Tools',False),('validatedPlayers','Play64',True)]:
 data=tree(R/rel,V/rel,skip);check(key,not data['errors'],data)
g=json.loads((W/'geometry-verification.json').read_text(encoding='utf-8'));check('existingSceneDataPreserved',all(not x['changedExistingData'] for x in g.values()),g)
font=R/'Project6.4/Assets/Resources/Fonts/AlibabaPuHuiTi-Regular.otf';check('unchangedSystemFont',h(font)==h(R/'Assets/AlibabaPuHuiTi-3-55-Regular.otf'),h(font))
assets=R/'Project6.4/Assets';guids={};errs=[]
for p in assets.rglob('*'):
 if not p.is_file() or p.suffix=='.meta':continue
 m=Path(str(p)+'.meta')
 if not m.exists():errs.append(str(p)+':missing-meta');continue
 match=re.search(r'^guid: (\w+)',m.read_text(encoding='utf-8-sig'),re.M)
 if match:
  guid=match[1]
  if guid in guids:errs.append(str(p)+':duplicate-guid')
  guids[guid]=p
for p in assets.rglob('*'):
 if p.suffix not in ['.unity','.prefab','.asset','.mat']:continue
 for guid in set(re.findall(r'guid: ([0-9a-f]{32})',p.read_text(encoding='utf-8-sig'))):
  if guid not in guids and not guid.startswith('0000000000000000'):errs.append(str(p)+':missing-'+guid)
check('metadataAndReferences',not errs,errs)
summary=json.loads((W/'final-test-summary.json').read_text(encoding='utf-8'))
for level,data in summary.items():
 f=R/'Docs/Logs/Build'/data['run'];diags=re.findall(r'(?:error|warning) CS\d+',(f/'editor-build.log').read_text(encoding='utf-8-sig'))
 check('build_'+level,data['result']=='PASS' and not diags and all(v['passed']>0 and v['failed']==0 for v in data['reports'].values()),dict(data,csharpDiagnostics=diags))
from PIL import Image,ImageStat
for folder in ['visuals-visible','visuals-1024x768']:
 files=list((W/folder).glob('*.png'));bad=[p.name for p in files if sum(ImageStat.Stat(Image.open(p).convert('RGB')).var)<2]
 check('actualRenderedFrames_'+folder,len(files)==13 and not bad,{'frames':len(files),'blank':bad,'review':'Visual layout inspected; process shutdown failed separately.'})
check('movementParametersPreserved',h(R/'Project6.4/Assets/Settings/Movement.asset')=='83cd54b2c4dd85630a362fd4be864390e618e872e821ca5c325b096c2019a402',h(R/'Project6.4/Assets/Settings/Movement.asset'))
if '--sources-only' not in sys.argv:
 for level in summary:
  variant='Startup' if level=='Seaside' else 'Whitebox'
  for kind in ['Windows','Project']:
   p=R/(level+'-Unity64-'+variant+'-'+kind+'.zip');errs=[];count=0
   with zipfile.ZipFile(p) as z:
    names=z.namelist();required=['Docs/LevelCopy.md']
    if kind=='Project':required+=['Project6.4/Assets/Scripts/seaside/SeasideStory.cs','Project6.4/Assets/Scripts/LevelTutorials.cs','Project6.4/Assets/Editor/LevelCopySetup.cs']
    else:
     prefix='Play64/'+('' if level=='Seaside' else level+'/');exe='SeasideWhitebox' if level=='Seaside' else level+'Whitebox';required+=[prefix+exe+'.exe',prefix+exe+'_Data/Managed/Assembly-CSharp.dll']
    errs+=['missing:'+n for n in required if n not in names]
    for info in z.infolist():
     if info.is_dir():continue
     count+=1;s=sha256()
     with z.open(info) as f:
      for c in iter(lambda:f.read(1024*1024),b''):s.update(c)
     if info.filename.endswith('/verification.json') or info.filename.startswith('Play64/Docs/Logs/Runtime/'):continue
     source=R/info.filename
     if not source.is_file() or h(source)!=s.hexdigest():errs.append(info.filename)
   check(p.name,not errs,{'entriesReadAndCrcChecked':count,'errors':errs});print(p.name, 'PASS' if not errs else 'FAIL',flush=True)
 for cn,key in [('森林','Forest'),('矿洞','Mine'),('雪山','Snow'),('灯塔','Lighthouse')]:check(cn+'Alias',h(R/(cn+'独立试玩.zip'))==h(R/(key+'-Unity64-Whitebox-Windows.zip')),key)
out={'result':'PASS' if not failures else 'FAIL','checks':checks,'failures':failures,'knownLimitations':['Normal window shutdown: native Unity crash reproduced both before and after this task (-1073740771); not fixed.','Screenshot content checked, but both visible screenshot processes failed during shutdown (-1073741819).','No manual full route playthrough.']}
name='source-verification.json' if '--sources-only' in sys.argv else 'verification.json';(W/name).write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps({'result':out['result'],'failures':failures},ensure_ascii=False));sys.exit(bool(failures))
