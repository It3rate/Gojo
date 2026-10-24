"""Independent false-read cases for the observed construction boundary."""
import copy
import unittest

from quickdraw_structure_observations import observe_record
from verify_quickdraw_interpretation import (
    audit_geometric_candidates, audit_observations, audit_source_construction, evaluate_program,
)


def acquisition_fixture():
    record = {"drawing": [
        [[2, 4, 4, 8], [3, 5, 5, 3], [40, 30, 20, 10]],
        [[], [], []],
        [[8], [3], [1]],
    ]}
    observation = {"strokes": [
        {"index": 0, "sourcePoints": [[2, 3], [4, 5], [4, 5], [8, 3]],
         "times": [40, 30, 20, 10], "spans": ["a", "b"]},
        {"index": 1, "sourcePoints": [], "times": [], "spans": []},
        {"index": 2, "sourcePoints": [[8, 3]], "times": [1], "spans": ["c"]},
    ], "spans": [
        {"id": "a", "strokeIndex": 0, "startIndex": 0, "endIndex": 2},
        {"id": "b", "strokeIndex": 0, "startIndex": 2, "endIndex": 3},
        {"id": "c", "strokeIndex": 2, "startIndex": 0, "endIndex": 0},
    ]}
    return record, observation


class ObservationContractTests(unittest.TestCase):
    def test_acquired_duplicates_backwards_times_and_empty_strokes_remain_evidence(self):
        result = audit_observations(*acquisition_fixture())
        self.assertEqual(result["sourceStrokeCount"], 3)
        self.assertEqual(result["sourcePointCount"], 5)
        self.assertEqual(result["consecutiveDuplicateSamples"], 1)
        self.assertEqual(result["backwardsTimeSteps"], 3)

    def test_dropped_samples_and_repeated_partition_intervals_are_rejected(self):
        for start in (1, 3):
            record, observation = acquisition_fixture()
            observation["spans"][1]["startIndex"] = start
            with self.subTest(start=start), self.assertRaises(AssertionError):
                audit_observations(record, observation)

    def test_deduplicated_acquisition_points_do_not_pass_as_the_original_trace(self):
        record, observation = acquisition_fixture()
        observation["strokes"][0]["sourcePoints"].pop(2)
        with self.assertRaisesRegex(AssertionError, "sample events"):
            audit_observations(record, observation)

    def test_timestamp_sorting_cannot_replace_recorded_draw_order(self):
        record, observation = acquisition_fixture()
        observation["strokes"][0]["times"].sort()
        with self.assertRaisesRegex(AssertionError, "times"):
            audit_observations(record, observation)

    def test_span_membership_cannot_hide_reversed_acquisition_order(self):
        record, observation = acquisition_fixture()
        observation["spans"].reverse()
        for stroke in observation["strokes"]:
            stroke["spans"].reverse()
        with self.assertRaisesRegex(AssertionError, "stroke order"):
            audit_observations(record, observation)

    def test_pen_lift_singleton_cannot_be_erased_by_coordinate_equality(self):
        record, observation = acquisition_fixture()
        observation["strokes"].pop()
        observation["spans"].pop()
        with self.assertRaisesRegex(AssertionError, "stroke count"):
            audit_observations(record, observation)


class CandidateEvidenceContractTests(unittest.TestCase):
    @staticmethod
    def record():
        return {"drawing": [[[0, 0, 100, 100], [20, 100, 100, 20]],
                            [[40, 60], [10, 10]], [[50, 50], [0, 20]]]}

    def test_actual_cross_candidate_retains_both_observed_arms(self):
        record = self.record()
        observation = observe_record(record)
        audit_observations(record, observation)
        self.assertEqual(audit_geometric_candidates(record, observation)["crossCandidates"], 1)

    def test_named_cross_without_two_observed_arms_fails_the_audit(self):
        record = self.record()
        observation = observe_record(record)
        candidate = observation["crosses"][0]
        candidate["spans"][1] = candidate["spans"][0]
        with self.assertRaisesRegex(AssertionError, "two distinct"):
            audit_geometric_candidates(record, observation)

    def test_forged_cross_position_does_not_inherit_source_support(self):
        record = self.record()
        observation = observe_record(record)
        observation["crosses"][0]["point"][0] += 12
        with self.assertRaisesRegex(AssertionError, "unsupported"):
            audit_geometric_candidates(record, observation)

    def test_roughly_right_local_corners_leave_nonparallel_residual(self):
        record = {"drawing": [[[0, 0, 20, 22], [0, 10, 12, 22]]]}
        observation = observe_record(record)
        self.assertTrue(observation["unresolved"])
        audit_geometric_candidates(record, observation)
        observation["unresolved"][0]["outerParallelResidualDegrees"] = 0
        with self.assertRaisesRegex(AssertionError, "residual changed"):
            audit_geometric_candidates(record, observation)


class RelativeConstructionContractTests(unittest.TestCase):
    @staticmethod
    def program():
        return {"parameters": [{"id": "reach", "value": 3}], "sites": [
            {"id": "origin", "at": 0, "x": 0, "y": 0},
            {"id": "extent", "at": 0, "x": 100, "y": 80},
            {"id": "guide", "at": 0,
             "x": {"from": "origin", "to": "extent", "numerator": 1, "denominator": 4},
             "y": {"from": "origin", "to": "extent", "numerator": 1, "denominator": 2}},
            {"id": "detail", "at": 1,
             "x": {"origin": "guide", "from": "origin", "to": "extent", "numerator": "reach", "denominator": 16},
             "y": {"origin": "guide", "from": "origin", "to": "extent", "numerator": -1, "denominator": 16}},
        ]}

    def test_finite_signed_offsets_and_actual_earlier_anchor_are_replayed(self):
        values, dependencies = evaluate_program(self.program())
        self.assertEqual(values["detail"], [43, 35])
        self.assertIn("guide", dependencies["detail"])

    def test_upstream_guide_changes_move_its_detail(self):
        program = self.program()
        before = evaluate_program(program)[0]
        program["sites"][2]["x"]["numerator"] = 2
        after = evaluate_program(program)[0]
        self.assertEqual(after["detail"][0] - before["detail"][0], 25)

    def test_private_source_and_render_cache_cannot_supply_geometry(self):
        program = self.program()
        before = evaluate_program(program)
        changed = copy.deepcopy(program)
        changed["sourceMapping"] = {"sourcePoints": [[999999, -999999]], "expectedSites": {"detail": [1, 2]}}
        changed["privateAudit"] = {"controls": [[44, 55]]}
        self.assertEqual(evaluate_program(changed), before)

    def test_array_forward_and_future_rung_references_are_distinct_failures(self):
        program = self.program()
        program["sites"][2]["x"]["origin"] = "detail"
        with self.assertRaisesRegex(AssertionError, "forward"):
            evaluate_program(program)
        program = self.program()
        program["sites"][2]["at"] = 2
        with self.assertRaisesRegex(AssertionError, "Future-rung"):
            evaluate_program(program)


class AcquisitionToConstructionContractTests(unittest.TestCase):
    @staticmethod
    def fixture():
        observation = observe_record({"key_id": "fixture", "drawing": [
            [[0, 20], [0, 10]], [[20, 20], [10, 30]],
        ]})
        def expression(origin, amount):
            return {"origin": origin, "from": "page-a", "to": "page-b", "numerator": round(amount), "denominator": 512}
        sites = [{"id": "page-a", "at": 0, "x": 0, "y": 0},
                 {"id": "page-b", "at": 0, "x": 512, "y": 512},
                 {"id": "guide-a", "at": 0, "x": expression("page-a", 0), "y": expression("page-a", 0)},
                 {"id": "guide-b", "at": 0, "x": expression("page-a", 512), "y": expression("page-a", 512)}]
        edges = [{"id": "rough", "at": 0, "controls": ["guide-a", "guide-b"]}]
        span_map = {}
        for i, span in enumerate(observation["spans"]):
            a, b = span["controls"]
            start, end = f"s{i}-a", f"s{i}-b"
            sites.extend([{"id": start, "at": i + 2, "x": expression("guide-a", a[0]), "y": expression("guide-a", a[1])},
                          {"id": end, "at": i + 2, "x": expression(start, b[0] - a[0]), "y": expression(start, b[1] - a[1])}])
            edge_id = f"e{i}"
            edges.append({"id": edge_id, "at": i + 2, "controls": [start, end]})
            span_map[span["id"]] = edge_id
        return {"id": "fixture", "parameters": [], "sites": sites, "edges": edges,
                "rungs": [{"visible": ["rough"]}, {"visible": ["rough"]}, {"visible": ["e0"]}, {"visible": ["e0", "e1"]}],
                "sourceMapping": {"sourceId": "fixture", "spanMap": span_map}}, observation

    def test_pen_lift_coordinate_coincidence_stays_distinct_and_all_samples_are_counted(self):
        program, observation = self.fixture()
        result = audit_source_construction(program, observation)
        self.assertEqual(result["orderedSampleError"]["samples"], 4)
        self.assertTrue(result["preservedPenLiftIdentity"])

    def test_equal_position_cannot_authorize_a_shared_acquisition_site(self):
        program, observation = self.fixture()
        program["edges"][-1]["controls"][0] = program["edges"][-2]["controls"][-1]
        with self.assertRaisesRegex(AssertionError, "pen lift"):
            audit_source_construction(program, observation)

    def test_final_coordinate_lookalike_without_earlier_drawn_dependency_is_rejected(self):
        program, observation = self.fixture()
        expected = evaluate_program(program)[0]
        for site in program["sites"]:
            if site["at"] >= 2:
                site["x"], site["y"] = expected[site["id"]]
        with self.assertRaisesRegex(AssertionError, "no actual dependency"):
            audit_source_construction(program, observation)


if __name__ == "__main__":
    unittest.main()
