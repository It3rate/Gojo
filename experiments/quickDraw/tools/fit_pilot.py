"""Bounded, label-blind measured proposals; NOT canonical Gojo meaning.

Source JSON remains authoritative. Fits are observations in an explicitly
selected aspect-preserving basis. Coincidence never welds acquisition sites.
Only the Python standard library is needed for fitting; Pillow renders reviews.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
from pathlib import Path


SCALES = (0.005, 0.01, 0.02)


def distance(a, b):
    return math.hypot(a[0] - b[0], a[1] - b[1])


def segment_distance(p, a, b):
    dx, dy = b[0] - a[0], b[1] - a[1]
    den = dx * dx + dy * dy
    t = max(0.0, min(1.0, ((p[0]-a[0])*dx + (p[1]-a[1])*dy) / den)) if den else 0.0
    return distance(p, (a[0] + t*dx, a[1] + t*dy))


def parameters(points):
    lengths = [0.0]
    for a, b in zip(points, points[1:]):
        lengths.append(lengths[-1] + distance(a, b))
    return [v/lengths[-1] for v in lengths] if lengths[-1] else [0.0 for _ in points]


def bezier(controls, t):
    work = controls
    while len(work) > 1:
        work = [((1-t)*a[0]+t*b[0], (1-t)*a[1]+t*b[1]) for a, b in zip(work, work[1:])]
    return work[0]


def polynomial_fit(points, degree):
    """Endpoint-fixed least squares at chord-length parameters, no reparameterization.

    This is a numerical adapter proposal, not inferred intended control points.
    The tiny 1x1/2x2 systems are deliberately dependency free.
    """
    ts = parameters(points)
    start, end = points[0], points[-1]
    if degree == 2:
        weights = [2*t*(1-t) for t in ts]
        den = sum(w*w for w in weights)
        if den < 1e-15:
            return None
        control = tuple(sum(w*(p[d]-(1-t)**2*start[d]-t*t*end[d]) for p,t,w in zip(points,ts,weights))/den for d in (0,1))
        controls = [start, control, end]
    else:
        aa = [3*t*(1-t)**2 for t in ts]
        bb = [3*t*t*(1-t) for t in ts]
        a2, ab, b2 = sum(a*a for a in aa), sum(a*b for a,b in zip(aa,bb)), sum(b*b for b in bb)
        det = a2*b2-ab*ab
        if det < 1e-15:
            return None
        c1, c2 = [], []
        for d in (0,1):
            residual = [p[d]-(1-t)**3*start[d]-t**3*end[d] for p,t in zip(points,ts)]
            ar, br = sum(a*r for a,r in zip(aa,residual)), sum(b*r for b,r in zip(bb,residual))
            c1.append((ar*b2-br*ab)/det)
            c2.append((br*a2-ar*ab)/det)
        controls = [start, tuple(c1), tuple(c2), end]
    errors = [distance(p, bezier(controls,t)) for p,t in zip(points,ts)]
    return {"degree": degree, "controls": controls, "sample_parameter": ts,
            "sample_errors": errors, "max_error": max(errors),
            "rms_error": math.sqrt(sum(e*e for e in errors)/len(errors)),
            "metric": "sample distance at chord-length parameter"}


def line_fit(points):
    errors = [segment_distance(p, points[0], points[-1]) for p in points]
    dx,dy = points[-1][0]-points[0][0],points[-1][1]-points[0][1]
    den=dx*dx+dy*dy
    progress=[((p[0]-points[0][0])*dx+(p[1]-points[0][1])*dy)/den if den else 0.0 for p in points]
    return {"degree": 1, "controls": [points[0], points[-1]], "sample_errors": errors,
            "max_error": max(errors), "rms_error": math.sqrt(sum(e*e for e in errors)/len(errors)),
            "projected_progress": progress, "backward_progress_steps":sum(b<a for a,b in zip(progress,progress[1:])),
            "metric": "sample to bounded segment distance"}


def fit_sections(points, source_indices, epsilon, curves):
    if len(points) < 2:
        return []
    pending, accepted = [(0, len(points)-1)], []
    while pending:
        a,b = pending.pop()
        part = points[a:b+1]
        fits = [line_fit(part)]
        if curves and len(part) >= 4:
            fits.extend(f for degree in (2,3) if (f := polynomial_fit(part, degree)) is not None)
        chosen = next((f for f in fits if f["max_error"] <= epsilon), None)
        if chosen is None:
            best = min(fits, key=lambda f: f["max_error"])
            k = max(range(1,len(part)-1), key=lambda i: best["sample_errors"][i])
            # Retain order, and terminate even if the worst error lies at an end.
            split = a+k
            pending.extend(((split,b),(a,split)))
            continue
        accepted.append({"source_start": source_indices[a], "source_end": source_indices[b],
                         "source_indices": source_indices[a:b+1], **chosen})
    return sorted(accepted, key=lambda f: f["source_start"])


def normalize(drawing):
    """Read geometry only. Parser/native ingestion separately validates metadata/time."""
    strokes, flat = [], []
    for stroke in drawing:
        if len(stroke) not in (2,3) or len(stroke[0]) != len(stroke[1]):
            raise ValueError("Stroke needs equally sized x/y arrays and optional time")
        if len(stroke) == 3 and len(stroke[2]) != len(stroke[0]):
            raise ValueError("Time count must match sample count")
        points = []
        for x,y in zip(stroke[0],stroke[1]):
            if isinstance(x,bool) or isinstance(y,bool) or not isinstance(x,(int,float)) or not isinstance(y,(int,float)):
                raise ValueError("Coordinates must be numeric")
            if not math.isfinite(x) or not math.isfinite(y):
                raise ValueError("Coordinates must be finite")
            points.append((float(x),float(y)))
        strokes.append(points)
        flat.extend(points)
    if not flat:
        return strokes, {"origin": None, "source_units_per_normalized_unit": None, "degenerate": True}
    low = (min(p[0] for p in flat),min(p[1] for p in flat))
    width,height = max(p[0] for p in flat)-low[0], max(p[1] for p in flat)-low[1]
    extent = max(width,height)
    if not math.isfinite(extent):
        raise ValueError("Coordinate extent exceeds the finite measured adapter basis")
    scale = extent or 1.0
    return [[((x-low[0])/scale,(y-low[1])/scale) for x,y in stroke] for stroke in strokes], {
        "origin": low, "source_units_per_normalized_unit": scale, "source_bounds": [width,height],
        "degenerate": extent == 0, "axis_orientation": "source x-right/y-down retained",
        "numeric_read": "IEEE-754 binary64 approximation; exact JSON retained separately",
        "promise": "uniform scale and translation; no aspect stretching, rotation, or reflection"}


def analyze(drawing, scales=SCALES):
    """No prompt, recognized flag, source identity, or category enters this function."""
    strokes,basis = normalize(drawing)
    result = {"schema": "gojo.quickdraw.measured-proposals.v1", "basis": basis, "strokes": [],
              "interpretation": "observational numerical adapter; not native semantic evidence",
              "closure_policy": "every acquisition sample has independent identity; no inferred welds",
              "scales": list(scales), "endpoint_proposals": []}
    for si,raw_points in enumerate(strokes):
        points,indices,duplicates = [],[],[]
        for i,p in enumerate(raw_points):
            if points and p == points[-1]:
                duplicates.append({"sample":i,"same_coordinate_as":indices[-1],"identity": "distinct acquisition event retained in source"})
            else:
                points.append(p)
                indices.append(i)
        row = {"stroke":si,"source_count":len(raw_points),"retained_indices":indices,"duplicate_read":duplicates,
               "normalized_samples":raw_points, "alternatives":[]}
        if len(points)>1:
            gap = distance(points[0],points[-1])
            row["endpoint_gap"] = gap
            row["coordinate_return"] = gap == 0
            row["closure_proposal"] = {"within_threshold":gap <= .03,"threshold":.03,"status":"hypothesis only; source topology remains open"}
        for epsilon in scales:
            for curves in (False,True):
                sections = fit_sections(points,indices,epsilon,curves)
                row["alternatives"].append({"epsilon":epsilon,"grammar":"line-quadratic-cubic" if curves else "line",
                    "sections":sections,"section_count":len(sections),
                    "max_error":max((s["max_error"] for s in sections),default=0.0),
                    "qualification":"bounds sampled residual only; no global Hausdorff, topology, or intended-corner guarantee"})
        result["strokes"].append(row)
    ends = [(si,i,stroke[i]) for si,stroke in enumerate(strokes) if stroke for i in sorted({0,len(stroke)-1})]
    for j,(sa,ia,pa) in enumerate(ends):
        for sb,ib,pb in ends[j+1:]:
            if sa == sb:
                continue
            gap = distance(pa,pb)
            if gap <= .03:
                result["endpoint_proposals"].append({"a":[sa,ia],"b":[sb,ib],"gap":gap,"threshold":.03,
                    "status":"possible contact, independent acquisition identities retained"})
    return result


def render_review(rows, output):
    from PIL import Image, ImageDraw, ImageFont
    font = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 18)
    small = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 14)
    cell_w,cell_h,cols = 280,240,4
    # One source per row: observation and three scale choices. Curves orange.
    for category in sorted({r["category"] for r in rows}):
        chosen = [r for r in rows if r["category"]==category]
        img = Image.new("RGB",(cell_w*cols,64+cell_h*len(chosen)),"white")
        draw = ImageDraw.Draw(img)
        for col,title in enumerate(("Source acquisition","Fit at 0.5%","Fit at 1%","Fit at 2%")):
            draw.text((col*cell_w+12,12),title,fill="#172433",font=font)
        for ri,row in enumerate(chosen):
            top = 64+ri*cell_h
            data = row["analysis"]
            def screen(p,col):
                return (col*cell_w+35+p[0]*178,top+26+p[1]*178)
            for col in range(cols):
                count = 0
                for stroke in data["strokes"]:
                    pts=stroke["normalized_samples"]
                    if len(pts)>1:
                        draw.line([screen(p,col) for p in pts], fill="#202e39" if col==0 else "#d2d7dc",width=2)
                    if col:
                        alt=next(a for a in stroke["alternatives"] if a["epsilon"]==SCALES[col-1] and a["grammar"]=="line-quadratic-cubic")
                        count+=alt["section_count"]
                        for section in alt["sections"]:
                            controls=section["controls"]
                            path=[bezier(controls,i/50) for i in range(51)]
                            draw.line([screen(p,col) for p in path],fill="#087e8b" if section["degree"]==1 else "#bd4e17",width=2)
                            x,y=screen(controls[0],col)
                            draw.ellipse((x-2,y-2,x+2,y+2),fill="#8844aa")
                caption = row["key_id"] if col==0 else f"{count} sections; proposed, not semantic"
                draw.text((col*cell_w+12,top+212),caption,fill="#334155",font=small)
            draw.line((0,top+cell_h-1,cell_w*cols,top+cell_h-1),fill="#e2e8f0")
        img.save(output/f"{category}-fits.png")


def run(input_path, output):
    output.mkdir(parents=True,exist_ok=True)
    rows,summary=[],[]
    for raw in input_path.read_bytes().splitlines():
        if not raw.strip():
            continue
        record=json.loads(raw)
        data=analyze(record["drawing"])
        key=str(record["key_id"])
        # The evaluator envelope is not a public evidence packet.
        envelope={"key_id":key,"category":record.get("word","unknown"),
                  "source_line_sha256":hashlib.sha256(raw).hexdigest(),"analysis":data}
        (output/f"{key}.fits.json").write_text(json.dumps(envelope,separators=(",",":")),encoding="utf-8")
        rows.append(envelope)
        for epsilon in SCALES:
            for grammar in ("line","line-quadratic-cubic"):
                alts=[a for s in data["strokes"] for a in s["alternatives"] if a["epsilon"]==epsilon and a["grammar"]==grammar]
                sections=[section for a in alts for section in a["sections"]]
                summary.append({"key_id":key,"category":envelope["category"],"epsilon":epsilon,"grammar":grammar,
                    "strokes":len(data["strokes"]),"raw_points":sum(s["source_count"] for s in data["strokes"]),
                    "duplicates":sum(len(s["duplicate_read"]) for s in data["strokes"]),
                    "sections":len(sections),"lines":sum(s["degree"]==1 for s in sections),
                    "quadratics":sum(s["degree"]==2 for s in sections),"cubics":sum(s["degree"]==3 for s in sections),
                    "max_error":max((s["max_error"] for s in sections),default=0),
                    "endpoint_candidates":len(data["endpoint_proposals"]),
                    "near_return_strokes":sum(s.get("closure_proposal",{}).get("within_threshold",False) for s in data["strokes"])})
    (output/"summary.json").write_text(json.dumps(summary,indent=2),encoding="utf-8")
    render_review(rows,output)
    print(json.dumps({"records":len(rows),"conditions":len(summary),"output":str(output)}))


if __name__ == "__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input",type=Path,required=True)
    parser.add_argument("--output",type=Path,required=True)
    args=parser.parse_args()
    run(args.input,args.output)
