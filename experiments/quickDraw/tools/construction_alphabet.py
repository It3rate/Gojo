"""Construct every authored capital, and their atlas, through earlier guides.

The source supplies measured control sites, not a raster to trace.  The four
passes are an authored drawing proposal: a primitive positioning guide, a
coarse path network, shared junctions, then the source's complete paths.  This
does not claim to recover the order in which a person would draw a letter.
"""
from __future__ import annotations

from dataclasses import dataclass, field

from authored_line_art import alphabet_scene
from construction_engines import Frame, SourceRecipe
from construction_recipes import loc


LETTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
OVAL_LETTERS = frozenset("CGOQ")
LANDMARK_LETTERS = frozenset("LMNVWXY")

# These are authored selections of principal paths, not inferred typography.
# Their actual meaning remains the connected source endpoint/control rows.
SUBORDINATE = {
    "A": {"bar"},
    "E": {"bar-middle"},
    "F": {"bar-middle"},
    "G": {"bar"},
    "H": {"bar"},
    "I": {"top-left-bar", "top-right-bar", "bottom-left-bar", "bottom-right-bar"},
    "K": {"arm-upper", "arm-lower"},
    "Q": {"tail-inner", "tail-outer"},
    "R": {"leg"},
    "T": {"bar-left", "bar-right"},
    "Z": {"bottom-bar"},
}


@dataclass
class LetterConstruction:
    letter: str
    origin: tuple[int, int]
    source_edges: list[str]
    rough: Frame | None = None
    working: Frame | None = None
    landmarks: Frame | None = None
    mass: list[str] = field(default_factory=list)
    coarse: list[str] = field(default_factory=list)
    coarse_sites: dict[str, str] = field(default_factory=dict)
    coarse_coordinates: dict[str, tuple[int, int]] = field(default_factory=dict)
    principal: list[str] = field(default_factory=list)
    markers: list[str] = field(default_factory=list)


def _letters(scene, selection=LETTERS):
    result = []
    for letter in selection:
        ordinal = LETTERS.index(letter)
        column, row = ordinal % 6, ordinal // 6
        if row == 4:
            column += 2
        origin = (52 + column * 200, 40 + row * 190)
        edges = [e["id"] for e in scene["edges"] if e["id"].startswith(letter + "-")]
        result.append(LetterConstruction(letter, origin, edges))
    return result


def _mass(r, state, parent=None):
    x, y = state.origin
    # A positioning template may extend beyond the final paths.  No occupied
    # material or containment condition is inferred from its boundary.
    bounds = (x - 8, y - 8, x + 104, y + 136)
    kind = "oval" if state.letter in OVAL_LETTERS else "rectangle"
    if state.letter == "A":
        frame = r.source_frame(state.letter + "-mass", bounds, parent, kind="triangle")
    elif state.letter == "V":
        frame = r.source_frame(state.letter + "-mass", bounds, parent, kind="inverted-triangle")
    elif state.letter in "TXY":
        frame = r.source_frame(state.letter + "-mass", bounds, parent, kind="cross")
    else:
        frame = r.source_frame(state.letter + "-mass", bounds, parent, kind=kind)
    state.rough, state.mass = frame, list(frame.edges)
    if state.letter == "S":
        # Two loose oval masses position the alternating upper/lower bends.
        for index, offset in enumerate((0, 64)):
            oval = r.source_frame(f"S-lobe-{index}",
                                  (x - 8, y + offset - 8, x + 104, y + offset + 72),
                                  frame, kind="oval")
            state.mass.extend(oval.edges)


def _coarse_point(state, point):
    """A declared finite block-in calibration, deliberately distinct from final.

    Small snapping plus a modest inset makes the placement approximate even
    for entirely straight letters.  These source integers become ordinary
    affine locators through the earlier mass frame.
    """
    x, y = state.origin
    px, py = point
    snap_x = round((px - x) / 12) * 12
    snap_y = round((py - y) / 16) * 16
    return x + 4 + round(snap_x * .92), y + 6 + round(snap_y * .90)


def _midpoint(controls):
    # Bézier evaluation at t=1/2, with an explicitly finite authored tick.
    if len(controls) == 3:
        weights, denominator = (1, 2, 1), 4
    elif len(controls) == 4:
        weights, denominator = (1, 3, 3, 1), 8
    else:
        raise ValueError("A coarse bend requires a quadratic or cubic source")
    return tuple(round(sum(w * p[d] for w, p in zip(weights, controls)) / denominator)
                 for d in (0, 1))


def _coarse(r, state):
    x, y = state.origin
    state.working = r.source_frame(state.letter + "-working", (x, y, x + 96, y + 128),
                                   state.rough, emit=False)
    for source_id in state.source_edges:
        edge = r.source_edges[source_id]
        for source_site in (edge["from"], edge["to"]):
            if source_site not in state.coarse_sites:
                source = r.source_sites[source_site]
                point = _coarse_point(state, (source["x"], source["y"]))
                state.coarse_coordinates[source_site] = point
                state.coarse_sites[source_site] = r.source_point("coarse-site-" + source_site,
                                                                point, state.working)
        controls = [state.coarse_sites[edge["from"]]]
        if len(edge["controls"]) > 2:
            controls.append(r.source_point("coarse-bend-" + source_id,
                _coarse_point(state, _midpoint(edge["controls"])), state.working))
        controls.append(state.coarse_sites[edge["to"]])
        state.coarse.extend(r.poly("coarse-path-" + source_id, controls, False))


def _landmark_mark(r, state, source_id, site):
    # Locator crosses are construction lines, not additional letter strokes.
    # Each consumes the actual shared site that later final paths consume.
    frame = state.landmarks
    width = frame.bounds[2] - frame.bounds[0]
    height = frame.bounds[3] - frame.bounds[1]
    stem = "landmark-" + source_id
    left = r.point(stem + "-left", loc(frame.b, frame.a, 2, width, origin=site), loc(site))
    right = r.point(stem + "-right", loc(frame.a, frame.b, 2, width, origin=site), loc(site))
    top = r.point(stem + "-top", loc(site), loc(frame.b, frame.a, 2, height, origin=site))
    bottom = r.point(stem + "-bottom", loc(site), loc(frame.a, frame.b, 2, height, origin=site))
    return [r.edge(stem + "-horizontal", [left, site]),
            r.edge(stem + "-horizontal-end", [site, right]),
            r.edge(stem + "-vertical", [top, site]),
            r.edge(stem + "-vertical-end", [site, bottom])]


def _landmarks(r, state):
    x, y = state.origin
    state.landmarks = r.source_frame(state.letter + "-landmark-frame",
                                     (x, y, x + 96, y + 128), state.working, emit=False)
    used = dict.fromkeys(s for edge_id in state.source_edges
                        for s in (r.source_edges[edge_id]["from"], r.source_edges[edge_id]["to"]))
    width, height = state.landmarks.bounds[2] - x, state.landmarks.bounds[3] - y
    for source_id in used:
        source = r.source_sites[source_id]
        coarse_x, coarse_y = state.coarse_coordinates[source_id]
        coarse_site = state.coarse_sites[source_id]
        # The correction consumes an actual earlier path vertex.  A final
        # endpoint therefore retains correspondence to its rough predecessor,
        # rather than independently asking the page for the same coordinates.
        site = r.point("source-site-" + source_id,
            loc(state.landmarks.a, state.landmarks.b, source["x"] - coarse_x,
                width, origin=coarse_site),
            loc(state.landmarks.a, state.landmarks.b, source["y"] - coarse_y,
                height, origin=coarse_site))
        r.site_map[source_id] = site
        if state.letter in LANDMARK_LETTERS:
            state.markers.extend(_landmark_mark(r, state, source_id, site))
    if state.letter not in LANDMARK_LETTERS:
        omitted = SUBORDINATE.get(state.letter, set())
        principal = [edge_id for edge_id in state.source_edges
                     if edge_id.removeprefix(state.letter + "-") not in omitted]
        for edge_id in principal:
            source = r.source_edges[edge_id]
            # Straight source paths may already be carried into the final
            # rung.  Curves initially expose their endpoint chord only.
            state.principal.append(r.source_edge(edge_id, state.landmarks,
                                                  chord=len(source["controls"]) > 2))


def _finish(r, states, rung):
    mass = [edge for state in states for edge in state.mass]
    coarse = [edge for state in states for edge in state.coarse]
    principal = [edge for state in states for edge in state.principal]
    markers = [edge for state in states for edge in state.markers]
    if rung == 0:
        r.finish(mass)
    elif rung == 1:
        r.finish(coarse, mass)
    elif rung == 2:
        r.finish(principal + markers, mass + coarse)
    else:
        visible = []
        for state in states:
            visible.extend(r.source_scope(state.source_edges, state.landmarks))
        r.finish(visible, mass + coarse + principal + markers)


def _construct(scene, selection, atlas=False):
    states = _letters(scene, selection)
    source_edges = [e for state in states for e in state.source_edges]
    if atlas:
        key, title = "alphabet-atlas", "The complete alphabet, drawn in four passes"
        family, view_box, variant = "alphabet-atlas", scene["viewBox"], "A–Z together"
    else:
        letter, (x, y) = states[0].letter, states[0].origin
        key, title = "alphabet-" + letter.lower(), "Capital letters from construction guides"
        family, view_box, variant = "alphabet-series", [x - 24, y - 24, 192, 192], letter
    r = SourceRecipe(scene, key, title, source_edges, family, view_box=view_box)
    r.data["variantLabel"] = variant
    r.data["description"] = (
        "Four authored passes: primitive positioning guides, approximate path network, "
        "shared junctions and principal paths, then measured source curves and details. "
        "The guides may extend beyond the final letter; no containment is imposed.")
    notes = [
        ("Place primitive positioning guides", "Rectangles, triangles, ovals and connected crosses establish rough working areas. Their bounds are guides, with no inside/outside condition."),
        ("Block in the path network", "Earlier mass frames locate a deliberately approximate connected skeleton. Bends become two-segment chords, and source endpoint aliases remain shared within this rough network."),
        ("Resolve junctions and principal paths", "Earlier working frames locate the actual shared endpoints. Principal strokes and curve chords establish structure; simple line-only letters display their endpoint and junction locators."),
        ("Refine profiles and add subordinate strokes", "Final curves consume the earlier endpoints. Bars, branches and tails complete the source construction; carried straight paths retain their identity."),
    ]
    for rung, (title, note) in enumerate(notes):
        r.begin(title, note)
        if rung == 0:
            r.frame()
        for state in states:
            if rung == 0:
                _mass(r, state)
            elif rung == 1:
                _coarse(r, state)
            elif rung == 2:
                _landmarks(r, state)
        _finish(r, states, rung)
    r.data["alphabetConstruction"] = {
        "letters": selection,
        "source": "authored_line_art.alphabet_scene",
        "coarsePlacement": "12×16 source-tick snapping, then .92×.90 inset with offset (4,6)",
        "curvedBlockIn": "finite Bézier midpoint at t=1/2, joined by two straight spans",
        "provenance": "authored recipe; not recovered human drawing order or learned intent",
    }
    return r.finalize_source_mapping()


def build_alphabet():
    """Return 26 independent capital programs and one original-layout atlas."""
    scene = alphabet_scene()
    return [*[_construct(scene, letter) for letter in LETTERS],
            _construct(scene, LETTERS, atlas=True)]


if __name__ == "__main__":
    import json
    print(json.dumps({"programs": len(build_alphabet()), "letters": len(LETTERS)}))
