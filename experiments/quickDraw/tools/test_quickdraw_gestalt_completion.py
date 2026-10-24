"""Interpretation history, real joins and negative view reads."""
import copy
import gzip
import json
from pathlib import Path
import unittest

from quickdraw_gestalt_completion import extend, evaluate, distance
from quickdraw_gestalt_geometry import build, axis_read
from quickdraw_structure_observations import observe_record


class CompletionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        path = Path(__file__).resolve().parents[1]/"results"/"quickdraw-construction"/"observations.ndjson.gz"
        with gzip.open(path, "rt", encoding="utf-8") as handle:
            cls.observations = [json.loads(line) for line in handle if line.strip()]
        cls.base = [build(o) for o in cls.observations]
        cls.outputs = [extend(o, p) for o, p in zip(cls.observations, cls.base)]

    def test_preserves_input_and_entire_four_step_prefix(self):
        observation, proposal = copy.deepcopy(self.observations[0]), copy.deepcopy(self.base[0])
        before = copy.deepcopy((observation, proposal))
        output = extend(observation, proposal)
        self.assertEqual(before, (observation, proposal))
        self.assertEqual(proposal["steps"], output["steps"][:4])
        self.assertEqual(7, len(output["steps"]))

    def test_all_original_intervals_remain_exactly_once_in_main_or_complement(self):
        for observation, output in zip(self.observations, self.outputs):
            expected = {s["id"] for s in observation["spans"]}
            for step in output["steps"][4:]:
                support = [sid for edge in step["edges"]+step["residueEdges"] for sid in edge["spans"]]
                self.assertEqual(expected, set(support))
                self.assertEqual(len(expected), len(support))

    def test_replay_and_actual_no_forward_control_correspondences(self):
        for output in self.outputs:
            active = {e["id"]: e for e in output["steps"][3]["edges"]}
            residue = {e["id"]: e for e in output["steps"][3]["residueEdges"]}
            known = dict(active, **residue)
            for step in output["steps"][4:]:
                for op in step["operations"]:
                    for edge in op["beforeEdges"]:
                        self.assertIn(edge["id"], active)
                        self.assertEqual(edge["controls"], active[edge["id"]]["controls"])
                    if not op["accepted"]:
                        continue
                    if op["kind"].startswith("carry-"):
                        for edge in op["beforeEdges"]:
                            residue[edge["id"]] = active.pop(edge["id"])
                        continue
                    bindings = {row["targetSiteKey"]: row for row in op["construction"]["endpointBindings"]}
                    for edge in op["afterEdges"]:
                        for key, point in zip(edge["siteKeys"], edge["controls"]):
                            self.assertIn(key, bindings)
                            binding = bindings[key]
                            for axis, field in enumerate(("xInputs", "yInputs")):
                                refs = binding[field]
                                self.assertAlmostEqual(1.0, sum(r["weight"] for r in refs))
                                self.assertTrue(all(r["edgeId"] in known for r in refs))
                                self.assertAlmostEqual(point[axis], evaluate(refs, known, axis), places=7)
                    for edge in op["beforeEdges"]:
                        active.pop(edge["id"])
                    active.update((e["id"], e) for e in op["afterEdges"])
                    known.update((e["id"], e) for e in op["afterEdges"])
                self.assertEqual(set(active), {e["id"] for e in step["edges"]})
                self.assertEqual(set(residue), {e["id"] for e in step["residueEdges"]})

    def test_shared_target_keys_have_one_position_across_all_incident_profiles(self):
        for output in self.outputs:
            for step in output["steps"][4:]:
                positions = {}
                for edge in step["edges"]+step["residueEdges"]:
                    for key, point in zip(edge["siteKeys"], edge["controls"]):
                        if key in positions:
                            self.assertLess(distance(point, positions[key]), 1e-7)
                        positions[key] = point

    def test_first_church_roof_wall_and_existing_base_share_real_target_keys(self):
        step = self.outputs[0]["steps"][-1]
        roof = [e for e in step["edges"] if e["role"] == "roof-region"]
        walls = [e for e in step["edges"] if e["role"] == "wall-region"]
        sites = {key: p for e in walls for key, p in zip(e["siteKeys"], e["controls"])}
        self.assertIn(roof[0]["siteKeys"][0], sites)
        self.assertIn(roof[1]["siteKeys"][-1], sites)
        for edge in walls:
            a, b = edge["controls"]
            self.assertTrue(abs(a[0]-b[0]) < 1e-8 or abs(a[1]-b[1]) < 1e-8)
        self.assertAlmostEqual(step["metrics"]["facadeAxisX"], roof[0]["controls"][-1][0])

    def test_two_supported_side_views_have_actual_parallelogram_outputs(self):
        views = [o["steps"][-1]["viewHypothesis"]["kind"] for o in self.outputs]
        self.assertEqual(["front", "oblique", "oblique", "front", "front"], views)
        for output in self.outputs:
            final = output["steps"][-1]
            side_ops = [op for op in final["operations"] if op["kind"] == "synthesize-supported-side-parallelogram"]
            if final["viewHypothesis"]["kind"] == "front":
                self.assertFalse(side_ops)
                continue
            self.assertEqual(1, len(side_ops))
            op = side_ops[0]
            positions = {key: p for e in op["afterEdges"] for key, p in zip(e["siteKeys"], e["controls"])}
            frame = op["construction"]["parallelograms"][0]
            a, b, c, d = [positions[frame[k]] for k in ("aSiteKey", "bSiteKey", "cSiteKey", "dSiteKey")]
            for axis in range(2):
                self.assertAlmostEqual(d[axis], b[axis]+c[axis]-a[axis])
            self.assertGreater((b[0]-a[0])*op["construction"]["depthSign"], 0)

    def test_later_steps_accept_worse_original_fit_and_keep_objectives_separate(self):
        self.assertGreater(self.outputs[3]["steps"][-1]["metrics"]["rms"], self.outputs[3]["steps"][3]["metrics"]["rms"]+10)
        for output in self.outputs:
            weights = [s["policyWeights"] for s in output["steps"][4:]]
            self.assertGreater(weights[0]["originalInk"], weights[2]["originalInk"])
            self.assertLess(weights[0]["geometricPrior"], weights[2]["geometricPrior"])
            for step in output["steps"][4:]:
                for op in step["operations"]:
                    if not op["kind"].startswith("carry-"):
                        self.assertFalse(op["thresholds"]["rawDistanceIsAcceptanceGate"])
                        self.assertIn("previousPass", op["measurements"])

    def test_side_plane_consumes_the_lower_outer_wall_instead_of_a_roof_slope(self):
        final = self.outputs[1]["steps"][-1]
        op = next(o for o in final["operations"] if o["kind"] == "synthesize-supported-side-parallelogram")
        # The actual lower wall interval is replaced, while the angular rear
        # roof remains separately supported rather than posing as this wall.
        wall_span = "stroke-005-span-000"
        self.assertTrue(any(wall_span in e["spans"] for e in op["beforeEdges"]))
        self.assertTrue(any(wall_span in e["spans"] for e in op["afterEdges"]))
        front_left = min(p[0] for e in final["edges"] if e["role"] == "wall-region" for p in e["controls"])
        exterior_uprights = [e for e in final["edges"] if len(e["controls"]) == 2
                              and max(p[0] for p in e["controls"]) < front_left-35
                              and axis_read(e["controls"])[0] == "vertical" and axis_read(e["controls"])[1] < 10
                              and abs(e["controls"][-1][1]-e["controls"][0][1]) > 80]
        self.assertEqual(1, len(exterior_uprights))
        self.assertEqual("side-plane-region", exterior_uprights[0]["role"])

    def test_no_cross_or_door_is_invented_and_three_towers_remain_three(self):
        for base, output in zip(self.base, self.outputs):
            for role in ("cross-region", "door-region"):
                self.assertEqual(sum(e["role"] == role for e in base["steps"][-1]["edges"]), sum(e["role"] == role for e in output["steps"][-1]["edges"]))
        self.assertFalse(any(e["role"] == "cross-region" for e in self.outputs[2]["steps"][-1]["edges"]))
        contour = [e for e in self.outputs[3]["steps"][-1]["edges"] if any(r["strokeIndex"] == 0 for r in e["sourceRanges"])]
        peaks = [a["controls"][-1] for a, b in zip(contour, contour[1:]) if a["controls"][-1][1] < a["controls"][0][1]-30 and a["controls"][-1][1] < b["controls"][-1][1]-30]
        self.assertEqual(3, len(peaks))

    def test_source_id_is_not_a_template_dispatch_key(self):
        observation = copy.deepcopy(self.observations[0])
        proposal = copy.deepcopy(self.base[0])
        observation["sourceId"] = proposal["sourceId"] = "unseen-source"
        result = extend(observation, proposal)
        self.assertEqual(self.outputs[0]["steps"][4:], result["steps"][4:])

    def test_empty_input_remains_open_without_an_invented_church(self):
        observation = observe_record({"key_id": "empty", "drawing": []})
        output = extend(observation, build(observation))
        self.assertEqual(7, len(output["steps"]))
        self.assertTrue(all(not s["edges"] and not s["operations"] for s in output["steps"]))
        self.assertEqual("unresolved", output["steps"][-1]["viewHypothesis"]["kind"])


if __name__ == "__main__":
    unittest.main()
