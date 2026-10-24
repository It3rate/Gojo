"""Construction dependencies, actual primitive participation and retained ink."""
import gzip
import json
from pathlib import Path
import unittest

from quickdraw_gestalt import Author
from quickdraw_gestalt_geometry import build


def replay(program, override=None):
    """Independent integer locator read; overrides perturb one exposed site."""
    points = {}
    override = override or {}
    for site in program["sites"]:
        if site["id"] in override:
            points[site["id"]] = tuple(override[site["id"]])
            continue
        result = []
        for axis, field in enumerate(("x", "y")):
            value = site[field]
            if isinstance(value, int):
                result.append(value)
            else:
                first, last = points[value["from"]][axis], points[value["to"]][axis]
                numerator = (last-first)*value["numerator"]
                displacement = (abs(numerator)//value["denominator"])*(-1 if numerator < 0 else 1)
                result.append(points[value.get("origin", value["from"])][axis]+displacement)
        points[site["id"]] = tuple(result)
    return points


class QuickDrawGestaltAuthorTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        path = Path(__file__).resolve().parents[1]/"results"/"quickdraw-construction"/"observations.ndjson.gz"
        with gzip.open(path, "rt", encoding="utf-8") as handle:
            observations = [json.loads(line) for line in handle if line.strip()]
        cls.cases = []
        for observation in observations:
            proposal = build(observation)
            author = Author(observation, proposal)
            program, manifest = author.run()
            cls.cases.append((observation, proposal, author, program, manifest))

    def test_every_locator_uses_previously_constructed_sites_and_replays_without_side_tables(self):
        for _, _, author, program, _ in self.cases:
            positions = replay(json.loads(json.dumps(program)))
            self.assertEqual(author.positions, positions)
            known = {}
            for site in program["sites"]:
                for axis in ("x", "y"):
                    expr = site[axis]
                    if not isinstance(expr, dict):
                        continue
                    for key in (expr["from"], expr["to"], expr.get("origin", expr["from"])):
                        self.assertIn(key, known)
                        self.assertLessEqual(known[key], site["at"])
                known[site["id"]] = site["at"]

    def test_replacement_outputs_respond_to_actual_consumed_input_controls(self):
        checked = 0
        for _, proposal, author, program, _ in self.cases:
            normal = replay(program)
            for step in proposal["steps"]:
                for operation in step["operations"]:
                    if not operation["accepted"] or operation["kind"] == "carry-detail-complement":
                        continue
                    inputs = [site for eid in operation["sourceEdgeIds"] for site in author.controls(eid)]
                    outputs = [site for eid in operation["targetEdgeIds"] for site in author.controls(eid)]
                    changed = False
                    for site in dict.fromkeys(inputs):
                        old = normal[site]
                        altered = replay(program, {site: (old[0]+512, old[1]+256)})
                        if any(altered[out] != normal[out] for out in outputs):
                            changed = True
                            break
                    self.assertTrue(changed, operation["kind"])
                    checked += 1
        self.assertGreater(checked, 20)

    def test_visible_rectangle_sides_use_actual_rectangle_engine_vertices(self):
        found = 0
        for _, proposal, author, program, _ in self.cases:
            native_edges = {e["id"]: e for e in program["edges"]}
            for rung, step in enumerate(proposal["steps"]):
                for operation in step["operations"]:
                    if not operation["accepted"] or operation["construction"].get("engine") != "rectangle":
                        continue
                    prefix = f"r{rung}-{operation['id']}-rectangle-"
                    helper = [e for e in program["edges"] if e["id"].startswith(prefix)]
                    self.assertEqual(4, len(helper))
                    vertices = {s for e in helper for s in e["controls"]}
                    self.assertEqual(4, len(vertices))
                    actual_sides = [e for e in operation["afterEdges"] if e["primitive"]["name"] == "rectangle-side"]
                    self.assertEqual(3, len(actual_sides))
                    for edge in actual_sides:
                        self.assertLessEqual(set(author.controls(edge["id"])), vertices)
                    guides = set(program["rungs"][rung]["guides"])
                    self.assertLessEqual({e["id"] for e in helper}, guides)
                    self.assertFalse({e["id"] for e in helper} & set(program["rungs"][rung]["visible"]))
                    found += 1
        self.assertGreater(found, 0)

    def test_triangle_outputs_share_the_actual_engine_vertices_including_sloping_base(self):
        sloped = 0
        for _, proposal, author, program, _ in self.cases:
            points = replay(program)
            for rung, step in enumerate(proposal["steps"]):
                for operation in step["operations"]:
                    if not operation["accepted"] or operation["construction"].get("engine") != "triangle":
                        continue
                    prefix = f"r{rung}-{operation['id']}-triangle-"
                    helper = [e for e in program["edges"] if e["id"].startswith(prefix)]
                    self.assertEqual(3, len(helper))
                    vertices = {s for e in helper for s in e["controls"]}
                    self.assertEqual(3, len(vertices))
                    for edge in operation["afterEdges"]:
                        if edge["primitive"]["name"] != "triangle-side":
                            continue
                        self.assertLessEqual(set(author.controls(edge["id"])), vertices)
                    wanted = {tuple(round(v) for v in p) for p in operation["construction"]["vertices"]}
                    self.assertEqual(wanted, {points[v] for v in vertices})
                    if operation["construction"].get("closed"):
                        ys = sorted(p[1] for p in wanted)
                        sloped += ys[-1] != ys[-2]
        self.assertGreater(sloped, 0)

    def test_roof_relaxation_reads_old_apex_and_actual_base_midpoint(self):
        checked = 0
        for _, proposal, author, program, _ in self.cases:
            points = replay(program)
            for step in proposal["steps"]:
                for operation in step["operations"]:
                    if not operation["accepted"] or operation["kind"] != "balance-local-roof":
                        continue
                    first, second = operation["sourceEdgeIds"][:2]
                    left, old_peak, right = author.controls(first)[0], author.controls(first)[-1], author.controls(second)[-1]
                    peak = author.controls(operation["targetEdgeIds"][0])[-1]
                    expected = points[old_peak][0]+operation["construction"]["relaxation"]*((points[left][0]+points[right][0])/2-points[old_peak][0])
                    self.assertLessEqual(abs(points[peak][0]-expected), 1)
                    moved = replay(program, {right: (points[right][0]+80, points[right][1])})
                    self.assertGreater(moved[peak][0], points[peak][0])
                    checked += 1
        self.assertGreater(checked, 0)

    def test_centered_cross_arms_consume_one_actual_center_site(self):
        count = 0
        for _, proposal, author, program, _ in self.cases:
            sites = {s["id"]: s for s in program["sites"]}
            points = replay(program)
            for rung, step in enumerate(proposal["steps"]):
                for operation in step["operations"]:
                    if not operation["accepted"] or operation["kind"] != "center-observed-cross-arms":
                        continue
                    center = f"r{rung}-{operation['id']}-shared-center"
                    self.assertIn(center, sites)
                    outputs = [site for eid in operation["targetEdgeIds"][:2] for site in author.controls(eid)]
                    for output in outputs:
                        self.assertEqual(center, sites[output]["x"].get("origin"))
                        self.assertEqual(center, sites[output]["y"].get("origin"))
                    moved = replay(program, {center: (points[center][0]+20, points[center][1]-30)})
                    for output in outputs:
                        self.assertEqual((points[output][0]+20, points[output][1]-30), moved[output])
                    count += 1
        self.assertEqual(6, count)

    def test_affine_reuse_consumes_actual_template_control_for_each_output_control(self):
        count = 0
        for _, proposal, author, program, _ in self.cases:
            sites = {s["id"]: s for s in program["sites"]}
            points = replay(program)
            for step in proposal["steps"]:
                for operation in step["operations"]:
                    if not operation["accepted"] or operation["kind"] != "reuse-affine-profile":
                        continue
                    target = next(e for e in operation["afterEdges"] if e.get("transform"))
                    transform = target["transform"]
                    template = author.controls(transform["sourceEdgeId"])
                    if transform["reversed"]:
                        template = list(reversed(template))
                    output = author.controls(target["id"])
                    for a, b in zip(template, output):
                        self.assertEqual(a, sites[b]["x"]["to"])
                        self.assertEqual(a, sites[b]["y"]["to"])
                        moved = replay(program, {a: (points[a][0]+100, points[a][1])})
                        self.assertNotEqual(points[b], moved[b])
                    count += 1
        self.assertGreater(count, 0)

    def test_unmatched_source_and_active_faint_residue_are_distinct_reads(self):
        residue_found = 0
        for observation, _, _, program, _ in self.cases:
            source = {s["id"] for s in observation["spans"]}
            for mapping in program["sourceMapping"]["passes"]:
                linked = {sid for link in mapping["links"] for sid in link["spanIds"]}
                unmatched = set(mapping["unmatchedSpanIds"])
                self.assertEqual(source, linked | unmatched)
                self.assertFalse(linked & unmatched)
                residue = set(mapping["residueEdgeIds"])
                rung = program["rungs"][mapping["rung"]]
                self.assertLessEqual(residue, set(rung["guides"]))
                self.assertFalse(residue & set(rung["visible"]))
                self.assertLessEqual(residue, {eid for link in mapping["links"] for eid in link["edgeIds"]})
                residue_found += len(residue)
        self.assertGreater(residue_found, 0)


if __name__ == "__main__":
    unittest.main()
