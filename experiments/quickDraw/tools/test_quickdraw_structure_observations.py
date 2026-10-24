"""Ordered source support and important false reads for geometric acquisition."""
import copy
import math
import unittest

from quickdraw_structure_observations import fit_stroke, observe_record, polyline_indices


def record(*strokes):
    return {"key_id": "test", "drawing": [[[p[0] for p in points], [p[1] for p in points]]
                                            for points in strokes]}


class QuickDrawObservationTests(unittest.TestCase):
    def test_order_duplicates_and_backwards_times_are_preserved(self):
        source = {"key_id": "ordered", "drawing": [[[20, 20, 40, 30], [50, 50, 60, 90], [4, 2, 8, 7]],
                                                     [[20, 30], [50, 40], [0, 1]]]}
        before = copy.deepcopy(source)
        observed = observe_record(source)
        self.assertEqual(source, before)
        for stroke, raw in zip(observed["strokes"], source["drawing"]):
            self.assertEqual(stroke["sourcePoints"], [list(p) for p in zip(raw[0], raw[1])])
            self.assertEqual(stroke["times"], raw[2])
        self.assertNotEqual(observed["strokes"][0]["id"], observed["strokes"][1]["id"])

    def test_collinear_retrace_retains_the_turnaround_not_only_its_extent(self):
        points = [[0, 0], [10, 0], [0, 0], [10, 0]]
        self.assertEqual(polyline_indices(points, 2), [0, 1, 2, 3])
        spans = fit_stroke(points, 0, {"polylineTolerance": 2})
        self.assertEqual([(s["startIndex"], s["endIndex"]) for s in spans], [(0, 1), (1, 2), (2, 3)])
        self.assertEqual([s["controls"] for s in spans], [points[0:2], points[1:3], points[2:4]])

    def test_segments_cover_the_acquired_intervals_once_and_share_boundaries(self):
        points = [[0, 0], [10, 0], [30, 0], [30, 20], [30, 50], [0, 50], [0, 0]]
        spans = fit_stroke(points, 2, {"polylineTolerance": 2})
        self.assertEqual(spans[0]["startIndex"], 0)
        self.assertEqual(spans[-1]["endIndex"], len(points)-1)
        actual = []
        for span in spans:
            actual.extend(range(span["startIndex"], span["endIndex"]))
            self.assertEqual(span["controls"][0], points[span["startIndex"]])
            self.assertEqual(span["controls"][-1], points[span["endIndex"]])
        self.assertEqual(actual, list(range(len(points)-1)))

    def test_round_arch_supports_curves_but_right_corner_stays_a_corner(self):
        arc = [[100+80*math.cos(math.pi*i/40), 100-80*math.sin(math.pi*i/40)] for i in range(41)]
        spans = fit_stroke(arc, 0)
        self.assertTrue(any(s["kind"] == "quadratic" for s in spans))
        corner = fit_stroke([[0, 0], [50, 0], [100, 0], [100, 50], [100, 100]], 0)
        self.assertEqual([s["kind"] for s in corner], ["line", "line"])
        self.assertEqual(corner[0]["endIndex"], 2)

    def test_near_horizontal_read_does_not_straighten_measured_geometry(self):
        points = [[0, 10], [50, 13], [100, 16]]
        span = fit_stroke(points, 0)[0]
        self.assertEqual(span["orientation"], "horizontal")
        self.assertEqual(span["controls"], [points[0], points[-1]])
        self.assertEqual(span["axisDelta"], 6)
        self.assertGreater(span["axisDeviation"], 0)

    def test_nearby_pen_lifts_retain_independent_stroke_and_sample_references(self):
        observed = observe_record(record([(0, 0), (100, 0)], [(100, 0), (100, 100)]))
        joins = observed["nearJoins"]
        self.assertEqual(len(joins), 1)
        self.assertEqual(joins[0]["distance"], 0)
        self.assertEqual(joins[0]["strokeIndices"], [0, 1])
        self.assertEqual(joins[0]["sampleIndices"], [1, 0])
        self.assertNotEqual(observed["strokes"][0]["spans"][-1], observed["strokes"][1]["spans"][0])

    def test_two_near_right_angles_can_expose_nonparallel_outer_directions(self):
        observed = observe_record(record([(0, 0), (100, 8), (110, 100), (210, 85)]))
        self.assertEqual(len(observed["unresolved"]), 1)
        self.assertGreater(observed["unresolved"][0]["outerParallelResidualDegrees"], 10)
        self.assertEqual(observed["strokes"][0]["sourcePoints"][-1], [210, 85])

    def test_tilted_two_arm_cross_is_supported_without_identity_merge(self):
        body = [(0, 60), (0, 150), (100, 150), (100, 60)]
        observed = observe_record(record(body, [(40, 10), (30, 40)], [(20, 17), (50, 27)]))
        self.assertEqual(len(observed["crosses"]), 1)
        candidate = observed["crosses"][0]
        self.assertEqual(set(candidate["strokeIndices"]), {1, 2})
        self.assertEqual(len(set(candidate["spans"])), 2)
        self.assertAlmostEqual(candidate["rightAngleResidualDegrees"], 0)

    def test_t_endpoint_and_missing_arm_are_not_crosses(self):
        body = [(0, 60), (0, 150), (100, 150), (100, 60)]
        self.assertFalse(observe_record(record(body, [(50, 10), (50, 40)]))["crosses"])
        self.assertFalse(observe_record(record(body, [(50, 10), (50, 40)], [(20, 10), (80, 10)]))["crosses"])

    def test_near_closed_oval_preserves_gap_while_open_arch_is_not_closed(self):
        oval = [[100+40*math.cos(2*math.pi*i/40), 100+70*math.sin(2*math.pi*i/40)] for i in range(39)]
        observed = observe_record(record(oval))
        self.assertEqual(len(observed["circles"]), 1)
        self.assertGreater(observed["circles"][0]["endpointGap"], 0)
        self.assertFalse(observe_record(record(oval[:20]))["circles"])

    def test_single_sample_and_repeated_points_remain_in_source_support(self):
        observed = observe_record(record([(4, 8)], [(4, 8), (4, 8), (4, 8)]))
        self.assertEqual(observed["counts"]["samples"], 4)
        self.assertEqual(observed["spans"][0]["startIndex"], 0)
        self.assertEqual(observed["spans"][0]["endIndex"], 0)
        self.assertEqual(observed["spans"][1]["endIndex"], 2)

    def test_empty_stroke_has_order_without_invented_geometry_and_empty_read_stays_open(self):
        source = {"drawing": [[[5], [8], [3]], [[], [], []], [[5, 8], [8, 10], [2, 1]]]}
        observed = observe_record(source)
        empty = observed["strokes"][1]
        self.assertEqual(empty["index"], 1)
        self.assertEqual(empty["points"], [])
        self.assertEqual(empty["times"], [])
        self.assertEqual(empty["spans"], [])
        self.assertIsNone(empty["bounds"])
        self.assertEqual(observed["strokes"][2]["times"], [2, 1])
        entirely_empty = observe_record({"drawing": [[[], [], []]]})
        self.assertTrue(entirely_empty["emptyRead"])
        self.assertIsNone(entirely_empty["bounds"])
        self.assertIsNone(entirely_empty["normalization"]["sourceBounds"])
        self.assertIsNone(entirely_empty["normalization"]["scale"])
        self.assertEqual(entirely_empty["spans"], [])
        self.assertEqual(entirely_empty["outlineStrokeIds"], [])

    def test_invalid_sample_alignment_does_not_manufacture_missing_values(self):
        with self.assertRaises(ValueError):
            observe_record({"drawing": [[[0, 1], [2], [0, 1]]]})
        with self.assertRaises(ValueError):
            observe_record({"drawing": [[[0, 1], [2, 3], [0]]]})


if __name__ == "__main__":
    unittest.main()
