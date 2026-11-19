from pathlib import Path
from hashlib import sha256
import collections,json,re,sys,zipfile
from datetime import datetime,timezone
ROOT=Path('D:/UnityProjects/Demo');WORK=Path(__file__).parent
BACKUP=ROOT/'Docs/Backups/20261119-project-cleanup'
VALIDATION=ROOT/'Docs/Backups/20261118-shared-systems-script-folders/Validation'
report={'checkedAtUtc':datetime.now(timezone.utc).isoformat(),'checks':{},'failures':[]};cache={}
def digest(p):
    p=Path(p)
    if p not in cache:
        h=sha256()
        with p.open('rb') as f:
            for chunk in iter(lambda:f.read(1024*1024),b''):h.update(chunk)
        cache[p]=h.hexdigest()
    return cache[p]
def check(name,ok,detail):
    report['checks'][name]=detail
    if not ok:report['failures'].append(name)
manifest=json.loads((WORK/'cleanup-manifest.json').read_text(encoding='utf-8-sig'))
absent=[];archive_missing=[]
for item in manifest['archive']:
    for suffix in ['','.meta']:
        rel=item['path']+suffix;p=ROOT/rel
        if p.exists():absent.append(rel)
        if (BACKUP/rel).exists() or (BACKUP/'Removed'/rel).exists():continue
        if suffix=='' or (BACKUP/Path(item['path']+'.meta')).exists():archive_missing.append(rel)
check('retiredPathsRemoved',not absent,{'remaining':absent})
check('recoverableArchiveExists',not archive_missing,{'missing':archive_missing})
expected={'StartGame.unity','TheFirstScene_Seaside.unity','ForestWhitebox.unity','MineWhitebox.unity','SnowWhitebox.unity','LighthouseWhitebox.unity'}
scenes={p.name for p in (ROOT/'Project6.4/Assets/Scenes').glob('*.unity')}
check('onlyCurrentSixScenes',scenes==expected,{'scenes':sorted(scenes)})
changed=[]
for name in expected:
    rel=Path('Project6.4/Assets/Scenes')/name
    for suffix in ['','.meta']:
        p=Path(str(rel)+suffix)
        if digest(ROOT/p)!=digest(BACKUP/p):changed.append(p.as_posix())
for rel in ['Project6.4/Assets/Settings/Movement.asset','Project6.4/Assets/Resources/GameSystems.asset','Project6.4/ProjectSettings/EditorBuildSettings.asset']:
    if digest(ROOT/rel)!=digest(BACKUP/rel):changed.append(rel)
check('gameScenesAndParametersUnchanged',not changed,{'changed':changed})
fixture=manifest['relocate'][0];fixture_errors=[]
for suffix in ['','.meta']:
    if digest(BACKUP/(fixture['path']+suffix))!=digest(ROOT/(fixture['destination']+suffix)):fixture_errors.append(suffix)
check('fixtureBytesAndGuidPreserved',not fixture_errors,{'destination':fixture['destination'],'errors':fixture_errors})
for name,left,right,skip_logs in [('assetsMatchBuiltSources',ROOT/'Project6.4/Assets',VALIDATION/'Project6.4/Assets',False),('buildsMatchValidatedOutputs',VALIDATION/'Play64',ROOT/'Play64',True)]:
    errors=[];count=0;left_files={p.relative_to(left).as_posix():p for p in left.rglob('*') if p.is_file()};right_files={p.relative_to(right).as_posix():p for p in right.rglob('*') if p.is_file()}
    keys=set(left_files)|set(right_files)
    for rel in sorted(keys):
        if skip_logs and rel.startswith('Docs/Logs/Runtime/'):continue
        count+=1
        if rel not in left_files or rel not in right_files or digest(left_files[rel])!=digest(right_files[rel]):errors.append(rel)
    check(name,not errors,{'filesChecked':count,'errors':errors})

assets=ROOT/'Project6.4/Assets';guid_map={};missing_meta=[]
for p in assets.rglob('*'):
    if not p.is_file() or p.suffix=='.meta':continue
    meta=Path(str(p)+'.meta')
    if not meta.is_file():missing_meta.append(p.relative_to(ROOT).as_posix());continue
    m=re.search(r'^guid: (\w+)',meta.read_text(encoding='utf-8-sig'),re.M)
    if m:guid_map[m.group(1)]=p
missing_refs=[]
for p in assets.rglob('*'):
    if not p.is_file() or p.suffix not in ['.unity','.prefab','.asset','.mat']:continue
    text=p.read_text(encoding='utf-8-sig')
    for guid in set(re.findall(r'guid: ([0-9a-f]{32})',text)):
        if guid not in guid_map and not guid.startswith('0000000000000000'):missing_refs.append({'file':p.relative_to(ROOT).as_posix(),'guid':guid})
check('assetMetadataAndSerializedReferences',not missing_meta and not missing_refs,{'missingMeta':missing_meta,'missingGuidReferences':missing_refs})
retired_guids=[]
for item in manifest['archive']:
    if not item['path'].startswith('Project6.4/Assets/') or not item['path'].endswith('.cs'):continue
    meta=BACKUP/'Removed'/(item['path']+'.meta');retired_guids+=re.findall(r'^guid: (\w+)',meta.read_text(),re.M)
check('removedScriptsNotReferenced',not any(g in {r['guid'] for r in missing_refs} for g in retired_guids),{'retiredScriptCount':len(retired_guids)})

runs=[('Seaside','20261119-232524-143',None,None),('Forest','20261119-232654-870','forest-runtime-checks.txt','forest-editor-checks.txt'),('Mine','20261119-232711-773','mine-runtime-checks.txt','mine-editor-checks.txt'),('Snow','20261119-232725-788','snow-runtime-checks.txt','snow-editor-checks.txt'),('Lighthouse','20261119-232739-496','lighthouse-runtime-checks.txt','lighthouse-editor-checks.txt')]
summary={}
for name,run,runtime,scene in runs:
    folder=ROOT/'Docs/Logs/Build'/run;status=json.loads((folder/'status.json').read_text(encoding='utf-8-sig'));counts={}
    paths=['editor-reports/system-editor-checks.txt','editor-reports/physics-tests.txt','editor-reports/playtest-regression.txt']
    if runtime:paths+=[runtime]
    if scene:paths+=['editor-reports/'+scene]
    for rel in paths:
        c=collections.Counter(line.split(' ',1)[0] for line in (folder/rel).read_text(encoding='utf-8-sig').splitlines());counts[rel]={'passed':c['PASS'],'failed':c['FAIL']}
    diagnostics=re.findall(r'(?:error|warning) CS\d+',(folder/'editor-build.log').read_text(encoding='utf-8-sig'))
    summary[name]={'run':run,'result':status['result'],'reports':counts,'csharpDiagnostics':diagnostics}
    check('build_'+name,status['result']=='PASS' and not diagnostics and all(c['failed']==0 and c['passed']>0 for c in counts.values()),summary[name])
(WORK/'final-test-summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
camera=(WORK/'camera-checks.log').read_text(encoding='utf-8-sig')
check('cameraChecksUseCurrentScene', 'CAMERA_BACKGROUND_BOUNDS_PASS' in camera and 'Exception:' not in camera,{'log':'camera-checks.log'})
actions=json.loads((WORK/'actions.json').read_text(encoding='utf-8-sig'));removed=[a for a in actions if a['action']=='removed-from-active-tree']
report['cleanupTotals']={'removedFiles':sum(a['files'] for a in removed),'removedBytes':sum(a['bytes'] or 0 for a in removed),'backupPath':str(BACKUP)}
if '--sources-only' not in sys.argv:
    banned=[item['path'].replace('\\','/') for item in manifest['archive']]
    banned.append('Project6.4/Assets/Scenes/Seaside01.unity')
    for name,_,_,_ in runs:
        variant='Startup' if name=='Seaside' else 'Whitebox'
        for kind in ['Windows','Project']:
            archive=ROOT/(name+'-Unity64-'+variant+'-'+kind+'.zip');errors=[];banned_entries=[];count=0
            with zipfile.ZipFile(archive) as z:
                names=z.namelist()
                prefix='Play64/'+('' if name=='Seaside' else name+'/');exe='SeasideWhitebox' if name=='Seaside' else name+'Whitebox'
                required=['Project6.4/Assets/Editor/SeasideTestScenes.cs',fixture['destination'],'Project6.4/Assets/Scenes/TheFirstScene_Seaside.unity'] if kind=='Project' else [prefix+exe+'.exe',prefix+exe+'_Data/Managed/Assembly-CSharp.dll',prefix+'UnityPlayer.dll',prefix+'MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll']
                errors += ['missing:'+p for p in required if p not in names]
                for info in z.infolist():
                    if info.is_dir():continue
                    count+=1;h=sha256()
                    with z.open(info) as f:
                        for chunk in iter(lambda:f.read(1024*1024),b''):h.update(chunk)
                    if any(info.filename==b or info.filename.startswith(b+'/') for b in banned):banned_entries.append(info.filename)
                    if info.filename.startswith('Play64/Docs/Logs/Runtime/') or info.filename.endswith('/verification.json'):continue
                    source=ROOT/info.filename
                    if not source.is_file() or digest(source)!=h.hexdigest():errors.append(info.filename)
            check(archive.name,not errors and not banned_entries,{'entriesReadAndCrcChecked':count,'errors':errors,'retiredEntries':banned_entries,'sha256':digest(archive)})
            print(archive.name,'PASS' if not errors and not banned_entries else 'FAIL',flush=True)
    for alias,target in [('森林','Forest'),('矿洞','Mine'),('雪山','Snow'),('灯塔','Lighthouse')]:
        check(alias+'试玩别名',digest(ROOT/(alias+'独立试玩.zip'))==digest(ROOT/(target+'-Unity64-Whitebox-Windows.zip')),{'target':target})
report['result']='PASS' if not report['failures'] else 'FAIL'
output=WORK/('source-verification.json' if '--sources-only' in sys.argv else 'verification.json')
output.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'result':report['result'],'failures':report['failures'],'cleanupTotals':report['cleanupTotals']},ensure_ascii=False))
raise SystemExit(0 if report['result']=='PASS' else 1)
