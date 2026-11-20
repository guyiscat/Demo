from pathlib import Path
from hashlib import sha256
import collections,json,re,sys,zipfile
from datetime import datetime,timezone
ROOT=Path('D:/UnityProjects/Demo');WORK=Path(__file__).parent
BACKUP=ROOT/'Docs/Backups/20261120-campaign-saves'
VALIDATION=ROOT/'Docs/Backups/20261118-shared-systems-script-folders/Validation'
result={'checkedAtUtc':datetime.now(timezone.utc).isoformat(),'checks':{},'failures':[]};hashes={}
def digest(p):
    p=Path(p)
    if p not in hashes:
        h=sha256()
        with p.open('rb') as f:
            for chunk in iter(lambda:f.read(1024*1024),b''):h.update(chunk)
        hashes[p]=h.hexdigest()
    return hashes[p]
def check(name,ok,detail):
    result['checks'][name]=detail
    if not ok:result['failures'].append(name)
def tree(left,right,skip_runtime=False):
    files={p.relative_to(left).as_posix():p for p in left.rglob('*') if p.is_file()}
    other={p.relative_to(right).as_posix():p for p in right.rglob('*') if p.is_file()};errors=[];count=0
    for rel in sorted(set(files)|set(other)):
        if skip_runtime and rel.startswith('Docs/Logs/Runtime/'):continue
        count+=1
        if rel not in files or rel not in other or digest(files[rel])!=digest(other[rel]):errors.append(rel)
    return {'filesChecked':count,'mismatches':errors}
for name,left,right,skip in [('builtSources',ROOT/'Project6.4/Assets',VALIDATION/'Project6.4/Assets',False),('buildTools',ROOT/'Project6.4/Tools',VALIDATION/'Project6.4/Tools',False),('validatedPlayers',ROOT/'Play64',VALIDATION/'Play64',True)]:
    data=tree(left,right,skip);check(name,not data['mismatches'],data)
for rel in ['Project6.4/Assets/Scenes/TheFirstScene_Seaside.unity','Project6.4/Assets/Scenes/ForestWhitebox.unity','Project6.4/Assets/Settings/Movement.asset']:
    check('preserved_'+rel,digest(ROOT/rel)==digest(BACKUP/rel),{'sha256':digest(ROOT/rel)})

def blocks(path):
    text=path.read_text(encoding='utf-8-sig');result={}
    for match in re.finditer(r'^--- !u!(\d+) &(-?\d+)\n(.*?)(?=^--- !u!|\Z)',text,re.M|re.S):result[match.group(2)]=(match.group(1),match.group(3))
    return result
for key in ['Mine','Snow','Lighthouse']:
    rel=Path('Project6.4/Assets/Scenes')/(key+'Whitebox.unity');before=blocks(BACKUP/rel);after=blocks(ROOT/rel);errors=[]
    for object_id,(kind,old) in before.items():
        if object_id not in after:errors.append(object_id+':missing');continue
        new=after[object_id][1]
        if kind=='4':
            fields=['m_LocalPosition','m_LocalRotation','m_LocalScale','m_Father']
        elif kind=='61':fields=['m_Size','m_Offset','m_IsTrigger','m_GameObject','m_Enabled']
        elif kind=='1':fields=['m_Layer','m_IsActive','m_Name']
        elif kind=='114' and 'Assembly-CSharp::Seaside.SeasideCell' in old:fields=['excelAddress','kind','attachable','respawn','respawnAnchor','checkpointId']
        else:continue
        for field in fields:
            a=re.search(r'^  '+re.escape(field)+r':.*$',old,re.M);b=re.search(r'^  '+re.escape(field)+r':.*$',new,re.M)
            if (a.group(0) if a else None)!=(b.group(0) if b else None):errors.append(object_id+':'+field)
    check(key+'ExistingGeometryPreserved',not errors,{'existingSerializedObjects':len(before),'errors':errors})
font=ROOT/'Project6.4/Assets/Resources/Fonts/AlibabaPuHuiTi-Regular.otf'
check('originalFontByteIdentity',digest(font)==digest(ROOT/'Assets/AlibabaPuHuiTi-3-55-Regular.otf'),{'sha256':digest(font)})

assets=ROOT/'Project6.4/Assets';guids={};errors=[]
for p in assets.rglob('*'):
    if not p.is_file() or p.suffix=='.meta':continue
    meta=Path(str(p)+'.meta')
    if not meta.exists():errors.append(str(p)+':missing-meta');continue
    match=re.search(r'^guid: (\w+)',meta.read_text(encoding='utf-8-sig'),re.M)
    if match:
        guid=match.group(1)
        if guid in guids:errors.append(str(p)+':duplicate-guid')
        guids[guid]=p
for p in assets.rglob('*'):
    if not p.is_file() or p.suffix not in ['.unity','.prefab','.asset','.mat']:continue
    for guid in set(re.findall(r'guid: ([0-9a-f]{32})',p.read_text(encoding='utf-8-sig'))):
        if guid not in guids and not guid.startswith('0000000000000000'):errors.append(str(p)+':missing-'+guid)
check('assetMetadataAndReferences',not errors,{'errors':errors})

runs=[('Seaside','20261120-221931-875',None,None),('Forest','20261120-222339-305','forest-runtime-checks.txt','forest-editor-checks.txt'),('Mine','20261120-222356-969','mine-runtime-checks.txt','mine-editor-checks.txt'),('Snow','20261120-222415-207','snow-runtime-checks.txt','snow-editor-checks.txt'),('Lighthouse','20261120-222432-944','lighthouse-runtime-checks.txt','lighthouse-editor-checks.txt')]
summary={}
for key,run,runtime,scene in runs:
    folder=ROOT/'Docs/Logs/Build'/run;status=json.loads((folder/'status.json').read_text(encoding='utf-8-sig'))
    files=['editor-reports/system-editor-checks.txt','editor-reports/physics-tests.txt','editor-reports/playtest-regression.txt']
    if key=='Seaside':files+=['campaign-runtime-checks.txt','save-probe-write.txt','save-probe-read.txt']
    else:files+=[runtime,'editor-reports/'+scene]
    counts={}
    for rel in files:
        c=collections.Counter(line.split(' ',1)[0] for line in (folder/rel).read_text(encoding='utf-8-sig').splitlines());counts[rel]={'passed':c['PASS'],'failed':c['FAIL']}
    diagnostics=re.findall(r'(?:error|warning) CS\d+',(folder/'editor-build.log').read_text(encoding='utf-8-sig'))
    summary[key]={'run':run,'result':status['result'],'reports':counts,'csharpDiagnostics':diagnostics}
    check('build_'+key,status['result']=='PASS' and not diagnostics and all(c['passed']>0 and c['failed']==0 for c in counts.values()),summary[key])
(WORK/'final-test-summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

if '--sources-only' not in sys.argv:
    for target,_,_,_ in runs:
        variant='Startup' if target=='Seaside' else 'Whitebox'
        for kind in ['Windows','Project']:
            p=ROOT/(target+'-Unity64-'+variant+'-'+kind+'.zip');errors=[];count=0
            with zipfile.ZipFile(p) as z:
                prefix='Play64/'+('' if target=='Seaside' else target+'/');exe='SeasideWhitebox' if target=='Seaside' else target+'Whitebox'
                required=['Project6.4/Assets/Scripts/CampaignFlow.cs','Project6.4/Assets/Editor/CampaignSetup.cs','Docs/CampaignAndSaves.md'] if kind=='Project' else [prefix+exe+'.exe',prefix+exe+'_Data/Managed/Assembly-CSharp.dll',prefix+'UnityPlayer.dll',prefix+'MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll']
                errors+=['missing:'+n for n in required if n not in z.namelist()]
                for info in z.infolist():
                    if info.is_dir():continue
                    count+=1;h=sha256()
                    with z.open(info) as stream:
                        for chunk in iter(lambda:stream.read(1024*1024),b''):h.update(chunk)
                    if info.filename.startswith('Play64/Docs/Logs/Runtime/') or info.filename.endswith('/verification.json'):continue
                    source=ROOT/info.filename
                    if not source.is_file() or digest(source)!=h.hexdigest():errors.append(info.filename)
            check(p.name,not errors,{'entriesReadAndCrcChecked':count,'sourceMismatches':errors,'sha256':digest(p)})
            print(p.name,'PASS' if not errors else 'FAIL',flush=True)
    for cn,key in [('森林','Forest'),('矿洞','Mine'),('雪山','Snow'),('灯塔','Lighthouse')]:check(cn+'Alias',digest(ROOT/(cn+'独立试玩.zip'))==digest(ROOT/(key+'-Unity64-Whitebox-Windows.zip')),{'target':key})
result['result']='PASS' if not result['failures'] else 'FAIL'
filename='source-verification.json' if '--sources-only' in sys.argv else 'verification.json'
(WORK/filename).write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'result':result['result'],'failures':result['failures']},ensure_ascii=False));raise SystemExit(0 if result['result']=='PASS' else 1)
