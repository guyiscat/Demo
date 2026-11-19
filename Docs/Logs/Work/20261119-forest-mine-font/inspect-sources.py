from pathlib import Path
import collections, importlib.util, json, re, zipfile, xml.etree.ElementTree as E
from PIL import Image
ROOT = Path('D:/UnityProjects/Demo')
OUT = ROOT/'Docs/Logs/Work/20261119-forest-mine-font'
N={'s':'http://schemas.openxmlformats.org/spreadsheetml/2006/main','a':'http://schemas.openxmlformats.org/drawingml/2006/main'}
for name in ['森林地图.xlsx','矿洞地图.xlsx']:
    with zipfile.ZipFile(ROOT/'maps'/name) as z:
        styles=E.fromstring(z.read('xl/styles.xml'));fills=styles.find('s:fills',N);fmts=styles.find('s:cellXfs',N)
        strings=[]
        if 'xl/sharedStrings.xml' in z.namelist():
            strings=[''.join(si.itertext()) for si in E.fromstring(z.read('xl/sharedStrings.xml'))]
        print('\nMAP',name)
        for file in [f for f in z.namelist() if re.fullmatch(r'xl/worksheets/sheet\d+.xml',f)]:
            sheet=E.fromstring(z.read(file));counts=collections.Counter();examples=collections.defaultdict(list)
            for c in sheet.findall('.//s:sheetData/s:row/s:c',N):
                fill=fills[int(fmts[int(c.get('s','0'))].get('fillId','0'))];key=E.tostring(fill,encoding='unicode');counts[key]+=1
                if len(examples[key])<7:examples[key].append(c.get('r'))
                v=c.find('s:v',N)
                text=strings[int(v.text)] if v is not None and c.get('t')=='s' else ''.join(c.find('s:is',N).itertext()) if c.find('s:is',N) is not None else v.text if v is not None else ''
                if text: print('TEXT',c.get('r'),text)
            print('SHEET',file,'dimension',sheet.find('s:dimension',N).attrib)
            for key,n in counts.most_common():print('FILL',n,examples[key],key)
        for f in [f for f in z.namelist() if re.fullmatch(r'xl/drawings/drawing\d+.xml',f)]:
            print('DRAWINGS',f);(OUT/(name+'.drawings.xml')).write_bytes(z.read(f))
            print(z.read(f).decode()[:7000])
print('PSD_TOOLS', bool(importlib.util.find_spec('psd_tools')))
for path in (ROOT/'Assets').glob('IMG*.psd'):
    im=Image.open(path);print('PSD',path.name, im.size,im.mode, im.info.keys());im.thumbnail((1200,900));im.convert('RGB').save(OUT/(path.stem+'.png'))
