"""Read the two object-based historical libraries through the actual .NET descriptor."""
import argparse
import csv
from html import escape
import json
from pathlib import Path
import subprocess
import tempfile
from xml.sax.saxutils import escape as xml_escape


HOST = r"""
using System.Text.Json;
using Gojo.Experiments.OrnamentTension;
var train=HistoricalReferences.Examples.Where(e=>!e.Holdout).ToArray();
var validation=HistoricalReferences.Examples.Where(e=>e.Holdout).ToArray();
var comparisons=new List<object>();
foreach(var item in validation) {
    var signature=TensionCritics.Describe(HistoricalReferences.Draw(item),4);
    foreach(var reference in train) {
        var read=HistoricalReferences.Read(signature,reference.Family,false);
        comparisons.Add(new {holdout=item.Family,training=reference.Family,similarity=read.Match});
    }
}
File.WriteAllText(args[0],JsonSerializer.Serialize(new{examples=HistoricalReferences.Examples,comparisons},
    new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase}));
"""


def native_read(runtime,output):
    with tempfile.TemporaryDirectory(prefix="GojoHistoricalCompare-") as folder:
        src=Path(folder)
        refs=[]
        for name in ("OrnamentTension","OrnamentPathCampaign","PrimitiveMeasure"):
            file=runtime/f"{name}.dll"
            if not file.exists(): raise ValueError(file)
            refs.append(f'<Reference Include="{name}"><HintPath>{xml_escape(str(file))}</HintPath></Reference>')
        (src/"Survey.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
            '<TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType>'
            '<ImplicitUsings>enable</ImplicitUsings></PropertyGroup><ItemGroup>'
            +''.join(refs)+'</ItemGroup></Project>',encoding="utf-8")
        (src/"Program.cs").write_text(HOST,encoding="utf-8")
        subprocess.run(["dotnet","run","--project",str(src/"Survey.csproj"),"-c","Release","--",str(output)],check=True)


def sketch(example,width=280,height=82):
    paths=[[[int(n) for n in xy.split(",")] for xy in path.split()] for path in example["paths"]]
    pitch=example["pitch"]
    ink=[[(x+c*pitch,y) for x,y in path] for c in range(3) for path in paths]
    points=[p for line in ink for p in line]
    minx=min(p[0] for p in points);maxx=max(p[0] for p in points)
    miny=min(p[1] for p in points);maxy=max(p[1] for p in points)
    scale=min((width-18)/max(1,maxx-minx),(height-18)/max(1,maxy-miny))
    lines=[]
    for path in ink:
        coords=" ".join(f"{width/2+(x-(maxx+minx)/2)*scale:.2f},{height/2-(y-(maxy+miny)/2)*scale:.2f}"
                        for x,y in path)
        lines.append(f'<polyline points="{coords}" fill="none" stroke="#235449" stroke-width="2" stroke-linejoin="round"/>')
    return f'<svg viewBox="0 0 {width} {height}" aria-label="Three angular reference repeats">'+''.join(lines)+'</svg>'


def build(runtime,folder):
    folder.mkdir(parents=True,exist_ok=True)
    native_read(runtime,folder/"packets.json")
    records=json.loads((folder/"packets.json").read_text(encoding="utf-8-sig"))
    samples=records["examples"]
    keys=[e["family"] for e in samples if not e["holdout"]]
    by_key={(e["family"],e["holdout"]):e for e in samples}
    comparisons=records["comparisons"]
    ranks=[]
    for family in keys:
        candidates=sorted((r for r in comparisons if r["holdout"]==family),key=lambda x:-x["similarity"])
        match=next(r["similarity"] for r in candidates if r["training"]==family)
        rank=1+sum(r["similarity"]>match+1e-12 for r in candidates)
        second=max(r["similarity"] for r in candidates if r["training"]!=family)
        ranks.append(dict(family=family,rank=rank,best=candidates[0]["training"],similarity=match,
                          margin_against_best_other=match-second))
    with (folder/"matrix.csv").open("w",encoding="utf-8",newline="") as f:
        w=csv.DictWriter(f,fieldnames=list(comparisons[0]));w.writeheader();w.writerows(comparisons)
    (folder/"reference-ranks.json").write_text(json.dumps(ranks,indent=2),encoding="utf-8")
    html=['''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Historical reference packets</title><style>
body{margin:2.5rem auto;padding:0 1rem;max-width:1020px;background:#f7f4e9;color:#203e36;font:16px/1.5 system-ui}
h1,h2{font-family:Georgia,serif;font-weight:normal}h1{font-size:clamp(32px,5vw,60px);margin:.3em 0}h2{margin:1rem 0 .15rem}
h3{font-size:1rem;margin:0}a{color:#216655}p{max-width:80ch}article{border-top:1px solid #9ba99e;padding:.7rem 0}
.pair{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:.8rem}figure{margin:0}svg{width:100%;height:100px;background:#eeeadd}
table{display:block;overflow:auto;border-collapse:collapse;font-size:13px}td,th{border:1px solid #c2c9ba;padding:.35rem}.target{background:#c8dec8}code{font-size:13px}
@media(max-width:600px){.pair{grid-template-columns:1fr}}
</style><h1>Historical reference packets</h1><p>Left and right show independently sourced interpretations of the same requested focus.
The printed outlines of Meyer's two examples were read against the construction grid. The other lines are new angular illustrations of museum descriptions.
Only the silhouettes enter the reference reader. Colors, culture, actual pen order and depth are unavailable here.</p>''']
    correct=sum(r["rank"]==1 for r in ranks)
    html.append(f'<p><strong>{correct}/8</strong> withheld sketches retrieved their named training family first among all eight. '
                'The source drawings and eight family labels were selected by the experimenters. '
                'This retrieval check characterizes the instrument; it does not rate beauty or establish human preference.</p>')
    for r in ranks:
        family=r["family"]; html.append(f'<article><h2>{escape(family)}</h2><div class="pair">')
        for held in (False,True):
            item=by_key[family,held]
            html.append(f'<figure><h3>{"Withheld" if held else "Training"} focus</h3>{sketch(item)}'
                        f'<figcaption>{escape(item["focus"])}. <a href="{escape(item["source"])}">{escape(item["object"])}</a>'
                        '</figcaption></figure>')
        html.append(f'</div><p>Intended similarity {r["similarity"]:.3f}; rank {r["rank"]} of 8. '
                    f'Nearest training family: {escape(r["best"])}.</p></article>')
    html.append('<h2>All sixty-four comparisons</h2><table><tr><th>Withheld / training</th>')
    for family in keys:html.append(f'<th>{escape(family)}</th>')
    html.append('</tr>')
    lookup={(r["holdout"],r["training"]):r["similarity"] for r in comparisons}
    for family in keys:
        html.append(f'<tr><th>{escape(family)}</th>')
        for train in keys:
            style=' class="target"' if family==train else ''
            html.append(f'<td{style}>{lookup[family,train]:.3f}</td>')
        html.append('</tr>')
    html.append('</table><p><a href="matrix.csv">Raw similarities</a> · <a href="packets.json">All normalized input paths and sources</a></p></html>')
    (folder/"Index.html").write_text("".join(html),encoding="utf-8")
    # Standalone print companion. Each focus stays a vector projection at its
    # own scale; its museum object link remains part of the SVG itself.
    sheet=['<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1000 1000" role="img">'
           '<title>Eight historical focuses, two source objects each</title><rect width="1000" height="1000" fill="#f8f5eb"/>'
           '<g font-family="system-ui" fill="#24483c"><text x="35" y="46" font-family="Georgia,serif" font-size="33">'
           'Two sources for each family</text><text x="35" y="72" font-size="15">Angular interpretations of the selected focus; '
           'Meyer outlines observed on the construction grid. Three repeats per sketch.</text>'
           '<text x="210" y="111" font-weight="bold">Training object focus</text><text x="600" y="111" font-weight="bold">Withheld object focus</text>']
    for i,r in enumerate(ranks):
        y=125+i*106
        sheet.append(f'<path d="M30 {y}h930" stroke="#cbd0bc"/><text x="35" y="{y+40}" font-size="15">{escape(r["family"])}</text>')
        for j,held in enumerate((False,True)):
            item=by_key[r["family"],held]
            geometry=sketch(item)
            geometry=geometry.replace('<svg viewBox="0 0 280 82"',f'<svg x="{190+390*j}" y="{y}" width="350" height="75" viewBox="0 0 280 82"')
            sheet.append(f'<a href="{escape(item["source"])}">{geometry}<text x="{200+390*j}" y="{y+91}" font-size="13">'
                         f'{escape(item["object"])}</text></a>')
    sheet.append('</g></svg>')
    (folder/"Specimens.svg").write_text(''.join(sheet),encoding="utf-8")
    print(correct,"of eight families recovered.",[(r["family"],r["best"],r["rank"]) for r in ranks])


if __name__=="__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("runtime",type=Path);parser.add_argument("folder",type=Path)
    args=parser.parse_args();build(args.runtime.resolve(),args.folder.resolve())
