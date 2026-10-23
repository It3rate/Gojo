"""Audit restored native output and render an offline reconstruction review.

Predicted geometry is read only from the native CLI's restored-program output.
Source traces and names enter this evaluator, never the native decoder.
"""
from __future__ import annotations

import argparse
import json
import math
from pathlib import Path

from faithful_pipeline import decode, numpy, write_json
from fit_pilot import bezier, distance, parameters


def rows(path):
    return [json.loads(line) for line in path.read_text(encoding="utf-8").splitlines() if line.strip()]


def resample(points, count=129):
    """Equal-arclength traversal; repeated coordinates remain in the source audit."""
    if not points:
        return []
    ts = parameters(points)
    result, index = [], 1
    for i in range(count):
        t = i / (count - 1)
        while index < len(points)-1 and ts[index] < t:
            index += 1
        if len(points) == 1:
            result.append(points[0])
        else:
            width = ts[index] - ts[index-1]
            f = (t-ts[index-1])/width if width else 0
            result.append([a+f*(b-a) for a, b in zip(points[index-1], points[index])])
    return result


def directed_distances(points, path):
    """Sample-to-polyline distance, without cross-stroke matches or alignment."""
    np = numpy()
    p, target = np.asarray(points, dtype=float), np.asarray(path, dtype=float)
    if len(target) == 1:
        return np.linalg.norm(p-target[0], axis=1).tolist()
    a, d = target[:-1], target[1:]-target[:-1]
    norm = np.sum(d*d, axis=1)
    safe = np.where(norm == 0, 1, norm)
    f = np.clip(np.sum((p[:, None, :]-a)*d, axis=2)/safe, 0, 1)
    offsets = p[:, None, :]-a-f[:, :, None]*d
    return np.sqrt(np.min(np.sum(offsets*offsets, axis=2), axis=1)).tolist()


def measure(source, predicted):
    if len(source) != len(predicted):
        raise ValueError("Stroke count changed")
    ordered, geometric, endpoints = [], [], []
    for original, reconstructed in zip(source, predicted):
        if bool(original) != bool(reconstructed):
            raise ValueError("Empty/nonempty stroke changed")
        if not original:
            continue
        a, b = resample(original), resample(reconstructed)
        ordered.extend(distance(x, y) for x, y in zip(a, b))
        geometric.extend(directed_distances(a, reconstructed))
        geometric.extend(directed_distances(b, original))
        endpoints.extend((distance(original[0], reconstructed[0]), distance(original[-1], reconstructed[-1])))
    rms = lambda values: math.sqrt(sum(v*v for v in values)/len(values)) if values else 0
    return {"orderedArcRms": rms(ordered), "symmetricSampleRms": rms(geometric),
            "maximumSampleDistance": max(geometric, default=0), "maximumEndpointError": max(endpoints, default=0)}


def curves(row):
    q = row["coordinateQuantum"]
    return [[[[v/q for v in point] for point in section["controls"]] for section in stroke["sections"]]
            for stroke in row["strokes"]]


def polylines(curve_strokes):
    result = []
    for stroke in curve_strokes:
        points = []
        for i, control in enumerate(stroke):
            if points and distance(points[-1], control[0]) > 1e-12:
                raise ValueError("Noncontiguous sections require separate path/gap evaluation")
            points.extend(bezier(control, k/64) for k in range(0 if i == 0 else 1, 65))
        result.append(points)
    return result


def evaluate(directory, native):
    audits, decoded = rows(directory/"private-audit.ndjson"), rows(native/"decoded.ndjson")
    packet_rows = rows(directory/"packets.ndjson")
    packets = {r["id"]: r for r in packet_rows}
    if len(packets) != len(packet_rows) or len({a["id"] for a in audits}) != len(audits):
        raise ValueError("Repeated packet or audit ID")
    weights = json.loads((directory/"model.json").read_text())["weights"] if (directory/"model.json").exists() else None
    by_id = {r["id"]: r for r in decoded}
    if len(by_id) != len(decoded) or set(by_id) != {a["id"] for a in audits} or set(packets) != set(by_id):
        raise ValueError("Native output/audit IDs differ or repeat")
    reports, gallery = [], {}
    for audit in audits:
        output = by_id[audit["id"]]
        if not output["nativeRestoreVerified"]:
            raise ValueError("Native program was not restored")
        packet = packets[audit["id"]]
        if output["coordinateQuantum"] != packet["coordinateQuantum"]:
            raise ValueError("Coordinate basis changed")
        if weights:
            expected = [[decode(h, weights) for h in s["sections"]] for s in packet["strokes"]]
            actual = [[s["controls"] for s in stroke["sections"]] for stroke in output["strokes"]]
            if actual != expected:
                raise ValueError("Restored native output differs from integer equation reference")
        sequence = [s["sequence"] for stroke in output["strokes"] for s in stroke["sections"]]
        if sequence != list(range(audit["effectiveBudget"])):
            raise ValueError("Section count/order differs")
        controls = curves(output)
        report = {"id": audit["id"], "category": audit["category"], "budget": audit["requestedBudget"],
                  "sections": audit["effectiveBudget"], **measure(audit["sourceStrokes"], polylines(controls))}
        reports.append(report)
        item = gallery.setdefault(audit["key_id"], {"id": audit["key_id"], "category": audit["category"],
                    "split": audit["split"], "source": audit["sourceStrokes"], "budgets": {}})
        item["budgets"][str(audit["requestedBudget"])] = {"curves": controls, "metric": report,
                 "hintBytes": len(json.dumps({k: v for k, v in packet.items() if k != "id"}, separators=(",", ":")).encode()),
                 "strokeFloor": audit["strokeFloor"]}
    summary = []
    for budget in sorted({r["budget"] for r in reports}):
        subset = [r for r in reports if r["budget"] == budget]
        summary.append({"budget": budget, "drawings": len(subset),
                        **{f"mean_{key}": sum(r[key] for r in subset)/len(subset)
                           for key in ("orderedArcRms", "symmetricSampleRms")},
                        "maximumSampleDistance": max(r["maximumSampleDistance"] for r in subset),
                        "maximumEndpointError": max(r["maximumEndpointError"] for r in subset)})
    return list(gallery.values()), {"schema": "gojo.quickdraw.native-review.v1", "nativePackets": len(audits),
                "integerModelParityVerified": bool(weights), "summary": summary,
                "metric": "129 equally spaced arclength samples per nonempty stroke; each stroke equally weighted; native cubics approximated by 64 segments; no spatial alignment",
                "limitations": "Sampled distances, not certified bounds; geometry metric is blind to within-stroke traversal; orderedArcRms preserves traversal but not time/speed; original sample events remain in audit.",
                "rows": reports}


HTML = r'''<!doctype html><html lang="en"><meta charset="utf-8"><title>Observed drawing reconstruction</title>
<style>body{font:16px system-ui;background:#f5f4ef;color:#202a31;margin:30px auto;max-width:1450px;padding:0 20px}h1{font-size:30px;margin-bottom:8px}p{line-height:1.5;max-width:1000px}.tools{position:sticky;top:0;background:#f5f4eff5;padding:14px 0;display:flex;gap:20px;flex-wrap:wrap;z-index:2}select,input{font:inherit}#grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(280px,1fr));gap:16px}.card{background:white;border:1px solid #dcdcd4;border-radius:10px;padding:16px}canvas{width:100%;height:auto;aspect-ratio:1}small{color:#58646a}.metrics{font-size:13px;line-height:1.6}b{font-weight:650}.key{font-size:11px;overflow-wrap:anywhere}.source{color:#89949a}.result{color:#cf5729}a{color:#175f82}</style>
<h1>Reconstructing the observed drawing</h1>
<p>Gray is the original trace. Orange is generated from restored native evidence and a frozen learned local curve model. Position, facing, proportions, stroke order and pen lifts follow the source. No ideal shape or category label enters generation.</p>
<p>Reduce the section budget to see what the hints lose. A section carries endpoints plus three coarse profile observations; three sections are more than three facts. The budget cannot fall below one section per nonempty stroke. The progress slider reveals the generated order; it does not replay drawing speed.</p>
<div class="tools"><label>Category <select id="category"><option value="all">All</option></select></label><label>Requested sections <select id="budget"><option>3</option><option>6</option><option>12</option><option selected>20</option></select></label><label><input type="checkbox" id="overlay" checked> Source overlay</label><label>Draw prefix <input id="progress" type="range" min="0" max="100" value="100"></label></div><div id="grid"></div>
<p><small>Ordered error compares equal-arclength traversals of corresponding strokes. Geometric error compares sampled distances to the matching stroke path in both directions. Both use the original frame, with no rotation/reflection alignment; percentages are relative to the source's larger extent. Neither is a certified maximum error or a judgment of drawing skill. Construction guides and semantic intent are not inferred.</small></p>
<script>const drawings=__DATA__;
const category=document.querySelector('#category'),budget=document.querySelector('#budget'),overlay=document.querySelector('#overlay'),progress=document.querySelector('#progress'),grid=document.querySelector('#grid');
for(const c of [...new Set(drawings.map(d=>d.category))]){let o=document.createElement('option');o.value=c;o.textContent=c;category.append(o)}
function render(){grid.replaceChildren();for(const d of drawings.filter(d=>category.value==='all'||d.category===category.value)){const r=d.budgets[budget.value];if(!r)continue;let card=document.createElement('div');card.className='card';let title=document.createElement('b');title.textContent=d.category+' · '+d.split;card.append(title);let canvas=document.createElement('canvas');canvas.width=520;canvas.height=520;card.append(canvas);let ctx=canvas.getContext('2d');let all=d.source.flat().concat(r.curves.flat(2));let xs=all.map(p=>p[0]),ys=all.map(p=>p[1]);let minX=Math.min(0,...xs),maxX=Math.max(1,...xs),minY=Math.min(0,...ys),maxY=Math.max(1,...ys);let scale=450/Math.max(maxX-minX,maxY-minY);const pt=p=>[35+(p[0]-minX)*scale,35+(p[1]-minY)*scale];ctx.lineCap='round';ctx.lineJoin='round';if(overlay.checked){ctx.strokeStyle='#bac1c5';ctx.fillStyle='#bac1c5';ctx.lineWidth=5;for(const stroke of d.source){if(!stroke.length)continue;ctx.beginPath();stroke.forEach((p,i)=>i?ctx.lineTo(...pt(p)):ctx.moveTo(...pt(p)));ctx.stroke();if(stroke.length===1){ctx.beginPath();ctx.arc(...pt(stroke[0]),3,0,7);ctx.fill()}}}let n=Math.ceil(r.metric.sections*Number(progress.value)/100),index=0;ctx.strokeStyle='#cf5729';ctx.fillStyle='#cf5729';ctx.lineWidth=2.5;for(const stroke of r.curves){for(const control of stroke){if(index++>=n)continue;ctx.beginPath();ctx.moveTo(...pt(control[0]));ctx.bezierCurveTo(...pt(control[1]),...pt(control[2]),...pt(control[3]));ctx.stroke();ctx.beginPath();ctx.arc(...pt(control[0]),3,0,7);ctx.fill()}}let m=document.createElement('div');m.className='metrics';m.textContent=r.metric.sections+' sections · '+r.hintBytes+' hint bytes · stroke floor '+r.strokeFloor+' | ordered '+(100*r.metric.orderedArcRms).toFixed(2)+'% · geometric '+(100*r.metric.symmetricSampleRms).toFixed(2)+'%';card.append(m);let key=document.createElement('div');key.className='key';key.textContent=d.id;card.append(key);grid.append(card)}}
for(const e of [category,budget,overlay,progress])e.addEventListener('input',render);render();</script></html>'''


def sheets(gallery, output):
    from PIL import Image, ImageDraw, ImageFont
    try:
        font = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 15)
        small = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 11)
    except OSError:
        font = small = ImageFont.load_default()
    for category in sorted({r["category"] for r in gallery}):
        subset = [r for r in gallery if r["category"] == category]
        image = Image.new("RGB", (1150, 55+235*len(subset)), "#f5f4ef")
        pen = ImageDraw.Draw(image)
        for col, title in enumerate(("Original trace", "3 sections", "6 sections", "12 sections", "20 sections")):
            pen.text((col*230+18, 18), title, fill="#202a31", font=font)
        for row, item in enumerate(subset):
            for col, budget in enumerate((None, "3", "6", "12", "20")):
                x, y = col*230+18, row*235+60
                predicted = polylines(item["budgets"][budget]["curves"]) if budget else []
                points = [p for stroke in item["source"]+predicted for p in stroke]
                min_x, min_y = min([0]+[p[0] for p in points]), min([0]+[p[1] for p in points])
                extent = max(max([1]+[p[0] for p in points])-min_x, max([1]+[p[1] for p in points])-min_y)
                project = lambda p: (x+(p[0]-min_x)*185/extent, y+(p[1]-min_y)*185/extent)
                for strokes, color, width in ((item["source"], "#aeb7bd" if budget else "#253743", 2), (predicted, "#cf5729", 2)):
                    for stroke in strokes:
                        if len(stroke) > 1:
                            pen.line([project(p) for p in stroke], fill=color, width=width)
                        elif stroke:
                            px, py = project(stroke[0]); pen.ellipse((px-2, py-2, px+2, py+2), fill=color)
                label = item["id"] if budget is None else f'{item["budgets"][budget]["metric"]["sections"]} actual; geom {100*item["budgets"][budget]["metric"]["symmetricSampleRms"]:.2f}%'
                pen.text((x, y+198), label, fill="#415361", font=small)
        image.save(output/f"{category}-reconstruction.png")


def run(directory, native, output):
    output.mkdir(parents=True, exist_ok=True)
    gallery, report = evaluate(directory, native)
    write_json(output/"native-metrics.json", report)
    (output/"review.html").write_text(HTML.replace("__DATA__", json.dumps(gallery, separators=(",", ":")).replace("<", "\\u003c")), encoding="utf-8")
    sheets(gallery, output)
    print(json.dumps({"drawings": len(gallery), "packets": report["nativePackets"], "integerParity": report["integerModelParityVerified"], "summary": report["summary"], "output": str(output)}))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", type=Path, required=True)
    parser.add_argument("--native", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    run(args.input, args.native, args.output)
