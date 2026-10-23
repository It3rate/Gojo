"""Bounded SVG centerline importer for the pinned Lucide/Tabler references.

Supports path-only, untransformed, unfilled SVG with M/L/H/V/A/Z commands.
Unsupported paint, elements, attributes, and commands fail explicitly. SVG
document/subpath order is retained; it is not human drawing-time evidence.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
from pathlib import Path
import re
import xml.etree.ElementTree as ET


TOKEN = re.compile(r"[A-Za-z]|[-+]?(?:\d*\.\d+|\d+\.?\d*)(?:[eE][-+]?\d+)?")
ARITY = {"M": 2, "L": 2, "H": 1, "V": 1, "A": 7, "Z": 0}


def tokens(data: str) -> list[str]:
    result = []
    end = 0
    for match in TOKEN.finditer(data):
        if data[end:match.start()].strip(" \t\r\n,"):
            raise ValueError("Invalid path token")
        result.append(match.group())
        end = match.end()
    if data[end:].strip(" \t\r\n,"):
        raise ValueError("Invalid path suffix")
    return result


def arc_cubics(start: tuple[float, float], end: tuple[float, float],
               rx: float, ry: float, rotation: float, large: float, sweep: float) -> list[list[tuple[float, float]]]:
    """SVG endpoint arc algorithm, divided into at most quarter-turn cubics.

    Center/radius conversion follows https://www.w3.org/TR/SVG/implnote.html
    B.2.4/B.2.5. The cubic representation is an approximation, not an SVG arc.
    """
    if large not in (0, 1) or sweep not in (0, 1):
        raise ValueError("Arc flags must be zero or one")
    if start == end:
        return []  # SVG identical-endpoint arc draws nothing.
    rx, ry = abs(rx), abs(ry)
    if rx == 0 or ry == 0:
        return [[start, end]]
    phi = math.radians(rotation % 360)
    cp, sp = math.cos(phi), math.sin(phi)
    dx, dy = (start[0] - end[0]) / 2, (start[1] - end[1]) / 2
    xp, yp = cp * dx + sp * dy, -sp * dx + cp * dy
    scale = xp * xp / (rx * rx) + yp * yp / (ry * ry)
    if scale > 1:
        factor = math.sqrt(scale)
        rx, ry = rx * factor, ry * factor
    numerator = max(0.0, rx * rx * ry * ry - rx * rx * yp * yp - ry * ry * xp * xp)
    denominator = rx * rx * yp * yp + ry * ry * xp * xp
    factor = math.sqrt(numerator / denominator) * (-1 if large == sweep else 1)
    cxp, cyp = factor * rx * yp / ry, -factor * ry * xp / rx
    cx = cp * cxp - sp * cyp + (start[0] + end[0]) / 2
    cy = sp * cxp + cp * cyp + (start[1] + end[1]) / 2
    ux, uy = (xp - cxp) / rx, (yp - cyp) / ry
    vx, vy = (-xp - cxp) / rx, (-yp - cyp) / ry
    theta = math.atan2(uy, ux)
    delta = math.atan2(ux * vy - uy * vx, ux * vx + uy * vy)
    if not sweep and delta > 0:
        delta -= 2 * math.pi
    elif sweep and delta < 0:
        delta += 2 * math.pi
    count = max(1, math.ceil(abs(delta) / (math.pi / 2) - 1e-12))

    def point(a: float) -> tuple[float, float]:
        return cx + rx * cp * math.cos(a) - ry * sp * math.sin(a), cy + rx * sp * math.cos(a) + ry * cp * math.sin(a)

    def tangent(a: float) -> tuple[float, float]:
        return -rx * cp * math.sin(a) - ry * sp * math.cos(a), -rx * sp * math.sin(a) + ry * cp * math.cos(a)

    result = []
    for index in range(count):
        a, b = theta + delta * index / count, theta + delta * (index + 1) / count
        p0, p3 = start if index == 0 else point(a), end if index == count - 1 else point(b)
        d0, d1 = tangent(a), tangent(b)
        k = 4 / 3 * math.tan((b - a) / 4)
        result.append([p0, (p0[0] + k * d0[0], p0[1] + k * d0[1]),
                       (p3[0] - k * d1[0], p3[1] - k * d1[1]), p3])
    return result


def parse_path(data: str) -> list[dict]:
    seq = tokens(data)
    result, current, origin = [], (0.0, 0.0), (0.0, 0.0)
    command, position, command_index = None, 0, 0
    while position < len(seq):
        explicit = seq[position].isalpha()
        if explicit:
            command = seq[position]
            position += 1
        if command is None or command.upper() not in ARITY:
            raise ValueError(f"Unsupported path command: {command}")
        op, relative = command.upper(), command.islower()
        command_index += 1
        if op == "Z":
            if not result:
                raise ValueError("Close before move")
            result.append(dict(op="Z", controls=[current, origin], command=command_index))
            current, command = origin, None
            continue
        n = ARITY[op]
        if position + n > len(seq) or any(x.isalpha() for x in seq[position:position + n]):
            raise ValueError("Incomplete path command")
        values = [float(x) for x in seq[position:position + n]]
        if not all(math.isfinite(x) for x in values):
            raise ValueError("Non-finite path coordinate")
        position += n
        if op in ("M", "L", "A"):
            end = tuple(values[-2:])
            if relative:
                end = current[0] + end[0], current[1] + end[1]
        elif op == "H":
            end = (current[0] + values[0] if relative else values[0], current[1])
        else:
            end = (current[0], current[1] + values[0] if relative else values[0])
        if op == "M":
            result.append(dict(op="M", point=end, command=command_index))
            origin = end
            command = "l" if relative else "L"
        else:
            if not result:
                raise ValueError("Path must begin with move")
            curves = arc_cubics(current, end, *values[:5]) if op == "A" else [[current, end]]
            for part, controls in enumerate(curves):
                result.append(dict(op=op, controls=controls, command=command_index, part=part))
        current = end
    return result


def import_svg(data: bytes, scene_id: str, provenance: dict | None = None) -> dict:
    if b"<!DOCTYPE" in data.upper() or b"<!ENTITY" in data.upper():
        raise ValueError("DTD/entity declarations unsupported")
    root = ET.fromstring(data)
    if root.tag != "{http://www.w3.org/2000/svg}svg":
        raise ValueError("Expected SVG root")
    allowed = {"width", "height", "viewBox", "fill", "stroke", "stroke-width", "stroke-linecap", "stroke-linejoin"}
    if set(root.attrib) - allowed:
        raise ValueError("Unsupported root attributes")
    if root.get("fill") != "none" or root.get("stroke") not in ("currentColor", "black", "#000", "#000000"):
        raise ValueError("Only unfilled visible stroked centerlines supported")
    if float(root.get("stroke-width", "1")) <= 0:
        raise ValueError("Invisible stroke unsupported")
    viewbox = [float(x) for x in re.split(r"[\s,]+", root.get("viewBox", "").strip())]
    if len(viewbox) != 4 or not all(math.isfinite(x) for x in viewbox) or min(viewbox[2:]) <= 0:
        raise ValueError("Invalid viewBox")
    scale = 512 / max(viewbox[2:])

    def quantize(point):
        return [math.floor((point[i] - viewbox[i]) * scale + 0.5) for i in range(2)]

    scene = dict(id=scene_id, title=f"{scene_id.split('-')[0].title()} church — imported SVG centerlines",
                 viewBox=[0, 0, 512, 512], coordinateQuantum=1, sites=[], edges=[], strokes=[],
                 objects=[], groups=[], regions=[], correspondences=[], crossings=[], contacts=[])
    evidence = []

    def site(point):
        sid = f"site-{len(scene['sites']):03d}"
        x, y = quantize(point)
        scene["sites"].append(dict(id=sid, x=x, y=y))
        return sid

    for path_index, element in enumerate(root):
        if element.tag != "{http://www.w3.org/2000/svg}path" or set(element.attrib) != {"d"}:
            raise ValueError("Only path elements with a d attribute supported")
        stroke, start, previous = None, None, None
        for item in parse_path(element.attrib["d"]):
            if item["op"] == "M":
                start = previous = site(item["point"])
                stroke = dict(id=f"stroke-{len(scene['strokes']):03d}", edges=[])
                scene["strokes"].append(stroke)
                continue
            if stroke is None:
                raise ValueError("Missing subpath start")
            target = start if item["op"] == "Z" else site(item["controls"][-1])
            eid = f"edge-{len(scene['edges']):03d}"
            controls = [quantize(point) for point in item["controls"]]
            scene["edges"].append(dict(id=eid, **{"from": previous, "to": target}, controls=controls))
            stroke["edges"].append(eid)
            evidence.append(dict(edge=eid, svgPathIndex=path_index, commandIndex=item["command"],
                                 command=item["op"], arcPart=item.get("part")))
            previous = target
    scene["notes"] = [
        "Source SVG document and subpath order retained; this is not human pen-time order.",
        "Topology asserts only each explicit path's continuity and Z closure. Equal coordinates across paths are distinct sites.",
        "Object membership, closed-region meaning, figure/ground, mirroring, repetition, template/detail, contact, and occlusion remain unknown.",
        "Circular/elliptical arcs are approximated by cubic spans of at most 90 degrees, then all controls quantized to one tick in a 512-unit basis.",
        "Visible stroke width, cap/join paint, and filled area are not reproduced by this centerline geometry.",
    ]
    scene["source"] = dict(provenance or {}, sha256=hashlib.sha256(data).hexdigest(), originalViewBox=viewbox,
                           uniformScale=scale, translation=[-viewbox[0], -viewbox[1]],
                           acquisitionOrder="SVG document/subpath order", edgeCorrespondence=evidence,
                           approximation="A to cubic spans <=90 degrees; nearest integer control ticks, ties toward positive infinity")
    return scene


def convert_references(directory: Path, output: Path) -> list[dict]:
    manifest = json.loads((directory / "manifest.json").read_text(encoding="utf-8"))
    scenes = []
    for name in ("lucide", "tabler"):
        item = next(x for x in manifest["items"] if x["collection"] == name and x["role"] == "asset")
        data = (directory / name / "church.svg").read_bytes()
        if hashlib.sha256(data).hexdigest() != item["sha256"]:
            raise ValueError(f"Source hash mismatch: {name}")
        provenance = {key: item[key] for key in ("collection", "sourceUrl", "sourcePage", "license", "attribution", "revision")}
        scenes.append(import_svg(data, name + "-church", provenance))
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text("".join(json.dumps(scene, separators=(",", ":"), ensure_ascii=False) + "\n" for scene in scenes), encoding="utf-8")
    return scenes


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--references", type=Path, default=Path("D:/_quickDraw/gojo-line-art/references"))
    parser.add_argument("--output", type=Path, default=Path("D:/_quickDraw/gojo-line-art/references/imported-scenes.ndjson"))
    args = parser.parse_args()
    scenes = convert_references(args.references, args.output)
    print(json.dumps([dict(id=x["id"], sites=len(x["sites"]), edges=len(x["edges"]), strokes=len(x["strokes"])) for x in scenes]))
