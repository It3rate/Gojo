"""Local proposal history, real source coverage and negative simplification reads."""
import copy
import gzip
import json
from pathlib import Path
import unittest

from quickdraw_gestalt_geometry import build, _Builder, distance, length
from quickdraw_structure_observations import observe_record


def observe(*strokes):
    return observe_record({"key_id": "fixture", "drawing": [[[p[0] for p in stroke], [p[1] for p in stroke]] for stroke in strokes]})


class QuickDrawGestaltGeometryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        path = Path(__file__).resolve().parents[1]/"results"/"quickdraw-construction"/"observations.ndjson.gz"
        with gzip.open(path, "rt", encoding="utf-8") as handle:
            cls.observations = [json.loads(line) for line in handle if line.strip()]
        cls.before = copy.deepcopy(cls.observations)
        cls.proposals = [build(o) for o in cls.observations]

    def test_all_five_have_actual_simplification_without_rewriting_source(self):
        self.assertEqual(self.before, self.observations)
        self.assertEqual(5, len(self.proposals))
        for proposal in self.proposals:
            self.assertEqual(4, len(proposal["steps"]))
            initial, final = proposal["steps"][0], proposal["steps"][-1]
            self.assertLess(final["metrics"]["edgeCount"], initial["metrics"]["edgeCount"])
            self.assertFalse(proposal["policy"]["learned"])
            self.assertFalse(proposal["convergence"]["globalOptimumClaimed"])

    def test_every_source_interval_is_active_or_explicit_complement_exactly_once(self):
        for observation, proposal in zip(self.observations, self.proposals):
            source = {s["id"] for s in observation["spans"]}
            for step in proposal["steps"]:
                support = [s for edge in step["edges"]+step["residueEdges"] for s in edge["spans"]]
                self.assertEqual(source, set(support))
                self.assertEqual(len(source), len(support))
                self.assertEqual(source, set(step["residue"]["sourceSpanIds"]))

    def test_new_edges_resolve_to_previous_rung_and_shared_sites_have_one_position(self):
        for proposal in self.proposals:
            prior = set()
            for i, step in enumerate(proposal["steps"]):
                positions = {}
                edges = step["edges"]+step["residueEdges"]
                for edge in edges:
                    self.assertEqual(len(edge["controls"]), len(edge["siteKeys"]))
                    if i and edge["id"] not in prior:
                        self.assertTrue(edge["parents"])
                        self.assertLessEqual(set(edge["parents"]), prior)
                    for key, p in zip(edge["siteKeys"], edge["controls"]):
                        if key in positions:
                            self.assertLess(distance(p, positions[key]), 1e-8)
                        positions[key] = p
                prior = {edge["id"] for edge in edges}

    def test_accepted_geometry_patches_reduce_declared_local_objective_and_bound_new_ink(self):
        rejected = 0
        for proposal in self.proposals:
            for step in proposal["steps"]:
                for operation in step["operations"]:
                    if not operation["accepted"]:
                        rejected += 1
                        self.assertTrue(operation["reason"])
                        continue
                    if operation["kind"] == "carry-detail-complement":
                        self.assertTrue(operation["measurements"]["retainedExactly"])
                        continue
                    self.assertLess(operation["scoreAfter"], operation["scoreBefore"])
                    self.assertLessEqual(operation["measurements"]["after"]["max"], operation["thresholds"]["maximumSymmetricDistance"])
                    self.assertIn("proposalRms", operation["measurements"]["after"])
        self.assertGreater(rejected, 0)

    def test_patch_timeline_replays_without_unreported_replacements(self):
        for proposal in self.proposals:
            active = {e["id"]: e for e in proposal["steps"][0]["edges"]}
            complement = {}
            for step in proposal["steps"][1:]:
                for operation in step["operations"]:
                    for edge in operation["beforeEdges"]:
                        self.assertIn(edge["id"], active)
                        self.assertEqual(edge["controls"], active[edge["id"]]["controls"])
                    if not operation["accepted"]:
                        continue
                    if operation["kind"] == "carry-detail-complement":
                        for eid in operation["sourceEdgeIds"]:
                            complement[eid] = active.pop(eid)
                    else:
                        for eid in operation["sourceEdgeIds"]:
                            active.pop(eid)
                        active.update((e["id"], e) for e in operation["afterEdges"])
                self.assertEqual(set(active), {e["id"] for e in step["edges"]})
                self.assertEqual(set(complement), {e["id"] for e in step["residueEdges"]})

    def test_crosses_require_existing_two_arm_evidence_and_clock_stays_cross_free(self):
        total = 0
        for observation, proposal in zip(self.observations, self.proposals):
            operations = [o for s in proposal["steps"] for o in s["operations"] if o["accepted"] and o["kind"] == "center-observed-cross-arms"]
            self.assertLessEqual(len(operations), len(observation["crosses"]))
            total += len(operations)
            if not observation["crosses"]:
                self.assertFalse(operations)
        self.assertEqual(6, total)

    def test_transformed_profile_controls_are_actual_template_correspondences(self):
        found = 0
        for proposal in self.proposals:
            for i, step in enumerate(proposal["steps"][1:], start=1):
                prior = {e["id"]: e for e in proposal["steps"][i-1]["edges"]}
                for edge in step["edges"]:
                    transform = edge.get("transform")
                    if not transform:
                        continue
                    source = prior[transform["sourceEdgeId"]]
                    a, b, tx, c, d, ty = transform["matrix"]
                    expected = [[a*p[0]+b*p[1]+tx, c*p[0]+d*p[1]+ty] for p in source["controls"]]
                    if transform["reversed"]:
                        expected.reverse()
                    self.assertEqual(len(expected), len(edge["controls"]))
                    self.assertLess(max(distance(p, q) for p, q in zip(expected, edge["controls"])), 1e-8)
                    found += 1
        self.assertGreater(found, 0)

    def test_missing_rectangle_side_stays_a_guide_and_any_triangle_closure_is_declared(self):
        rectangles = closures = 0
        for proposal in self.proposals:
            for operation in [o for s in proposal["steps"] for o in s["operations"] if o["accepted"]]:
                if operation["kind"] == "three-sided-rectangle":
                    self.assertFalse(operation["construction"]["closed"])
                    self.assertTrue(operation["construction"]["missingSideIsGuideOnly"])
                    rectangles += 1
                if operation["kind"] == "near-closed-roof-triangle":
                    self.assertTrue(operation["construction"]["inferredClosure"])
                    sites = operation["construction"]["inferredJoin"]["sourceSiteKeys"]
                    self.assertNotEqual(sites[0], sites[1])
                    closures += 1
        self.assertGreater(rectangles, 0)
        self.assertGreater(closures, 0)

    def test_finite_measure_deduplicates_acquired_events_not_equal_coordinates(self):
        observation = observe([(0, 0), (10, 0), (0, 0), (10, 0)])
        builder = _Builder(observation)
        measured = builder.measure(builder.edges+builder.edges, builder.edges)
        self.assertEqual(4, measured["sourceSampleCount"])
        self.assertEqual(3, measured["sourceSegmentCount"])
        self.assertAlmostEqual(length(observation["strokes"][0]["points"]), measured["sourceTravel"])

    def test_reverse_distance_prevents_free_unobserved_ink(self):
        builder = _Builder(observe([(0, 0), (100, 0)]))
        edge = builder.edges[0]
        extra = builder.new_edge([[32, 400], [480, 400]], [edge])
        measured = builder.measure([edge], [edge, extra])
        self.assertAlmostEqual(0, measured["sourceMax"])
        self.assertGreater(measured["proposalMax"], 100)
        self.assertFalse(builder.patch("unsupported-extra-ink", [edge], [edge, extra], prior_before=100000))
        self.assertEqual([edge], builder.edges)

    def test_unchanged_shared_endpoint_reintroduces_incident_profile_but_not_equal_pen_lift(self):
        observation = observe([(0, 0), (50, 0), (50, 50)], [(50, 0), (90, 0)])
        builder = _Builder(observation)
        selected = builder.edges[0]
        incident = builder.edges[1]
        pen_lift = builder.edges[2]
        replacement = builder.new_edge(selected["controls"], [selected])
        self.assertTrue(builder.patch("explicit-shared-primitive", [selected], [replacement], prior_before=50))
        operation = builder.operations[-1]
        self.assertIn(incident["id"], operation["sourceEdgeIds"])
        self.assertNotIn(pen_lift["id"], operation["sourceEdgeIds"])
        carried = next(e for e in operation["afterEdges"] if e["spans"] == incident["spans"])
        self.assertNotEqual(incident["id"], carried["id"])
        self.assertEqual(incident["controls"], carried["controls"])
        self.assertEqual(replacement["endpointKeys"][-1], carried["endpointKeys"][0])

    def test_empty_source_does_not_manufacture_a_church(self):
        proposal = build(observe([]))
        self.assertEqual(4, len(proposal["steps"]))
        self.assertTrue(all(not step["edges"] and not step["operations"] for step in proposal["steps"]))


if __name__ == "__main__":
    unittest.main()
