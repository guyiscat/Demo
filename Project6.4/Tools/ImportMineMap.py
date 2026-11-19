"""Read-only workbook import. One authored cell is one world unit."""
from pathlib import Path
import argparse, collections, hashlib, json, re, zipfile
import xml.etree.ElementTree as ET
from PIL import Image, ImageDraw

ROOT=Path(__file__).resolve().parents[2]
NS={'s':'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}

def main():
    p=argparse.ArgumentParser();p.add_argument('--report-dir',required=True,type=Path);args=p.parse_args();args.report_dir.mkdir(parents=True,exist_ok=True)
    source=ROOT/'maps/矿洞地图.xlsx';raw=source.read_bytes();cells=[]
    with zipfile.ZipFile(source) as z:
        styles=ET.fromstring(z.read('xl/styles.xml'));fills=styles.find('s:fills',NS);formats=styles.find('s:cellXfs',NS)
        sheet=ET.fromstring(z.read('xl/worksheets/sheet1.xml'))
        for c in sheet.findall('.//s:sheetData/s:row/s:c',NS):
            fill=fills[int(formats[int(c.get('s','0'))].get('fillId','0'))].find('s:patternFill/s:fgColor',NS)
            if fill is None:continue
            theme,rgb,tint=fill.get('theme'),fill.get('rgb'),float(fill.get('tint','0'))
            kind={'FFFF0000':'spike','FF7030A0':'checkpoint','FFFFC000':'lift','FF00BAF0':'relic','FF0070C0':'marker'}.get(rgb)
            if theme=='1':kind='ground'
            elif theme=='0' and tint<0:kind='noGrip'
            elif theme=='4':kind='route'
            elif theme=='7':kind='zipline'
            elif theme=='8':kind='workbench'
            if kind:
                letters,row=re.fullmatch(r'([A-Z]+)(\d+)',c.get('r')).groups();col=0
                for ch in letters:col=col*26+ord(ch)-64
                cells.append(dict(address=c.get('r'),col=col-1,row=int(row)-1,kind=kind))
    left=min(c['col'] for c in cells);top=min(c['row'] for c in cells);bottom=max(c['row'] for c in cells);right=max(c['col'] for c in cells)
    for c in cells:c.update(x=c['col']-left,y=bottom-c['row'])
    data=dict(source='maps/矿洞地图.xlsx',sourceSha256=hashlib.sha256(raw).hexdigest(),sheet='Sheet1',width=right-left+1,height=bottom-top+1,minColumn=left,topRow=top,bottomRow=bottom,teleportDistance=10,cells=cells,drawings=[])
    (ROOT/'Project6.4/Assets/Resources/MineMap.json').write_text(json.dumps(data,ensure_ascii=False,separators=(',',':')),encoding='utf-8')
    groups={}
    for kind in sorted({c['kind'] for c in cells}):
        pending={(c['x'],c['y']):c for c in cells if c['kind']==kind};result=[]
        while pending:
            stack=[next(iter(pending))];group=[]
            while stack:
                x,y=stack.pop();c=pending.pop((x,y),None)
                if c is None:continue
                group.append(c);stack.extend([(x+1,y),(x-1,y),(x,y+1),(x,y-1)])
            result.append(dict(first=group[0]['address'],count=len(group),minX=min(c['x'] for c in group),maxX=max(c['x'] for c in group)+1,minY=min(c['y'] for c in group),maxY=max(c['y'] for c in group)+1))
        groups[kind]=result
    report=dict(sourceSha256=data['sourceSha256'],width=data['width'],height=data['height'],counts=dict(collections.Counter(c['kind'] for c in cells)),groups=groups)
    (args.report_dir/'mine-import.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    scale=5;margin=40;im=Image.new('RGB',(data['width']*scale+margin*2,data['height']*scale+margin*2),(237,241,232));d=ImageDraw.Draw(im)
    palette=dict(ground='#101820',noGrip='#888888',spike='#ed3333',zipline='#43ad4b',route='#4278df',workbench='#50c6b0',lift='#ffc000',checkpoint='#7030a0',relic='#00baf0',marker='#0070c0')
    for c in cells:
        x=margin+c['x']*scale;y=margin+(data['height']-1-c['y'])*scale;d.rectangle((x,y,x+scale-1,y+scale-1),fill=palette[c['kind']])
    for x in range(0,data['width'],10):d.text((margin+x*scale,12),str(x),fill='black')
    for y in range(0,data['height'],10):d.text((5,margin+(data['height']-y)*scale),str(y),fill='black')
    im.save(args.report_dir/'mine-source-map.png');print(json.dumps(report,ensure_ascii=False,indent=2))

if __name__=='__main__':main()
