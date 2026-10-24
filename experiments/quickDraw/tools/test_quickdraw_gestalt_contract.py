"""Adversarial contracts for the independent simplification audit."""
from copy import deepcopy
import unittest

from verify_quickdraw_gestalt import audit_program, audit_proposal, audit_shared_sites, audit_affine_controls, measure, source_selection


def fixture():
    observation = {"sourceId": "fixture", "sourceDrawingSha256": "source-hash",
        "spans": [{"id": "s0", "strokeIndex": 0, "startIndex": 0, "endIndex": 1, "controls": [[0, 0], [10, 0]]},
                  {"id": "s1", "strokeIndex": 1, "startIndex": 0, "endIndex": 1, "controls": [[12, 0], [20, 0]]}],
        "strokes": [{"index": 0, "points": [[0, 0], [10, 0]]}, {"index": 1, "points": [[12, 0], [20, 0]]}], "crosses": []}
    def edge(key, controls, ids, ends):
        return {"id": key, "controls": controls, "spans": ids,
                "sourceRanges": [{"spanId": span["id"], **{field: span[field] for field in ("strokeIndex", "startIndex", "endIndex")}}
                                 for span in observation["spans"] if span["id"] in ids],
                "endpointKeys": ends, "parents": [], "primitive": {"name": "line"}}
    before = [edge("e0", [[0, 0], [10, 0]], ["s0"], ["a", "b"]),
              edge("e1", [[12, 0], [20, 0]], ["s1"], ["c", "d"])]
    after = [edge("merged", [[0, 0], [20, 0]], ["s0", "s1"], ["a", "d"])]
    policy = {"learned": False, "profileSubdivisions": 24, "maximumPasses": 3, "edgeCost": 7, "controlCost": 3, "errorWeight": .45}
    mb, ma = measure(observation, ["s0", "s1"], before), measure(observation, ["s0", "s1"], after)
    operation = {"id": "merge", "kind": "gap-approximation", "accepted": True, "sourceEdgeIds": ["e0", "e1"], "targetEdgeIds": ["merged"],
                 "sourceSpans": ["s0", "s1"], "beforeEdges": deepcopy(before), "afterEdges": deepcopy(after),
                 "measurements": {"before": mb, "after": ma, "complexityBefore": 26, "complexityAfter": 13,
                                  "expectationBefore": 0, "expectationAfter": 0},
                 "scoreBefore": .45*mb["rms"]**2+26, "scoreAfter": .45*ma["rms"]**2+13,
                 "thresholds": {"maximumSymmetricDistance": 4}, "reason": "Explicit local complexity gain, acquired gap retained.", "construction": {}}
    def step(key, edges, operations):
        metrics = measure(observation, ["s0", "s1"], edges)
        return {"id": key, "edges": deepcopy(edges), "residueEdges": [], "operations": operations,
                "metrics": metrics | {"edgeCount": len(edges), "residueEdgeCount": 0, "controlCount": sum(len(e["controls"]) for e in edges)},
                "sourceCoverage": {"active": ["s0", "s1"], "complement": []}, "residue": {"sourceSpanIds": ["s0", "s1"]},
                "convergence": {"accepted": len(operations), "rejected": 0}}
    proposal = {"sourceId": "fixture", "sourceDrawingSha256": "source-hash", "policy": policy,
                "steps": [step("observed", before, []), step("simplified", after, [operation])],
                "convergence": {"accepted": 1, "globalOptimumClaimed": False}}
    def coordinate(origin, numerator=0):
        return {"origin": origin, "from": "a", "to": "d", "numerator": numerator, "denominator": 1}
    program = {"id": "fixture-program", "parameters": [],
        "sites": [{"id": key, "at": 0, "x": x, "y": y} for key, x, y in (("page", 0, 0), ("extent", 100, 100), ("a", 0, 0), ("b", 10, 0), ("c", 12, 0), ("d", 20, 0))]
                 + [{"id": "m0", "at": 1, "x": coordinate("a"), "y": coordinate("a")},
                    {"id": "m1", "at": 1, "x": coordinate("a", 1), "y": coordinate("a", 1)}],
        "edges": [{"id": "envelope", "at": 0, "controls": ["page", "extent"]},
                  {"id": "g-e0", "at": 0, "controls": ["a", "b"]}, {"id": "g-e1", "at": 0, "controls": ["c", "d"]},
                  {"id": "g-merged", "at": 1, "controls": ["m0", "m1"]}],
        "rungs": [{"visible": ["g-e0", "g-e1"], "guides": ["envelope"], "selected": [], "introduced": ["envelope", "g-e0", "g-e1"]},
                  {"visible": ["g-merged"], "guides": ["envelope", "g-e0", "g-e1"], "selected": ["g-e0", "g-e1"], "introduced": ["g-merged"]}],
        "sourceMapping": {"sourceId": "fixture", "envelopeSiteIds": ["page", "extent"], "passes": [
            {"rung": 0, "links": [{"spanIds": ["s0"], "edgeIds": ["g-e0"]}, {"spanIds": ["s1"], "edgeIds": ["g-e1"]}], "unmatchedSpanIds": [], "inferredEdgeIds": [], "residueEdgeIds": []},
            {"rung": 1, "links": [{"spanIds": ["s0", "s1"], "edgeIds": ["g-merged"]}], "unmatchedSpanIds": [], "inferredEdgeIds": [], "residueEdgeIds": []}]}}
    return observation, proposal, program


class GestaltContracts(unittest.TestCase):
    def test_valid_merge_is_observation_with_nonzero_gap_residue(self):
        observation, proposal, program = fixture()
        self.assertGreater(proposal["steps"][1]["metrics"]["proposalMax"], 0)
        self.assertEqual(1, audit_proposal(observation, proposal)["acceptedPatches"])
        self.assertEqual(0, audit_program(program, observation, proposal)["maximumProposalControlDisplacement"])
        self.assertEqual([[12, 0], [20, 0]], observation["strokes"][1]["points"])

    def test_repeated_source_support_does_not_multiply_acquisition_weight(self):
        observation, proposal, _ = fixture()
        edges = proposal["steps"][1]["edges"]
        once = measure(observation, ["s0", "s1"], edges)
        repeated = measure(observation, ["s0", "s1", "s0", "s1"], edges)
        self.assertEqual(once, repeated)
        self.assertEqual(4, once["sourceSampleCount"])
        self.assertEqual(18, once["sourceTravel"])

    def test_equal_coordinates_remain_distinct_acquired_events(self):
        observation, _, _ = fixture()
        observation["strokes"][1]["points"][0] = [10, 0]
        points, segments = source_selection(observation, ["s0", "s1"])
        self.assertEqual(4, len(points))
        self.assertEqual(2, points.count([10, 0]))
        self.assertEqual(2, len(segments))

    def test_forged_metric_and_objective_are_rejected(self):
        for field in ("metric", "score"):
            with self.subTest(field=field):
                observation, proposal, _ = fixture()
                operation = proposal["steps"][1]["operations"][0]
                if field == "metric":
                    operation["measurements"]["after"]["proposalMax"] = 0
                else:
                    operation["scoreAfter"] += 1
                with self.assertRaises(AssertionError):
                    audit_proposal(observation, proposal)

    def test_acceptance_cannot_bypass_its_distance_allowance(self):
        observation, proposal, _ = fixture()
        proposal["steps"][1]["operations"][0]["thresholds"]["maximumSymmetricDistance"] = 0
        with self.assertRaisesRegex(AssertionError, "allowance"):
            audit_proposal(observation, proposal)

    def test_worse_raw_fit_is_valid_under_a_declared_conceptual_weight(self):
        observation, proposal, _ = fixture()
        operation = proposal["steps"][1]["operations"][0]
        self.assertGreater(operation["measurements"]["after"]["rms"], operation["measurements"]["before"]["rms"])
        operation["thresholds"]["errorWeight"] = .04
        for suffix in ("Before", "After"):
            operation["score"+suffix] = .04*operation["measurements"][suffix.lower()]["rms"]**2+operation["measurements"]["complexity"+suffix]
        self.assertEqual(1, audit_proposal(observation, proposal)["acceptedPatches"])

    def test_rejected_candidate_is_retained_without_applying_it(self):
        observation, proposal, _ = fixture()
        final, baseline = proposal["steps"][1], proposal["steps"][0]
        final["operations"][0]["accepted"] = False
        final["edges"] = deepcopy(baseline["edges"])
        final["metrics"] = deepcopy(baseline["metrics"])
        final["convergence"] = {"accepted": 0, "rejected": 1}
        proposal["convergence"]["accepted"] = 0
        self.assertEqual(1, audit_proposal(observation, proposal)["rejectedPatches"])
        final["edges"] = deepcopy(final["operations"][0]["afterEdges"])
        with self.assertRaisesRegex(AssertionError, "accepted local patches"):
            audit_proposal(observation, proposal)

    def test_changed_interval_or_dropped_source_residue_is_rejected(self):
        for field in ("interval", "residue"):
            observation, proposal, _ = fixture()
            if field == "interval":
                proposal["steps"][1]["edges"][0]["sourceRanges"][0]["startIndex"] = 1
            else:
                proposal["steps"][1]["residue"]["sourceSpanIds"] = ["s0"]
            with self.subTest(field=field), self.assertRaises(AssertionError):
                audit_proposal(observation, proposal)

    def test_matched_and_unmatched_cannot_claim_the_same_source(self):
        observation, _, program = fixture()
        program["sourceMapping"]["passes"][1]["unmatchedSpanIds"] = ["s1"]
        with self.assertRaisesRegex(AssertionError, "source accounting"):
            audit_program(program, observation)

    def test_historical_guide_is_not_an_active_target(self):
        observation, _, program = fixture()
        program["sourceMapping"]["passes"][1]["links"][0]["edgeIds"] = ["g-e0"]
        with self.assertRaisesRegex(AssertionError, "inactive"):
            audit_program(program, observation)

    def test_active_residue_guide_has_explicit_source_support(self):
        observation, _, program = fixture()
        final = program["sourceMapping"]["passes"][1]
        final["links"] = [{"spanIds": ["s0"], "edgeIds": ["g-merged"]}, {"spanIds": ["s1"], "edgeIds": ["g-e1"]}]
        final["residueEdgeIds"] = ["g-e1"]
        self.assertEqual(2, audit_program(program, observation)["mapping"][1]["activeTargets"])

    def test_new_literal_or_forward_reference_cannot_hide_a_source_oracle(self):
        for field in ("literal", "forward"):
            observation, _, program = fixture()
            if field == "literal":
                program["sites"][-2]["x"] = 0
            else:
                program["sites"][-2]["x"]["origin"] = "m1"
            with self.subTest(field=field), self.assertRaises(AssertionError):
                audit_program(program, observation)

    def test_inactive_future_geometry_and_lost_guides_are_rejected(self):
        for field in ("future", "guide"):
            observation, _, program = fixture()
            if field == "future":
                program["edges"][1]["at"] = 1
            else:
                program["rungs"][1]["guides"].remove("g-e0")
            with self.subTest(field=field), self.assertRaises(AssertionError):
                audit_program(program, observation)

    def test_declared_finite_import_tolerance_is_checked(self):
        observation, proposal, program = fixture()
        program["sites"][-1]["x"]["numerator"] = 2
        with self.assertRaisesRegex(AssertionError, "two-unit"):
            audit_program(program, observation, proposal)

    def test_shared_raw_endpoint_needs_one_actual_native_site(self):
        raw = [{"id": "left", "controls": [[0, 0], [10, 0]], "endpointKeys": ["a", "joint"]},
               {"id": "right", "controls": [[10, 0], [10, 10]], "endpointKeys": ["joint", "b"]}]
        native = {"g-left": {"controls": ["left-a", "left-joint"]}, "g-right": {"controls": ["right-joint", "right-b"]}}
        with self.assertRaisesRegex(AssertionError, "distinct native"):
            audit_shared_sites(raw, native)
        native["g-right"]["controls"][0] = "left-joint"
        audit_shared_sites(raw, native)

    def test_affine_outputs_cannot_borrow_an_unrelated_equal_endpoint(self):
        raw = {"id": "output", "transform": {"sourceEdgeIds": ["template"], "reversed": True}}
        native = {"g-template": {"controls": ["t0", "t1", "t2"]}, "g-output": {"controls": ["o0", "o1", "o2"]}}
        ancestors = {"o0": {"t2"}, "o1": {"t1"}, "o2": {"unrelated-equal-endpoint"}}
        with self.assertRaisesRegex(AssertionError, "corresponding template"):
            audit_affine_controls(raw, native, ancestors)
        ancestors["o2"] = {"t0"}
        audit_affine_controls(raw, native, ancestors)


if __name__ == "__main__":
    unittest.main()
