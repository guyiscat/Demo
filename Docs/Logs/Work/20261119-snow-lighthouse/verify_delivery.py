"""Verify shipped sources/binaries/ZIPs without changing project or input maps."""
from pathlib import Path
from hashlib import sha256
import collections
import json
import re
import sys
import zipfile
from datetime import datetime, timezone

ROOT=Path('D:/UnityProjects/Demo')
WORK=Path(__file__).parent
BACKUP=ROOT/'Docs/Backups/20261119-snow-lighthouse'
VALIDATION=ROOT/'Docs/Backups/20261118-shared-systems-script-folders/Validation'
result={'checkedAtUtc':datetime.now(timezone.utc).isoformat(),'checks':{},'failures':[]}
hashes={}
def digest(path):
    path=Path(path)
    if path not in hashes:
        h=sha256()
        with path.open('rb') as stream:
            for chunk in iter(lambda:stream.read(1024*1024),b''):h.update(chunk)
        hashes[path]=h.hexdigest()
    return hashes[path]
def check(name,ok,detail):
    result['checks'][name]=detail
    if not ok:result['failures'].append(name)
def tree(left,right,skip_runtime=False):
    missing=[];changed=[];excluded=[];count=0
    for p in left.rglob('*'):
        if not p.is_file():continue
        relative=p.relative_to(left)
        if skip_runtime and relative.as_posix().startswith('Docs/Logs/Runtime/'):
            excluded.append(relative.as_posix());continue
        count+=1;target=right/relative
        if not target.is_file():missing.append(relative.as_posix())
        elif digest(p)!=digest(target):changed.append(relative.as_posix())
    return {'filesChecked':count,'missing':missing,'changed':changed,'excludedAppendOnlyRuntimeLogs':excluded}
for name,left,right,skip in [
    ('assetsMatchBuiltSources',VALIDATION/'Project6.4/Assets',ROOT/'Project6.4/Assets',False),
    ('mainBuildsMatchValidatedBuilds',VALIDATION/'Play64',ROOT/'Play64',True),
    ('toolsMatchBuildTools',ROOT/'Project6.4/Tools',VALIDATION/'Project6.4/Tools',False),
]:
    report=tree(left,right,skip);check(name,not report['missing'] and not report['changed'],report)

protected=[]
for p in (BACKUP/'Project6.4/Assets/Scenes').glob('*'):
    if p.is_file():protected.append(p.relative_to(BACKUP))
protected += [Path('Project6.4/Assets/Settings/Movement.asset'),Path('Project6.4/Assets/Resources/GameSystems.asset'),Path('Project6.4/ProjectSettings/EditorBuildSettings.asset')]
changed=[str(p) for p in protected if digest(BACKUP/p)!=digest(ROOT/p)]
check('originalScenesAndSharedParametersUnchanged',not changed,{'files':len(protected),'changed':changed})
guid_errors=[];script_count=0
for old in (BACKUP/'Project6.4/Assets').rglob('*.cs'):
    script_count+=1;relative=old.relative_to(BACKUP);current=ROOT/relative
    old_meta=Path(str(old)+'.meta');new_meta=Path(str(current)+'.meta')
    old_guid=re.search(r'^guid: (\w+)',old_meta.read_text(encoding='utf-8-sig'),re.M).group(1)
    if not current.is_file() or not new_meta.is_file() or not re.search(r'^guid: '+old_guid+r'\s*$',new_meta.read_text(encoding='utf-8-sig'),re.M):guid_errors.append(str(relative))
check('allPreexistingScriptGuidsPreserved',not guid_errors,{'scripts':script_count,'errors':guid_errors})
no_meta=[p.relative_to(ROOT).as_posix() for p in (ROOT/'Project6.4/Assets').rglob('*.cs') if not Path(str(p)+'.meta').is_file()]
check('allScriptsHaveMetadata',not no_meta,{'missing':no_meta})
for key,filename in [('Snow','雪山地图.xlsx'),('Lighthouse','灯塔地图.xlsx')]:
    imported=json.loads((ROOT/'Project6.4/Assets/Resources'/(key+'Map.json')).read_text(encoding='utf-8-sig'))
    source_hash=digest(ROOT/'maps'/filename)
    check(key+'SourceUnchanged',source_hash==digest(BACKUP/'maps'/filename)==imported['sourceSha256'],{'sourceSha256':source_hash,'importedSha256':imported['sourceSha256']})
font=ROOT/'Project6.4/Assets/Resources/Fonts/AlibabaPuHuiTi-Regular.otf'
check('bundledFontUnchanged',digest(font)==digest(ROOT/'Assets/AlibabaPuHuiTi-3-55-Regular.otf')==digest(BACKUP/'Project6.4/Assets/Resources/Fonts/AlibabaPuHuiTi-Regular.otf'),{'sha256':digest(font)})
plan=Path('C:/Users/user/Desktop/maker games/demo/DEMO游戏策划案.pdf')
result['sources']={'fullDesignPath':str(plan),'fullDesignSha256':digest(plan),'dialogueSha256':digest(ROOT/'Assets/文案.pdf')}

runs={
    'Snow':('20261119-212540-178','snow-runtime-checks.txt','snow-editor-checks.txt'),
    'Lighthouse':('20261119-212608-599','lighthouse-runtime-checks.txt','lighthouse-editor-checks.txt'),
    'Forest':('20261119-212746-874','forest-runtime-checks.txt','forest-editor-checks.txt'),
    'Mine':('20261119-212800-825','mine-runtime-checks.txt','mine-editor-checks.txt'),
    'Seaside':('20261119-212815-293',None,None),
}
summary={}
for target,(run,runtime,scene) in runs.items():
    folder=ROOT/'Docs/Logs/Build'/run;status=json.loads((folder/'status.json').read_text(encoding='utf-8-sig'))
    reports={}
    names=['editor-reports/system-editor-checks.txt','editor-reports/physics-tests.txt','editor-reports/playtest-regression.txt']
    if scene:names+=['editor-reports/'+scene]
    if runtime:names+=[runtime]
    for name in names:
        counts=collections.Counter(line.split(' ',1)[0] for line in (folder/name).read_text(encoding='utf-8-sig').splitlines())
        reports[name]={'passed':counts['PASS'],'failed':counts['FAIL']}
    code_errors=re.findall(r'(?:error|warning) CS\d+',(folder/'editor-build.log').read_text(encoding='utf-8-sig'))
    summary[target]={'run':run,'result':status['result'],'reports':reports,'csharpDiagnostics':code_errors}
    check('finalBuild_'+target,status['result']=='PASS' and not code_errors and all(x['failed']==0 and x['passed']>0 for x in reports.values()),summary[target])
(WORK/'final-test-summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
if '--sources-only' in sys.argv:
    result['result']='PASS' if not result['failures'] else 'FAIL'
    (WORK/'source-verification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({'result':result['result'],'failures':result['failures']},ensure_ascii=False))
    raise SystemExit(0 if result['result']=='PASS' else 1)

project_required=['Project6.4/Assets/Scenes/SnowWhitebox.unity','Project6.4/Assets/Scenes/LighthouseWhitebox.unity',
    'Project6.4/Assets/Scripts/lighthouse/LighthouseCollapse.cs','Project6.4/Assets/Scripts/snow/SnowLevel.cs',
    'Project6.4/Assets/Scripts/ThermalZipline.cs','Project6.4/Assets/Resources/Fonts/AlibabaPuHuiTi-Regular.otf',
    'maps/雪山地图.xlsx','maps/灯塔地图.xlsx','snow.cmd','lighthouse.cmd','mine.cmd','forest.cmd','statgame.cmd']
for target in ['Snow','Lighthouse','Forest','Mine','Seaside']:
    variant='Startup' if target=='Seaside' else 'Whitebox'
    for kind in ['Windows','Project']:
        path=ROOT/(target+'-Unity64-'+variant+'-'+kind+'.zip');missing=[];errors=[];excluded=[];count=0
        with zipfile.ZipFile(path) as z:
            names=z.namelist();prefix='Play64/'+(target+'/' if target!='Seaside' else '');exe='SeasideWhitebox' if target=='Seaside' else target+'Whitebox'
            required=project_required if kind=='Project' else [prefix+exe+'.exe',prefix+'UnityPlayer.dll',prefix+'MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll',prefix+exe+'_Data/Managed/Assembly-CSharp.dll',target.lower()+'.cmd' if target!='Seaside' else 'statgame.cmd']
            if target=='Seaside' and kind=='Windows':required+=['Play64/Snow/SnowWhitebox.exe','Play64/Lighthouse/LighthouseWhitebox.exe','snow.cmd','lighthouse.cmd']
            missing=[p for p in required if p not in names]
            for info in z.infolist():
                if info.is_dir():continue
                h=sha256()
                with z.open(info) as stream:
                    for chunk in iter(lambda:stream.read(1024*1024),b''):h.update(chunk)
                count+=1;source=ROOT/info.filename
                if info.filename.startswith('Play64/Docs/Logs/Runtime/'):
                    excluded.append(info.filename);continue
                if info.filename.endswith('/delivery-verification.json') or info.filename.endswith('/final-test-summary.json'):
                    # This report is written after packaging; do not create a cyclic hash dependency.
                    excluded.append(info.filename);continue
                if not source.is_file() or digest(source)!=h.hexdigest():errors.append(info.filename)
        check(path.name,not missing and not errors,{'entriesReadAndCrcVerified':count,'missingRequired':missing,'sourceMismatches':errors,'excludedMutableReports':excluded,'sha256':digest(path)})
        print(path.name,'PASS' if not missing and not errors else 'FAIL',flush=True)
for alias,standard in [('雪山独立试玩.zip','Snow-Unity64-Whitebox-Windows.zip'),('灯塔独立试玩.zip','Lighthouse-Unity64-Whitebox-Windows.zip'),('森林独立试玩.zip','Forest-Unity64-Whitebox-Windows.zip'),('矿洞独立试玩.zip','Mine-Unity64-Whitebox-Windows.zip')]:
    check(alias,digest(ROOT/alias)==digest(ROOT/standard),{'source':standard,'sha256':digest(ROOT/alias)})
for folder in ['森林独立试玩','Forest-Unity64-Whitebox-Windows']:
    report=tree(ROOT/'Play64/Forest',ROOT/folder/'Play64/Forest')
    check('unpacked_'+folder,not report['missing'] and not report['changed'] and digest(ROOT/'Docs/ScriptLayout.md')==digest(ROOT/folder/'Docs/ScriptLayout.md'),report)

result['result']='PASS' if not result['failures'] else 'FAIL'
(WORK/'delivery-verification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'result':result['result'],'failures':result['failures']},ensure_ascii=False))
raise SystemExit(0 if result['result']=='PASS' else 1)
