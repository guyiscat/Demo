"""Read-only Snow/Lighthouse import. Colored cells retain their authored addresses."""
from pathlib import Path
import argparse, collections, hashlib, json, re, zipfile
import xml.etree.ElementTree as ET
from PIL import Image, ImageDraw

ROOT=Path(__file__).resolve().parents[2]
NS={'s':'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}

def groups(cells):
    pending={(c['col'],c['row']):c for c in cells};result=[]
    while pending:
        stack=[next(iter(pending))];group=[]
        while stack:
            x,y=stack.pop();c=pending.pop((x,y),None)
            if c is None:continue
            group.append(c);stack.extend([(x+1,y),(x-1,y),(x,y+1),(x,y-1)])
        result.append(group)
    return result

def import_map(filename,resource,output):
    source=ROOT/'maps'/filename;raw=source.read_bytes();cells=[]
    with zipfile.ZipFile(source) as z:
        styles=ET.fromstring(z.read('xl/styles.xml'));fills=styles.find('s:fills',NS);formats=styles.find('s:cellXfs',NS)
        sheet=ET.fromstring(z.read('xl/worksheets/sheet1.xml'));unknown=collections.Counter()
        for c in sheet.findall('.//s:sheetData/s:row/s:c',NS):
            pattern=fills[int(formats[int(c.get('s','0'))].get('fillId','0'))].find('s:patternFill',NS)
            if pattern is None or pattern.get('patternType')!='solid':continue
            fill=pattern.find('s:fgColor',NS)
            theme,rgb,tint=fill.get('theme'),fill.get('rgb'),float(fill.get('tint','0'))
            kind=None
            if theme=='1':kind='ground'
            elif theme=='0' and tint<0:kind='noGrip'
            elif rgb=='FFFF0000':kind='spike'
            elif theme=='4':kind='checkpoint'
            elif theme=='9':kind='hot'
            elif rgb=='FF00B0F0' or resource=='LighthouseMap' and theme=='8':kind='cold'
            elif resource=='SnowMap' and theme=='8':kind='fragment'
            if kind is None:unknown[str(fill.attrib)]+=1;continue
            letters,row=re.fullmatch(r'([A-Z]+)(\d+)',c.get('r')).groups();col=0
            for ch in letters:col=col*26+ord(ch)-64
            cells.append(dict(address=c.get('r'),col=col-1,row=int(row)-1,kind=kind))
        if unknown:raise ValueError(f'Unmapped solid fills in {filename}: {unknown}')
    for temperature in ['cold','hot']:
        for group in groups([c for c in cells if c['kind']==temperature]):
            width=max(c['col'] for c in group)-min(c['col'] for c in group)+1
            height=max(c['row'] for c in group)-min(c['row'] for c in group)+1
            kind=temperature+'Point' if width<=2 and height<=2 and len(group)==4 else temperature+'Zipline' if height==1 and width>=5 else temperature+'Surface'
            for c in group:c['kind']=kind
    left=min(c['col'] for c in cells);top=min(c['row'] for c in cells);bottom=max(c['row'] for c in cells);right=max(c['col'] for c in cells)
    for c in cells:c.update(x=c['col']-left,y=bottom-c['row'])
    data=dict(source='maps/'+filename,sourceSha256=hashlib.sha256(raw).hexdigest(),sheet='Sheet1',width=right-left+1,height=bottom-top+1,minColumn=left,topRow=top,bottomRow=bottom,teleportDistance=10,cells=cells,drawings=[])
    (ROOT/'Project6.4/Assets/Resources'/(resource+'.json')).write_text(json.dumps(data,ensure_ascii=False,separators=(',',':')),encoding='utf-8')
    summary=dict(source=data['source'],sourceSha256=data['sourceSha256'],width=data['width'],height=data['height'],counts=dict(collections.Counter(c['kind'] for c in cells)),groups={})
    for kind in sorted({c['kind'] for c in cells}):
        summary['groups'][kind]=[dict(first=g[0]['address'],count=len(g),minX=min(c['x'] for c in g),maxX=max(c['x'] for c in g)+1,minY=min(c['y'] for c in g),maxY=max(c['y'] for c in g)+1) for g in groups([c for c in cells if c['kind']==kind])]
    output.mkdir(parents=True,exist_ok=True)
    (output/(resource+'-import.json')).write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
    palette=dict(ground='#111b17',noGrip='#999d9b',spike='#ef3333',checkpoint='#4874cb',coldSurface='#00b0f0',coldZipline='#00b0f0',coldPoint='#00b0f0',hotSurface='#ef94a0',hotZipline='#ef94a0',hotPoint='#ef94a0',fragment='#30c0b4')
    scale=5;pad=35;im=Image.new('RGB',(data['width']*scale+pad*2,data['height']*scale+pad*2),(224,233,214));d=ImageDraw.Draw(im)
    for c in cells:
        x=pad+c['x']*scale;y=pad+(data['height']-1-c['y'])*scale
        d.rectangle((x,y,x+scale-1,y+scale-1),fill=palette[c['kind']])
    im.save(output/(resource+'-source.png'))
    print(resource,data['width'],data['height'],summary['counts'],flush=True)

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--report-dir',required=True,type=Path);args=parser.parse_args()
    import_map('雪山地图.xlsx','SnowMap',args.report_dir)
    import_map('灯塔地图.xlsx','LighthouseMap',args.report_dir)
