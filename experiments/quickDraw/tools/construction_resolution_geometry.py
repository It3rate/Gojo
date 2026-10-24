"""Finite authored coordinate reads with retained arrangement and correspondences.

This adapter chooses a less precise source fixture for a construction program.
It is not a native geometry solver or a learned prior. The native receiver is
given the chosen finite coordinates, never a hidden exact correction. Exact
source geometry remains a separate reference for evaluating the experiment.

Equal scalar coordinates share an alignment read, not endpoint identity.
Declared mirrors and translated copies restrict which scalar reads may move
together. Rational elimination finds those allowed directions; finite rounding
then selects among them. An unavailable coarse placement leaves an explicit
finer exception rather than collapsing an opening or reversing its order.
"""
from __future__ import annotations

from copy import deepcopy
from fractions import Fraction
from math import gcd, lcm, sqrt


DIVISIONS = (0, 64, 32, 16, 8)


def _paired_controls(scene, relation):
    edges = {edge["id"]: edge for edge in scene["edges"]}
    for first, second, reverse in zip(relation["source"], relation["target"],
                                      relation["reversed"], strict=True):
        a, b = edges[first]["controls"], edges[second]["controls"]
        if reverse:
            b = b[::-1]
        yield from zip(a, b, strict=True)


def _directions(scene, dimension, values):
    """Read homogeneous correspondence equations, returning exact free moves.

    A mirror supplies a+b=c+d along its reflected axis. A translated copy
    supplies b-a=d-c. The other mirror axis supplies equal displacement. The
    coordinate variables are observation slots; endpoint identities are not
    merged. Original coordinates satisfy every equation before rounding starts.
    """
    index = {value: ordinal for ordinal, value in enumerate(values)}
    rows = []
    for relation in scene.get("correspondences", []):
        if relation["kind"] not in ("mirror", "repeat"):
            continue
        if relation["kind"] == "mirror" and (len(relation["axis"]) != 2 or
                relation["axis"][0][0] != relation["axis"][1][0]):
            raise ValueError("This finite fixture adapter supports declared vertical mirror axes")
        pairs = list(_paired_controls(scene, relation))
        if not pairs:
            continue
        first, second = pairs[0]
        for a, b in pairs[1:]:
            terms = [(a[dimension], 1), (b[dimension], 1),
                     (first[dimension], -1), (second[dimension], -1)] \
                if relation["kind"] == "mirror" and dimension == 0 else [
                    (b[dimension], 1), (a[dimension], -1),
                    (second[dimension], -1), (first[dimension], 1)]
            row = {}
            for value, weight in terms:
                column = index[value]
                row[column] = row.get(column, Fraction(0)) + weight
            row = {column: value for column, value in row.items() if value}
            if row:
                if sum(values[column] * weight for column, weight in row.items()):
                    raise ValueError("Source correspondence has inconsistent controls")
                rows.append(row)

    pivots = {}
    for row in rows:
        for column, basis in sorted(pivots.items()):
            weight = row.get(column, 0)
            if weight:
                for other, value in basis.items():
                    row[other] = row.get(other, 0) - weight * value
                row = {other: value for other, value in row.items() if value}
        if row:
            column = min(row)
            scale = row[column]
            pivots[column] = {other: value / scale for other, value in row.items()}
    for column in sorted(pivots, reverse=True):
        for earlier in sorted(pivots):
            if earlier >= column:
                break
            weight = pivots[earlier].get(column, 0)
            if weight:
                for other, value in pivots[column].items():
                    pivots[earlier][other] = pivots[earlier].get(other, 0) - weight * value
                pivots[earlier] = {other: value for other, value in pivots[earlier].items() if value}
    result = []
    for free in range(len(values)):
        if free in pivots:
            continue
        direction = {free: Fraction(1)}
        direction.update({column: -row[free] for column, row in pivots.items() if row.get(free)})
        result.append((free, direction))
    return result


def _nearest_lattice_values(value, quantum, packet):
    """Nearest grid values whose change preserves every integer coordinate."""
    # Solve target = 0 (mod quantum), target = value (mod packet).
    common = gcd(quantum, packet)
    if value % common:
        return []
    period = lcm(quantum, packet)
    residue = next((candidate for candidate in range(0, period, quantum)
                    if (candidate - value) % packet == 0), None)
    if residue is None:
        return []
    lower = residue + ((value - residue) // period) * period
    upper = lower + period
    return sorted({lower, upper}, key=lambda candidate: (abs(candidate - value), candidate))


def _axis_read(scene, dimension, nominal_quantum):
    name = "xy"[dimension]
    values = sorted({site[name] for site in scene["sites"]} |
                    {point[dimension] for edge in scene["edges"] for point in edge["controls"]})
    selected = list(values)
    directions = _directions(scene, dimension, values)
    mirrors = [list(_paired_controls(scene, relation))[0]
               for relation in scene.get("correspondences", [])
               if relation["kind"] == "mirror" and relation["source"]]
    index = {value: ordinal for ordinal, value in enumerate(values)}
    decisions = []
    for free, direction in directions:
        before = selected[free]
        packet = lcm(*(weight.denominator for weight in direction.values()))
        quantum, attempts = nominal_quantum, []
        chosen = None
        while quantum >= 1:
            candidates = _nearest_lattice_values(before, quantum, packet)
            rejected = []
            for target in candidates:
                delta = target - before
                candidate = list(selected)
                for column, weight in direction.items():
                    result = Fraction(candidate[column]) + delta * weight
                    if result.denominator != 1:
                        raise AssertionError("Lattice packet did not preserve integer coordinates")
                    candidate[column] = int(result)
                if any(a >= b for a, b in zip(candidate, candidate[1:])):
                    rejected.append("would collapse or reverse an ordered scalar interval")
                    continue
                if dimension == 0 and any((candidate[index[a[0]]] + candidate[index[b[0]]]) % 2
                                          for a, b in mirrors):
                    rejected.append("would require a fractional authored mirror axis")
                    continue
                chosen = candidate
                break
            if chosen is not None:
                selected = chosen
                break
            attempts.append({"quantum": quantum,
                             "reason": "; ".join(dict.fromkeys(rejected)) or
                             "grid cannot preserve integer correspondence participation"})
            quantum //= 2
        if chosen is None:
            raise AssertionError("The exact one-tick source read must remain available")
        decisions.append({"axis": name, "source": values[free], "before": before,
                          "selected": selected[free], "nominalQuantum": nominal_quantum,
                          "effectiveQuantum": quantum, "correspondencePacket": packet,
                          "dependentReads": len(direction) - 1, "finerException": quantum < nominal_quantum,
                          "attempts": attempts})
    return dict(zip(values, selected, strict=True)), decisions, len(values) - len(directions)


def coarsen_scene(scene, divisions):
    """Return ``(finite_scene, report)`` for the selected nominal local grid.

    0 retains the exact fixture. Churches use a 512-tick reference span;
    alphabet glyphs use their 128-tick cap height, including in the atlas.
    The grid is a requested grade, not a promise that every coordinate can use
    it. Necessary finer slots are reported individually and stay numeric.
    """
    if divisions not in DIVISIONS:
        raise ValueError(f"Supported division counts are {DIVISIONS}")
    result = deepcopy(scene)
    reference_span = 128 if scene["id"] == "diagnostic-alphabet" else 512
    quantum = reference_span // divisions if divisions else 1
    if divisions:
        xmap, xdecisions, xdependent = _axis_read(scene, 0, quantum)
        ymap, ydecisions, ydependent = _axis_read(scene, 1, quantum)
        for site in result["sites"]:
            site["x"], site["y"] = xmap[site["x"]], ymap[site["y"]]
        for edge in result["edges"]:
            edge["controls"] = [[xmap[x], ymap[y]] for x, y in edge["controls"]]
        for relation in result.get("correspondences", []):
            pairs = list(_paired_controls(result, relation))
            if not pairs:
                continue
            first, second = pairs[0]
            if relation["kind"] == "mirror":
                center = (first[0] + second[0]) // 2
                relation["axis"] = [[center, point[1]] for point in relation["axis"]]
            elif relation["kind"] == "repeat":
                relation["translation"] = [second[d] - first[d] for d in (0, 1)]
        decisions = xdecisions + ydecisions
        dependent = xdependent + ydependent
    else:
        decisions, dependent = [], 0
        xmap = {value: value for value in
                {site["x"] for site in scene["sites"]} |
                {point[0] for edge in scene["edges"] for point in edge["controls"]}}
        ymap = {value: value for value in
                {site["y"] for site in scene["sites"]} |
                {point[1] for edge in scene["edges"] for point in edge["controls"]}}
    displacements = []
    changed_controls = 0
    for source, finite in zip(scene["edges"], result["edges"], strict=True):
        for a, b in zip(source["controls"], finite["controls"], strict=True):
            distance = sqrt(sum((x - y) ** 2 for x, y in zip(a, b)))
            displacements.append(distance)
            changed_controls += a != b
    histogram = {}
    for decision in decisions:
        key = str(decision["effectiveQuantum"])
        histogram[key] = histogram.get(key, 0) + 1
    report = {
        "sourceScene": scene["id"], "divisions": divisions,
        "referenceSpan": reference_span, "nominalQuantum": quantum,
        "law": "finite rounding along declared alignment, mirror and translation correspondences",
        "qualification": "Authored finite numeric fixture; local finer exceptions preserve arrangement. Not a pre-numeric encoding or learned prior.",
        "preserved": ["endpoint identity and incidence", "ordered source edge and stroke membership",
                      "strict scalar order and alignment of endpoints and controls",
                      "declared mirror scopes", "declared translated-copy scopes"],
        "independentReads": len(decisions), "dependentReads": dependent,
        "retainedFineCount": sum(decision["finerException"] for decision in decisions),
        "effectiveQuantumHistogram": histogram, "decisions": decisions,
        "changedControls": changed_controls, "totalControls": len(displacements),
        "maximumDisplacement": max(displacements, default=0),
        "meanDisplacement": sum(displacements) / len(displacements) if displacements else 0,
        "axisReadMap": {"x": [[a, b] for a, b in sorted(xmap.items())],
                        "y": [[a, b] for a, b in sorted(ymap.items())]},
    }
    return result, report
