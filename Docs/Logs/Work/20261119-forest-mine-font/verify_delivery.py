"""Read-only delivery checks; writes only the resulting report beside this file."""
from pathlib import Path
from hashlib import sha256
import json
import re
import zipfile
from datetime import datetime, timezone

ROOT = Path(r'D:\UnityProjects\Demo')
WORK = Path(__file__).parent
VALIDATION = ROOT / 'Docs/Backups/20261118-shared-systems-script-folders/Validation'
report = {'checkedAtUtc': datetime.now(timezone.utc).isoformat(), 'checks': {}, 'failures': []}
cache = {}

def digest(path):
    path = Path(path)
    if path not in cache:
        with path.open('rb') as stream:
            h = sha256()
            for chunk in iter(lambda: stream.read(1024 * 1024), b''):
                h.update(chunk)
        cache[path] = h.hexdigest()
    return cache[path]

def check(name, value, detail):
    report['checks'][name] = detail
    if not value:
        report['failures'].append(name)

def tree_compare(left, right, exclude_runtime=False):
    count, missing, changed, skipped = 0, [], [], []
    for source in left.rglob('*'):
        if not source.is_file():
            continue
        relative = source.relative_to(left)
        if exclude_runtime and relative.as_posix().startswith('Docs/Logs/Runtime/'):
            skipped.append(relative.as_posix())
            continue
        target = right / relative
        count += 1
        if not target.is_file():
            missing.append(relative.as_posix())
        elif digest(source) != digest(target):
            changed.append(relative.as_posix())
    return {'filesChecked': count, 'missing': missing, 'changed': changed,
            'excludedAppendOnlyRuntimeLogs': skipped}

for name, left, right, exclude in [
    ('mainBuildMatchesValidatedBuild', VALIDATION/'Play64', ROOT/'Play64', True),
    ('mainAssetsMatchValidatedSources', VALIDATION/'Project6.4/Assets', ROOT/'Project6.4/Assets', False),
]:
    result = tree_compare(left, right, exclude)
    check(name, not result['missing'] and not result['changed'], result)

mapping = json.loads((WORK/'script-guids.json').read_text(encoding='utf-8-sig'))
guid_errors = []
for item in mapping:
    script = ROOT/'Project6.4/Assets'/item['after']
    meta = Path(str(script)+'.meta')
    if not script.is_file() or not meta.is_file() or not re.search(
        r'^guid: '+re.escape(item['guid'])+r'\s*$', meta.read_text(encoding='utf-8-sig'), re.M):
        guid_errors.append(item)
check('originalScriptGuids', len(mapping) == 69 and not guid_errors,
      {'originalCount': len(mapping), 'errors': guid_errors})
old_paths = [str(p.relative_to(ROOT)) for base in ['Scripts', 'Editor']
             for folder in ['第一关','第二关']
             for p in [ROOT/'Project6.4/Assets'/base/folder, ROOT/'Project6.4/Assets'/base/(folder+'.meta')]
             if p.exists()]
check('oldChineseFoldersAbsent', not old_paths, {'remaining': old_paths})
missing_meta = [p.relative_to(ROOT).as_posix() for p in (ROOT/'Project6.4/Assets').rglob('*.cs')
                if not Path(str(p)+'.meta').is_file()]
check('allScriptsHaveMeta', not missing_meta, {'missing': missing_meta})

for name, map_json in [('森林地图.xlsx','ForestMap.json'), ('矿洞地图.xlsx','MineMap.json')]:
    imported = json.loads((ROOT/'Project6.4/Assets/Resources'/map_json).read_text(encoding='utf-8-sig'))
    current = digest(ROOT/'maps'/name)
    check('mapSource_'+map_json, current == imported['sourceSha256'],
          {'source': name, 'sourceSha256': current, 'importedSha256': imported['sourceSha256']})
original_font = ROOT/'Assets/AlibabaPuHuiTi-3-55-Regular.otf'
runtime_font = ROOT/'Project6.4/Assets/Resources/Fonts/AlibabaPuHuiTi-Regular.otf'
check('fontByteIdentity', digest(original_font) == digest(runtime_font), {'sha256': digest(runtime_font)})

protected = json.loads((WORK/'protected-content.json').read_text(encoding='utf-8-sig'))
for name, old in protected.items():
    if isinstance(old, dict) and 'sha256' in old:
        current = digest(ROOT/name)
        check('protected_'+name, current == old['sha256'], {'sha256': current})

project_required = [
    'Project6.4/Assets/Scenes/MineWhitebox.unity',
    'Project6.4/Assets/Scripts/mine/MineLevel.cs',
    'Project6.4/Assets/Scripts/seaside/SeasideHUD.cs',
    'Project6.4/Assets/Scripts/forest/ForestLevel.cs',
    'Project6.4/Assets/Resources/Fonts/AlibabaPuHuiTi-Regular.otf',
    'maps/森林地图.xlsx','maps/矿洞地图.xlsx','mine.cmd','forest.cmd','statgame.cmd']
targets = [('Forest','Whitebox','ForestWhitebox'), ('Mine','Whitebox','MineWhitebox'),
           ('Seaside','Startup','SeasideWhitebox')]
for level, variant, exe in targets:
    for kind in ['Windows', 'Project']:
        archive = ROOT/f'{level}-Unity64-{variant}-{kind}.zip'
        errors, missing, excluded, count = [], [], [], 0
        with zipfile.ZipFile(archive) as z:
            names = z.namelist()
            prefix = 'Play64/' + (level+'/' if level != 'Seaside' else '')
            required = project_required if kind == 'Project' else [
                prefix+exe+'.exe', prefix+'UnityPlayer.dll',
                prefix+'MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll',
                prefix+exe+'_Data/Managed/Assembly-CSharp.dll',
                'forest.cmd' if level == 'Forest' else 'mine.cmd' if level == 'Mine' else 'statgame.cmd']
            if kind == 'Windows' and level == 'Seaside':
                required += ['Play64/Mine/MineWhitebox.exe','Play64/Forest/ForestWhitebox.exe']
            missing = [name for name in required if name not in names]
            for info in z.infolist():
                if info.is_dir():
                    continue
                # Reading to EOF also verifies the stored ZIP CRC for every entry.
                h = sha256()
                with z.open(info) as stream:
                    for chunk in iter(lambda: stream.read(1024*1024), b''):
                        h.update(chunk)
                count += 1
                path = ROOT/info.filename
                if info.filename.startswith('Play64/Docs/Logs/Runtime/'):
                    excluded.append(info.filename)
                    continue
                if not path.is_file():
                    errors.append({'entry': info.filename, 'error':'sourceMissing'})
                elif h.hexdigest() != digest(path):
                    errors.append({'entry': info.filename, 'error':'sourceHashMismatch'})
        check(archive.name, not missing and not errors,
              {'entriesReadAndCrcChecked':count,'missingRequired':missing,'sourceMismatches':errors,
               'excludedAppendOnlyRuntimeLogs':excluded,'archiveSha256':digest(archive)})
        print(archive.name, 'PASS' if not missing and not errors else 'FAIL', flush=True)

for alias, source in [('森林独立试玩.zip','Forest-Unity64-Whitebox-Windows.zip'),
                      ('矿洞独立试玩.zip','Mine-Unity64-Whitebox-Windows.zip')]:
    check(alias, digest(ROOT/alias) == digest(ROOT/source), {'source':source,'sha256':digest(ROOT/alias)})
for folder in ['森林独立试玩','Forest-Unity64-Whitebox-Windows']:
    result = tree_compare(ROOT/'Play64/Forest', ROOT/folder/'Play64/Forest')
    extras = [p.relative_to(ROOT/folder/'Play64/Forest').as_posix()
              for p in (ROOT/folder/'Play64/Forest').rglob('*') if p.is_file()
              and not (ROOT/'Play64/Forest'/p.relative_to(ROOT/folder/'Play64/Forest')).exists()]
    result['extraFiles'] = extras
    matches = all(digest(ROOT/folder/'Docs'/name) == digest(ROOT/'Docs'/name)
                  for name in ['ForestWhitebox.md','ScriptLayout.md'])
    check('unpacked_'+folder, not result['missing'] and not result['changed'] and not extras and matches
          and digest(ROOT/folder/'forest.cmd') == digest(ROOT/'forest.cmd'), result)

report['result'] = 'PASS' if not report['failures'] else 'FAIL'
(WORK/'delivery-verification.json').write_text(json.dumps(report, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
print(json.dumps({'result': report['result'], 'failures': report['failures']}, ensure_ascii=False))
raise SystemExit(0 if report['result'] == 'PASS' else 1)
