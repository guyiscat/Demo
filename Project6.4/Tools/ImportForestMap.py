"""Read the original workbook without Excel automation or modifying it.

Cell geometry uses one square cell per Unity unit, as in Seaside. Drawing
anchors use fractional cell coordinates (the file uses 19 1/3 px columns and
18 2/3 px rows). Existing scenes are never opened or rewritten by this tool.
"""
from pathlib import Path
import argparse
import collections
import hashlib
import json
import math
import re
import xml.etree.ElementTree as ET
import zipfile

ROOT = Path(__file__).resolve().parents[2]
NS = {"s": "http://schemas.openxmlformats.org/spreadsheetml/2006/main",
      "x": "http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing",
      "a": "http://schemas.openxmlformats.org/drawingml/2006/main"}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--report-dir", type=Path, required=True)
    args = parser.parse_args()
    args.report_dir.mkdir(parents=True, exist_ok=True)
    source = ROOT / "maps/森林地图.xlsx"
    with source.open("rb") as file:
        source_hash = hashlib.sha256(file.read()).hexdigest()
    cells = []
    with zipfile.ZipFile(source) as archive:
        styles = ET.fromstring(archive.read("xl/styles.xml"))
        fills = styles.find("s:fills", NS)
        formats = styles.find("s:cellXfs", NS)
        sheet = ET.fromstring(archive.read("xl/worksheets/sheet1.xml"))
        for cell in sheet.findall(".//s:sheetData/s:row/s:c", NS):
            fmt = formats[int(cell.get("s", "0"))]
            fill = fills[int(fmt.get("fillId", "0"))].find("s:patternFill/s:fgColor", NS)
            if fill is None:
                continue
            theme, tint, rgb = fill.get("theme"), float(fill.get("tint", "0")), fill.get("rgb")
            kind = None
            if theme == "1": kind = "ground"
            elif theme == "0" and tint < 0: kind = "noGrip"
            elif rgb == "FFFF0000": kind = "spike"
            elif theme == "4": kind = "route"
            elif theme == "7": kind = "zipline"
            elif theme == "8": kind = "unlock"
            if kind:
                address = cell.get("r")
                letters, row = re.fullmatch(r"([A-Z]+)(\d+)", address).groups()
                col = 0
                for letter in letters: col = col * 26 + ord(letter) - 64
                cells.append(dict(col=col - 1, row=int(row) - 1, kind=kind, address=address))
        drawing = ET.fromstring(archive.read("xl/drawings/drawing1.xml"))
        shapes = []
        for anchor in drawing.findall("x:twoCellAnchor", NS):
            shape = anchor.find("x:sp", NS)
            geom = shape.find("x:spPr/a:prstGeom", NS).get("prst")
            points = []
            for tag in ("from", "to"):
                pt = anchor.find("x:" + tag, NS)
                col, row = int(pt.find("x:col", NS).text), int(pt.find("x:row", NS).text)
                col += int(pt.find("x:colOff", NS).text) / 184150
                row += int(pt.find("x:rowOff", NS).text) / 177800
                points.append([col, row])
            shapes.append(dict(kind=geom, start=points[0], end=points[1]))

    # The two upper-left blue squares are distance legend markers, not routes.
    legend = [c for c in cells if c["row"] < 10]
    level = [c for c in cells if c["row"] >= 10]
    min_col, max_col = min(c["col"] for c in level), max(c["col"] for c in level)
    min_row, max_row = min(c["row"] for c in level), max(c["row"] for c in level)
    def position(col, row): return dict(x=round(col-min_col, 5), y=round(max_row+1-row, 5))
    for c in level:
        c.update(x=c["col"]-min_col, y=max_row-c["row"])
    legend_cols = sorted(set(c["col"] for c in legend))
    teleport = (sum(legend_cols[-2:])-sum(legend_cols[:2])) / 2
    drawing_data = []
    for shape in shapes:
        drawing_data.append(dict(kind=shape["kind"], start=position(*shape["start"]), end=position(*shape["end"])))
    data = dict(source="maps/森林地图.xlsx", sourceSha256=source_hash, sheet="Sheet1",
                width=max_col-min_col+1, height=max_row-min_row+1,
                minColumn=min_col, topRow=min_row, bottomRow=max_row,
                teleportDistance=teleport, legend="L6:M7 → V6:W7 (center distance = 10 cells)",
                cells=level, drawings=drawing_data)
    dest = ROOT / "Project6.4/Assets/Resources/ForestMap.json"
    dest.write_text(json.dumps(data, ensure_ascii=False, separators=(",", ":")), encoding="utf-8")
    groups = {}
    for kind in sorted(set(c["kind"] for c in level)):
        pending = {(c["col"], c["row"]): c for c in level if c["kind"] == kind}
        result = []
        while pending:
            stack = [next(iter(pending))]; group = []
            while stack:
                p = stack.pop()
                c = pending.pop(p, None)
                if c is None: continue
                group.append(c)
                stack.extend((p[0]+dx, p[1]+dy) for dx, dy in [(1,0),(-1,0),(0,1),(0,-1)])
            result.append(dict(count=len(group), first=group[0]["address"],
                               minX=min(c["x"] for c in group), maxX=max(c["x"] for c in group)+1,
                               minY=min(c["y"] for c in group), maxY=max(c["y"] for c in group)+1))
        groups[kind] = result
    summary = dict(sourceSha256=source_hash, width=data["width"], height=data["height"],
                   bounds=[min_col,min_row,max_col,max_row], teleportDistance=teleport,
                   counts=dict(collections.Counter(c["kind"] for c in level)), drawings=drawing_data, groups=groups)
    (args.report_dir / "map-import.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
    # A read-only diagnostic raster: exact cell fills plus the three drawing anchors.
    from PIL import Image, ImageDraw
    scale=5; margin=45
    image=Image.new("RGB",(data["width"]*scale+2*margin,data["height"]*scale+2*margin),(237,241,232))
    draw=ImageDraw.Draw(image)
    colors=dict(ground="#101820", noGrip="#888888", spike="#ed3333", zipline="#43ad4b", route="#4278df",unlock="#50c6de")
    for c in level:
        x=margin+c["x"]*scale;y=margin+(data["height"]-1-c["y"])*scale
        draw.rectangle((x,y,x+scale-1,y+scale-1),fill=colors[c["kind"]])
    for shape in drawing_data:
        a,b=shape["start"],shape["end"]
        rect=(margin+a["x"]*scale,margin+(data["height"]-a["y"])*scale,margin+b["x"]*scale,margin+(data["height"]-b["y"])*scale)
        if shape["kind"]=="ellipse":draw.ellipse(rect,outline="#7d5129",width=2)
        elif shape["kind"]=="rect":draw.rectangle(rect,fill="#8dcde5",outline="#3394ce",width=1)
    for x in range(0,data["width"],10):draw.text((margin+x*scale,15),str(x),fill="black")
    for y in range(0,data["height"],10):draw.text((5,margin+(data["height"]-y)*scale),str(y),fill="black")
    image.save(args.report_dir / "forest-source-map.png")
    print(json.dumps({k:v for k,v in summary.items() if k!="groups"},ensure_ascii=False,indent=2))
    for k in ["zipline","route","unlock","noGrip"]:print(k,json.dumps(groups[k]))

if __name__ == "__main__": main()
