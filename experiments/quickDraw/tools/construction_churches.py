"""Authored construction ladders for every church in the line-art study.

The 12 analytic fixtures and two pinned SVG centerline imports keep their exact
final controls and explicit endpoint identities. Earlier masses are positioning
guides, with no containment promise. These recipes describe authored drawing
steps; source SVG order remains separate acquisition provenance.
"""
from __future__ import annotations

import hashlib
import json
from pathlib import Path

from authored_line_art import church_scenes
from construction_engines import SourceRecipe
from import_line_art_svg import import_svg


SOURCE_DIRECTORY = Path(__file__).resolve().parents[1] / "sources" / "line-art"


def church_sources():
    """Reproduce all 14 sources using repository-owned, revision-pinned inputs."""
    scenes = church_scenes()
    manifest = json.loads((SOURCE_DIRECTORY / "manifest.json").read_text(encoding="utf-8"))
    for collection in ("lucide", "tabler"):
        item = next(row for row in manifest["items"]
                    if row["collection"] == collection and row["role"] == "asset")
        contents = (SOURCE_DIRECTORY / item["file"]).read_bytes()
        if hashlib.sha256(contents).hexdigest() != item["sha256"]:
            raise ValueError(f"Pinned church source changed: {collection}")
        provenance = {key: item[key] for key in
                      ("collection", "sourceUrl", "sourcePage", "license", "attribution", "revision")}
        scenes.append(import_svg(contents, collection + "-church", provenance))
    return scenes


# These are rough positioning choices, not proposed architectural constraints.
# In particular towers may extend through roofs and arches through rectangles.
MASS_PLANS = {
    "church-01": [("wall", "rectangle", (116, 278, 396, 440)),
                  ("roof", "triangle", (116, 176, 396, 278))],
    "church-02": [("wall", "rectangle", (110, 294, 402, 440)),
                  ("roof", "triangle", (110, 224, 402, 294)),
                  ("tower", "rectangle", (208, 94, 304, 294)),
                  ("spire", "triangle", (208, 94, 304, 142))],
    "church-03": [("wall", "rectangle", (190, 270, 412, 440)),
                  ("roof", "triangle", (190, 190, 412, 270)),
                  ("side-tower", "rectangle", (94, 96, 190, 440)),
                  ("spire", "triangle", (94, 96, 190, 148))],
    "church-04": [("wall", "rectangle", (84, 268, 428, 440)),
                  ("roof", "triangle", (168, 208, 344, 268)),
                  ("tower-left", "rectangle", (84, 126, 168, 440)),
                  ("tower-right", "rectangle", (344, 126, 428, 440)),
                  ("spire-left", "triangle", (84, 126, 168, 186)),
                  ("spire-right", "triangle", (344, 126, 428, 186))],
    "church-05": [("wall", "rectangle", (122, 272, 390, 440)),
                  ("drum", "rectangle", (182, 136, 330, 272)),
                  ("dome", "oval", (182, 136, 330, 284))],
    "church-06": [("wall", "rectangle", (82, 294, 430, 440)),
                  ("roof", "triangle", (82, 252, 430, 294)),
                  ("upper-mass", "rectangle", (166, 146, 346, 294)),
                  ("upper-roof", "triangle", (166, 146, 346, 204))],
    "church-07": [("wall", "rectangle", (114, 256, 366, 440)),
                  ("roof", "triangle", (114, 164, 366, 256)),
                  ("annex", "rectangle", (366, 300, 438, 440)),
                  ("annex-roof", "triangle", (366, 300, 438, 338))],
    "church-08": [("wall", "rectangle", (136, 286, 376, 440)),
                  ("roof", "triangle", (136, 100, 376, 286))],
    "church-09": [("wall", "rectangle", (100, 252, 430, 440)),
                  ("roof", "triangle", (176, 252, 430, 312)),
                  ("side-tower", "rectangle", (100, 122, 176, 440)),
                  ("spire", "triangle", (100, 122, 176, 172)),
                  ("apse", "oval", (356, 252, 468, 440))],
    "church-10": [("wall", "rectangle", (114, 288, 398, 440)),
                  ("roof", "triangle", (114, 182, 398, 288))],
    "church-11": [("wall", "rectangle", (126, 282, 386, 440)),
                  ("roof", "triangle", (126, 220, 386, 282)),
                  ("tower", "rectangle", (210, 116, 302, 282)),
                  ("spire", "triangle", (210, 116, 302, 164))],
    "church-12": [("triangular-shell", "triangle", (84, 108, 428, 440))],
    "lucide-church": [("side-masses", "rectangle", (43, 238, 469, 448)),
                      ("central-mass", "rectangle", (128, 43, 384, 448)),
                      ("roof", "triangle", (128, 43, 384, 149))],
    "tabler-church": [("wall", "rectangle", (128, 299, 384, 448)),
                      ("roof", "triangle", (85, 171, 427, 341))],
}


def _source_scopes(scene):
    """Select authored scopes, retaining import topology without inventing joints."""
    if scene["id"] == "lucide-church":
        return {"outline": [f"edge-{i:03d}" for i in range(6, 22)],
                "door": [f"edge-{i:03d}" for i in range(2, 6)],
                "cross": ["edge-000", "edge-001"]}
    if scene["id"] == "tabler-church":
        return {"outline": [f"edge-{i:03d}" for i in (0, 7, 8, 9, 10)],
                "door": [f"edge-{i:03d}" for i in range(1, 5)],
                "cross": ["edge-005", "edge-006"]}
    return {row["id"]: list(row["edges"]) for row in scene["groups"]}


def _bounds(scene, edges):
    wanted = set(edges)
    points = [point for edge in scene["edges"] if edge["id"] in wanted
              for point in edge["controls"]]
    return (min(p[0] for p in points), min(p[1] for p in points),
            max(p[0] for p in points), max(p[1] for p in points))


def _nearest_mass(point, masses):
    """Choose an authored local positioning frame; outside locations stay legal."""
    x, y = point

    def score(frame):
        left, top, right, bottom = frame.bounds
        dx, dy = max(left - x, 0, x - right), max(top - y, 0, y - bottom)
        return dx * dx + dy * dy, (right - left) * (bottom - top)

    return min(masses, key=score)


def _add_scope(recipe, source_ids, frame, masses=None, chords=False):
    # Prebind endpoints to the most local earlier mass. Distinct source identities
    # remain distinct even when their coordinates happen to coincide.
    if masses:
        for edge_id in source_ids:
            edge = recipe.source_edges[edge_id]
            for source_id in (edge["from"], edge["to"]):
                site = recipe.source_sites[source_id]
                recipe.source_site(source_id, _nearest_mass((site["x"], site["y"]), masses))
    return recipe.source_scope(source_ids, frame, chords=chords)


def _build_church(scene):
    recipe = SourceRecipe(scene, scene["id"], title="Churches from construction guides", family="church-series")
    recipe.data["variantLabel"] = scene["title"]
    recipe.data["description"] = (
        "An authored rough-to-finished construction of the existing line-art source. "
        "Overlapping masses are positioning guides; later controls consume earlier "
        "frames and repeated controls consume their actual source occurrence.")
    recipe.data["sourceProvenance"] = {
        "sourceScene": scene["id"], "source": scene.get("source", {}),
        "sourceStrokeOrder": scene["strokes"],
        "constructionOrder": "New authored approximation steps; not recorded human pen order.",
        "roughMassContainment": "Unconstrained positioning guides, not occupied boundaries.",
    }
    scopes = _source_scopes(scene)
    repeats = [row for row in scene["correspondences"] if row["kind"] == "repeat"]
    copied_edges = {edge for row in repeats for edge in row["target"]}
    detail_names = [name for name in scopes if name.startswith("cross") or name == "timber-bracing"]
    opening_names = [name for name in scopes if name != "outline" and name not in detail_names]
    primary_names = [name for name in opening_names if not set(scopes[name]) <= copied_edges]

    recipe.begin("Place overlapping rough masses",
                 "Small rectangle, triangle and oval constructions establish positions. "
                 "Their overlaps and extensions are permitted approximation choices.")
    recipe.frame()
    page = recipe.source_frame("source-page", (0, 0, 512, 512), emit=False)
    masses = [recipe.source_frame("mass-" + key, bounds, parent=page, kind=kind)
              for key, kind, bounds in MASS_PLANS[scene["id"]]]
    mass_edges = [edge for frame in masses for edge in frame.edges]
    recipe.finish(mass_edges)

    recipe.begin("Trace a coarse connected silhouette",
                 "Silhouette endpoints consume the earlier positioning masses. Curved "
                 "outline spans begin as straight chords with the same endpoint identities.")
    outline_chords = _add_scope(recipe, scopes["outline"], masses[0], masses, chords=True)
    recipe.finish(outline_chords, mass_edges)

    recipe.begin("Reserve the first opening areas",
                 "Rectangular proxies locate the doorway and each first opening. "
                 "Repeated copies will be constructed from these first occurrences later.")
    opening_frames = {name: recipe.source_frame("opening-" + name,
                      _bounds(scene, scopes[name]), parent=masses[0]) for name in primary_names}
    opening_boxes = [edge for frame in opening_frames.values() for edge in frame.edges]
    recipe.finish(outline_chords + opening_boxes, mass_edges)

    recipe.begin("Refine outlines and opening profiles",
                 "Replace straight proxies by the source's line, quadratic or cubic "
                 "profiles. The original boxes and chords remain available as guides.")
    outline = _add_scope(recipe, scopes["outline"], masses[0], masses)
    first_openings = [edge for name in primary_names
                      for edge in _add_scope(recipe, scopes[name], opening_frames[name])]
    active = outline + first_openings
    guides = mass_edges + opening_boxes + outline_chords
    recipe.finish(active, guides)

    copied = []
    if repeats:
        recipe.begin("Build the repeated occurrences",
                     "Declared source repetitions translate the actual earlier control sites "
                     "and preserve each occurrence's distinct identities. Nonrepeating parts are carried.")
        for row in repeats:
            dx, dy = row["translation"]
            copied.extend(recipe.translate_scope(row["id"], row["source"], row["target"],
                                                 dx, dy, masses[0]))
        recipe.finish(active + copied, guides)

    recipe.begin("Add the remaining local detail",
                 "Crosses, braces and other subordinate strokes consume existing sites "
                 "or earlier masses. The final source drawing retains its original endpoint topology.")
    for name in detail_names:
        _add_scope(recipe, scopes[name], masses[0], masses)
    # The source order here is an exact final membership/order read. It does not
    # rewrite the distinct authored order in which each construction was added.
    final = recipe.source_scope([edge["id"] for edge in scene["edges"]], masses[0])
    recipe.finish(final, guides)
    recipe.finalize_source_mapping()
    return recipe.data


def build_churches():
    """Return one five/six-rung executable source recipe per existing church."""
    return [_build_church(scene) for scene in church_sources()]
