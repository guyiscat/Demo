from pathlib import Path
import collections, hashlib, json, re, zipfile, xml.etree.ElementTree as E
from pypdf import PdfReader
from PIL import Image, ImageDraw, ImageFont
ROOT=Path('D:/UnityProjects/Demo')
OUT=Path(__file__).parent
NS={'s':'http://schemas.openxmlformats.org/spreadsheetml/2006/main','a':'http://schemas.openxmlformats.org/drawingml/2006/main','x':'http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing'}
for filename in ['雪山地图.xlsx','灯塔地图.xlsx']:
    source=ROOT/'maps'/filename
    raw=source.read_bytes()
    report={'file':filename,'sha256':hashlib.sha256(raw).hexdigest(),'sheets':[],'drawings':[]}
    with zipfile.ZipFile(source) as z:
        styles=E.fromstring(z.read('xl/styles.xml'));fills=styles.find('s:fills',NS);formats=styles.find('s:cellXfs',NS)
        strings=[]
        if 'xl/sharedStrings.xml' in z.namelist():strings=[''.join(si.itertext()) for si in E.fromstring(z.read('xl/sharedStrings.xml'))]
        for file in sorted(f for f in z.namelist() if re.fullmatch(r'xl/worksheets/sheet\d+.xml',f)):
            sheet=E.fromstring(z.read(file));colored=[];texts=[];counts=collections.Counter();examples=collections.defaultdict(list)
            for c in sheet.findall('.//s:sheetData/s:row/s:c',NS):
                fill=fills[int(formats[int(c.get('s','0'))].get('fillId','0'))]
                key=E.tostring(fill,encoding='unicode');counts[key]+=1
                if len(examples[key])<8:examples[key].append(c.get('r'))
                v=c.find('s:v',NS);inline=c.find('s:is',NS)
                value=strings[int(v.text)] if v is not None and c.get('t')=='s' else ''.join(inline.itertext()) if inline is not None else v.text if v is not None else ''
                if value:texts.append({'address':c.get('r'),'text':value})
                pattern=fill.find('s:patternFill',NS)
                if pattern is not None and pattern.get('patternType')=='solid':
                    letters,row=re.fullmatch(r'([A-Z]+)(\d+)',c.get('r')).groups();col=0
                    for ch in letters:col=col*26+ord(ch)-64
                    colored.append({'address':c.get('r'),'col':col-1,'row':int(row)-1,'fill':dict(pattern.find('s:fgColor',NS).attrib)})
            details={'path':file,'dimension':sheet.find('s:dimension',NS).get('ref'),'texts':texts,'fills':[{'xml':key,'count':n,'examples':examples[key]} for key,n in counts.most_common()],'colored':colored}
            report['sheets'].append(details)
            if colored:
                minx=min(c['col'] for c in colored);maxx=max(c['col'] for c in colored);miny=min(c['row'] for c in colored);maxy=max(c['row'] for c in colored)
                scale=4;pad=45;im=Image.new('RGB',((maxx-minx+1)*scale+pad*2,(maxy-miny+1)*scale+pad*2),(238,242,234));d=ImageDraw.Draw(im)
                colors={'1':'101820','0':'999999','4':'4472c4','5':'ed7d31','6':'a5a5a5','7':'70ad47','8':'5b9bd5','9':'ffc000'}
                for c in colored:
                    f=c['fill'];color=f.get('rgb',colors.get(f.get('theme'),'aaffaa'))[-6:]
                    x=pad+(c['col']-minx)*scale;y=pad+(c['row']-miny)*scale
                    d.rectangle((x,y,x+scale-1,y+scale-1),fill='#'+color)
                for x in range(minx,maxx+1,10):d.text((pad+(x-minx)*scale,12),str(x+1),fill='black')
                for y in range(miny,maxy+1,10):d.text((4,pad+(y-miny)*scale),str(y+1),fill='black')
                im.save(OUT/(filename+'.png'))
        for file in sorted(f for f in z.namelist() if re.fullmatch(r'xl/drawings/drawing\d+.xml',f)):
            xml=E.fromstring(z.read(file))
            for anchor in xml:
                report['drawings'].append({'path':file,'text':[''.join(t.itertext()) for t in anchor.findall('.//a:t',NS)],'xml':E.tostring(anchor,encoding='unicode')})
    (OUT/(filename+'.json')).write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    compact={**report,'sheets':[{k:v for k,v in s.items() if k!='colored'} for s in report['sheets']]}
    print(json.dumps(compact,ensure_ascii=False,indent=2))
reader=PdfReader(ROOT/'Assets/文案.pdf')
(OUT/'design-text.txt').write_text('\n\n'.join(f'PAGE {i+1}\n'+p.extract_text() for i,p in enumerate(reader.pages)),encoding='utf-8')
print('PDF pages',len(reader.pages))
