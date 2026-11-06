"""Sample high-utility, dissimilar training profiles and draw them with the frozen path runtime."""
import argparse
import csv
import gzip
import hashlib
import json
import math
import subprocess
import sys
from collections import defaultdict
from pathlib import Path
from statistics import fmean
from xml.sax.saxutils import escape

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "OrnamentPathCampaign"))
import Gallery
from PIL import Image, ImageDraw

CALIBRATIONS = {
    "a": dict(a=2, b=1, pitch=6, cells=5, phase=0, seedBit=1),
    "b": dict(a=3, b=4, pitch=7, cells=5, phase=0, seedBit=1),
}
COUNT = 4


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write_json(path, value):
    s = json.dumps(value, indent=2, ensure_ascii=False, allow_nan=False) + "\n"
    if path.exists() and path.read_text(encoding="utf-8") != s:
        raise ValueError(f"Different existing record: {path}")
    path.write_text(s, encoding="utf-8")


def training_pool(wave, brief, keys):
    by_program = defaultdict(dict)
    for rep in range(4):
        archive = wave / "histories" / f"{brief}_full_{rep}.csv.gz"
        with gzip.open(archive, "rt", encoding="utf-8-sig", newline="") as f:
            for row in csv.DictReader(f):
                hexcode = row["genome"]
                profile = [float(row[k]) for k in keys]
                score = float(row["score"])
                item = by_program[hexcode].get(rep)
                if item is None:
                    by_program[hexcode][rep] = dict(trial=int(row["trial"]), paid=1, profile=profile, score=score)
                else:
                    if item["profile"] != profile or item["score"] != score:
                        raise ValueError("One program/replicate produced two different scores")
                    item["paid"] += 1
    records = []
    for code, observations in by_program.items():
        members = list(observations.values())
        records.append(dict(brief=brief, genome=code, train=fmean(x["score"] for x in members),
                            profile=[fmean(x["profile"][i] for x in members) for i in range(len(keys))],
                            occurrences=[dict(replicate=r, trial=x["trial"], paid=x["paid"])
                                         for r, x in observations.items()]))
    records.sort(key=lambda x: (-x["train"], x["genome"]))
    return records


def select(wave, briefs, criteria):
    keys = [c["key"] for c in criteria]
    groups = defaultdict(list)
    for i, c in enumerate(criteria):
        groups[c["group"]].append(i)
    per_brief = {}
    summary = {}
    for b in briefs:
        pool = training_pool(wave, b, keys)
        limit = math.ceil(len(pool) / 4)
        per_brief[b] = pool[:limit]
        summary[b] = dict(unique=len(pool), paid=sum(g["paid"] for x in pool for g in x["occurrences"]),
                          topQuarter=limit, floor=pool[limit-1]["train"])

    def distance(x, y):
        return fmean(fmean(abs(x["profile"][i]-y["profile"][i]) for i in indices) for indices in groups.values())

    used = set()
    result = defaultdict(list)
    # Round robin over the frozen brief order so cross-brief duplicates do not
    # let the first brief choose its entire quota before another chooses a leader.
    for rank in range(COUNT):
        for b in briefs:
            eligible = [r for r in per_brief[b] if r["genome"] not in used]
            if not eligible:
                raise ValueError(f"Too few distinct genomes in {b}")
            prior = result[b]
            if prior:
                ranked = [(min(distance(x, y) for y in prior), x) for x in eligible]
                gap, best = min(ranked, key=lambda gx: (-gx[0], -gx[1]["train"], gx[1]["genome"]))
            else:
                gap, best = None, eligible[0]
            pick = dict(best, rank=rank+1, nearestProfileDistance=gap)
            result[b].append(pick)
            used.add(best["genome"])
    return [item for b in briefs for item in result[b]], summary


def request_id(item, calibration):
    return f"{item['brief']}-{item['rank']}-{calibration}"


def realize(output, records, runtime):
    requests = [dict(id=request_id(s, c), hex=s["genome"], input=inputs)
                for s in records for c, inputs in CALIBRATIONS.items()]
    write_json(output/"requests.json", requests)
    response = output/"native-traces.json"
    if not response.exists():
        subprocess.run(["dotnet", str(runtime), str(output/"requests.json"), str(response)], check=True)
    answers = json.loads(response.read_text(encoding="utf-8-sig"))
    if [x["id"] for x in answers] != [r["id"] for r in requests]:
        raise ValueError("Replay records do not match the exact input order")
    return {x["id"]: x for x in answers}


def exhibit(sample, cal, response, design):
    trace = response["trace"]
    metrics = {c["key"]: v for c, v in zip(design["criteria"], response["metrics"], strict=True)}
    coords = trace["sites"]
    strokes = []
    for edge in trace["edges"]:
        a, b = edge["from"], edge["to"]
        strokes.append(dict(a=a, b=b, x1=coords[a]["x"], y1=coords[a]["y"],
                            x2=coords[b]["x"], y2=coords[b]["y"],
                            **{k: edge[k] for k in ("owner","beat","cell","row","direction","extent")}))
    viable = len({s["direction"] % 4 for s in strokes}) > 1
    weights = design["weights"][sample["brief"]]
    utility = sum(w*(v or 0) for w,v in zip(weights, response["metrics"], strict=True)) if viable else 0
    groups = defaultdict(list)
    for c, v in zip(design["criteria"], response["metrics"], strict=True):
        if v is not None:
            groups[c["group"]].append(v)
        else:
            groups.setdefault(c["group"], [])
    group_means = {g: fmean(v) if v else None for g, v in groups.items()}
    display = CALIBRATIONS[cal]
    how = "Utility leader" if sample["rank"] == 1 else f"Profile choice {sample['rank']}"
    final = trace["finalProgram"]
    if isinstance(final, str):
        import base64
        final = base64.b64decode(final).hex().upper()
    return dict(id=response["id"], brief=sample["brief"], arm="full training population",
                label=f"{how}, calibration {cal.upper()}", scenario="display-"+cal,
                description=f"Selection used training utility {sample['train']:.3f}. The scores below use this displayed input.",
                realization=display, utility=utility, trainingUtility=sample["train"], genome=sample["genome"],
                finalGenome=final, sites=coords, strokes=strokes, events=trace["events"], periodX=trace["pitch"],
                seamX=0, cells=trace["cells"], owners=trace["owners"], metrics=metrics, groups=group_means)


def drawing_lines(e, x, y, width, height):
    xs = [0, e["cells"]*e["periodX"]]
    ys = []
    for s in e["strokes"]:
        xs.extend((s["x1"],s["x2"])); ys.extend((s["y1"],s["y2"]))
    if not ys:
        return []
    x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)
    scale = min(width/max(1,x1-x0), height/max(1,y1-y0))
    cx, cy = x + width/2, y + height/2
    return [(cx+(s["x1"]-(x0+x1)/2)*scale, cy-(s["y1"]-(y0+y1)/2)*scale,
             cx+(s["x2"]-(x0+x1)/2)*scale, cy-(s["y2"]-(y0+y1)/2)*scale) for s in e["strokes"]]


def print_sheet(output, samples, exhibits, briefs):
    width, pad, top = 1560, 40, 175
    col, row = 370, 237
    height = top+row*len(briefs)+35
    paint, ink, muted = "#f8f6ef", "#192e2b", "#51635a"
    svg = [f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width} {height}" role="img">',
           '<title>Twenty path machines from the training population</title>',
           f'<rect width="{width}" height="{height}" fill="{paint}"/>']
    scale = 2
    img = Image.new("RGB", (width*scale,height*scale),paint)
    dra = ImageDraw.Draw(img)
    def text(x,y,msg,size,bold=False,serif=False,color=ink):
        svg.append(f'<text x="{x}" y="{y+size}" font-size="{size}" font-family="{"Georgia,serif" if serif else "Segoe UI, sans-serif"}" font-weight="{"bold" if bold else "normal"}" fill="{color}">{escape(msg)}</text>')
        dra.text((scale*x,scale*y),msg,font=Gallery.font(size*scale,serif,bold),fill=color)
    def line(x1,y1,x2,y2,weight=1,color=ink):
        svg.append(f'<path d="M{x1:.3f} {y1:.3f}L{x2:.3f} {y2:.3f}" fill="none" stroke="{color}" stroke-width="{weight}" stroke-linecap="round"/>')
        dra.line((x1*scale,y1*scale,x2*scale,y2*scale),fill=color,width=weight*scale)
    text(pad,22,"The path design sampler",42,serif=True)
    text(pad,88,"Four high-utility training programs per brief. Each drawing shows one complete five-period run.",18)
    text(pad,119,"A  /  extents 2 and 1, period 6.       B  /  extents 3 and 4, period 7.       Drawings fit their own display frames.",15,color=muted)
    indexed={e["id"]:e for e in exhibits}
    for s in samples:
        x=pad+col*(s["rank"]-1)
        y=top+row*briefs.index(s["brief"])
        line(x,y,x+col-14,y,color="#c9c9bc")
        text(x,y+8,s["brief"].replace("-"," ")+"  /  "+("leader" if s["rank"]==1 else "profile "+str(s["rank"])),16,bold=True)
        text(x,y+34,"Training utility "+f"{s['train']:.3f}",13,color=muted)
        for i,cal in enumerate(CALIBRATIONS):
            e=indexed[request_id(s,cal)]
            y_art=y+70+i*76
            svg.append(f'<a href="Explore.html#drawing={e["id"]}"><title>{escape(e["id"])}, display utility {e["utility"]:.3f}</title>')
            text(x,y_art+14,cal.upper(),13,bold=True,color=muted)
            draw_x, draw_w, draw_h = x+25, col-52, 55
            for seg in drawing_lines(e,draw_x,y_art,draw_w,draw_h):
                x1, y1, x2, y2 = seg
                if (min(x1,x2) < draw_x-1e-6 or max(x1,x2) > draw_x+draw_w+1e-6
                        or min(y1,y2) < y_art-1e-6 or max(y1,y2) > y_art+draw_h+1e-6):
                    raise ValueError("A projected line escaped its dedicated drawing area")
                line(*seg,weight=2)
            svg.append('</a>')
    svg.append('</svg>')
    (output/"Sampler.svg").write_text("\n".join(svg)+"\n",encoding="utf-8")
    img.resize((width,height), Image.Resampling.LANCZOS).save(output/"Sampler.png")


def main(study):
    wave=study/"wave1"
    output=study/"sampler"
    output.mkdir(parents=True,exist_ok=True)
    runtime=study/"runtime-review/OrnamentPathReview.dll"
    path_design=wave/"design/design.json"
    design=json.loads(path_design.read_text(encoding="utf-8-sig"))
    briefs=[b["name"] for b in design["briefs"]]
    archives=sorted((wave/"histories").glob("*_full_*.csv.gz"))
    settings=dict(source="Wave-one paid full-training histories only", perBrief=COUNT,
                  retain="First ceil(N/4) distinct programs ordered by mean training utility, then byte string",
                  select="Round-robin first by training utility then farthest minimum distance to prior choices in this brief; uniform mean absolute criterion distance within group, uniform mean across six groups; ties by higher training utility then byte string",
                  calibrations=CALIBRATIONS, archiveHashes={p.name:digest(p) for p in archives},
                  runtimeHashes={p.name:digest(p) for p in runtime.parent.glob("*.dll")},designHash=digest(path_design))
    write_json(output/"selection-design.json",settings)
    chosen,population=select(wave,briefs,design["criteria"])
    write_json(output/"selected.json",dict(population=population,criteria=[c["key"] for c in design["criteria"]],programs=chosen))
    answers=realize(output,chosen,runtime)
    exhibits=[exhibit(s,c,answers[request_id(s,c)],design) for s in chosen for c in CALIBRATIONS]
    write_json(output/"exhibits.json",dict(examples=exhibits))
    target=output/"design"
    target.mkdir(exist_ok=True)
    write_json(target/"design.json",dict(briefs=design["briefs"],criteria=design["criteria"]))
    (target/"Criteria.md").write_bytes((wave/"design/Criteria.md").read_bytes())
    Gallery.build(output)
    print_sheet(output,chosen,exhibits,briefs)
    description = """# Designs from the training population

The sheet shows twenty packed machines that competed in wave one. Each program runs twice using the same seed bit, clock phase and five executed cells. The two rows change the bound extents and period. All marks were written by the machine. Scaling to fit the page is a display operation.

[Printable vector sheet](Sampler.svg) · [Compact contact sheet](Sampler.png) · [Explore individual strokes, events and scores](Explore.html).

## How the machines were selected

We considered only the complete-objective evolutionary histories, across the four training replicates of each brief. Within a replicate every repeated occurrence of the same packed bytes had the same score and criterion profile. We collapsed those duplicates, then gave each observed replicate equal weight. A program's aggregate only covers replicates where it actually occurred. Each brief retained the first quarter of distinct programs by mean training utility, rounded up. The first selection was its highest scorer. The next three maximized the minimum distance from any earlier choice under that brief. Distance is the mean absolute difference of training criteria within each perspective, then an equal average over the six perspectives. Equal distances break by higher aggregate training utility, then byte string. We selected in rounds through the frozen brief order and used each byte string once overall.

The two display calibrations are (A=2, B=1, pitch=6) and (A=3, B=4, pitch=7), both with phase zero, seed one, five cells. Selection was sealed before these runs. The sampler uses training histories and these two public realizations. It inspects no reserve outcomes. The spans and projection differ between the two displays. They have been fitted independently.

Training utility in each sheet caption is the averaged history score used in selection. The gallery's utility and individual criteria are recomputed after the particular displayed realization. A missing criterion is open and earns no utility credit. Different numeric profiles can draw coincident geometry. Selection demonstrates that the population contains other executable structures; neither its utility nor its diversity certifies aesthetic preference.

[Inputs and hashes](selection-design.json) · [Training scores, profiles and trial provenance](selected.json) · [Native drawing traces](native-traces.json).
"""
    unique=len({Gallery.features_for_sheet(e) for e in exhibits})
    description+=f"\nThe 40 displayed runs have {unique} distinct normalized geometric supports under the sheet's segment comparison; it removes exact duplicate segments, translation and uniform scale. Different subdivisions can still distinguish two entries.\n"
    (output/"Readme.md").write_text(description,encoding="utf-8")
    print(f"{len(chosen)} programs, {len(exhibits)} runs, {unique} normalized segment sets")


if __name__=="__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("study",type=Path)
    main(parser.parse_args().study.resolve())
